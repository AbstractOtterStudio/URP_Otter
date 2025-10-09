using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;
using System;

namespace BehaviorTreeNodes
{
    public class PlayerWithinDistance : Conditional
    {
        // The tag of the targets
        public LayerMask TargetLayerMask;
        public SharedTargetDesc OutTarget;

        [Range(.2f, 10f)]
        public float DetectionRange;

        NPCAgent agent;

        public override void OnAwake()
        {
            if (!Owner.TryGetComponent(out agent))
            {
                Debug.LogError("NPCAgent is required for player within distance Conditional");
                return;
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (agent == null)
            {
                return TaskStatus.Failure;
            }

            Collider[] colliders = Physics.OverlapSphere(Owner.transform.position, DetectionRange, TargetLayerMask);
            if (colliders.Length > 0)
            {
                OutTarget.Value = new TargetDesc
                {
                    Target = colliders[0].transform,
                    RelativeVel = colliders[0].attachedRigidbody.velocity - agent.Rigidbody.velocity
                };
                return TaskStatus.Success;
            }
            return TaskStatus.Failure;
        }

        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.red.A(0.2f);
            Gizmos.DrawSphere(Owner.transform.position, DetectionRange);
        }
    }
}