using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.Cinemachine;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Scriptable Objects/Projectile Skill")]
public class ProjectileSkill : SkillBase
{
    public ProjectileType ProjectileType;
    public float Speed;
    public float LifeTime;
    public float KnockBackForce = 1f;


    public override IEnumerator Active(Transform origin, Dictionary<StatType, Stat> stats, GameObject Target)
    {
        if (ProjectileManager.Instance == null || ProjectileType == ProjectileType.None ||
            origin == null || stats == null || stats.TryGetValue(StatType.Mana, out var mp) == false) yield break;

        var owner = origin.GetComponentInParent<EnemyController>() as Component ?? origin;
        var context = CreateContext(owner, origin);
        yield return BeginSkill(owner, origin, mp, context);

        if (context.Failed == false && stats.TryGetValue(StatType.AttackPower, out var atk))
        {
            if (Target == null) yield break;

            origin.LookAt(Target.transform);
            UpdateStartPose(origin, context);

            var pool = ProjectileManager.Instance.ProjectileDic[ProjectileType];
            var bullet = pool.GetObject();
            var trans = bullet.transform;
            trans.position = context.Center;
            trans.rotation = origin.rotation;

            var velocity = context.Forward * Speed;

            var Damage = 0f;
            var atts = SkillStats.Where(skillStat => skillStat.StatType == StatType.AttackPower).ToArray();
            if (atts.Length > 0)
            {
                var baseDamage = atts[0].StatModifier.FixedValue * (atts[0].StatModifier.PercentValue + 1);
                Damage = atts[0].EnforceStatFactor * context.State.EnforceLevel + baseDamage;
            }

            bullet.Init(
                Damage + atk.TotalValue,
                LifeTime,
                velocity,
                KnockBackForce,
                TagManager.TryGetTargetTag(Target.tag),
                pool,
                this,
                owner);
        }
    }
}
