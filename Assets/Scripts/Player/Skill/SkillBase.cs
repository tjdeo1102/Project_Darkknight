using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class SkillRuntimeState
{
    public bool IsActive;
    public int EnforceLevel;
    public float LastUseTime;

    public SkillRuntimeState(SkillBase skill)
    {
        IsActive = skill.CanActive;
        EnforceLevel = skill.EnforceLevel;
        LastUseTime = -skill.Cooldown;
    }
}

public sealed class SkillRuntimeStateStore : MonoBehaviour
{
    private readonly Dictionary<SkillBase, SkillRuntimeState> m_states = new();

    public SkillRuntimeState GetState(SkillBase skill)
    {
        if (skill == null) return null;
        if (m_states.TryGetValue(skill, out var state) == false)
        {
            state = new SkillRuntimeState(skill);
            m_states.Add(skill, state);
        }
        return state;
    }

    public static SkillRuntimeStateStore GetOrCreate(Component owner)
    {
        if (owner == null) return null;
        var store = owner.GetComponent<SkillRuntimeStateStore>();
        return store != null ? store : owner.gameObject.AddComponent<SkillRuntimeStateStore>();
    }
}

public sealed class SkillExecutionContext
{
    public SkillRuntimeState State;
    public Vector3 Center;
    public Vector3 Forward;
    public Vector3 Right;
    public Vector3 Up;
    public bool Failed;
}

[CreateAssetMenu(fileName = "SkillBase", menuName = "Scriptable Objects/Skill Base")]
public abstract class SkillBase : CombatActionSO
{
    private static int skillAnimationParam = Animator.StringToHash("SkillType");

    [Header("CSV Parse")]
    [CSVField(CSVFIledType.None)]
    public string SkillName = "� ��ų";
    [CSVField(CSVFIledType.None)]
    public string Description = "��ų ������ ���ּ���.";
    [CSVField(CSVFIledType.None)]
    public float Cooldown = 5f;
    [CSVField(CSVFIledType.None)]
    public float CostMP = 10f;
    [CSVField(CSVFIledType.None)]
    public float EnforceBaseCost;
    [CSVField(CSVFIledType.None)]
    public float EnforceCostFactor;
    [CSVField(CSVFIledType.None)]
    public int EnforceLevel = 0;
    [CSVField(CSVFIledType.None)]
    public bool CanActive = false;
    [CSVField(CSVFIledType.None)]
    public bool CanUnlock = false;
    [CSVField(CSVFIledType.StatArr)]
    public StatChange[] SkillStats;

    [Header("Unity Editor Setting")]
    public Sprite Icon;
    public VFX SkillType = VFX.None;
    public WeaponType RequireWeapon;
    public SkillBase[] RequireSkill;

    [Header("Skill Detail")]
    public Vector3 StartOffset = Vector3.zero;
    public float ActiveDelay = 1f;
    public float EffectDelay = 0.5f;

    public float GetEnforceCost(SkillRuntimeState state)
    {
        var level = state?.EnforceLevel ?? EnforceLevel;
        return EnforceBaseCost * (1 + level * EnforceCostFactor);
    }

    public bool CanUseSkill(Component owner, Stat mp, bool currentUse = true)
    {
        var state = SkillRuntimeStateStore.GetOrCreate(owner)?.GetState(this);
        if (state == null) return false;

        if (mp == null) mp = new Stat();

        if (Time.time - state.LastUseTime < Cooldown
            || CostMP > mp.TotalValue)
        {
            return false;
        }

        if (currentUse)
        {
            mp.AddModifier(new StatModifier(-CostMP, 0), StatModifyType.SkillUse);
            state.LastUseTime = Time.time;
        }

        return true;
    }

    protected SkillExecutionContext CreateContext(Component owner, Transform origin)
    {
        var state = SkillRuntimeStateStore.GetOrCreate(owner)?.GetState(this);
        var context = new SkillExecutionContext
        {
            State = state
        };
        UpdateStartPose(origin, context);
        return context;
    }

    protected void UpdateStartPose(Transform origin, SkillExecutionContext context)
    {
        context.Forward = origin.forward;
        context.Right = origin.right;
        context.Up = origin.up;
        context.Center = origin.position
                         + context.Forward * StartOffset.z
                         + context.Right * StartOffset.x
                         + context.Up * StartOffset.y;
        var navAgent = origin.GetComponentInParent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.updateRotation = false;
            origin.LookAt(context.Center);
            navAgent.updateRotation = true;
        }
    }

    protected IEnumerator BeginSkill(
        Component owner,
        Transform origin,
        Stat mp,
        SkillExecutionContext context,
        PlayerController player = null)
    {
        if (CanUseSkill(owner, mp) == false)
        {
            context.Failed = true;
            yield break;
        }

        if (SkillType != VFX.None)
        {
            if (player != null)
            {
                player.animator.SetInteger(skillAnimationParam, (int)SkillType);
                if (CombatActionRunner.TryPlayAnimation(this, player.animator))
                    player.combat?.MarkNextSkillAnimationAlreadyPlayed();

                player.machine.ChangeState(StateType.Skill);
            }

            var effectDelay = Mathf.Max(0f, EffectDelay);
            if (effectDelay <= 0f)
            {
                PlaySkillEffect(origin);
            }
            else if (owner is MonoBehaviour coroutineOwner)
            {
                coroutineOwner.StartCoroutine(PlaySkillEffectAfterDelay(origin, effectDelay));
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, ActiveDelay));
    }

    private IEnumerator PlaySkillEffectAfterDelay(Transform origin, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySkillEffect(origin);
    }

    private void PlaySkillEffect(Transform origin)
    {
        if (origin == null) return;

        var effectContext = new SkillExecutionContext();
        UpdateStartPose(origin, effectContext);
        SkillEffectManager.Instance?.PlayVFX(SkillType, effectContext.Center, origin.rotation);
    }

    public virtual IEnumerator Active(PlayerController player)
    {
        if (player == null || player.model.Stats.TryGetValue(StatType.Mana, out var mp) == false) yield break;

        var context = CreateContext(player.combat, player.transform);
        yield return BeginSkill(player.combat, player.transform, mp, context, player);
    }

    public virtual IEnumerator Active(Transform origin, Dictionary<StatType,Stat> stats, GameObject Target)
    {
        if (origin == null || stats == null || stats.TryGetValue(StatType.Mana, out var mp) == false) yield break;

        var owner = origin.GetComponentInParent<EnemyController>() as Component ?? origin;
        var context = CreateContext(owner, origin);
        yield return BeginSkill(owner, origin, mp, context);
    }
}
