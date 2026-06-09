using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyStat : MonoBehaviour
{
    [Header("Require Setting")]
    public EnemyController Ctrl;
    public List<StatBaseSO> StatData;
    public HitEffect HitEffect;
    public bool CanKnockBack = true;
    public float DeathAnimationMaxWait = 3f;
    public float DeathFadeDuration = 1f;
    public float DeathSinkDistance = 0.5f;

    [Header("Stat")]
    public Stat Health = new();
    public Stat Mana = new();
    public Stat AttackPower = new();
    public Stat Defense = new();
    public Stat Speed = new();
    public Stat RunSpeed = new();
    public Stat LifeSteel = new();
    public Stat Money = new();

    public Dictionary<StatType, Stat> StatDic;
    private bool m_hasPendingHit;
    private bool m_hasPendingDeath;
    private bool m_isDead;
    private Vector3 m_pendingHitDirection;
    private float m_pendingKnockBackForce;
    private Coroutine m_navRestoreRoutine;
    private bool m_isStageSubscribed;
    private static readonly int KnockBackHash = Animator.StringToHash("KnockBack");
    private static readonly int StateHash = Animator.StringToHash("State");

    private void OnEnable()
    {
        m_hasPendingHit = false;
        m_hasPendingDeath = false;
        m_isDead = false;
        m_navRestoreRoutine = null;
        SubscribeStageLevel();

        if (Ctrl != null && Ctrl.AI != null && Ctrl.AI.NavAgent != null)
        {
            Ctrl.AI.NavAgent.updatePosition = true;
            if (Ctrl.AI.NavAgent.enabled && Ctrl.AI.NavAgent.isOnNavMesh)
                Ctrl.AI.NavAgent.isStopped = false;
        }
    }

    private void OnDisable()
    {
        UnsubscribeStageLevel();
    }

    private void Start()
    {
        StatDic = new Dictionary<StatType, Stat>()
        {
            { StatType.Health, Health },
            { StatType.Mana, Mana },
            { StatType.AttackPower, AttackPower },
            { StatType.Defense, Defense },
            { StatType.Speed, Speed},
            { StatType.RunSpeed, RunSpeed},
            { StatType.LifeSteel, LifeSteel},
            { StatType.Money, Money},
        };

        SubscribeStageLevel();
        if (m_isStageSubscribed == false)
            UpdateStat(1);
    }

    public void UpdateStat(int newLevel)
    {
        if (newLevel > StatData.Count) return;
        StatData[newLevel - 1].SetStat(ref StatDic);
    }

    private void SubscribeStageLevel()
    {
        if (m_isStageSubscribed || StatDic == null ||
            InGameLoop.Instance == null || InGameLoop.Instance.StageLevel == null)
        {
            return;
        }

        InGameLoop.Instance.StageLevel.OnValueChanged += UpdateStat;
        m_isStageSubscribed = true;
        UpdateStat(InGameLoop.Instance.StageLevel.Value);
    }

    private void UnsubscribeStageLevel()
    {
        if (m_isStageSubscribed == false || InGameLoop.Instance == null ||
            InGameLoop.Instance.StageLevel == null)
        {
            m_isStageSubscribed = false;
            return;
        }

        InGameLoop.Instance.StageLevel.OnValueChanged -= UpdateStat;
        m_isStageSubscribed = false;
    }

    public void ApplyDamage(float damage, Vector3 attackerPos, float force)
    {
        if (m_isDead || StatDic == null) return;

        if (StatDic.TryGetValue(StatType.Health, out var value))
        {
            value.AddModifier(new StatModifier(-damage), StatModifyType.Damage);

            if (value.TotalValue < 0.001f)
            {
                m_hasPendingDeath = true;
                m_hasPendingHit = false;
            }
            else if (CanKnockBack)
            {
                m_pendingHitDirection = (transform.position - attackerPos).normalized;
                m_pendingKnockBackForce = force;
                m_hasPendingHit = true;
            }
        }
    }

    public bool ConsumePendingHit(out Vector3 direction, out float force)
    {
        direction = m_pendingHitDirection;
        force = m_pendingKnockBackForce;

        if (!m_hasPendingHit || m_isDead)
            return false;

        m_hasPendingHit = false;
        return true;
    }

    public bool ConsumePendingDeath()
    {
        if (!m_hasPendingDeath || m_isDead)
            return false;

        m_hasPendingDeath = false;
        return true;
    }

    public void ApplyBtHitReaction(Vector3 direction, float force)
    {
        if (m_isDead || Ctrl == null || Ctrl.Rigid == null) return;

        if (direction.sqrMagnitude < 0.001f)
            direction = -transform.forward;

        HitEffect?.OnHit();
        Ctrl.Anim?.SetTrigger(KnockBackHash);

        if (Ctrl.AI != null && Ctrl.AI.NavAgent != null)
        {
            StopNavAgentForHit(Ctrl.AI.NavAgent);
        }

        Ctrl.Rigid.isKinematic = false;
        Ctrl.Rigid.useGravity = true;
        Ctrl.Rigid.linearVelocity = Vector3.zero;
        Ctrl.Rigid.AddForce(direction.normalized * force * Ctrl.Rigid.mass, ForceMode.Impulse);
    }

    private void StopNavAgentForHit(NavMeshAgent navAgent)
    {
        if (navAgent.enabled == false)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                navAgent.enabled = true;
            }
            else
            {
                return;
            }
        }

        if (navAgent.isOnNavMesh == false) return;

        navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;
        navAgent.isStopped = true;
        navAgent.updatePosition = false;
        navAgent.Warp(transform.position);

        if (m_navRestoreRoutine != null)
            StopCoroutine(m_navRestoreRoutine);

        m_navRestoreRoutine = StartCoroutine(RestoreNavAgentAfterHit(navAgent));
    }

    private IEnumerator RestoreNavAgentAfterHit(NavMeshAgent navAgent)
    {
        yield return new WaitForSeconds(0.15f);

        if (m_isDead || navAgent == null)
        {
            m_navRestoreRoutine = null;
            yield break;
        }

        if (navAgent.enabled == false)
        {
            m_navRestoreRoutine = null;
            yield break;
        }

        if (NavMesh.SamplePosition(transform.position, out var hit, 2.0f, NavMesh.AllAreas))
        {
            navAgent.Warp(hit.position);
        }

        if (Ctrl != null && Ctrl.Rigid != null)
        {
            if (Ctrl.Rigid.isKinematic == false)
                Ctrl.Rigid.linearVelocity = Vector3.zero;
            Ctrl.Rigid.useGravity = false;
            Ctrl.Rigid.isKinematic = true;
        }

        navAgent.updatePosition = true;
        if (navAgent.isOnNavMesh)
            navAgent.isStopped = false;

        m_navRestoreRoutine = null;
    }

    public void Die()
    {
        if (m_isDead || Ctrl == null || Ctrl.Rigid == null) return;

        m_isDead = true;
        m_hasPendingHit = false;
        m_hasPendingDeath = false;
        HitEffect?.OnHit();

        if (m_navRestoreRoutine != null)
        {
            StopCoroutine(m_navRestoreRoutine);
            m_navRestoreRoutine = null;
        }
        StopNavigationForDeath();

        if (InGameLoop.Instance != null)
        {
            InGameLoop.Instance.Player.model.Stats[StatType.Money].AddModifier(new StatModifier(StatDic[StatType.Money].TotalValue), StatModifyType.KillEnemy);
            if (Ctrl.EnemyType == EnemyType.Boss_1 ||
                Ctrl.EnemyType == EnemyType.Boss_2 ||
                Ctrl.EnemyType == EnemyType.Boss_3)
                InGameLoop.Instance.ClearBoss();
            else InGameLoop.Instance.KillCount.Value++;
        }

        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        Ctrl.AI?.BTAgent?.SetVariableValue("CurrentType", EnemyStateType.Die);
        Ctrl.Anim?.SetInteger(StateHash, (int)EnemyStateType.Die);

        if (Ctrl.Rigid.isKinematic == false)
            Ctrl.Rigid.linearVelocity = Vector3.zero;
        Ctrl.Rigid.useGravity = false;
        Ctrl.Rigid.isKinematic = true;
        if (Ctrl.AI != null && Ctrl.AI.NavAgent != null)
        {
            if (Ctrl.AI.BTAgent != null)
                Ctrl.AI.BTAgent.enabled = false;

            StopNavigationForDeath();
        }

        var cols = Ctrl.GetComponentsInChildren<Collider>();
        foreach (var item in cols)
        {
            item.enabled = false;
        }
        yield return null;

        var renderers = Ctrl.GetComponentsInChildren<Renderer>();
        var originMaterials = new Dictionary<Renderer, Material[]>();
        var fadeMaterials = new List<Material>();
        var originPosition = Ctrl.transform.position;
        foreach (var renderer in renderers)
        {
            originMaterials[renderer] = renderer.sharedMaterials;
            var materials = renderer.materials;
            foreach (var mat in materials)
            {
                if (mat != null && mat.HasProperty("_Color"))
                    fadeMaterials.Add(mat);
            }
        }

        var waitTime = 0f;
        while (waitTime < DeathAnimationMaxWait)
        {
            var anim = Ctrl.Anim;
            if (anim == null) break;

            var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Die") && stateInfo.normalizedTime >= 1f)
                break;

            waitTime += Time.deltaTime;
            yield return null;
        }

        var sequence = DOTween.Sequence();
        sequence.Join(Ctrl.transform.DOMoveY(originPosition.y - DeathSinkDistance, DeathFadeDuration));
        if (fadeMaterials.Count > 0)
        {
            sequence.Join(DOTween.To(() => 1f, alpha =>
            {
                foreach (var mat in fadeMaterials)
                {
                    if (mat == null) continue;
                    var col = mat.color;
                    col.a = alpha;
                    mat.color = col;
                }
            }, 0f, DeathFadeDuration));
        }

        yield return sequence.WaitForCompletion();

        CleanupAfterDeath(cols, originMaterials, originPosition);
    }

    private void StopNavigationForDeath()
    {
        if (Ctrl == null || Ctrl.AI == null || Ctrl.AI.NavAgent == null) return;

        if (Ctrl.AI.BTAgent != null)
            Ctrl.AI.BTAgent.enabled = false;

        var navAgent = Ctrl.AI.NavAgent;
        if (navAgent.enabled == false)
            navAgent.enabled = true;

        if (navAgent.isOnNavMesh == false)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 2.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                navAgent.Warp(hit.position);
            }
            else
            {
                return;
            }
        }

        navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;
        navAgent.isStopped = true;
        navAgent.updatePosition = false;
        navAgent.Warp(transform.position);
    }

    private void CleanupAfterDeath(Collider[] cols, Dictionary<Renderer, Material[]> originMaterials, Vector3 originPosition)
    {
        foreach (var item in StatDic.Values)
        {
            item.RemoveAllModifier();
        }

        Ctrl.Rigid.useGravity = false;
        Ctrl.Rigid.isKinematic = true;
        Ctrl.transform.position = originPosition;
        foreach (var item in cols)
        {
            if (item != null)
                item.enabled = true;
        }

        foreach (var kvp in originMaterials)
        {
            if (kvp.Key != null)
                kvp.Key.sharedMaterials = kvp.Value;
        }

        if (InGameLoop.Instance != null && InGameLoop.Instance.EnemySpawner != null)
            InGameLoop.Instance.EnemySpawner.DestroyEnemy(Ctrl);
        else
            gameObject.SetActive(false);
    }
}
