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
        [SerializeField] public StatType statType;
        [SerializeField] public ConstStatChange con;
        [SerializeField] public PercentStatChange per;
        [SerializeField] public bool isPercent;
        [SerializeField] public bool isPersist;
        [SerializeField] public float duration;
    }

    [Serializable]
    public struct ConstStatChange
    {
        [SerializeField] public float changeAmount;
    }

    [Serializable]
    public struct PercentStatChange
    {
        [SerializeField] public float changeAmount;
        [SerializeField] public float minRequireAmount;
    }

    public StatChange[] stats;

    public override IEnumerator Active(PlayerController player)
    {
        // 요구 조건 만족 후, 스킬 사용
        foreach (StatChange statChange in stats)
        {
            float require = statChange.isPercent ? statChange.per.minRequireAmount : statChange.con.changeAmount;

            if (require + player.model.Stats[statChange.statType].Value < 0f) yield break;
        }

        yield return base.Active(player);

        if (isBreak)
        {
            isBreak = false;
            yield break;
        }

        List<Coroutine> curRoutine = new List<Coroutine>();
        // 스탯 관련 로직 적용
        foreach (StatChange statChange in stats)
        {
            curRoutine.Add(player.StartCoroutine(ApplyStatChange(player, statChange)));
        }

        while (curRoutine.Count > 0)
        {
            yield return null;
        }
        yield break;
    }

    private IEnumerator ApplyStatChange(PlayerController player, StatChange statChange)
    {
        StatType type = statChange.statType;

        float originalValue = player.model.Stats[type].Value;
        float changeValue;

        if (statChange.isPercent)
        {
            changeValue = originalValue * (statChange.per.changeAmount - 1f);
            if (changeValue < 0)
            {
                if (changeValue > statChange.per.minRequireAmount) changeValue = statChange.per.minRequireAmount;
            }
        }
        else
        {
            changeValue = statChange.con.changeAmount;
        }

        player.model.Stats[type].Value += changeValue;

        yield return new WaitForSeconds(statChange.duration);

        if (!statChange.isPersist)
        {
            player.model.Stats[type].Value -= changeValue;
        }
    }
}
