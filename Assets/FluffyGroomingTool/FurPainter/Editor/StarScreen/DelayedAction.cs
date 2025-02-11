using UnityEditor;
using UnityEngine.Events;

/**
 * Kind of useless code here, but since there isn't any good support for delayed tasks without external packages we
 * just use this simple class.
 */
namespace FluffyGroomingTool {
    public class DelayedAction {
        
        private float startTime;
        public UnityAction action;
        public float delay;

        public DelayedAction(float delay, UnityAction action) {
            startTime = (float) EditorApplication.timeSinceStartup;
            this.delay = delay;
            this.action = action;
        }

        public bool isComplete() {
            if (startTime + delay < EditorApplication.timeSinceStartup) {
                action.Invoke();
                return true;
            }

            return false;
        }
    }
}