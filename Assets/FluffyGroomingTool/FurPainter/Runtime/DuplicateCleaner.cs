using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace FluffyGroomingTool {
    public static class DuplicateCleaner {
#if UNITY_EDITOR
        [MenuItem("Tools/Fluffy Grooming Tool/Cleanup Duplicates")]
        private static void Cleanup() {
            if (doDuplicateCheck() == 0) {
                Debug.Log("Scene is clean and lean. All good to go.");
            }

            Debug.Log(
                "Check the following link for info on this issue: https://danielzeller427.gitbook.io/fluffy-grooming-tool/troubleshooting/cleanup-duplicates");
        }
#endif
        /**
         * There was a previous bug in Fluffy where the hidden GameObject of Fluffy was not cleaned from the project. These objects did not
         * do anything but could contribute to increasing the scene size.
         * This automatically cleans those objects. Only runs once per scene.
         */
        public static void checkDuplicates() {
#if UNITY_EDITOR
            var scenePath = SceneManager.GetActiveScene().path;
            if (!PerProjectPreferences.hasKey(scenePath)) {
                PerProjectPreferences.setInt(scenePath, 1);
                doDuplicateCheck();
            }
#endif
        }

        private static int doDuplicateCheck() {
#if UNITY_EDITOR
#if UNITY_2023_1_OR_NEWER
            var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var furRenderers = Object.FindObjectsByType<FurRenderer>(FindObjectsSortMode.None);
            var hairRenderers = Object.FindObjectsByType<HairRenderer>(FindObjectsSortMode.None);
#else
            var allRenderers = Object.FindObjectsOfType<MeshRenderer>();
            var furRenderers = Object.FindObjectsOfType<FurRenderer>();
            var hairRenderers = Object.FindObjectsOfType<HairRenderer>();
#endif
            var leakedObjects = new List<GameObject>();

            foreach (var go in allRenderers) {
                if (go.name.Contains("FluffyRenderer")) {
                    var isLegitObject = false;
                    foreach (var furRenderer in furRenderers) {
                        if (furRenderer.renderersController.hairMeshRendererObject == go.gameObject ||
                            furRenderer.renderersController.motionVectorRendererObject == go.gameObject) {
                            isLegitObject = true;
                        }
                    }

                    foreach (var hairRenderer in hairRenderers) {
                        if (hairRenderer.fluffyRenderersController.hairMeshRendererObject == go.gameObject ||
                            hairRenderer.fluffyRenderersController.motionVectorRendererObject == go.gameObject) {
                            isLegitObject = true;
                        }
                    }

                    if (!isLegitObject) {
                        leakedObjects.Add(go.gameObject);
                    }
                }
            }

            if (leakedObjects.Count > 0) {
                Debug.Log("Cleaned up " + leakedObjects.Count + " objects.");
                foreach (var gameObject in leakedObjects) {
                    Object.DestroyImmediate(gameObject);
                }

                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            return leakedObjects.Count;
#else
            return 0;
#endif
        }
    }
}