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

    private void Awake()
    {
        m_rigid = GetComponent<Rigidbody>();
    }

    public void Init(float damage, float lifeTime, Vector3 velocity, float knockBackForce,TargetTag tag,ObjectPool<Projectile> pool)
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
            other.GetComponentInParent<PlayerModel>()?
                .ApplyDamage(Damage, transform.position, KnockBackForce);
        }
        else if (Tag == TargetTag.Enemy)
        {
            other.GetComponentInParent<EnemyStat>()?
                .ApplyDamage(Damage, transform.position, KnockBackForce);
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
