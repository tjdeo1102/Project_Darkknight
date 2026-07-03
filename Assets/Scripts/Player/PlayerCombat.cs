using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class PlayerCombat : MonoBehaviour
{
    [Header("Require Setting")]
    public PlayerController ctrl;
    public WeaponType CurType;
    public List<SkillBase> skills;

    private WeaponBase curWeapon;
    private Dictionary<WeaponType, WeaponBase> weapons;
    private bool m_hasComboReserved = false;
    private SkillRuntimeStateStore m_skillStates;
    private bool m_nextSkillAnimationAlreadyPlayed;

    private readonly int lastWeaponParam = Animator.StringToHash("LastWeapon");
    private readonly int curWeaponParam = Animator.StringToHash("CurWeapon");

    #region Weapon & General Attack

    public void ChangeWeapon(WeaponType type)
    {
        ctrl.animator.SetInteger(lastWeaponParam, (int)CurType);
        if (weapons.TryGetValue(type, out var nextWeapon) == false)
            weapons.TryGetValue(WeaponType.None, out nextWeapon);

        CurType = nextWeapon != null ? type : WeaponType.None;
        curWeapon = nextWeapon;
        ctrl.animator.SetInteger(curWeaponParam, (int)CurType);

        ctrl.machine.ChangeState(StateType.SwapWeapon);
    }

    public void RegisterWeapon(WeaponType type, WeaponBase weapon)
    {
        if (!weapons.ContainsKey(type))
        {
            weapons.Add(type, weapon);
        }
        else
        {
            Debug.LogWarning($"{type}�� Ÿ���� ����� �̹� �߰���.");
        }
    }

    public void DeleteWeapon(WeaponType type)
    {
        if (type == WeaponType.None || weapons.Remove(type) == false) return;
        if (CurType != type) return;

        curWeapon?.ActiveWeapon(false);
        ChangeWeapon(WeaponType.None);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started != false ||curWeapon == null) return;

        // 만약 현재 상태가 이미 공격 상태라면? -> 상태 전환을 하지 않고 입력 예약만 설정!
        if (ctrl.machine.CurType == StateType.Attack)
        {
            m_hasComboReserved = true;
            return;
        }

        // 공격 상태가 아니라면 기존처럼 일반적인 첫 공격 시작
        if (ctrl.machine.CanOtherAction())
        {
            curWeapon.Attack();
            ctrl.machine.ChangeState(StateType.Attack);
        }
    }

    public bool HasComboReserved() => m_hasComboReserved;
    public void ClearComboReservation() => m_hasComboReserved = false;

    public void OnSwapWeapon(InputAction.CallbackContext context)
    {
        if (ctrl.machine.CanOtherAction())
        {
            curWeapon?.ActiveWeapon(false);
            ChangeWeapon(GetNextWeaponType());
            curWeapon?.ActiveWeapon(true);
        }
    }
    #endregion

    #region Skill
    public void OnSkill(InputAction.CallbackContext context)
    {
        var size = (int)InputSkill.Size;
        var slotIndex = GetSkillSlotIndex(context);
        if (slotIndex < 0 || slotIndex >= size) return;
        if (ctrl.machine.CanOtherAction() == false || slotIndex >= skills.Count) return;

        var skill = skills[slotIndex];
        if (IsEquippedSkillActive(skill) == false) return;

        if (HasRequiredWeapon(skill) == false)
        {
            UIController.Instance?.ShowRequiredWeaponMessage(skill.RequireWeapon);
            return;
        }

        StartCoroutine(skill.Active(ctrl));
    }
    #endregion

    private void Awake()
    {
        m_skillStates = SkillRuntimeStateStore.GetOrCreate(this);
        weapons = new()
        {
            {WeaponType.None, null},
        };
        CurType = WeaponType.None;

        skills ??= new List<SkillBase>();
        var requiredSkillSlots = (int)InputSkill.Size;
        while (skills.Count < requiredSkillSlots)
        {
            skills.Add(null);
        }
    }

    public WeaponBase GetCurWeapon()
    {
        return curWeapon;
    }

    public static int GetSkillSlotIndex(InputAction.CallbackContext context)
    {
        var bindingIndex = context.action.GetBindingIndexForControl(context.control);
        if (bindingIndex < 0 || bindingIndex >= context.action.bindings.Count)
            return -1;

        return context.action.bindings[bindingIndex].name switch
        {
            nameof(InputSkill.SkillQ) => (int)InputSkill.SkillQ,
            nameof(InputSkill.SkillW) => (int)InputSkill.SkillW,
            nameof(InputSkill.SkillE) => (int)InputSkill.SkillE,
            _ => bindingIndex < (int)InputSkill.Size ? bindingIndex : -1,
        };
    }

    private WeaponType GetNextWeaponType()
    {
        var size = (int)WeaponType.Size;
        for (var offset = 1; offset < size; offset++)
        {
            var candidate = (WeaponType)(((int)CurType + offset) % size);
            if (weapons.ContainsKey(candidate))
                return candidate;
        }

        return WeaponType.None;
    }

    public SkillRuntimeState GetSkillState(SkillBase skill)
    {
        m_skillStates ??= SkillRuntimeStateStore.GetOrCreate(this);
        return m_skillStates?.GetState(skill);
    }

    public void MarkNextSkillAnimationAlreadyPlayed()
    {
        m_nextSkillAnimationAlreadyPlayed = true;
    }

    public bool ConsumeNextSkillAnimationAlreadyPlayed()
    {
        var result = m_nextSkillAnimationAlreadyPlayed;
        m_nextSkillAnimationAlreadyPlayed = false;
        return result;
    }

    public bool IsSkillUnlocked(SkillBase skill)
    {
        if (skill == null) return false;
        if (skill.CanUnlock) return true;
        if (skill.RequireSkill == null || skill.RequireSkill.Length == 0) return false;

        foreach (var requiredSkill in skill.RequireSkill)
        {
            var requiredState = GetSkillState(requiredSkill);
            if (requiredSkill == null || requiredState == null || requiredState.IsActive == false)
            {
                return false;
            }
        }

        return true;
    }

    public bool CanUseEquippedSkill(SkillBase skill)
    {
        return IsEquippedSkillActive(skill) && HasRequiredWeapon(skill);
    }

    private bool IsEquippedSkillActive(SkillBase skill)
    {
        if (skill == null) return false;

        var state = GetSkillState(skill);
        return state != null && state.IsActive;
    }

    private bool HasRequiredWeapon(SkillBase skill)
    {
        return skill != null &&
               (skill.RequireWeapon == WeaponType.None || skill.RequireWeapon == CurType);
    }
}
