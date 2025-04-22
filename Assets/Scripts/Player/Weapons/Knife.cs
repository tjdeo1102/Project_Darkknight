using System.Collections.Generic;
using UnityEngine;

public class Knife : MonoBehaviour, IWeapon
{
    public float comboTime;
    public int MaxComboCount = 2;
    public PlayerController ctrl;

    private int attackCountParam = Animator.StringToHash("AttackCount");
    private int attackCount;
    private float lastAttackTime;
    public void AddWeapon()
    {
        ctrl.combat.RegisterWeapon(WeaponType.Knife, this);
    }

    public void Attack()
    {
        if (Time.time - lastAttackTime > comboTime)
        {
            attackCount = 0;
        }
        else attackCount++;

        ctrl.animator.SetInteger(attackCountParam, attackCount % MaxComboCount);

        lastAttackTime = Time.time;
    }

    private void Start()
    {
        AddWeapon();
        attackCount = 0;
    }
}
