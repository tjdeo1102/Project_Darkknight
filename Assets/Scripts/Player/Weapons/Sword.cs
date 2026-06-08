using UnityEngine;

public class Sword : WeaponBase
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
        AddWeapon(WeaponType.Sword);
        m_attackCount = 0;
    }
}
