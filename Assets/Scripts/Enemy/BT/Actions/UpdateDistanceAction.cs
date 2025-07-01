using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "UpdateDistance", story: "Update [Self] and [Target] [CurrentDistnace]", category: "Action", id: "ad538d1f92bf3343e046ac5ad473d275")]
public partial class UpdateDistanceAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> CurrentDistnace;


    protected override Status OnUpdate()
    {
        CurrentDistnace.Value = Vector3.Distance(Self.Value.transform.position,Target.Value.transform.position);
        return Status.Success;
    }
}

