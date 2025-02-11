using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class ExportMeshUI {
        private bool isExpanded = true;

        public void drawExportMeshUI(FurCreator furCreator, FluffyWindow window) {
            var rendererType = "Mesh";
            if (furCreator.GetComponent<SkinnedMeshRenderer>() != null) rendererType = "SkinnedMesh";
            var tooltip = getTooltip(rendererType);
            isExpanded = EditorGUILayout.Foldout(isExpanded, new GUIContent("Export To " + rendererType, tooltip));
            if (isExpanded) {
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                GUILayout.BeginVertical(window.brushPropertiesUI.PanelStyle);
                if (GUILayout.Button(new GUIContent("Export", tooltip), window.painterLayersUI.buttonStyle)) {
                    furCreator.createMesh();
                }

                GUILayout.EndVertical();
            }

            EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }

        private static string getTooltip(string rendererType) {
            return "Export to " + rendererType + "." +
                   " This option will render using a normal " + rendererType +
                   "Renderer and will not have wind physics and collider support. Useful for creating grass patches or bushes.";
        }
    }
}