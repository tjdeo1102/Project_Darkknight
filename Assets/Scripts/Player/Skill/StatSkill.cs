using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatSkill", menuName = "Scriptable Objects/Stat Skill")]
public class StatSkill: SkillBase
{
    public override IEnumerator Active(PlayerController player)
    {
        // 요구 조건 만족 후, 스킬 사용
        foreach (StatChange statChange in SkillStats)
        {
            if (player.model.Stats[statChange.StatType].TotalValue < statChange.MinNeeded)
                yield break;
        }

        yield return base.Active(player);

        if (isFailSkill) yield break;

        var maxDuration = 0f;
        // 스탯 관련 로직 적용
        foreach (StatChange statChange in SkillStats)
        {
            maxDuration = Mathf.Max(maxDuration, statChange.Duration);
            player.StartCoroutine(ApplyStatChange(player, statChange));
        }
        yield return new WaitForSeconds(maxDuration);
    }

    private IEnumerator ApplyStatChange(PlayerController player, StatChange statChange)
    {
        StatType type = statChange.StatType;

        // 강화 레벨 * (스탯 증폭치 + 1) * 원래 수치
        var factor = statChange.EnforceStatFactor < 0f ? 0f: statChange.EnforceStatFactor;
        var stat = statChange.StatModifier * (factor + 1) * EnforceLevel;

        // 일시적 버프
        if (statChange.Duration > 0f)
        {
            player.model.Stats[type].AddModifier(stat, StatModifyType.Buff);
            yield return new WaitForSeconds(statChange.Duration);
            player.model.Stats[type].AddModifier(-stat, StatModifyType.Buff);
        }
        // 영구 버프
        else player.model.Stats[type].AddModifier(stat, StatModifyType.Permanent);
    }
}
