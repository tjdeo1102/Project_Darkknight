using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public EnemyStat Model;
    public EnemyAI AI;

    // Test
    public List<GameObject> patrolPoints;
    public GameObject Target;
    public float PatrolPointDistance = 10f;

    private void Start()
    {
        if (ChunkManager.Instance != null)
        {
            patrolPoints = new List<GameObject>();

            for (int i = 0; i < 4; i++)
            {
                var obj = CreatePatrolPoint();
                if (obj != null)
                    patrolPoints.Add(obj);
            }
        }

        AI.Setup(Target, patrolPoints);
    }

    public GameObject CreatePatrolPoint()
    {
        Vector3 ranPos = Random.insideUnitCircle * PatrolPointDistance;
        ranPos += transform.position;
        if (NavMesh.SamplePosition(ranPos, out NavMeshHit hit, PatrolPointDistance, NavMesh.AllAreas))
        {
            GameObject obj = new GameObject("point");
            obj.transform.position = hit.position;
            return obj;
        }
        return null;
    }
}
