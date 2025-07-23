using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public struct StatChange
{
    [SerializeField] public StatType StatType;
    [SerializeField] public StatModifier StatModifier;
    [SerializeField] public float MinNeeded;
    [SerializeField] public float Duration;
    [SerializeField] public float EnforceStatFactor;
}

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

    [HideInInspector] public float maxValue;
    private Dictionary<StatModifyType,StatModifier> m_modifiers = new();

    public Action OnChangeStat;

    public void AddModifier(StatModifier modifier, StatModifyType type) 
    { 
        if (m_modifiers.ContainsKey(type))
        {
            m_modifiers[type] += modifier;
        }
        else m_modifiers.Add(type,modifier);
        UpdateTotalValue();
        OnChangeStat?.Invoke();
    }
    public void RemoveModifier(StatModifyType type) 
    {
        m_modifiers.Remove(type);
        UpdateTotalValue();
        OnChangeStat?.Invoke(); 
    }

    public void RemoveAllModifier()
    {
        m_modifiers.Clear();
        UpdateTotalValue();
    }

    public void UpdateTotalValue()
    {
        var total = BaseValue;
        var maxValue = BaseValue;
        var percentValue = 0f;
        var fixedValue = 0f; 
        var m_percentValue = 0f;
        var m_fixedValue = 0f;
        foreach (var modifier in m_modifiers)
        {
            var mod = modifier.Value;
            percentValue += mod.PercentValue;
            fixedValue += mod.FixedValue;
            if (modifier.Key == StatModifyType.Permanent)
            {
                m_fixedValue += mod.FixedValue;
                m_percentValue += mod.PercentValue;
            }
        }

        total += fixedValue;
        // 퍼센트 비율은 합연산 적용
        var per = 1 + percentValue;
        if (per > 0.1f) total *= per;

        maxValue += m_fixedValue;
        per = 1 + m_percentValue;
        if (per > 0.1f) maxValue *= per;

        // 최소 값 보정
        if (total < 0f) total = 0f;
        if (maxValue < 0f) maxValue = 0f;

        TotalValue = total;
        this.maxValue = maxValue;
    }

    public float GetTotalModifier(StatModifyType type)
    {
        var fixedValue = 0f;
        var percentValue = 0f;
        foreach (var mod in m_modifiers)
        {
            if ((mod.Key & type) != 0)
            {
                fixedValue += mod.Value.FixedValue;
                percentValue += mod.Value.PercentValue;
            }
        }
        var per = 1 + percentValue;
        if (per > 0.1f) fixedValue *= per;
        return fixedValue;
    }
}