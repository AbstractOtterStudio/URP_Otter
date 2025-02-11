using System;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class PainterMagnitudeUI {
        private GUIStyle buttonStyle;
        private GUIStyle helpStyle;

        public void drawMagnitudeUI(PainterBrushTypeUI brushMenu, FurCreator target) {
            int type = target.getPainterProperties().type;
            if (type == (int) PaintType.HEIGHT || type == (int) PaintType.RAISE || type == (int) PaintType.WIND_MAX_DISTANCE ||
                type == (int) PaintType.WIDTH) {
                if (buttonStyle == null) {
                    buttonStyle = PainterLayersUI.createButtonStyle("bg_button", "bg_button_hover");
                    buttonStyle.alignment = TextAnchor.MiddleCenter;
                }

                GUILayout.BeginVertical(brushMenu.BrushDetailsStyle);


                target.getPainterProperties().painterMagnitudeIntensity =
                    target.groomContainer.undoSlider("Magnitude:", target.getPainterProperties().painterMagnitudeIntensity);


                EditorGUILayout.Space(7);

                if (GUILayout.Button(new GUIContent("Flood", "This will set the magnitude to all strands in the layer."), buttonStyle)) {
                    target.flood();
                }

                if (type == (int) PaintType.WIND_MAX_DISTANCE) {
                    EditorGUILayout.Space(7);
                    // GUILayout.BeginVertical(brushMenu.BrushDetailsStyle);
                    if (helpStyle == null) helpStyle = BrushPropertiesUI.getHelpTextStyle();
                    EditorGUILayout.LabelField("Green: Full motion\r\nRed: No motion", helpStyle);
                    // GUILayout.EndVertical();
                }

                GUILayout.EndVertical();
            }
        }
    }
}