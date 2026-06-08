using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StatListener", story: "Listens for [speed] , etc... changes on the [Stat]", category: "Action", id: "7d562192129ae8dc1a6476ccb0985c16")]
public partial class StatListenerAction : Action
{
    [SerializeReference] public BlackboardVariable<float> Speed;
    [SerializeReference] public BlackboardVariable<EnemyStat> Stat;
    [SerializeReference] public BlackboardVariable<EnemyStateType> CurrentType;

    protected override Status OnUpdate()
    {
        var stat = Stat.Value;
        if (stat != null && stat.StatDic != null)
        {
            Speed.Value = stat.StatDic[StatType.Speed].TotalValue;

            if (stat.ConsumePendingDeath())
            {
                CurrentType.Value = EnemyStateType.Die;
                stat.Die();
                return Status.Success;
            }

            if (stat.ConsumePendingHit(out var hitDirection, out var knockBackForce))
            {
                CurrentType.Value = EnemyStateType.KnockBack;
                stat.ApplyBtHitReaction(hitDirection, knockBackForce);
                return Status.Success;
            }

            if (stat.StatDic[StatType.Health].TotalValue < 0.1f)
            {
                CurrentType.Value = EnemyStateType.Die;
            }
        }

        return Status.Success;
    }
}

