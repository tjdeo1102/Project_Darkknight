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

    private void Awake()
    {
        m_rigid = GetComponent<Rigidbody>();
    }

    public void Init(float damage, float lifeTime, Vector3 velocity, float knockBackForce,TargetTag tag,ObjectPool<Projectile> pool)
    {
        Damage = damage;
        KnockBackForce = knockBackForce;
        m_rigid.linearVelocity = velocity;
        m_LifeTIme = new WaitForSeconds(lifeTime);
        Tag = tag;
        m_pool = pool;
        StartCoroutine(BulletRoutine());
    }

    IEnumerator BulletRoutine()
    {
        yield return m_LifeTIme;
        m_pool.ReturnObject(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(TagManager.GetTagString(Tag)))
        {
            if (Tag == TargetTag.Player)
            {
                other.GetComponentInParent<PlayerModel>().ApplyDamage(Damage, transform.position, KnockBackForce);
            }
            else if (Tag == TargetTag.Enemy)
            {
                other.GetComponentInParent<EnemyStat>().ApplyDamage(Damage, transform.position, KnockBackForce);
            }
            if (m_pool != null) m_pool.ReturnObject(this);
        }
    }
}
