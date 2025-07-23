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
        if (ProjectileManager.Instance == null || ProjectileType == ProjectileType.None) yield break;
        yield return base.Active(origin, stats, Target);

        if (isFailSkill == false && stats.TryGetValue(StatType.AttackPower, out var atk) == true)
        {
            origin.LookAt(Target.transform);

            var pool = ProjectileManager.Instance.ProjectileDic[ProjectileType];
            var bullet = pool.GetObject();
            var trans = bullet.transform;
            trans.position = center;
            trans.rotation = origin.rotation;

            // 딜레이로 인한 foward 갱신 필요
            SetStartPos(origin);
            var velocity = foward * Speed;

            var Damage = 0f;
            var atts = SkillStats.Where(skillStat => skillStat.StatType == StatType.AttackPower).ToArray();
            if (atts.Length > 0)
            {
                var baseDamage = atts[0].StatModifier.FixedValue * (atts[0].StatModifier.PercentValue + 1);
                Damage = atts[0].EnforceStatFactor * EnforceLevel + baseDamage;
            }

            bullet.Init(Damage + atk.TotalValue, LifeTime, velocity, KnockBackForce, TagManager.TryGetTargetTag(Target.tag), pool);
        }
    }
}
