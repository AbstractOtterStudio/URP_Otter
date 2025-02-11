using System;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class PainterTwistUI {
        private GUIStyle buttonStyle;

        private FluffyToolbar fluffyToolbar = new FluffyToolbar() {
            activatedColor = new Color32(55, 210, 232, 255),
            activatedColorHover = new Color32(55, 210, 232, 200)
        };

        public void drawTwistUI(PainterBrushTypeUI brushMenu, FurCreator furCreator) {
            if (furCreator.getPainterProperties().type == (int) PaintType.TWIST) {
                if (buttonStyle == null) {
                    buttonStyle = PainterLayersUI.createButtonStyle("bg_button", "bg_button_hover");
                    buttonStyle.alignment = TextAnchor.MiddleCenter;
                }

                GUILayout.BeginVertical(brushMenu.BrushDetailsStyle);
                var p = furCreator.getPainterProperties();
                var tab = p.isClumpTwistSelected ? 1 : 0;
                tab = fluffyToolbar.drawToolbar(tab, new[] {"Twist Hair Strands", "Twist clumps"}, furCreator);
                p.isClumpTwistSelected = tab == 1;
                EditorGUILayout.Space(width: 7);
                //Add spread slider
                p.twistAmount = furCreator.groomContainer.undoSlider("Twist Amount:", p.twistAmount);
                p.twistSpread = furCreator.groomContainer.undoSlider("Spread:", p.twistSpread);
                EditorGUILayout.Space(width: 7);
                addFloodButton(furCreator);

                GUILayout.EndVertical();
            }
        }

        private void addFloodButton(FurCreator furCreator) {
            if (GUILayout.Button(new GUIContent("Flood", "This will set the Twist Amount and Spread to all strands or clumps in the layer."),
                buttonStyle)) {
                furCreator.flood();
            }
        }
    }
}