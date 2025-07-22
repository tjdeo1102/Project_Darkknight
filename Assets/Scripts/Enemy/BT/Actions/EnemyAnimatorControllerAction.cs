using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "EnemyAnimatorController", story: "[animator] Set [AnimationState] to [currentType]", category: "Action", id: "52c8c9f365c3bcf1d21c1b514a954f44")]
public partial class EnemyAnimatorControllerAction : Action
{
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<string> AnimationState;
    [SerializeReference] public BlackboardVariable<EnemyStateType> CurrentType;
    private int stateHash = -1;

    protected override Status OnStart()
    {
        stateHash = Animator.StringToHash(AnimationState.Value);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (animator.Value != null)
        {
            animator.Value.SetInteger(stateHash, (int)CurrentType.Value);
        }
        return Status.Success;
    }
}

