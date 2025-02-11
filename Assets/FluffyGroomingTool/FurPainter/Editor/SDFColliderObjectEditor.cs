using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    [CustomEditor(typeof(SDFCollider))]
    public class SDFColliderObjectEditor : Editor {
        private SerializedProperty isStatic;
        private SerializedProperty colliderSkinWidth;
        private SerializedProperty sdfColliderResolution;
        private SerializedProperty useForwardCollision;
        private GUIStyle buttonStyle;
        private SDFCollider sdfCollider;

        public override void OnInspectorGUI() {
            GUILayout.BeginVertical();
            initialize();
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
                EditorGUILayout.LabelField("SDF Colliders are currently not supported on Android.");
                sdfCollider.verletSimulationSettings.isUnsupportedSDFPlatform = true;
            } else {
                sdfCollider.verletSimulationSettings.isUnsupportedSDFPlatform = false;
                EditorGUILayout.PropertyField(isStatic);
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(colliderSkinWidth);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(sdfColliderResolution);
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(useForwardCollision);

                if (EditorGUI.EndChangeCheck()) {
                    sdfCollider.recreateSDF();
                }

                EditorGUILayout.Space(5);
                if (GUILayout.Button(new GUIContent("Attach To Fur/Hair-Renderers",
                        "This will add the SDF Collider to all FurRenderers in the scene."))) {
                    attachToFurRenderers();
                }

                serializedObject.ApplyModifiedProperties();
            }

            GUILayout.EndVertical();
        }

        private void attachToFurRenderers() {
#if UNITY_2023_1_OR_NEWER
            var furRenderers = FindObjectsByType<FurRenderer>(FindObjectsSortMode.InstanceID);
            var hairRenderers = FindObjectsByType<HairRenderer>(FindObjectsSortMode.InstanceID);
#else
            var furRenderers = FindObjectsOfType<FurRenderer>();
            var hairRenderers = FindObjectsOfType<HairRenderer>();
#endif

            var colliderWasAddedToFurRenderer = false;
            foreach (var fr in furRenderers) {
                if (!fr.sdfColliders.Contains(sdfCollider)) {
                    colliderWasAddedToFurRenderer = true;
                    addToRenderer(fr);
                }
            }

            foreach (var hr in hairRenderers) {
                if (!hr.sdfColliders.Contains(sdfCollider)) {
                    colliderWasAddedToFurRenderer = true;
                    addToRenderer(hr);
                }
            }

            if (furRenderers.Length == 0) {
                Debug.LogWarning("Fluffy did not find any Fur/Hair-Renderers in this scene.");
            } else if (!colliderWasAddedToFurRenderer) {
                Debug.LogWarning("The collider has already been added to all Fur/Hair-Renderers in this scene.");
            }
        }

        private void addToRenderer(Object renderer) {
            var so = new SerializedObject(renderer);
            so.ApplyModifiedProperties();
            var serializedProperty = so.FindProperty("sdfColliders");
            var serializedPropertyArraySize = serializedProperty.arraySize;
            serializedProperty.InsertArrayElementAtIndex(serializedPropertyArraySize);
            serializedProperty.GetArrayElementAtIndex(serializedPropertyArraySize).objectReferenceValue = sdfCollider;
            so.ApplyModifiedProperties();
            Debug.Log("Collider was added to: " + renderer.name);
        }

        private void initialize() {
            if (isStatic == null) {
                sdfCollider ??= (SDFCollider)serializedObject.targetObject;
                isStatic = serializedObject.FindProperty("isStatic");
                colliderSkinWidth = serializedObject.FindProperty("verletSimulationSettings.colliderSkinWidth");
                sdfColliderResolution = serializedObject.FindProperty("verletSimulationSettings.sdfColliderResolution");
                useForwardCollision = serializedObject.FindProperty("verletSimulationSettings.useForwardCollision");
                buttonStyle = PainterLayersUI.createButtonStyle("bg_button", "bg_button_hover");
                buttonStyle.alignment = TextAnchor.MiddleCenter;
            }
        }
    }
}