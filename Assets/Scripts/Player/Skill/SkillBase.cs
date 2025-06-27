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
    private HashSet<SkillBase> lockRequireSkills;
    private float m_lastSkillUseTime;
    protected Vector3 center;
    protected Vector3 foward;
    protected Vector3 right;
    protected Vector3 up;

    protected bool isBreak;



    public void OnEnable()
    {
        m_lastSkillUseTime = -Cooldown;
        lockRequireSkills = new HashSet<SkillBase>();
        foreach (SkillBase skill in RequireSkill)
        {
            lockRequireSkills.Add(skill);
        }
    }
    public void UnlockRequireSkill(SkillBase skill)
    {
        if (lockRequireSkills.Contains(skill))
        {
            lockRequireSkills.Remove(skill);
        }
        if (lockRequireSkills.Count < 1) CanUnlock = true;
    }

    public virtual IEnumerator Active(PlayerController m_player)
    {
        var mp = m_player.model.Stats[StatType.Mana];
        
        // ��Ÿ�� + mp Ȯ��
        if (Time.time - m_lastSkillUseTime < Cooldown
            || CostMP > mp.TotalValue)
        {
            isBreak = true;
            yield break;
        }
        
        this.m_player = m_player;
        var trans = m_player.transform;
        foward = trans.forward;
        right = trans.right;
        up = trans.up;
        center = trans.position + foward * StartOffset.z + right * StartOffset.x + up * StartOffset.y;

        if (m_player.combat.CurType != RequireWeapon && RequireWeapon != WeaponType.None)
        {
            Debug.Log("���� ���� ���� ��, ���");
            yield break;
        }

        if (SkillType != VFX.None)
        {
            // �� ��ų�� ���� �ִϸ��̼� ���
            m_player.animator.SetInteger(skillAnimationParam, (int)SkillType);
            m_player.machine.ChangeState(StateType.Skill);

            // �� ��ų�� ���� ����Ʈ ���
            SkillEffectManager.Instance.PlayVFX(SkillType, center, m_player.transform.rotation,EffectDelay);
        }

        m_lastSkillUseTime = Time.realtimeSinceStartup;
        // mp ���� ����
        mp.AddModifier(new StatModifier(-CostMP,0),StatModifyType.Perment);

        yield return new WaitForSeconds(ActiveDelay);
    }
}
