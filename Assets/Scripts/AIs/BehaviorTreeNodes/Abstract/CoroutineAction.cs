using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;

namespace BehaviorTreeNodes
{
    public abstract class CoroutineAction : Action
    {
        IEnumerator updateCoroutine;

        public override void OnStart()
        {
            updateCoroutine = OnUpdateCoroutine();
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

        public override void OnEnd()
        {
            if (updateCoroutine != null)
            {
                updateCoroutine = null;
            }
        }

        public abstract IEnumerator OnUpdateCoroutine();
    }
}