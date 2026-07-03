using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float Damage;
    public float KnockBackForce;
    public TargetTag Tag;
    private WaitForSeconds m_LifeTIme;
    private ObjectPool<Projectile> m_pool;
    private Rigidbody m_rigid;
    private Coroutine m_lifetimeRoutine;
    private bool m_isReturning;
    private CombatActionSO m_feedbackAction;
    private Component m_feedbackOwner;

    private void Awake()
    {
        m_rigid = GetComponent<Rigidbody>();
    }

    public void Init(float damage, float lifeTime, Vector3 velocity, float knockBackForce,TargetTag tag,ObjectPool<Projectile> pool)
    {
        Init(damage, lifeTime, velocity, knockBackForce, tag, pool, null, null);
    }

    public void Init(
        float damage,
        float lifeTime,
        Vector3 velocity,
        float knockBackForce,
        TargetTag tag,
        ObjectPool<Projectile> pool,
        CombatActionSO feedbackAction,
        Component feedbackOwner)
    {
        if (m_lifetimeRoutine != null)
        {
            StopCoroutine(m_lifetimeRoutine);
            m_lifetimeRoutine = null;
        }

        m_isReturning = false;
        Damage = damage;
        KnockBackForce = knockBackForce;
        m_rigid.linearVelocity = velocity;
        m_LifeTIme = new WaitForSeconds(lifeTime);
        Tag = tag;
        m_pool = pool;
        m_feedbackAction = feedbackAction;
        m_feedbackOwner = feedbackOwner;
        m_lifetimeRoutine = StartCoroutine(BulletRoutine());
    }

    IEnumerator BulletRoutine()
    {
        yield return m_LifeTIme;
        m_lifetimeRoutine = null;
        ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_isReturning || other.CompareTag(TagManager.GetTagString(Tag)) == false)
            return;

        if (Tag == TargetTag.Player)
        {
            var player = other.GetComponentInParent<PlayerModel>();
            if (player != null)
            {
                player.ApplyDamage(Damage, transform.position, KnockBackForce);
                CombatActionFeedback.PlayOnHit(m_feedbackAction, m_feedbackOwner);
            }
        }
        else if (Tag == TargetTag.Enemy)
        {
            var enemy = other.GetComponentInParent<EnemyStat>();
            if (enemy != null)
            {
                enemy.ApplyDamage(Damage, transform.position, KnockBackForce);
                CombatActionFeedback.PlayOnHit(m_feedbackAction, m_feedbackOwner);
            }
        }

        ReturnToPool();
    }

    private void OnDisable()
    {
        if (m_lifetimeRoutine != null)
        {
            StopCoroutine(m_lifetimeRoutine);
            m_lifetimeRoutine = null;
        }

        if (m_rigid != null)
            m_rigid.linearVelocity = Vector3.zero;

        m_feedbackAction = null;
        m_feedbackOwner = null;
    }

    private void ReturnToPool()
    {
        if (m_isReturning) return;
        m_isReturning = true;

        if (m_lifetimeRoutine != null)
        {
            StopCoroutine(m_lifetimeRoutine);
            m_lifetimeRoutine = null;
        }

        if (m_pool != null)
            m_pool.ReturnObject(this);
        else
            gameObject.SetActive(false);
    }
}
