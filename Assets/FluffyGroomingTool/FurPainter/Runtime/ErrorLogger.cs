#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FluffyGroomingTool {
    public static class ErrorLogger {
        public static void logMeshDistortionUponBuilds() {
#if UNITY_EDITOR
            if (BuildPipeline.isBuildingPlayer) {
                Debug.Log(
                    "Fluffy: If the groom looks distorted after building, please checkout the troubleshooting section in the documentation: " +
                    "https://danielzeller427.gitbook.io/fluffy-grooming-tool/"
                );
            }
#endif
        }
#if UNITY_EDITOR
        private static bool hasNoSkinningBeenShown;
#endif
        public static void checkGpuSkinning(MeshBaker meshBaker, FurRenderer fr) {
#if UNITY_EDITOR
            if (meshBaker.isSkinnedMesh() && !PlayerSettings.gpuSkinning && !hasNoSkinningBeenShown) {
                hasNoSkinningBeenShown = true;
                if (EditorUtility.DisplayDialog("GPU skinning",
                        "Fluffy requires GPU skinning to be enabled in order to work with SkinnedMeshRenderers, would" +
                        " you like Fluffy to enable this in the Player Settings?", "Yes", "No")) {
                    PlayerSettings.gpuSkinning = true;
                } else {
                    fr.enabled = false;
                    Debug.Log("Fluffy disabled the FurRenderer because GPU skinning is disabled.");
                }
            }
#endif
        }

        public static void logNoColliders() { Debug.Log("Please assign all collider slots. Collision detection disabled."); }

        public static void logRemoveFurCreator() {
            Debug.LogWarning("Fluffy: Please remove the FurCreator scripts in final builds to avoid allocating extra memory.");
        }

        public static void logNoCurvesFound() { Debug.Log("Fluffy error: The Alembic object dit not contain any curves."); }

        public static void logNoCamera() {
            if (Application.isPlaying) {
                Debug.LogError("Fluffy error: Could not find a Main Camera in your scene. Please assign one in FurRenderer->Lod Camera, " +
                               "or add the MainCamera tag to you camera.");
            }
        }
    }
}