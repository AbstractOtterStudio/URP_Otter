#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pinwheel.Griffin.Rendering
{
    public class GBatchTRS
    {
#if GRIFFIN_URP
        public const int MAX_LENGTH = 454;
#else
        public const int MAX_LENGTH = 511;
#endif
        public Matrix4x4[] matrices = new Matrix4x4[MAX_LENGTH];
        public int length = 0;
    }
}
#endif