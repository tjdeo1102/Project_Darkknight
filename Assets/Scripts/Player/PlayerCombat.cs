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

        ctrl.input.actions["Skill"].performed += OnSkill;
        ctrl.input.actions["SwapWeapon"].performed += OnSwapWeapon;
        ctrl.input.actions["Attack"].performed += OnAttack;
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

        ctrl.input.actions["Skill"].performed -= OnSkill;
        ctrl.input.actions["SwapWeapon"].performed -= OnSwapWeapon;
        ctrl.input.actions["Attack"].performed -= OnAttack;
        m_isInputSubscribed = false;
    }


    #region Weapon & General Attack

    public void ChangeWeapon(WeaponType type)
    {
        ctrl.animator.SetInteger(lastWeaponParam, (int)CurType);
        if (weapons.ContainsKey(type))
        {
            CurType = type;
        }
        else
        {
            CurType = WeaponType.None;
        }
        ctrl.animator.SetInteger(curWeaponParam, (int)CurType);

        ctrl.machine.ChangeState(StateType.SwapWeapon);
        curWeapon = weapons[CurType];
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
        if (weapons.ContainsKey(type))
        {
            weapons.Remove(type);
        }
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
            int next = ((int)CurType + 1) % (int)WeaponType.Size;
            ChangeWeapon((WeaponType)next);
            curWeapon?.ActiveWeapon(true);
        }
    }
    #endregion

    #region Skill
    public void OnSkill(InputAction.CallbackContext context)
    {
        var size = (int)InputSkill.Size;
        int bindingIndex = context.action.GetBindingIndexForControl(context.control);
        if (bindingIndex < 0 || bindingIndex >= size) return;

        if (ctrl.machine.CanOtherAction() &&
            bindingIndex < skills.Count &&
            skills[bindingIndex] != null)
        {
            StartCoroutine(skills[bindingIndex].Active(ctrl));
        }
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

        skills = new()
        {
            null, null, null,
        };
    }

    public WeaponBase GetCurWeapon()
    {
        return curWeapon;
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
}
