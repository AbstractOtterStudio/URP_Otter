using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    [CustomEditor(typeof(FurCreator))]
    public class FurCreatorObjectEditor : Editor {
        public override void OnInspectorGUI() {
            if (GUILayout.Button("Launch Fluffy Window"))
                FluffyWindow.launchFurPainter();

            EditorGUILayout.Space();
        }
    }
}