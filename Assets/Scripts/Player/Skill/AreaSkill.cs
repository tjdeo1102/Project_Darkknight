using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static UnityEngine.UI.Image;

[CreateAssetMenu(fileName = "AreaSkill", menuName = "Scriptable Objects/Area Skill")]
public class AreaSkill: SkillBase
{
    public Vector3 HitOffset = Vector3.zero;
    public Vector3 Range = Vector3.zero;
    public float KnockBackForce = 1f;
    public LayerMask TargetLayer;

    public override IEnumerator Active(PlayerController player)
    {
        if (player.combat.CurType != RequireWeapon)
        {
            isFailSkill = true;
            yield break;
        }

        yield return base.Active(player);

        if (isFailSkill) yield break;
        Hit(player.transform, player.model.Stats, TargetTag.Enemy);
        yield break;
    }

    public override IEnumerator Active(Transform origin, Dictionary<StatType, Stat> stats, GameObject Target)
    {
        yield return base.Active(origin, stats, Target);

        if (isFailSkill) yield break;
        Hit(origin, stats,TagManager.TryGetTargetTag(Target.tag));

        yield break;
    }

    public void Hit(Transform origin, Dictionary<StatType, Stat> stats, TargetTag Target)
    {
        SetStartPos(origin);
        var center = this.center + foward * HitOffset.z + right * HitOffset.x + up * HitOffset.y;
        var rotation = Quaternion.LookRotation(foward);
        Vector3 halfExtents = Range * 0.5f;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, TargetLayer);

        Tool.DrawOverlapBox(center, Range, rotation, Color.green, 2f);

        var atk = 0f;
        if (stats.TryGetValue(StatType.AttackPower, out var stat))
            atk = stat.TotalValue;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(TagManager.GetTagString(Target)))
            {
                var Damage = 0f;
                var atts = SkillStats.Where(skillStat => skillStat.StatType == StatType.AttackPower).ToArray();
                if (atts.Length > 0)
                {
                    var baseDamage = atts[0].StatModifier.FixedValue * (atts[0].StatModifier.PercentValue + 1);
                    Damage = atts[0].EnforceStatFactor * EnforceLevel + baseDamage;
                }
                if (Target == TargetTag.Player)
                {
                    hit.GetComponentInParent<PlayerModel>().ApplyDamage(Damage + atk, origin.transform.position, KnockBackForce);
                }
                else if (Target == TargetTag.Enemy)
                {
                    hit.GetComponentInParent<EnemyStat>().ApplyDamage(Damage + atk, origin.transform.position, KnockBackForce);
                }
            }
        }
    }
}
