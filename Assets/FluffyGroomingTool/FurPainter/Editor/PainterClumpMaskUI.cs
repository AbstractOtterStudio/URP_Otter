using System;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class PainterClumpMaskUI {
        private GUIStyle buttonStyle;
        private GUIStyle helpStyle;

        public void drawClumpMaskUI(PainterBrushTypeUI brushMenu, FurCreator furCreator) {
            int type = furCreator.getPainterProperties().type;
            if (type == (int) PaintType.CLUMPING_MASK) {
                if (buttonStyle == null) {
                    buttonStyle = PainterLayersUI.createButtonStyle("bg_button", "bg_button_hover");
                    buttonStyle.alignment = TextAnchor.MiddleCenter;
                }

                GUILayout.BeginVertical(brushMenu.BrushDetailsStyle);


                if (!furCreator.groomContainer.getActiveLayer().hasClumpLayer()) {
                    if (helpStyle == null) helpStyle = BrushPropertiesUI.getHelpTextStyle();
                    EditorGUILayout.LabelField("The Clump Mask tool requires that the active layer has a Clump Modifier.\r\n" +
                                               "Would you like to add one to the active layer?", helpStyle);
                    EditorGUILayout.Space(7);
                    if (GUILayout.Button(
                        new GUIContent("Add Clump Modifier", "Adds a clump modifier. This feature works best when using strand based rendering."),
                        buttonStyle)) {
                        furCreator.addClump();
                    }
                }
                else {
                    furCreator.getPainterProperties().clumpMaskIntensity =
                        furCreator.groomContainer.undoSlider("Clump Intensity:",
                            furCreator.getPainterProperties().clumpMaskIntensity);


                    EditorGUILayout.Space(7);

                    if (GUILayout.Button(new GUIContent("Flood", "This will set the intensity to all strands in the layer."), buttonStyle)) {
                        furCreator.flood();
                    }
                }

                GUILayout.EndVertical();
            }
        }
    }
}