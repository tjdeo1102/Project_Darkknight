using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ClosedAttack", story: "[Agent] with [stats] attacks [Target] using [Skill]", category: "Action", id: "8e96e39bd83ced1e8a89afdab50670fb")]
public partial class ClosedAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<EnemyStat> Stats;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<SkillBase> Skill;
    protected override Status OnStart()
    {
        Debug.Log("�� ����");
        return Status.Running;
    }

}

