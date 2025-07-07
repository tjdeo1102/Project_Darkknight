using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatSkill", menuName = "Scriptable Objects/Stat Skill")]
public class StatSkill: SkillBase
{
    [Serializable]
    public struct StatChange
    {
        [SerializeField] public StatType StatType;
        [SerializeField] public StatModifier StatModifier;
        [SerializeField] public float MinNeeded;
        [SerializeField] public float Duration;
        [SerializeField] public float EnforceStatFactor;
    }

    [CSVField(CSVFIledType.StatArr)]
    public StatChange[] Stats;

    public override IEnumerator Active(PlayerController player)
    {
        // 요구 조건 만족 후, 스킬 사용
        foreach (StatChange statChange in Stats)
        {
            if (player.model.Stats[statChange.StatType].TotalValue < statChange.MinNeeded)
                yield break;
        }

        yield return base.Active(player);

        if (isFailSkill) yield break;

        var maxDuration = 0f;
        // 스탯 관련 로직 적용
        foreach (StatChange statChange in Stats)
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
        var stat = statChange.StatModifier * (statChange.EnforceStatFactor + 1) * EnforceLevel;

        // 일시적 버프
        if (statChange.Duration > 0f)
        {
            player.model.Stats[type].AddModifier(stat, StatModifyType.Buff);
            yield return new WaitForSeconds(statChange.Duration);
            player.model.Stats[type].AddModifier(-stat, StatModifyType.Buff);
        }
        // 영구 버프
        else player.model.Stats[type].AddModifier(stat, StatModifyType.Perment);
    }
}
