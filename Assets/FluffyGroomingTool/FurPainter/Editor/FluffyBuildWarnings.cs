#if UNITY_EDITOR
using FluffyGroomingTool;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

class FluffyBuildWarnings : IPreprocessBuildWithReport {
    public int callbackOrder {
        get { return 0; }
    }

    private static readonly string OPTIMIZE_MESH_WARNING = "OPTIMIZE_MESH_WARNING";
    
    public void OnPreprocessBuild(BuildReport report) {
        if (PerProjectPreferences.getInt(OPTIMIZE_MESH_WARNING, -1) == -1 && PlayerSettings.stripUnusedMeshComponents) {
            if (EditorUtility.DisplayDialog("Optimize Mesh Data",
                "This message will only be displayed once :) Optimize Mesh Data is enabled in the Player Settings, " +
                "this will likely make the Fluffy fur look distorted. Would you Like Fluffy for disable the setting?", "Yes", "No")) {
                PlayerSettings.stripUnusedMeshComponents = false;
            }

            PerProjectPreferences.setInt(OPTIMIZE_MESH_WARNING, 1);
        }
    }
}
#endif