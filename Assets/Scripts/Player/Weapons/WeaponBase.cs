using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBase: MonoBehaviour
{
    private const int HitBufferSize = 32;

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
    private readonly Collider[] m_hitBuffer = new Collider[HitBufferSize];
    private readonly HashSet<EnemyStat> m_damagedTargets = new();

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

        if (m_hitRoutine != null)
            StopCoroutine(m_hitRoutine);

        var fallbackDelay = GetFallbackHitDelay(m_pendingAction);
        if (fallbackDelay >= 0f)
            m_hitRoutine = StartCoroutine(HitRoutine(m_pendingAction, m_attackSequence, fallbackDelay));

        m_lastAttackTime = Time.time;

    }

    IEnumerator HitRoutine(WeaponAttackSO action, int attackSequence, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (IsCurrentAttack(action, attackSequence))
        {
            PlayAttackMainVFX(action);
            ApplyHit(action);
            PlayAttackHitVFX(action);
        }
        m_hitRoutine = null;
    }

    public bool ApplyAnimationEventHit(WeaponAttackSO action, int attackSequence)
    {
        if (IsCurrentAttack(action, attackSequence) == false) return false;

        CancelFallbackHit(action, attackSequence);
        ApplyHit(action);
        return m_lastHitSucceeded;
    }

    public void CancelFallbackHit(WeaponAttackSO action, int attackSequence)
    {
        if (IsAttackSequenceCurrent(action, attackSequence) == false) return;

        if (m_hitRoutine != null)
        {
            StopCoroutine(m_hitRoutine);
            m_hitRoutine = null;
        }
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
        var halfExtents = range * 0.5f;
        var hitCount = Physics.OverlapBoxNonAlloc(
            center,
            halfExtents,
            m_hitBuffer,
            rotation,
            TargetLayerManager.GetLayerMask(TargetLayer.Enemy));

        if (action.ShowHitboxGizmo)
            Tool.DrawOverlapBox(center, range, rotation, Color.green, 2f);

        m_damagedTargets.Clear();
        m_lastHitSucceeded = false;
        m_lastHitPosition = Vector3.zero;
        var additionalDamage = action.AdditionalDamage;
        var knockBackForce = action.KnockBackForce;
        var enemyTag = TagManager.GetTagString(TargetTag.Enemy);
        for (var i = 0; i < hitCount; i++)
        {
            var hit = m_hitBuffer[i];
            if (hit.CompareTag(enemyTag))
            {
                var stat = hit.GetComponentInParent<EnemyStat>();
                if (stat != null && m_damagedTargets.Add(stat))
                {
                    stat.ApplyDamage(ctrl.model.AttackPower.TotalValue + additionalDamage, ctrl.transform.position , knockBackForce);
                    m_lastHitSucceeded = true;
                    m_lastHitPosition = GetHitEffectPosition(hit, center);
                }
            }
        }

        if (m_lastHitSucceeded)
            CombatActionFeedback.PlayOnHit(action, ctrl);
    }

    private Vector3 GetHitEffectPosition(Collider hit, Vector3 attackCenter)
    {
        var hitPosition = hit.ClosestPoint(attackCenter);
        if ((hitPosition - attackCenter).sqrMagnitude < 0.0001f)
            hitPosition = hit.bounds.center;

        return hitPosition;
    }

    public void PlayAttackMainVFX(WeaponAttackSO action)
    {
        if (action == null || action.MainVFXPrefab == null) return;

        PlayPrefabVFX(
            action.MainVFXPrefab,
            action.GetMainVFXPosition(transform),
            action.GetMainVFXRotation(transform),
            action.GetMainVFXScale());
    }

    public void PlayAttackHitVFX(WeaponAttackSO action)
    {
        if (action == null ||
            action.HitVFXPrefab == null ||
            m_lastHitSucceeded == false)
        {
            return;
        }

        PlayPrefabVFX(
            action.HitVFXPrefab,
            action.GetHitVFXPosition(transform, m_lastHitPosition),
            action.GetHitVFXRotation(transform),
            action.GetHitVFXScale());
    }

    private void PlayPrefabVFX(
        ParticleSystem prefab,
        Vector3 position,
        Quaternion rotation,
        Vector3 scaleMultiplier)
    {
        if (SkillEffectManager.Instance != null &&
            SkillEffectManager.Instance.PlayVFX(prefab, position, rotation, scaleMultiplier))
        {
            return;
        }

        var instance = Instantiate(prefab, position, rotation);
        instance.transform.localScale = Vector3.Scale(prefab.transform.localScale, scaleMultiplier);
        instance.Play();
        StartCoroutine(DestroyVFXAfterPlay(instance));
    }

    private IEnumerator DestroyVFXAfterPlay(ParticleSystem particle)
    {
        if (particle == null) yield break;

        yield return new WaitForSeconds(
            particle.main.duration +
            particle.main.startLifetime.constantMax +
            0.2f);

        if (particle != null)
            Destroy(particle.gameObject);
    }

    protected virtual int GetAttackCountSize()
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
        if (action == null) return -1f;
        if (action.UseAnimationEvent)
            return action.AnimationEventFallbackDelay > 0f
                ? action.AnimationEventFallbackDelay
                : -1f;

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

    public bool TryPlayCurrentAttackAnimation()
    {
        return CombatActionRunner.TryPlayAnimation(
            m_pendingAction,
            ctrl != null ? ctrl.animator : null);
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

    private void OnDrawGizmos()
    {
        if (AttackActions == null) return;

        for (var i = 0; i < AttackActions.Length; i++)
        {
            if (i != m_attackCount)
                continue;

            var action = AttackActions[i];
            if (action == null) continue;
            if (action.ShowHitboxGizmo == false && action.ShowMainVFXGizmo == false) continue;

            if (action.ShowHitboxGizmo)
            {
                var center = action.GetHitCenter(transform);
                var rotation = action.GetHitRotation(transform);
                Gizmos.color = Color.green;
                Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, action.Range);
                Gizmos.matrix = Matrix4x4.identity;
            }

            if (action.ShowMainVFXGizmo)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(action.GetMainVFXPosition(transform), 0.08f);
            }
        }
    }
}
