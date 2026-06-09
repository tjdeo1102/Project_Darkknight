using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "ListCountCondition", story: "[PatrolPoints] is [Operator] [ComparisonIntValue]", category: "Conditions", id: "3b40de411046536c259e667ddc439549")]
public partial class ListCountCondition : Condition
{
    [SerializeReference] public BlackboardVariable<List<GameObject>> PatrolPoints;

    [Comparison(comparisonType: ComparisonType.BlackboardVariables, variable: "PatrolPoints.Count", comparisonValue: "ComparisonIntValue")]
    [SerializeReference] public BlackboardVariable<ConditionOperator> Operator;
    [SerializeReference] public BlackboardVariable ComparisonIntValue;
    public override bool IsTrue()
    {
        if (PatrolPoints == null || ComparisonIntValue == null) return false;
        if (PatrolPoints.Value == null || ComparisonIntValue.Type != typeof(int)) return false;
        var validCount = 0;
        foreach (var patrolPoint in PatrolPoints.Value)
        {
            if (patrolPoint != null && patrolPoint.activeInHierarchy)
            {
                validCount++;
            }
        }

        return ConditionUtils.Evaluate(validCount, Operator, ComparisonIntValue);
    }
}
