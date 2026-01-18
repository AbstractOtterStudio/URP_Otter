using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;
using UnityEngine;
using UnityEngine.AI;
using TooltipAttribute = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;

namespace BehaviorTreeNodes
{
    [TaskCategory("NPCAgent")]
    [TaskDescription("Makes the agent chase a target until the agent is within a certain distance of the target")]
    public class Chase : Action
    {
        public SharedTargetDesc InTargetDesc;
        public float TargetDistance = 1.0f;
        public float SpeedMultiplier = 2.0f;

        NPCAgent agent;
        NavMeshAgent navMeshAgent;

        float originalStoppingDistance;
        float originalSpeed;
        public override void OnAwake()
        {
            if (!Owner.TryGetComponent(out agent))
            {
                Debug.LogError("NPCAgent is required for chase Action");
                return;
            }
            navMeshAgent = agent.NavMeshAgent;
            if (navMeshAgent == null)
            {
                Debug.LogError("NavMeshAgent is required for chase Action");
                return;
            }
        }

        public override void OnStart()
        {
            if (!navMeshAgent)
                return;

            originalStoppingDistance = navMeshAgent.stoppingDistance;
            navMeshAgent.stoppingDistance = TargetDistance;

            originalSpeed = navMeshAgent.speed;
            navMeshAgent.speed = originalSpeed * SpeedMultiplier;
        }

        public override TaskStatus OnUpdate()
        {
            if (navMeshAgent == null)
            {
                return TaskStatus.Failure;
            }

            if (InTargetDesc == null || InTargetDesc.Value == null || InTargetDesc.Value.Target == null)
            {
                return TaskStatus.Failure;
            }

            agent.MoveTo(InTargetDesc.Value.Target.position);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            if (!navMeshAgent)
                return;

            navMeshAgent.stoppingDistance = originalStoppingDistance;
            agent.StopMovement();
        }
    }
}