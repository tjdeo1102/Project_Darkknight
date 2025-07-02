using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public GameObject Target;
    public NavMeshAgent NavAgent;
    public BehaviorGraphAgent BTAgent;

    public void Setup(GameObject target, List<GameObject> patrolPoints)
    {
        Target = target;

        if (NavAgent == null || BTAgent == null) return;
        NavAgent.updateRotation = true;
        NavAgent.updateUpAxis = true;

        BTAgent.SetVariableValue("PatrolPoints", patrolPoints);
        BTAgent.SetVariableValue("Target", Target);
    }
}
