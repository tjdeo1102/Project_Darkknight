using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBase: MonoBehaviour
{
    public float ComboTime = 1;
    public WeaponAttackSO[] AttackActions;
    public PlayerController ctrl;
    public GameObject WeaponObject;

    protected int m_attackCountParam;
    protected int m_attackCount;
    protected float m_lastAttackTime;
    private bool m_hasPendingHit;
    private int m_pendingAttackIndex;
    private WeaponAttackSO m_pendingAction;
    private Coroutine m_hitRoutine;
    private bool m_lastHitSucceeded;
    private Vector3 m_lastHitPosition;
    private int m_attackSequence;

    public virtual void AddWeapon(WeaponType type)
    {
        ctrl.combat.RegisterWeapon(type, this);
    }
    public virtual void Attack()
    {
        var attackCount = GetAttackCountSize();
        if (attackCount == 0)
        {
            Debug.LogWarning($"{name} has no weapon attack actions.", this);
            return;
        }

        if (Time.time - m_lastAttackTime > ComboTime)
        {
            m_attackCount = 0;
        }
        else m_attackCount = (m_attackCount + 1) % attackCount;

        ctrl.animator.SetInteger(m_attackCountParam, m_attackCount);

        m_hasPendingHit = true;
        m_pendingAttackIndex = m_attackCount;
        m_pendingAction = GetAttackAction(m_attackCount);
        m_attackSequence++;

        if (ShouldUseAnimationEventHit() == false)
        {
            if (m_hitRoutine != null)
                StopCoroutine(m_hitRoutine);

            m_hitRoutine = StartCoroutine(HitRoutine(m_pendingAction, m_attackSequence));
        }

        m_lastAttackTime = Time.time;

    }

    IEnumerator HitRoutine(WeaponAttackSO action, int attackSequence)
    {
        var delay = GetFallbackHitDelay(action);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (IsCurrentAttack(action, attackSequence))
            ApplyHit(action);
        m_hitRoutine = null;
    }

    public bool ApplyAnimationEventHit(WeaponAttackSO action, int attackSequence)
    {
        if (IsCurrentAttack(action, attackSequence) == false) return false;

        if (m_hitRoutine != null)
        {
            StopCoroutine(m_hitRoutine);
            m_hitRoutine = null;
        }

        ApplyHit(action);
        return m_lastHitSucceeded;
    }

    private void ApplyHit(WeaponAttackSO action)
    {
        if (m_hasPendingHit == false) return;
        m_hasPendingHit = false;
        if (action == null)
        {
            Debug.LogWarning($"{name} attack action is empty at index {m_pendingAttackIndex}.", this);
            return;
        }

        var range = action.Range;
        var center = action.GetHitCenter(transform);
        var rotation = action.GetHitRotation(transform);
        Vector3 halfExtents = range * 0.5f;
        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, TargetLayerManager.GetLayerMask(TargetLayer.Enemy));

        Tool.DrawOverlapBox(center, range, rotation, Color.green, 2f);

        var damagedTargets = new HashSet<EnemyStat>();
        m_lastHitSucceeded = false;
        m_lastHitPosition = Vector3.zero;
        var additionalDamage = action.AdditionalDamage;
        var knockBackForce = action.KnockBackForce;
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(TagManager.GetTagString(TargetTag.Enemy)))
            {
                var stat = hit.GetComponentInParent<EnemyStat>();
                if (stat != null && damagedTargets.Add(stat))
                {
                    stat.ApplyDamage(ctrl.model.AttackPower.TotalValue + additionalDamage, ctrl.transform.position , knockBackForce);
                    m_lastHitSucceeded = true;
                    m_lastHitPosition = GetHitEffectPosition(hit, center);
                }
            }
        }
    }

    private Vector3 GetHitEffectPosition(Collider hit, Vector3 attackCenter)
    {
        var hitPosition = hit.ClosestPoint(attackCenter);
        if ((hitPosition - attackCenter).sqrMagnitude < 0.0001f)
            hitPosition = hit.bounds.center;

        return hitPosition;
    }

    private bool ShouldUseAnimationEventHit()
    {
        return m_pendingAction != null && m_pendingAction.UseAnimationEvent;
    }

    private int GetAttackCountSize()
    {
        if (AttackActions != null && AttackActions.Length > 0) return AttackActions.Length;
        return 0;
    }

    private WeaponAttackSO GetAttackAction(int attackIndex)
    {
        if (AttackActions == null || attackIndex < 0 || attackIndex >= AttackActions.Length) return null;
        return AttackActions[attackIndex];
    }

    private float GetFallbackHitDelay(WeaponAttackSO action)
    {
        if (action == null) return 0f;
        return Mathf.Max(0f, action.ActiveDelay);
    }

    public void ActiveWeapon(bool isActive)
    {
        if (WeaponObject != null)
            WeaponObject.SetActive(isActive);
    }

    public int GetAttackCount()
    {
        return m_attackCount;
    }

    public WeaponAttackSO GetCurrentAttackAction()
    {
        return GetAttackAction(m_attackCount);
    }

    public int GetCurrentAttackSequence()
    {
        return m_attackSequence;
    }

    public bool IsCurrentAttack(WeaponAttackSO action, int attackSequence)
    {
        return m_hasPendingHit && IsAttackSequenceCurrent(action, attackSequence);
    }

    public bool IsAttackSequenceCurrent(WeaponAttackSO action, int attackSequence)
    {
        return action != null &&
               action == m_pendingAction &&
               attackSequence == m_attackSequence;
    }

    public bool TryGetLastHitPosition(out Vector3 position)
    {
        position = m_lastHitPosition;
        return m_lastHitSucceeded;
    }

    private void OnDrawGizmosSelected()
    {
        if (AttackActions == null) return;

        for (var i = 0; i < AttackActions.Length; i++)
        {
            var action = AttackActions[i];
            if (action == null) continue;

            var center = action.GetHitCenter(transform);
            var rotation = action.GetHitRotation(transform);
            Gizmos.color = i == m_attackCount ? Color.green : Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, action.Range);

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(action.GetMainVFXPosition(transform), 0.08f);
        }
    }
}
