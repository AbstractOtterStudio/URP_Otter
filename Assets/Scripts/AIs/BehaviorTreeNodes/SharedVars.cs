using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace BehaviorTreeNodes
{
    [Serializable]
    public class TargetDesc
    {
        public Transform Target;
        public Vector3 RelativeVel;
    }

    [Serializable]
    public class SharedTargetDesc : SharedVariable<TargetDesc>
    {
        public static implicit operator SharedTargetDesc(TargetDesc value) { return new SharedTargetDesc { Value = value }; }
    }
}