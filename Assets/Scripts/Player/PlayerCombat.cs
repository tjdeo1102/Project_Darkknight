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
    private bool m_isInputSubscribed;
    private SkillRuntimeStateStore m_skillStates;
    private InputAction m_skillAction;
    private InputAction m_swapWeaponAction;
    private InputAction m_attackAction;

    private readonly int lastWeaponParam = Animator.StringToHash("LastWeapon");
    private readonly int curWeaponParam = Animator.StringToHash("CurWeapon");

    private void OnEnable()
    {
        SubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    private void SubscribeInput()
    {
        if (m_isInputSubscribed) return;
        if (ctrl == null) ctrl = GetComponentInParent<PlayerController>();
        if (ctrl == null || ctrl.input == null || ctrl.input.actions == null) return;

        m_skillAction = ctrl.input.actions.FindAction("Player/Skill");
        m_swapWeaponAction = ctrl.input.actions.FindAction("Player/SwapWeapon");
        m_attackAction = ctrl.input.actions.FindAction("Player/Attack");
        if (m_skillAction == null || m_swapWeaponAction == null || m_attackAction == null) return;

        m_skillAction.performed += OnSkill;
        m_swapWeaponAction.performed += OnSwapWeapon;
        m_attackAction.performed += OnAttack;
        m_isInputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (m_isInputSubscribed == false) return;
        if (ctrl == null || ctrl.input == null || ctrl.input.actions == null)
        {
            m_isInputSubscribed = false;
            return;
        }

        m_skillAction.performed -= OnSkill;
        m_swapWeaponAction.performed -= OnSwapWeapon;
        m_attackAction.performed -= OnAttack;
        m_isInputSubscribed = false;
    }


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
        if (ctrl.machine.CanOtherAction() && curWeapon != null)
        {
            curWeapon.Attack();
            ctrl.machine.ChangeState(StateType.Attack);
        }
    }

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
