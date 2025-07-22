using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.Cinemachine;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Scriptable Objects/Projectile Skill")]
public class ProjectileSkill: SkillBase
{
    public ObjectPool<Bullet> BulletPool;
    public float Speed;
    public float LifeTime;
    public float Damage;

    private WaitForSeconds m_delay;

    public override void OnEnable()
    {
        base.OnEnable();
        BulletPool.Init();
        m_delay = new WaitForSeconds(LifeTime);
    }

    public override IEnumerator Active(Transform origin, Dictionary<StatType, Stat> stats, GameObject Target)
    { 
        yield return base.Active(origin, stats, Target);

        if (isFailSkill == false && stats.TryGetValue(StatType.AttackPower, out var atk) == true)
        {
            origin.LookAt(Target.transform);

            var bullet = BulletPool.GetObject();
            var trans = bullet.transform;
            trans.position = center;
            trans.rotation = origin.rotation;

            var velocity = foward * Speed;
            bullet.Init(Damage + atk.TotalValue, LifeTime, velocity, TagManager.TryGetTargetTag(Target.tag) , BulletPool);
        }
    }
}
