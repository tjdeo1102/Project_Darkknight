using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponType
{
    None, Sword, Knife, Gun, Size
}

public class PlayerCombat : MonoBehaviour
{
    public PlayerController ctrl;
    public WeaponType curType;

    private IWeapon curWeapon;
    private Dictionary<WeaponType,IWeapon> weapons;
    private bool changeLocked = false;
    private bool attackLocked = false;

    private int lastWeaponParam = Animator.StringToHash("LastWeapon");
    private int curWeaponParam = Animator.StringToHash("CurWeapon");

    private void Awake()
    {
        weapons = new Dictionary<WeaponType, IWeapon>();
        weapons.Add(WeaponType.None, null);
        curType = WeaponType.None;
    }


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

        if (isPressed && ctrl.machine.curType != StateType.Attack)
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

        if (isPressed)
        {
            if (changeLocked) return;
            changeLocked = true;
            int next = ((int)curType + 1) % (int)WeaponType.Size;
            Debug.Log(next);
            ChangeWeapon((WeaponType)next);
        }
        else
        {
            changeLocked = false;
        }
    }
}
