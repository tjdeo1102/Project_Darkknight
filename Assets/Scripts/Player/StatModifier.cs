using System;
using UnityEngine;

[Serializable]
public class StatModifier
{
    public float FixedValue;
    public float PercentValue;

    public StatModifier(float fixedValue, float percentValue)
    {
        this.FixedValue = fixedValue;
        this.PercentValue = percentValue;
    }

    public static StatModifier operator +(StatModifier lhs, StatModifier rhs)
    {
        var a = lhs.FixedValue + rhs.FixedValue;
        var b = lhs.PercentValue + rhs.PercentValue;
        return new StatModifier(a, b);
    }

    public static StatModifier operator -(StatModifier rhs)
    {
        return new StatModifier(-rhs.FixedValue, -rhs.PercentValue);
    }

    public static StatModifier operator *(StatModifier lhs, float rhs)
    {
        var a = lhs.FixedValue * rhs;
        var b = lhs.PercentValue * rhs;
        return new StatModifier(a, b);
    }
}
