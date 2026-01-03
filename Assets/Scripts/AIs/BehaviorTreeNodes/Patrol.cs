using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;
using TooltipAttribute = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;

namespace BehaviorTreeNodes
{
    [TaskCategory("NPCAgent")]
    [TaskDescription("Moves the agent between a set of patrol points in order or randomly.")]
    public class Patrol : Action
    {
        [Tooltip("List of patrol points the NPC will visit.")]
        public SharedGameObjectList PatrolPoints;

        [Tooltip("Whether to patrol points in random order.")]
        public SharedBool RandomPatrol;

        [Tooltip("Distance from waypoint to consider it 'reached'.")]
        public float ArriveThreshold = 0.5f;

        [Tooltip("Optional delay when reaching a waypoint (seconds).")]
        public float WaitTime = 1.5f;

        private NPCAgent agent;
        private NavMeshAgent navMeshAgent;

        private int currentIndex = 0;
        private float waitTimer = 0f;
        private bool waiting = false;

        public override void OnStart()
        {
            if (!Owner.TryGetComponent(out agent))
            {
                Debug.LogError("NPCAgent is required for Patrol Action");
                return;
            }

            navMeshAgent = agent.NavMeshAgent;
            if (navMeshAgent == null)
            {
                Debug.LogError("NavMeshAgent is required for Patrol Action");
                return;
            }

            if (PatrolPoints == null || PatrolPoints.Value == null || PatrolPoints.Value.Count == 0)
            {
                Debug.LogWarning("PatrolPoints list is empty — Patrol will fail.");
                return;
            }

            // Start at first waypoint
            SetDestinationToCurrentPoint();
        }

        public override TaskStatus OnUpdate()
        {
            if (navMeshAgent == null || PatrolPoints.Value.Count == 0)
                return TaskStatus.Failure;

            if (waiting)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= WaitTime)
                {
                    waiting = false;
                    NextPatrolPoint();
                    SetDestinationToCurrentPoint();
                }
                return TaskStatus.Running;
            }

            // Check if arrived at target
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= ArriveThreshold)
            {
                waiting = true;
                waitTimer = 0f;
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            waiting = false;
            waitTimer = 0f;
            agent.StopMovement();
        }

        private void SetDestinationToCurrentPoint()
        {
            if (currentIndex < 0 || currentIndex >= PatrolPoints.Value.Count)
                return;

            var point = PatrolPoints.Value[currentIndex];
            if (point != null)
                agent.MoveTo(point.transform.position);
        }

        private void NextPatrolPoint()
        {
            if (RandomPatrol.Value)
            {
                currentIndex = Random.Range(0, PatrolPoints.Value.Count);
            }
            else
            {
                currentIndex = (currentIndex + 1) % PatrolPoints.Value.Count;
            }
        }

        public override void OnDrawGizmos()
        {
            if (PatrolPoints.Value != null)
            {
                for (int i = 0; i < PatrolPoints.Value.Count; i++)
                {
                    if (i > 0)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(PatrolPoints.Value[i - 1].transform.position, PatrolPoints.Value[i].transform.position);
                    }
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(PatrolPoints.Value[i].transform.position, 0.5f);
                }

                if (PatrolPoints.Value.Count > 2)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(PatrolPoints.Value[PatrolPoints.Value.Count - 1].transform.position, PatrolPoints.Value[0].transform.position);
                }
            }
        }
    }
}
