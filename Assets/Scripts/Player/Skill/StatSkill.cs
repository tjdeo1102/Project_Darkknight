using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "StatSkill", menuName = "Scriptable Objects/Stat Skill")]
public class StatSkill : SkillBase
{
    public override IEnumerator Active(PlayerController player)
    {
        if (player == null ||
            player.model.Stats.TryGetValue(StatType.Mana, out var mp) == false)
        {
            yield break;
        }

        // Validate requirements before consuming mana or cooldown.
        foreach (var statChange in SkillStats)
        {
            if (player.model.Stats.TryGetValue(statChange.StatType, out var stat) == false ||
                stat.TotalValue < statChange.MinNeeded)
            {
                yield break;
            }
        }

        var context = CreateContext(player.combat, player.transform);
        yield return BeginSkill(player.combat, player.transform, mp, context, player);

        if (context.Failed) yield break;

        var maxDuration = 0f;
        foreach (var statChange in SkillStats)
        {
            maxDuration = Mathf.Max(maxDuration, statChange.Duration);
            player.StartCoroutine(
                ApplyStatChange(player, statChange, context.State.EnforceLevel));
        }

        yield return new WaitForSeconds(maxDuration);
    }

    private IEnumerator ApplyStatChange(
        PlayerController player,
        StatChange statChange,
        int enforceLevel)
    {
        var factor = Mathf.Max(0f, statChange.EnforceStatFactor);
        var modifier = statChange.StatModifier * (factor + 1) * enforceLevel;
        var stat = player.model.Stats[statChange.StatType];

        if (statChange.Duration > 0f)
        {
            stat.AddModifier(modifier, StatModifyType.Buff);
            yield return new WaitForSeconds(statChange.Duration);
            stat.AddModifier(-modifier, StatModifyType.Buff);
        }
        else
        {
            stat.AddModifier(modifier, StatModifyType.Permanent);
        }
    }
}
