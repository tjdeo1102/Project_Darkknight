using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CustomPatrol", story: "[Agent] patrols along [Waypoints]", category: "Action", id: "139e38125df1786705dec22d3e4c8e09")]
public partial class CustomPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<List<GameObject>> Waypoints;
    [SerializeReference] public BlackboardVariable<float> Speed = new(3f);
    [SerializeReference] public BlackboardVariable<float> WaypointWaitTime = new(1.0f);
    [SerializeReference] public BlackboardVariable<float> DistanceThreshold = new(0.2f);
    [SerializeReference] public BlackboardVariable<string> AnimatorSpeedParam = new("SpeedMagnitude");
    [Tooltip("Should patrol restart from the latest point?")]
    [SerializeReference] public BlackboardVariable<bool> PreserveLatestPatrolPoint = new(false);

    private NavMeshAgent m_NavMeshAgent;
    private Animator m_Animator;
    [CreateProperty] private Vector3 m_CurrentTarget;
    [CreateProperty] private float m_OriginalStoppingDistance = -1f;
    [CreateProperty] private float m_OriginalSpeed = -1f;
    [CreateProperty] private float m_WaypointWaitTimer;
    private float m_CurrentSpeed;
    [CreateProperty] private int m_CurrentPatrolPoint = 0;
    [CreateProperty] private bool m_Waiting;

    protected override Status OnStart()
    {
        if (Agent == null || Agent.Value == null)
        {
            LogFailure("No agent assigned.");
            return Status.Failure;
        }

        if (HasValidWaypoint() == false)
        {
            LogFailure("No waypoints to patrol assigned.");
            return Status.Failure;
        }

        Initialize();

        m_Waiting = false;
        m_WaypointWaitTimer = 0.0f;

        return MoveToNextWaypoint() ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (Agent == null || Agent.Value == null || HasValidWaypoint() == false)
        {
            return Status.Failure;
        }

        if (m_Waiting)
        {
            if (m_WaypointWaitTimer > 0.0f)
            {
                m_WaypointWaitTimer -= Time.deltaTime;
            }
            else
            {
                m_WaypointWaitTimer = 0f;
                m_Waiting = false;
                if (MoveToNextWaypoint() == false)
                {
                    return Status.Failure;
                }
            }
        }
        else
        {
            float distance = GetDistanceToWaypoint();
            bool destinationReached = distance <= DistanceThreshold;

            // Check if we've reached the waypoint (ensuring NavMeshAgent has completed path calculation if available)
            if (destinationReached && (m_NavMeshAgent == null || !m_NavMeshAgent.pathPending))
            {
                m_WaypointWaitTimer = WaypointWaitTime.Value;
                m_Waiting = true;
                m_CurrentSpeed = 0;

                return Status.Running;
            }
            else if (m_NavMeshAgent == null) // transform-based movement
            {
                m_CurrentSpeed = SimpleMoveTowardsLocation(Agent.Value.transform, m_CurrentTarget, Speed, distance, 1f);
            }
        }

        UpdateAnimatorSpeed();

        return Status.Running;
    }

    protected override void OnEnd()
    {
        UpdateAnimatorSpeed(0f);

        if (m_NavMeshAgent != null)
        {
            if (m_NavMeshAgent.isOnNavMesh)
            {
                m_NavMeshAgent.ResetPath();
            }
            m_NavMeshAgent.speed = m_OriginalSpeed;
            m_NavMeshAgent.stoppingDistance = m_OriginalStoppingDistance;
        }
    }

    protected override void OnDeserialize()
    {
        if (Agent == null || Agent.Value == null)
        {
            m_NavMeshAgent = null;
            return;
        }

        // If using a navigation mesh, we need to reset default value before Initialize.
        m_NavMeshAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
        if (m_NavMeshAgent != null)
        {
            if (m_OriginalSpeed >= 0f)
                m_NavMeshAgent.speed = m_OriginalSpeed;
            if (m_OriginalStoppingDistance >= 0f)
                m_NavMeshAgent.stoppingDistance = m_OriginalStoppingDistance;

            m_NavMeshAgent.Warp(Agent.Value.transform.position);
        }

        int patrolPoint = m_CurrentPatrolPoint - 1;
        Initialize();
        // During deserialization, bypass PreserveLatestPatrolPoint.
        m_CurrentPatrolPoint = patrolPoint;
    }

    private void Initialize()
    {
        m_Animator = Agent.Value.GetComponentInChildren<Animator>();
        m_NavMeshAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
        if (m_NavMeshAgent != null)
        {
            if (m_NavMeshAgent.isOnNavMesh)
            {
                m_NavMeshAgent.ResetPath();
            }

            m_OriginalSpeed = m_NavMeshAgent.speed;
            m_NavMeshAgent.speed = Speed.Value;
            m_OriginalStoppingDistance = m_NavMeshAgent.stoppingDistance;
            m_NavMeshAgent.stoppingDistance = DistanceThreshold;
        }

        m_CurrentPatrolPoint = PreserveLatestPatrolPoint.Value ? m_CurrentPatrolPoint - 1 : -1;

        UpdateAnimatorSpeed(0f);
    }

    private float GetDistanceToWaypoint()
    {
        if (m_NavMeshAgent != null && m_NavMeshAgent.isOnNavMesh)
        {
            return m_NavMeshAgent.remainingDistance;
        }

        Vector3 targetPosition = m_CurrentTarget;
        Vector3 agentPosition = Agent.Value.transform.position;
        agentPosition.y = targetPosition.y; // Ignore y for distance check.
        return Vector3.Distance(agentPosition, targetPosition);
    }

    private bool MoveToNextWaypoint()
    {
        if (Waypoints == null || Waypoints.Value == null || Waypoints.Value.Count == 0) return false;

        for (var i = 0; i < Waypoints.Value.Count; i++)
        {
            m_CurrentPatrolPoint = (m_CurrentPatrolPoint + 1) % Waypoints.Value.Count;
            var waypoint = Waypoints.Value[m_CurrentPatrolPoint];
            if (waypoint == null || waypoint.activeInHierarchy == false) continue;
            if (ChunkManager.Instance != null && ChunkManager.Instance.IsInRestArea(waypoint.transform.position)) continue;

            m_CurrentTarget = waypoint.transform.position;
            if (m_NavMeshAgent != null && m_NavMeshAgent.isOnNavMesh)
            {
                m_NavMeshAgent.SetDestination(m_CurrentTarget);
            }

            return true;
        }

        return false;
    }

    private bool HasValidWaypoint()
    {
        if (Waypoints == null || Waypoints.Value == null) return false;

        for (var i = 0; i < Waypoints.Value.Count; i++)
        {
            var waypoint = Waypoints.Value[i];
            if (waypoint != null && waypoint.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateAnimatorSpeed(float explicitSpeed = -1f)
    {
        UpdateAnimatorSpeed(m_Animator, AnimatorSpeedParam, m_NavMeshAgent, m_CurrentSpeed, explicitSpeed: explicitSpeed);
    }

    private bool UpdateAnimatorSpeed(Animator animator, string speedParameterName, NavMeshAgent navMeshAgent, float currentSpeed, float minSpeedThreshold = 0.1f,
        float explicitSpeed = -1f)
    {
        if (animator == null || string.IsNullOrEmpty(speedParameterName))
        {
            return false;
        }

        float speedValue = 0;
        if (explicitSpeed >= 0)
        {
            speedValue = explicitSpeed;
        }
        else if (navMeshAgent != null)
        {
            speedValue = navMeshAgent.velocity.magnitude;
        }
        else
        {
            speedValue = currentSpeed;
        }

        if (speedValue <= minSpeedThreshold)
        {
            speedValue = 0;
        }

        animator.SetFloat(speedParameterName, speedValue);
        return true;
    }

        
    private float SimpleMoveTowardsLocation(Transform agentTransform, Vector3 targetLocation, float speed, float distance, float slowDownDistance = 0.0f,
        float minSpeedRatio = 0.1f)
    {
        if (agentTransform == null)
        {
            return 0f;
        }

        Vector3 agentPosition = agentTransform.position;
        float movementSpeed = speed;

        // Slowdown
        if (slowDownDistance > 0.0f && distance < slowDownDistance)
        {
            float ratio = distance / slowDownDistance;
            movementSpeed = Mathf.Max(speed * minSpeedRatio, speed * ratio);
        }

        Vector3 toDestination = targetLocation - agentPosition;
        toDestination.y = 0.0f;

        if (toDestination.sqrMagnitude > 0.0001f)
        {
            toDestination.Normalize();

            // Apply movement
            agentPosition += toDestination * (movementSpeed * Time.deltaTime);
            agentTransform.position = agentPosition;

            // Look at the target
            agentTransform.forward = toDestination;
        }

        return movementSpeed;
    }
}
