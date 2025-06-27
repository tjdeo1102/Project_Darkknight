using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour, IWeapon
{
    public float comboTime;
    public int MaxComboCount = 3;
    public PlayerController ctrl;

    private int m_attackCountParam = Animator.StringToHash("AttackCount");
    private int m_attackCount;
    private float m_lastAttackTime;
    public void AddWeapon()
    {
        ctrl.combat.RegisterWeapon(WeaponType.Sword, this);
    }

    public void Attack()
    {
        if (Time.time - m_lastAttackTime > comboTime)
        {
            m_attackCount = 0;
        }
        else m_attackCount = (m_attackCount + 1) % MaxComboCount;

        ctrl.animator.SetInteger(m_attackCountParam, m_attackCount);

        m_lastAttackTime = Time.time;
    }

    private void Start()
    {
        AddWeapon();
        m_attackCount = 0;
    }
}
