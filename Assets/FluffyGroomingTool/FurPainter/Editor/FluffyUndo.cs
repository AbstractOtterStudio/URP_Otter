using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FluffyGroomingTool {
    public static class FluffyUndo {
        public static void safelyUndo(params Object[] objects) {
            try {
                foreach (var obj in objects) {
                    Undo.RegisterCompleteObjectUndo(obj, "Fluffy");
                }
            }
            catch (Exception e) {
                Debug.LogWarning("An error occured, when adding a Fluffy Undo step.");
                Debug.LogException(e);
            }
        }

        public static float undoSlider(this FurCreator target, String label, float value, float min = 0, float max = 1f) {
            EditorGUI.BeginChangeCheck();
            var returnValue = EditorGUILayout.Slider(label, value, min, max);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(target, "Fluffy");
            }

            return returnValue;
        }

        public static float undoSlider(this FurCreator target, GUIContent label, float value, float min = 0, float max = 1f) {
            EditorGUI.BeginChangeCheck();
            var returnValue = EditorGUILayout.Slider(label, value, min, max);
            if (EditorGUI.EndChangeCheck()) Undo.RecordObject(target, "Fluffy");
            return returnValue;
        }

        public static float undoSlider(this ScriptableObject target, GUIContent label, float value, float min = 0, float max = 1f) {
            EditorGUI.BeginChangeCheck();
            var returnValue = EditorGUILayout.Slider(label, value, min, max);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RegisterCompleteObjectUndo(target, "Fluffy");
            }

            return returnValue;
        }

        public static float undoSlider(this GroomContainer container, String label, float value, float min = 0, float max = 1f) {
            EditorGUI.BeginChangeCheck();
            var returnValue = EditorGUILayout.Slider(label, value, min, max);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RegisterCompleteObjectUndo(container.PainterProperties, "Fluffy");
            }

            return returnValue;
        }

        public static bool toggle(this FurCreator target, GUIContent label, bool value) {
            EditorGUI.BeginChangeCheck();
            var returnValue = EditorGUILayout.Toggle(label, value);
            if (EditorGUI.EndChangeCheck()) Undo.RegisterCompleteObjectUndo(target.groomContainer.PainterProperties, "Fluffy");
            return returnValue;
        }
    }
}