
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTreeNodes
{
    public class Wander : Action
    {
        public Vector2 WanderDistRange;
        public Vector2 WanderTimerRange;


        NPCAgent agent;
        NavMeshAgent navMeshAgent;
        float timer = 0.0f;
        float wanderTimer;

        public override void OnStart()
        {
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

            wanderTimer = Random.Range(WanderTimerRange.x, WanderTimerRange.y);
        }

        public override TaskStatus OnUpdate()
        {
            if (navMeshAgent == null)
            {
                return TaskStatus.Failure;
            }

            timer += Time.deltaTime;

            // If time to choose a new destination
            if (timer >= wanderTimer)
            {
                wanderTimer = Random.Range(WanderTimerRange.x, WanderTimerRange.y);
                var wanderDist = Random.Range(WanderDistRange.x, WanderDistRange.y);

                Vector3 randDirection = Random.insideUnitSphere * wanderDist;
                randDirection += Owner.transform.position;

                NavMeshHit navHit;
                NavMesh.SamplePosition(randDirection, out navHit, wanderDist, -1);
                navMeshAgent.SetDestination(navHit.position);

                timer = 0;
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            timer = 0;
            navMeshAgent.ResetPath();
        }
    }
}