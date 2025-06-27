using System.Collections.Generic;
using UnityEngine;

public class Knife : MonoBehaviour, IWeapon
{
    public float comboTime;
    public int MaxComboCount = 2;
    public PlayerController ctrl;

    private static readonly int m_attackCountParam = Animator.StringToHash("AttackCount");
    private int m_attackCount;
    private float m_lastAttackTime;
    private void Start()
    {
        if (ctrl == null)
        {
            Debug.LogError("No ctrl");
            enabled = false;
            return;
        }
        AddWeapon();
        m_attackCount = 0;
    }
    public void AddWeapon()
    {
        ctrl.combat.RegisterWeapon(WeaponType.Knife, this);
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

}
