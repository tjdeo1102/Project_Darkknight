using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatBaseSO", menuName = "Scriptable Objects/StatBaseSO")]
public class StatBaseSO : CSVScriptableObject
{
    [CSVField(CSVFIledType.StatArr)]
    public StatChange[] Stats;

    public void SetStat(ref Dictionary<StatType,Stat> setStats)
    {
        foreach (var stat in Stats)
        {
            if (setStats.TryGetValue(stat.StatType, out var val))
            {
                val.BaseValue = stat.StatModifier.FixedValue;
                val.UpdateTotalValue();
                val.OnChangeStat?.Invoke();
            }
        }
    }
}
