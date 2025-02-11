using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class ExportFurContainerUI {
        private bool isExpanded = true;

        public void drawExportFurContainerUI(FurCreator furCreator, GUIStyle brushPropertiesPanelStyle, FluffyWindow window) {
            isExpanded = EditorGUILayout.Foldout(isExpanded, "Export Groom to file(recommended)");
            if (isExpanded) {
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                GUILayout.BeginVertical(brushPropertiesPanelStyle); 
                if (GUILayout.Button(new GUIContent("Export",
                    "Optimizes the fur and saves it to a FurContainer asset that can be reused. This option is recommended for fur rendering and supports" +
                    " wind, physics, colliders etc."), window.painterLayersUI.buttonStyle)) {
                    furCreator.finalizeGroomAndSaveFur();
                }

                GUILayout.EndVertical();
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
            }
        }
    }
}