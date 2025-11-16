
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace BehaviorTreeNodes
{
    [TaskCategory("NPCAgent")]
    [TaskDescription("Makes the agent wander around randomly")]
    public class Wander : CoroutineAction
    {
        public Vector2 WanderDistRange;
        public Vector2 WanderTimerRange;
        public SharedBool AllowUnderwater;
        NPCAgent agent;
        NavMeshAgent navMeshAgent;
        bool isUnderwaterInitial = false;

        public override void OnStart()
        {
            base.OnStart();

            if (!Owner.TryGetComponent(out agent))
            {
                Debug.LogError("NPCAgent is required for wander Action");
                return;
            }
            navMeshAgent = agent.NavMeshAgent;
            if (navMeshAgent == null)
            {
                Debug.LogError("Navmesh Agent is required for wander Action");
                return;
            }

            isUnderwaterInitial = agent.IsUnderwater;
        }

        public override IEnumerator OnUpdateCoroutine()
        {
            if (navMeshAgent == null)
            {
                yield return TaskStatus.Failure;
            }

            float wanderTimer = Random.Range(WanderTimerRange.x, WanderTimerRange.y);
            float timer = 0.0f;
            while (true)
            {
                timer += Time.deltaTime;
                if (timer >= wanderTimer)
                {
                    wanderTimer = Random.Range(WanderTimerRange.x, WanderTimerRange.y);
                    var wanderDist = Random.Range(WanderDistRange.x, WanderDistRange.y);

                    if (AllowUnderwater.Value)
                    {
                        bool shouldSwitch = Random.value < 0.5f;
                        if (shouldSwitch)
                        {
                            agent.SetUnderwater(!agent.IsUnderwater);

                            while (agent.IsSwitchingUnderwater)
                            {
                                yield return null;
                            }
                        }
                    }

                    Vector3 randDirection = Random.insideUnitSphere * wanderDist;
                    randDirection += Owner.transform.position;

                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(randDirection, out navHit, wanderDist, agent.GetLayerMask()))
                    {
                        navMeshAgent.SetDestination(navHit.position);
                    }
                    else
                    {
                        Debug.LogError("Failed to sample position for wander");
                    }

                    timer = 0;
                }

                yield return null;
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
            navMeshAgent.ResetPath();
        }
    }
}