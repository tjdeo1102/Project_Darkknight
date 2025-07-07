using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SpeedListener", story: "Listens for [speed] changes on the [Stat]", category: "Action", id: "7d562192129ae8dc1a6476ccb0985c16")]
public partial class SpeedListenerAction : Action
{
    [SerializeReference] public BlackboardVariable<float> Speed;
    [SerializeReference] public BlackboardVariable<EnemyStat> Stat;

    protected override Status OnStart()
    {
        Stat.Value.StatDic[StatType.Speed].OnChangeStat += OnChangeSpeed;
        OnChangeSpeed();
        return Status.Running;
    }

    public void OnChangeSpeed()
    {
        Speed.Value = Stat.Value.StatDic[StatType.Speed].TotalValue;
    }
    protected override Status OnUpdate()
    {
        return Status.Running;
    }

    protected override void OnEnd()
    {
        Stat.Value.StatDic[StatType.Speed].OnChangeStat -= OnChangeSpeed;

    }
}

