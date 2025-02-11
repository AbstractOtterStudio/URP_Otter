namespace FluffyGroomingTool {
    public static class MergeSortShaderUtil { 
        private static readonly int MAX_DIM_GROUPS = 1024;
        private static readonly int MAX_DIM_THREADS = (CSThread.GROUP_SIZE * MAX_DIM_GROUPS);

        public static void calcWorkSize(int length, out int x, out int y, out int z) {
            if (length <= MAX_DIM_THREADS) {
                x = (length - 1) / CSThread.GROUP_SIZE  + 1;
                y = z = 1;
            }
            else {
                x = MAX_DIM_GROUPS;
                y = (length - 1) / MAX_DIM_THREADS + 1;
                z = 1;
            }
        }
    }
}