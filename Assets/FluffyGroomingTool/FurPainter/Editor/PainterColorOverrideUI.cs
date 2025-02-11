using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class PainterColorOverrideUI {
        public void drawColorOverrideUI(PainterBrushTypeUI brushMenu, FurCreator furCreator) {
            if (furCreator.painterProperties.type == (int) PaintType.COLOR_OVERRIDE) {
                GUILayout.BeginVertical(brushMenu.BrushDetailsStyle);
                furCreator.painterProperties.overrideIntensity = furCreator.groomContainer.PainterProperties.undoSlider(
                    new GUIContent("Override Amount:", "How much to override the material color."),
                    furCreator.painterProperties.overrideIntensity
                );
                EditorGUI.BeginChangeCheck();
                var painterPropertiesOverrideColor = EditorGUILayout.ColorField(
                    new GUIContent("Override Color", "Override the material color to this color."),
                    furCreator.painterProperties.overrideColor
                );
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RegisterCompleteObjectUndo(furCreator.groomContainer.PainterProperties, "FurCreator");
                    furCreator.painterProperties.overrideColor = painterPropertiesOverrideColor;
                }

                GUILayout.EndVertical();
            }
        }
    }
}