using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SkillBase", menuName = "Scriptable Objects/Skill Base")]
public abstract class SkillBase : ScriptableObject
{
    private static int skillAnimationParam = Animator.StringToHash("SkillType");

    [Header("CSV Parse")]
    [CSVField(CSVFIledType.None)]
    public string ID = "";
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

    [Header("Unity Editor Setting")]
    public Sprite Icon;
    public VFX SkillType = VFX.None;
    public WeaponType RequireWeapon;
    public SkillBase[] RequireSkill;

    [Header("Skill Detail")]
    public Vector3 StartOffset = Vector3.zero;
    public float ActiveDelay = 1f;
    public float EffectDelay = 0.5f;

    private PlayerController m_player;
    private Transform m_origin;
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

    private bool CanUseSkill(Stat mp)
    {
        isFailSkill = false;
        if (mp ==  null) mp = new Stat();

        if (Time.time - m_lastSkillUseTime < Cooldown
            || CostMP > mp.TotalValue)
        {
            Debug.Log("no actgive");

            isFailSkill = true;
            return false;
        }

        mp.AddModifier(new StatModifier(-CostMP, 0), StatModifyType.Perment);
        m_lastSkillUseTime = Time.time;
        return true;
    }

    private void SetStartPos()
    {
        foward = m_origin.forward;
        right = m_origin.right;
        up = m_origin.up;
        center = m_origin.position + foward * StartOffset.z + right * StartOffset.x + up * StartOffset.y;
    }

    private void SkillEffect()
    {
        if (SkillType != VFX.None)
        {
            if (m_player != null)
            {
                m_player.animator.SetInteger(skillAnimationParam, (int)SkillType);
                m_player.machine.ChangeState(StateType.Skill);
            }
            SkillEffectManager.Instance.PlayVFX(SkillType, center, m_origin.rotation, EffectDelay);
        }
    }

    public virtual IEnumerator Active(PlayerController player)
    {
        if (player != null)
        {
            this.m_player = player;
            var mp = player.model.Stats[StatType.Mana];
            m_origin = player.transform;

            if (m_player.combat.CurType != RequireWeapon && RequireWeapon != WeaponType.None)
            {
                Debug.Log("전용무기 장착 후, 사용 필요");
                isFailSkill = true;
                yield break;
            }

            if (CanUseSkill(mp) == false) yield break;
            SetStartPos();
            SkillEffect();
        }
        yield return new WaitForSeconds(ActiveDelay);
    }

    public virtual IEnumerator Active(Transform origin, Dictionary<StatType,Stat> stats, GameObject Target)
    {
        if (origin != null && stats.TryGetValue(StatType.Mana,out var mp))
        {
            m_origin = origin;
            if (CanUseSkill(mp) == false) yield break;
            Debug.Log("Active");
            SetStartPos();
            SkillEffect();
        }

        yield return new WaitForSeconds(ActiveDelay);
    }
}
