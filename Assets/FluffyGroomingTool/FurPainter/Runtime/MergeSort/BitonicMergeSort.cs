using UnityEngine;

namespace FluffyGroomingTool {
    public class BitonicMergeSort {
        private static readonly string KERNEL_SORT = "BitonicSort";
        private static readonly string KERNEL_INIT = "InitKeys";

        private static readonly string PROP_BLOCK = "block";
        private static readonly string PROP_DIM = "dim";
        private static readonly string PROP_COUNT = "count";

        private static readonly string BUF_KEYS = "Keys";
        private static readonly string BUF_VALUES = "Values";

        private readonly ComputeShader compute;
        private readonly int kernelSort, kernelInit;

        public BitonicMergeSort(ComputeShader compute) {
            this.compute = compute;
            kernelInit = compute.FindKernel(KERNEL_INIT);
            kernelSort = compute.FindKernel(KERNEL_SORT);
        }

        public void inizialize(ComputeBuffer keys) {
            int x, y, z;
            MergeSortShaderUtil.calcWorkSize(keys.count, out x, out y, out z);
            compute.SetInt(PROP_COUNT, keys.count);
            compute.SetBuffer(kernelInit, BUF_KEYS, keys);
            compute.Dispatch(kernelInit, x, y, z);
        }

        public void sort(ComputeBuffer keys, ComputeBuffer values) {
            var count = keys.count;
            var trianglesCount = values.count;
            int x, y, z;
            MergeSortShaderUtil.calcWorkSize(count, out x, out y, out z);

            compute.SetInt(PROP_COUNT, count);
            compute.SetInt("trianglesCount", trianglesCount);

            for (var dim = 2; dim <= count; dim <<= 1) {
                compute.SetInt(PROP_DIM, dim);
                for (var block = dim >> 1; block > 0; block >>= 1) {
                    compute.SetInt(PROP_BLOCK, block);
                    compute.SetBuffer(kernelSort, BUF_KEYS, keys);
                    compute.SetBuffer(kernelSort, BUF_VALUES, values);
                    compute.Dispatch(kernelSort, x, y, z);
                }
            }
        }
    }
}