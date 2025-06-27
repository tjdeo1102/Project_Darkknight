using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class Stat
{
    [SerializeField] private float baseValue;
    public float BaseValue
    {
        get => baseValue;
        set
        {
            this.baseValue = value;
            if (value < 0f) this.baseValue = 0f;
        }
    }
    public float TotalValue;

    [HideInInspector] public float addValue;
    [HideInInspector] public float maxValue;
    private Dictionary<StatModifyType,StatModifier> modifiers = new();

    public Action OnChangeStat;

    public void AddModifier(StatModifier modifier, StatModifyType type) 
    { 
        if (modifiers.ContainsKey(type))
        {
            modifiers[type] += modifier;
        }
        else modifiers.Add(type,modifier);
        UpdateTotalValue();
        OnChangeStat?.Invoke();
    }
    public void RemoveModifier(StatModifyType type) 
    { 
        modifiers.Remove(type);
        UpdateTotalValue();
        OnChangeStat?.Invoke(); 
    }

    public void UpdateTotalValue()
    {
        var total = BaseValue;
        var maxValue = BaseValue;
        var percentValue = 0f;
        var fixedValue = 0f; 
        var m_percentValue = 0f;
        var m_fixedValue = 0f;
        foreach (var modifier in modifiers)
        {
            var mod = modifier.Value;
            percentValue += mod.PercentValue;
            fixedValue += mod.FixedValue;
            if (modifier.Key == StatModifyType.Perment)
            {
                m_fixedValue += mod.FixedValue;
                m_percentValue += mod.PercentValue;
            }
        }

        total += fixedValue;
        // 퍼센트 비율은 합연산 적용
        total *= 1 + percentValue;

        maxValue += m_fixedValue;
        maxValue *= 1 + m_percentValue;

        // 최소 값 보정
        if (total < 0f) total = 1f;
        if (maxValue < 0f) maxValue = 1f;

        TotalValue = total;
        addValue = total - BaseValue;
    }
}