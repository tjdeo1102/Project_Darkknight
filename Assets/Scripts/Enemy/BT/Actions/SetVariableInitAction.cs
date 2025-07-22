using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetVariableInit", story: "Variables set", category: "Action", id: "3d1b96b57180e3d897eceb5acbf19679")]
public partial class SetVariableInitAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<EnemyStat> Stat;
    [SerializeReference] public BlackboardVariable<float> Speed;
    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (animator.Value == null)
        {
            animator.Value = Agent.Value.GetComponentInChildren<Animator>();
        }
        if (Target.Value == null)
        {
            Target.Value = Agent.Value.GetComponentInChildren<EnemyController>().Target;
        }
        if (Stat.Value == null)
        {
            Stat.Value = Agent.Value.GetComponentInChildren<EnemyStat>();
        }
        Speed.Value = Stat.Value.Speed.TotalValue;
        
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

