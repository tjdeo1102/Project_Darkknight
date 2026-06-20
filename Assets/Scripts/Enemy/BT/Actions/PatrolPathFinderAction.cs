using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;
using System.Collections;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PatrolPathFinder", story: "[Agent] finds [PatrolPoints] [Target] [IsActiveNavAgent]", category: "Action", id: "9674cb5a61a9ebf7be683bc6445e5da2")]
public partial class PatrolPathFinderAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<List<GameObject>> PatrolPoints;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<bool> IsActiveNavAgent;
    [SerializeReference] public BlackboardVariable<float> RePathTime;
    [SerializeReference] public BlackboardVariable<int> PatrolPointCount;
    [SerializeReference] public BlackboardVariable<float> PatrolPointDistance;
    [SerializeReference] public BlackboardVariable<bool> IsExitRoutine;

    private NavMeshAgent m_navAgent;
    private ObjectPool<Transform> m_patrolPool;
    [CreateProperty] private float m_Timer = 5f;

    protected override Status OnStart()
    {
        return Init() ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (IsExitRoutine.Value)
            return Status.Success;

        if (IsTimeOut())
        {
            PathFind();
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        base.OnEnd();
    }

    private bool IsTimeOut()
    {
        m_Timer -= Time.deltaTime;
        if (m_Timer < 0f)
        {
            m_Timer = RePathTime;
            return true;
        }
        else return false;
    }

    public bool Init()
    {
        if (Agent == null || Agent.Value == null)
        {
            LogFailure("No agent assigned.");
            return false;
        }

        if (PatrolPoints == null)
        {
            LogFailure("No patrol point variable assigned.");
            return false;
        }

        m_navAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
        ReturnPatrolPoints();
        PatrolPoints.Value = new List<GameObject>();
        if (m_patrolPool == null)
        {
            m_patrolPool = new ObjectPool<Transform>();
            var template = new GameObject("Patrol Point Template").transform;
            template.SetParent(Agent.Value.transform, false);
            template.gameObject.SetActive(false);
            m_patrolPool.poolObj = template;
            m_patrolPool.InitSize = PatrolPointCount;
            m_patrolPool.Init(Agent.Value.transform);
        }

        if (InGameLoop.Instance != null)
            Target.Value = InGameLoop.Instance.Player.gameObject;

        m_Timer = 0f;
        return true;
    }

    public void Setup(List<GameObject> patrolPoints)
    {
        if (m_navAgent == null) return;
        m_navAgent.updateRotation = true;
        m_navAgent.updateUpAxis = true;

        PatrolPoints.Value = patrolPoints;
    }

    public GameObject CreatePatrolPoint()
    {
        const int maxAttempts = 12;
        for (var i = 0; i < maxAttempts; i++)
        {
            Vector3 ranPos = UnityEngine.Random.insideUnitCircle * PatrolPointDistance;
            ranPos += Agent.Value.transform.position;
            if (NavMesh.SamplePosition(ranPos, out NavMeshHit hit, PatrolPointDistance, NavMesh.AllAreas) == false)
                continue;
            if (ChunkManager.Instance != null && ChunkManager.Instance.IsInRestArea(hit.position))
                continue;

            var obj = m_patrolPool.GetObject();
            obj.position = hit.position;
            return obj.gameObject;
        }

        return null;
    }

    public void PathFind()
    {
        if (Agent == null || Agent.Value == null || m_navAgent == null || m_patrolPool == null) return;

        ReturnPatrolPoints();
        var patrolPoints = new List<GameObject>();
        // Check if Agent is currently placed on valid NavMesh Path
        if (m_navAgent != null && IsActiveNavAgent == true && m_navAgent.isOnNavMesh)
        {
            if (patrolPoints.Count < 1)
            {
                patrolPoints = new List<GameObject>();

                for (int i = 0; i < 4; i++)
                {
                    var obj = CreatePatrolPoint();
                    if (obj != null)
                        patrolPoints.Add(obj);
                }
                Setup(patrolPoints);
            }
        }
        // RePath & Check if Agent is currently placed on unvalid NavMesh Path
        else
        {
            if (NavMesh.SamplePosition(Agent.Value.transform.position, out NavMeshHit originHit, 2.0f, NavMesh.AllAreas))
            {
                m_navAgent.enabled = false;
                Agent.Value.transform.position = originHit.position;
                m_navAgent.enabled = true;
                IsActiveNavAgent.Value = true;

                if (patrolPoints.Count < 1)
                {
                    patrolPoints = new List<GameObject>();

                    for (int i = 0; i < 4; i++)
                    {
                        var obj = CreatePatrolPoint();
                        if (obj != null)
                            patrolPoints.Add(obj);
                    }
                    Setup(patrolPoints);
                }
            }
            else
            {
                m_navAgent.enabled = false;
                IsActiveNavAgent.Value = false;

                //Debug.LogWarning($"Agent {Agent.Value.name} is not on NavMesh at {Agent.Value.transform.position}");

                foreach (var obj in patrolPoints)
                {
                    m_patrolPool.ReturnObject(obj.transform);
                }
                patrolPoints.Clear();
            }
        }
    }

    private void ReturnPatrolPoints()
    {
        if (m_patrolPool == null || PatrolPoints?.Value == null) return;

        foreach (var patrolPoint in PatrolPoints.Value)
        {
            if (patrolPoint != null && patrolPoint.activeSelf)
            {
                m_patrolPool.ReturnObject(patrolPoint.transform);
            }
        }

        PatrolPoints.Value.Clear();
    }
}

