using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public EnemyController Ctrl;
    public NavMeshAgent NavAgent;
    public BehaviorGraphAgent BTAgent;

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
        NavAgent.enabled = false;
    }

    //private void RotationToTarget()
    //{
    //    Vector3 direction = NavAgent.desiredVelocity;
    //    direction.y = 0f;

    //    if (direction.sqrMagnitude > 0.01f)
    //    {
    //        Quaternion targetRotation = Quaternion.LookRotation(direction);
    //        Vector3 targetEuler = targetRotation.eulerAngles;

    //        // Y축만 회전
    //        Quaternion yOnlyRotation = Quaternion.Euler(0, targetEuler.y, 0);

    //        transform.rotation = Quaternion.Slerp(
    //            transform.rotation,
    //            yOnlyRotation,
    //            Time.deltaTime * turnSpeed
    //        );
    //    }
    //}
}
