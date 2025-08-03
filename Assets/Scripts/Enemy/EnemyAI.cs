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
        BTAgent.enabled = true;
        BTAgent.Init();
        BTAgent.Restart();

        NavAgent.updateRotation = false;
    }

    private void OnDisable()
    {
        BTAgent.enabled = false;
        if (NavAgent != null) NavAgent.enabled = false;
    }
}
