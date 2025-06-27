using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class PlayerCombat : MonoBehaviour
{
    public PlayerController ctrl;
    private void Start()
    {
        ctrl.input.actions["Skill"].performed += OnSkill;
        ctrl.input.actions["SwapWeapon"].performed += OnSwapWeapon;
    }

    private void OnDisable()
    {
        ctrl.input.actions["Skill"].performed -= OnSkill;
        ctrl.input.actions["SwapWeapon"].performed -= OnSwapWeapon;
    }


    #region Weapon & General Attack
    public WeaponType CurType;

    private IWeapon curWeapon;
    private Dictionary<WeaponType, IWeapon> weapons;
    private bool changeLocked = false;
    private bool attackLocked = false;

    private int lastWeaponParam = Animator.StringToHash("LastWeapon");
    private int curWeaponParam = Animator.StringToHash("CurWeapon");

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

    public void RegisterWeapon(WeaponType type, IWeapon weapon)
    {
        if (!weapons.ContainsKey(type))
        {
            weapons.Add(type, weapon);
        }
        else
        {
            Debug.LogWarning($"{type}의 타입의 무기는 이미 추가됨.");
        }
    }

    public void DeleteWeapon(WeaponType type)
    {
        if (weapons.ContainsKey(type))
        {
            weapons.Remove(type);
        }
    }

    public void OnAttack(InputValue value)
    {
        bool isPressed = value.isPressed;

        if (isPressed && ctrl.machine.CanOtherAction())
        {
            if (attackLocked) return;
            attackLocked = true;
            if (curWeapon != null)
            {
                curWeapon.Attack();
                ctrl.machine.ChangeState(StateType.Attack);
            }
        }
        else
        {
            attackLocked = false;
        }
    }

    public void OnSwapWeapon(InputAction.CallbackContext context)
    {
        if (ctrl.machine.CanOtherAction())
        {
            int next = ((int)CurType + 1) % (int)WeaponType.Size;

            ChangeWeapon((WeaponType)next);
        }
    }
    #endregion

    #region Skill
    private bool skillLocked = false;

    public List<SkillBase> skills;

    public void OnSkill(InputAction.CallbackContext context)
    {
        if (ctrl.InputSkillDic.ContainsKey(context.control.name) == false) return;

        int skillNum = (int)ctrl.InputSkillDic[context.control.name];
        if (ctrl.machine.CanOtherAction() && skills[skillNum] != null)
        {
            StartCoroutine(skills[skillNum].Active(ctrl));
        }
    }
    #endregion

    private void Awake()
    {
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
}
