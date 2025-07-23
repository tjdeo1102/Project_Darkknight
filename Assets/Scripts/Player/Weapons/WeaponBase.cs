using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class WeaponBase: MonoBehaviour
{
    [Serializable]
    public struct ComboContent
    {
        public float AdditionalDamage;
        public float KnockBackForce;
    }
    public float ComboTime = 1;
    public float HitDelay = 0.1f;
    public ComboContent[] Combo;
    public PlayerController ctrl;
    public GameObject WeaponObject;

    public Vector3 HitOffset = Vector3.zero;
    public Vector3 Range = Vector3.zero;

    protected int m_attackCountParam;
    protected int m_attackCount;
    protected float m_lastAttackTime;

    private WaitForSeconds hitDelay;
    public virtual void AddWeapon(WeaponType type)
    {
        ctrl.combat.RegisterWeapon(type, this);
    }
    public virtual void Attack()
    {
        if (Time.time - m_lastAttackTime > ComboTime)
        {
            m_attackCount = 0;
        }
        else m_attackCount = (m_attackCount + 1) % Combo.Length;

        ctrl.animator.SetInteger(m_attackCountParam, m_attackCount);

        StartCoroutine(HitRoutine(Combo[m_attackCount]));

        m_lastAttackTime = Time.time;

    }

    IEnumerator HitRoutine(ComboContent combo)
    {
        hitDelay ??= new WaitForSeconds(HitDelay);
        yield return new WaitForSeconds(HitDelay);

        var center = transform.position + transform.forward * HitOffset.z + transform.right * HitOffset.x + transform.up * HitOffset.y;
        Vector3 halfExtents = Range * 0.5f;
        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, TargetLayerManager.GetLayerMask(TargetLayer.Enemy));

        Tool.DrawOverlapBox(center, Range, Quaternion.LookRotation(transform.forward), Color.green, 2f);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(TagManager.GetTagString(TargetTag.Enemy)))
            {
                var stat = hit.GetComponentInParent<EnemyStat>();
                if (stat != null)
                {
                    stat.ApplyDamage(ctrl.model.AttackPower.TotalValue + combo.AdditionalDamage, ctrl.transform.position , combo.KnockBackForce);
                }
            }
        }
    }

    public void ActiveWeapon(bool isActive)
    {
        if (WeaponObject != null)
            WeaponObject.SetActive(isActive);
    }
}