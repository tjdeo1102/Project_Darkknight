using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class PlayerCombat : MonoBehaviour
{
    public PlayerController ctrl;
    
    #region Weapon & General Attack
    public WeaponType curType;

    private IWeapon curWeapon;
    private Dictionary<WeaponType, IWeapon> weapons;
    private bool changeLocked = false;
    private bool attackLocked = false;

    private int lastWeaponParam = Animator.StringToHash("LastWeapon");
    private int curWeaponParam = Animator.StringToHash("CurWeapon");

    public void ChangeWeapon(WeaponType type)
    {
        ctrl.animator.SetInteger(lastWeaponParam, (int)curType);
        if (weapons.ContainsKey(type))
        {
            curType = type;
        }
        else
        {
            curType = WeaponType.None;
        }
        ctrl.animator.SetInteger(curWeaponParam, (int)curType);

        ctrl.machine.ChangeState(StateType.SwapWeapon);
        curWeapon = weapons[curType];
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

    public void OnSwapWeapon(InputValue value)
    {
        bool isPressed = value.isPressed;

        if (isPressed && ctrl.machine.CanOtherAction())
        {
            if (changeLocked) return;
            changeLocked = true;
            int next = ((int)curType + 1) % (int)WeaponType.Size;

            ChangeWeapon((WeaponType)next);
        }
        else
        {
            changeLocked = false;
        }
    }
    #endregion

    #region Skill
    private bool skillLocked = false;

    public SkillBase testSkill1;
    public SkillBase testSkill2;
    public SkillBase testSkill3;

    public List<SkillBase> skills;
    public void OnSkillQ(InputValue value)
    {
        bool isPressed = value.isPressed;

        if (isPressed && ctrl.machine.CanOtherAction())
        {
            if (skillLocked) return;
            skillLocked = true;

            if (skills[0] != null)
            {
                StartCoroutine(skills[0].Active(ctrl));
            }
        }
        else
        {
            skillLocked = false;
        }
    }

    public void OnSkillW(InputValue value)
    {
        bool isPressed = value.isPressed;

        if (isPressed && ctrl.machine.CanOtherAction())
        {
            if (skillLocked) return;
            skillLocked = true;

            if (skills[1] != null) StartCoroutine(skills[1].Active(ctrl));
        }
        else
        {
            skillLocked = false;
        }
    }

    public void OnSkillE(InputValue value)
    {
        bool isPressed = value.isPressed;

        if (isPressed && ctrl.machine.CanOtherAction())
        {
            if (skillLocked) return;
            skillLocked = true;

            if (skills[2] != null) StartCoroutine(skills[2].Active(ctrl));
        }
        else
        {
            skillLocked = false;
        }
    }

    #endregion

    private void Awake()
    {
        weapons = new Dictionary<WeaponType, IWeapon>()
        {
            {WeaponType.None, null},
        };
        curType = WeaponType.None;

        skills = new List<SkillBase>()
        {
            { testSkill1 },
            { testSkill2 },
            { testSkill3 },
        };
    }
}
