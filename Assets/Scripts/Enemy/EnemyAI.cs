using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public EnemyController Ctrl;
    public NavMeshAgent NavAgent;
    public BehaviorGraphAgent BTAgent;

    private void Awake()
    {
        if (NavAgent != null) NavAgent.enabled = false;
    }

    private void OnEnable()
    {
        EnsureNavAgentReady();

        BTAgent.enabled = true;
        BTAgent.Init();
        BTAgent.Restart();

        if (NavAgent != null)
            NavAgent.updateRotation = false;
    }

    private void OnDisable()
    {
        BTAgent.enabled = false;
        if (NavAgent != null) NavAgent.enabled = false;
    }

    public bool EnsureNavAgentReady(float sampleDistance = 2f)
    {
        if (NavAgent == null) return false;

        NavAgent.updateRotation = false;

        if (NavAgent.enabled == false)
            NavAgent.enabled = true;

        if (NavAgent.isOnNavMesh)
        {
            NavAgent.isStopped = false;
            return true;
        }

        if (NavMesh.SamplePosition(transform.position, out var hit, sampleDistance, NavMesh.AllAreas) == false)
        {
            NavAgent.enabled = false;
            return false;
        }

        transform.position = hit.position;
        NavAgent.Warp(hit.position);
        NavAgent.isStopped = false;
        return true;
    }
}
