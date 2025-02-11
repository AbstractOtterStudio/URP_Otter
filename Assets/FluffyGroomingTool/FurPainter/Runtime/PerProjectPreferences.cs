using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FluffyGroomingTool {
    public static class PerProjectPreferences {
        public static bool hasKey(string key) {
#if UNITY_EDITOR
            return EditorPrefs.HasKey(Application.dataPath.GetHashCode() + key);
#else
return false;
#endif
        }

        public static void setInt(string key, int value) {
#if UNITY_EDITOR
            EditorPrefs.SetInt(Application.dataPath.GetHashCode() + key, value);
#endif
        }

        public static int getInt(string key, int defaultValue) {
#if UNITY_EDITOR
            return EditorPrefs.GetInt(Application.dataPath.GetHashCode() + key, defaultValue);
#else
return 0;
#endif
        }

        public static void deleteKey(string key) {
#if UNITY_EDITOR
            EditorPrefs.DeleteKey(Application.dataPath.GetHashCode() + key);
#endif
        }
    }
}