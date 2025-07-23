using System.Collections.Generic;
using UnityEngine;

public class Knife : WeaponBase
{
    private void Start()
    {
        m_attackCountParam = Animator.StringToHash("AttackCount");
        if (ctrl == null)
        {
            Debug.LogError("No ctrl");
            enabled = false;
            return;
        }
        ActiveWeapon(false);
        AddWeapon(WeaponType.Knife);
        m_attackCount = 0;
    }
}
