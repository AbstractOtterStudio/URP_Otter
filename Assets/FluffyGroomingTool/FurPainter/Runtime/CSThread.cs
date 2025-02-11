namespace FluffyGroomingTool {
    public static class CSThread {
        public static readonly int GROUP_SIZE = 256;
        public static readonly int GROUP_SIZE_LARGE = 1024;
        
        
        private static readonly int GROUP_SIZE_EDITOR = 512;
        public static int toCsGroups(this int totalThreads) {
            return (totalThreads + (GROUP_SIZE - 1)) / GROUP_SIZE;
        }
        public static int toCsGroupsLarge(this int totalThreads) {
            return (totalThreads + (GROUP_SIZE_LARGE - 1)) / GROUP_SIZE_LARGE;
        }
        public static int toCsGroupsEditor(this int totalThreads) {
            return (totalThreads + (GROUP_SIZE_EDITOR - 1)) / GROUP_SIZE_EDITOR;
        }
    }
}