#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Pinwheel.Griffin.Rendering
{
    public struct GQuadTree
    {
        public static NativeArray<T> Create<T>(int depth, Allocator allocator, NativeArrayOptions option = NativeArrayOptions.ClearMemory) where T : struct
        {
            //Debug.Assert(depth > 0 && depth <= MAX_DEPTH);

            int count = GetTreeLengthAtDepthIndex(depth - 1);
            return new NativeArray<T>(count, allocator, option);
        }

        //[UnityEditor.MenuItem("Polaris 3/Log Dummy")]
        public static void Log()
        {

        }

        public const int TREE_LENGTH_0 = 1;
        public const int TREE_LENGTH_1 = 5;
        public const int TREE_LENGTH_2 = 21;
        public const int TREE_LENGTH_3 = 85;
        public const int TREE_LENGTH_4 = 341;
        public const int TREE_LENGTH_5 = 1365;
        public const int TREE_LENGTH_6 = 5461;
        public const int TREE_LENGTH_7 = 21845;

        private static readonly int[] TREE_LENGTH = { 1, 5, 21, 85, 341, 1365, 5461, 21845 };

        public const int MAX_DEPTH = 8;
        public const int MAX_ELEMENT_INDEX = TREE_LENGTH_7 - 1;

        public static int GetTreeLengthAtDepthIndex(int depthIndex)
        {
            //float count = 0;
            //for (int i = 0; i <= depthIndex; ++i)
            //{
            //    count += Mathf.Pow(4, i);
            //}
            //return (int)count;

            return TREE_LENGTH[depthIndex];
        }

        public static int GetSiblingCountAtDepthIndex(int depthIndex)
        {
            //Debug.Assert(depthIndex >= 0 && depthIndex < MAX_DEPTH);
            //switch (depthIndex)
            //{
            //    //4^depthIndex
            //    case 0: return 1;
            //    case 1: return 4;
            //    case 2: return 16;
            //    case 3: return 64;
            //    case 4: return 256;
            //    case 5: return 1024;
            //    case 6: return 4096;
            //    case 7: return 16384;
            //    default: return -1;
            //}
            if (depthIndex == 0) return 1;
            else if (depthIndex == 1) return 4;
            else if (depthIndex == 2) return 16;
            else if (depthIndex == 3) return 64;
            else if (depthIndex == 4) return 256;
            else if (depthIndex == 5) return 1024;
            else if (depthIndex == 6) return 4096;
            else if (depthIndex == 7) return 16384;
            else return -1;
        }

        public static int GetFirstIndexAtDepthIndex(int depthIndex)
        {
            //Debug.Assert(depthIndex >= 0 && depthIndex < MAX_DEPTH);
            //switch (depthIndex)
            //{
            //    case 0: return 0;
            //    default: return GetTreeLengthAtDepthIndex(depthIndex - 1);
            //}
            if (depthIndex == 0) return 0;
            else return GetTreeLengthAtDepthIndex(depthIndex - 1);
        }

        public static int GetDepthIndex(int nodeIndex)
        {
            //Debug.Assert(nodeIndex >= 0 && nodeIndex <= MAX_ELEMENT_INDEX);
            for (int i = 0; i < 8; ++i)
            {
                if (nodeIndex < GetTreeLengthAtDepthIndex(i))
                    return i;
            }
            return -1;
        }

        public static int GetFirstChildIndexOf(int nodeIndex)
        {
            //Debug.Assert(nodeIndex >= 0 && nodeIndex <= MAX_ELEMENT_INDEX);
            int depthIndex = GetDepthIndex(nodeIndex);
            int firstIndexAtDepthIndex = GetFirstIndexAtDepthIndex(depthIndex);
            int offset = nodeIndex - firstIndexAtDepthIndex;
            int firstIndexAtNextDepthIndex = GetFirstIndexAtDepthIndex(depthIndex + 1);

            return firstIndexAtNextDepthIndex + offset * 4;
        }
    }
}
#endif