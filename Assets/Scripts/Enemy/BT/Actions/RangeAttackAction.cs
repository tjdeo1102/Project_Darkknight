using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RangeAttack", story: "[Agent] with [stats] attacks [Target] using [Skill]", category: "Action", id: "d155235ec6029411be037da07731909e")]
public partial class RangeAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<EnemyStat> Stats;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<SkillBase> Skill;

    protected override Status OnStart()
    {
        if (Skill.Value == null || 
            Stats.Value == null ||
            Agent.Value == null ||
            Target.Value == null)
        {
            return Status.Failure;
        }

        var mono = Stats.Value;
        mono.StartCoroutine(Skill.Value.Active(Agent.Value.transform, Stats.Value.StatDic, Target));

        return Status.Running;
    }
}

