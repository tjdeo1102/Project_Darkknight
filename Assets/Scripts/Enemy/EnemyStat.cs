using System.Collections.Generic;
using UnityEngine;

public class EnemyStat : MonoBehaviour
{
    public Stat Health = new();
    public Stat Mana = new();
    public Stat AttackPower = new();
    public Stat Defense = new();
    public Stat Speed = new();
    public Stat RunSpeed = new();
    public Stat LifeSteel = new();
    public Stat Money = new();

    public Dictionary<StatType, Stat> StatDic;

    private void Start()
    {
        StatDic = new Dictionary<StatType, Stat>()
        {
            { StatType.Health, Health },
            { StatType.Mana, Mana },
            { StatType.AttackPower, AttackPower },
            { StatType.Defense, Defense },
            { StatType.Speed, Speed},
            { StatType.RunSpeed, RunSpeed},
            { StatType.LifeSteel, LifeSteel},
            { StatType.Money, Money},
        };
        foreach (var item in StatDic)
        {
            item.Value.UpdateTotalValue();
            item.Value.OnChangeStat?.Invoke();
        }
    }

    public void ApplyDamage(StatModifier stat, StatType statType, StatModifyType modifyType)
    {
        if (StatDic.TryGetValue(statType, out var value))
        {
            value.AddModifier(stat, modifyType);
            value.UpdateTotalValue();
        }
    }
}
