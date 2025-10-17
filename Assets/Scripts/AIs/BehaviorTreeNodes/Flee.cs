using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TooltipAttribute = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;

namespace BehaviorTreeNodes
{
    [TaskCategory("NPCAgent")]
    [TaskDescription("Makes the agent flee from a target until a safe distance is reached")]
    public class Flee : Action
    {
        public SharedTargetDesc InTargetDesc;

        [Tooltip("The angle step for searching the flee point")]
        public float SearchAngleStep = 10.0f;
        public float SafeDist = 5.0f;
        public float SearchAngle = 45;
        public float SpeedMultiplier = 2.0f;

        NPCAgent agent;
        NavMeshAgent navMeshAgent;
        float originalSpeed;
        float originalStoppingDist;

        IEnumerator updateCoroutine;

        public override void OnAwake()
        {
            if (!Owner.TryGetComponent(out agent))
            {
                Debug.LogError("NPCAgent is required for flee Action");
                return;
            }
            navMeshAgent = agent.NavMeshAgent;
            if (navMeshAgent == null)
            {
                Debug.LogError("Navmesh Agent is required for flee Action");
                return;
            }
        }
        public override void OnStart()
        {
            if (!navMeshAgent)
                return;

            updateCoroutine = OnUpdateCoroutine();
            originalSpeed = navMeshAgent.speed;
            originalStoppingDist = navMeshAgent.stoppingDistance;
            navMeshAgent.speed = originalSpeed * SpeedMultiplier;
            navMeshAgent.stoppingDistance = originalStoppingDist * 0.2f;
            navMeshAgent.ResetPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (updateCoroutine == null || !updateCoroutine.MoveNext())
                return TaskStatus.Success;

            // If the coroutine yielded a TaskStatus, use it
            if (updateCoroutine.Current is TaskStatus status)
                return status;

            return TaskStatus.Running;
        }

        private IEnumerator OnUpdateCoroutine()
        {
            if (InTargetDesc == null || InTargetDesc.Value == null || InTargetDesc.Value.Target == null)
            {
                Debug.LogError("Target is required for flee Action");
                yield return TaskStatus.Failure;
            }

            bool IsSafe(out Vector3 toTarget)
            {
                Transform target = InTargetDesc.Value.Target;

                toTarget = agent.transform.position - target.position;
                float dist = toTarget.magnitude;
                return dist >= SafeDist;
            }

            Vector3 toTarget;
            while (!IsSafe(out toTarget))
            {
                // Try to find a new flee destination if the agent doesn't have one or we're slowing down
                if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= Mathf.Max(navMeshAgent.stoppingDistance * 2f, 0.5f))
                {
                    Vector3 bestFleePoint;
                    if (!FindFleePoint(toTarget, out bestFleePoint))
                    {
                        navMeshAgent.ResetPath();

                        // Randomly pick a point if we are cornered
                        var navAgent = agent.NavMeshAgent;
                        Vector3 randDirection = Random.insideUnitSphere * SafeDist;
                        randDirection += Owner.transform.position;

                        NavMeshHit navHit;
                        NavMesh.SamplePosition(randDirection, out navHit, SafeDist, -1);
                        bestFleePoint = navHit.position;
                    }

                    // Move to flee point
                    navMeshAgent.SetDestination(bestFleePoint);
                }

                yield return null;
            }

            yield return TaskStatus.Success;
        }

        private bool FindFleePoint(Vector3 awayDir, out Vector3 fleePoint)
        {
            Vector3 start = agent.transform.position;
            awayDir.y = 0;
            awayDir.Normalize();

            float bestScore = float.MinValue;
            Vector3 bestPoint = Vector3.zero;

            IEnumerable<Vector3> PotentialDirections()
            {
                yield return awayDir;

                for (float angle = -SearchAngle; angle <= SearchAngle; angle += SearchAngleStep)
                {
                    if (Mathf.Approximately(angle, 0.0f)) continue;
                    yield return Quaternion.Euler(0, angle, 0) * awayDir;
                }
            }

            // Sweep in an arc around the away direction
            foreach (var dir in PotentialDirections())
            {
                Vector3 candidate = start + dir * SafeDist;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (navMeshAgent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        // Higher score for being farthest from target and closest to safe dist
                        float score = Vector3.Distance(hit.position, start);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestPoint = hit.position;
                        }
                    }
                }
            }

            fleePoint = bestPoint;
            return bestScore > float.MinValue;
        }

        public override void OnDrawGizmos()
        {
            if (agent == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(agent.transform.position, SafeDist);

            if (InTargetDesc != null && InTargetDesc.Value != null && InTargetDesc.Value.Target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(agent.transform.position, InTargetDesc.Value.Target.position);
            }
        }

        public override void OnEnd()
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.speed = originalSpeed;
                navMeshAgent.stoppingDistance = originalStoppingDist;
            }
            updateCoroutine = null;
        }

        public override void OnPause(bool paused)
        {
            if (!navMeshAgent)
            {
                return;
            }

            if (paused)
            {
                navMeshAgent.speed = originalSpeed;
                navMeshAgent.stoppingDistance = originalStoppingDist;
            }
            else
            {
                navMeshAgent.speed = originalSpeed * SpeedMultiplier;
                navMeshAgent.stoppingDistance = originalStoppingDist * 0.2f;
            }
        }
    }
}