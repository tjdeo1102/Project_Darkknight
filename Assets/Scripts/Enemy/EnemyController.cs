using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public EnemyStat Model;
    public EnemyAI AI;

    // Test
    public List<GameObject> patrolPoints;
    public GameObject Target;

    private void Start()
    {
        AI.Setup(Target, patrolPoints);
    }
}
