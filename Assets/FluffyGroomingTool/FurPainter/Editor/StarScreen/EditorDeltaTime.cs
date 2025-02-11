using UnityEditor;

namespace FluffyGroomingTool {
    public class EditorDeltaTime {
        
        public float deltaTime;
        private float lastTimeSinceStartup;
        
        public void Update() {
            SetEditorDeltaTime();
        }

        private void SetEditorDeltaTime() {
            if (lastTimeSinceStartup == 0f) {
                lastTimeSinceStartup = (float) EditorApplication.timeSinceStartup;
            }

            deltaTime = (float) EditorApplication.timeSinceStartup - lastTimeSinceStartup;
            lastTimeSinceStartup = (float) EditorApplication.timeSinceStartup;
        }
    }
}