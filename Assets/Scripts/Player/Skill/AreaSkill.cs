using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AreaSkill", menuName = "Scriptable Objects/Area Skill")]
public class AreaSkill: SkillBase
{
    public Vector3 HitOffset = Vector3.zero;
    public Vector3 Range = Vector3.zero;
    public float KnockBackForce = 1f;
    public LayerMask TargetLayer;

    public override IEnumerator Active(PlayerController player)
    {
        if (player == null ||
            (RequireWeapon != WeaponType.None &&
             player.combat.CurType != RequireWeapon))
        {
            yield break;
        }
        if (player.model.Stats.TryGetValue(StatType.Mana, out var mp) == false) yield break;

        var context = CreateContext(player.combat, player.transform);
        yield return BeginSkill(player.combat, player.transform, mp, context, player);

        if (context.Failed) yield break;
        Hit(player.transform, player.model.Stats, TargetTag.Enemy, context);
        yield break;
    }

    public override IEnumerator Active(Transform origin, Dictionary<StatType, Stat> stats, GameObject Target)
    {
        if (origin == null || stats == null || stats.TryGetValue(StatType.Mana, out var mp) == false) yield break;

        var owner = origin.GetComponentInParent<EnemyController>() as Component ?? origin;
        var context = CreateContext(owner, origin);
        yield return BeginSkill(owner, origin, mp, context);

        if (context.Failed || Target == null) yield break;
        Hit(origin, stats, TagManager.TryGetTargetTag(Target.tag), context);
        yield break;
    }

    public void Hit(
        Transform origin,
        Dictionary<StatType, Stat> stats,
        TargetTag target,
        SkillExecutionContext context)
    {
        UpdateStartPose(origin, context);
        var center = context.Center
                     + context.Forward * HitOffset.z
                     + context.Right * HitOffset.x
                     + context.Up * HitOffset.y;
        var rotation = Quaternion.LookRotation(context.Forward);
        Vector3 halfExtents = Range * 0.5f;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, TargetLayer);

        Tool.DrawOverlapBox(center, Range, rotation, Color.green, 2f);

        var atk = 0f;
        if (stats.TryGetValue(StatType.AttackPower, out var stat))
            atk = stat.TotalValue;

        var damagedPlayers = new HashSet<PlayerModel>();
        var damagedEnemies = new HashSet<EnemyStat>();
        var hasHitTarget = false;
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(TagManager.GetTagString(target)))
            {
                var Damage = 0f;
                var atts = SkillStats.Where(skillStat => skillStat.StatType == StatType.AttackPower).ToArray();
                if (atts.Length > 0)
                {
                    var baseDamage = atts[0].StatModifier.FixedValue * (atts[0].StatModifier.PercentValue + 1);
                    Damage = atts[0].EnforceStatFactor * context.State.EnforceLevel + baseDamage;
                }
                if (target == TargetTag.Player)
                {
                    var player = hit.GetComponentInParent<PlayerModel>();
                    if (player != null && damagedPlayers.Add(player))
                    {
                        player.ApplyDamage(Damage + atk, origin.transform.position, KnockBackForce);
                        hasHitTarget = true;
                    }
                }
                else if (target == TargetTag.Enemy)
                {
                    var enemy = hit.GetComponentInParent<EnemyStat>();
                    if (enemy != null && damagedEnemies.Add(enemy))
                    {
                        enemy.ApplyDamage(Damage + atk, origin.transform.position, KnockBackForce);
                        hasHitTarget = true;
                    }
                }
            }
        }

        if (hasHitTarget)
            CombatActionFeedback.PlayOnHit(this, origin);
    }
}
