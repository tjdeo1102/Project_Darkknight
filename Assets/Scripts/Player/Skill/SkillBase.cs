using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SkillBase", menuName = "Scriptable Objects/Skill Base")]
public abstract class SkillBase : CSVScriptableObject
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

    private HashSet<SkillBase> m_lockRequireSkills;
    private float m_lastSkillUseTime;
    protected Vector3 center;
    protected Vector3 foward;
    protected Vector3 right;
    protected Vector3 up;

    protected bool isFailSkill;


    public virtual void OnEnable()
    {
        m_lastSkillUseTime = -Cooldown;
        m_lockRequireSkills = new HashSet<SkillBase>();
        foreach (SkillBase skill in RequireSkill)
        {
            m_lockRequireSkills.Add(skill);
        }
    }
    public void UnlockRequireSkill(SkillBase skill)
    {
        if (m_lockRequireSkills.Contains(skill))
        {
            m_lockRequireSkills.Remove(skill);
        }
        if (m_lockRequireSkills.Count < 1) CanUnlock = true;
    }

    public bool CanUseSkill(Stat mp, bool currentUse = true)
    {
        if (currentUse) isFailSkill = false;
        if (mp ==  null) mp = new Stat();

        if (Time.time - m_lastSkillUseTime < Cooldown
            || CostMP > mp.TotalValue)
        {
            if (currentUse) isFailSkill = true;
            return false;
        }

        if (currentUse)
        {
            mp.AddModifier(new StatModifier(-CostMP, 0), StatModifyType.SkillUse);
            m_lastSkillUseTime = Time.time;
        }

        return true;
    }

    protected void SetStartPos(Transform origin)
    {
        foward = origin.forward;
        right = origin.right;
        up = origin.up;
        center = origin.position + foward * StartOffset.z + right * StartOffset.x + up * StartOffset.y;
        var navAgent = origin.GetComponentInParent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.updateRotation = false;
            origin.LookAt(center);
            navAgent.updateRotation = true;
        }
    }

    private void SkillEffect(Transform origin, PlayerController player = null)
    {
        if (SkillType != VFX.None)
        {
            if (player != null)
            {
                player.animator.SetInteger(skillAnimationParam, (int)SkillType);
                player.machine.ChangeState(StateType.Skill);
            }
            SkillEffectManager.Instance.PlayVFX(SkillType, center, origin.rotation, EffectDelay);
        }
    }

    public virtual IEnumerator Active(PlayerController player)
    {
        if (player != null && player.model.Stats.TryGetValue(StatType.Mana, out var mp))
        {
            if (CanUseSkill(mp) == false) yield break;
            SetStartPos(player.transform);
            SkillEffect(player.transform,player);
        }
        yield return new WaitForSeconds(ActiveDelay);
    }

    public virtual IEnumerator Active(Transform origin, Dictionary<StatType,Stat> stats, GameObject Target)
    {
        if (origin != null && stats.TryGetValue(StatType.Mana,out var mp))
        {
            if (CanUseSkill(mp) == false) yield break;
            SetStartPos(origin);
            SkillEffect(origin);
        }

        yield return new WaitForSeconds(ActiveDelay);
    }
}
