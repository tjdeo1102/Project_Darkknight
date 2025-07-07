using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public float Damage;
    public TargetTag Tag;
    private WaitForSeconds m_LifeTIme;
    private ObjectPool<Bullet> m_pool;

    public void Init(float damage, float lifeTime, Vector3 velocity,TargetTag tag,ObjectPool<Bullet> pool)
    {
        Damage = damage;
        GetComponent<Rigidbody>().linearVelocity = velocity;
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
                other.GetComponentInParent<PlayerModel>().Stats[StatType.Health].AddModifier(new StatModifier(Damage), StatModifyType.Damage);
            }
            else if (Tag == TargetTag.Enemy)
            {
                other.GetComponentInParent<EnemyStat>().StatDic[StatType.Health].AddModifier(new StatModifier(Damage), StatModifyType.Damage);
            }
        }
    }
}
