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
    protected override Status OnStart()
    {
        Speed.Value = Stat.Value.StatDic[StatType.Speed].TotalValue;
        if (Stat.Value.StatDic[StatType.Health].TotalValue < 0.1f)
            CurrentType.Value = EnemyStateType.Die;
        return Status.Running;
    }
}

