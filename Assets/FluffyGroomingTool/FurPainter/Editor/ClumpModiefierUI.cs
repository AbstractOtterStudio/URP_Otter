using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class ClumpModifierUI {
        private bool isExpanded = true;
        private GUIStyle headerText;
        private GUIStyle panelStyle;

        private void createStyles() {
            if (panelStyle == null) {
                headerText = new GUIStyle(GUI.skin.label) {
                    fontStyle = FontStyle.Bold, fontSize = 12, normal = {textColor = new Color(1, 1, 1, 0.8f)}
                };
                panelStyle = new GUIStyle(GUI.skin.label) {
                    border = new RectOffset(20, 20, 20, 20),
                    padding = new RectOffset(15, 16, 10, 17),
                    normal = {background = Resources.Load<Texture2D>("bg_box")}
                };
                headerText.alignment = TextAnchor.MiddleLeft;
            }
        }


        public void drawClumpingUI(FurCreator furCreator, GUIStyle brushPropertiesPanelStyle, PainterLayersUI painterLayersUI) {
            isExpanded = EditorGUILayout.Foldout(isExpanded, "Clumps");
            if (isExpanded) {
                createStyles();
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                GUILayout.BeginVertical(brushPropertiesPanelStyle);
                for (int j = 0; j < furCreator.groomContainer.getActiveLayer().clumpModifiers.Length; j++) {
                    GUILayout.BeginVertical(panelStyle);
                    EditorGUILayout.LabelField("Clump Modifier " + (j + 1), headerText);

                    var rect = GUILayoutUtility.GetLastRect();
                    rect = new Rect(rect.width + 42, rect.y - 6, 29, 29);


                    if (GUI.Button(rect, new GUIContent("", "Delete the Clump Modifier"), painterLayersUI.trashButtonStyle)) {
                        furCreator.removeClumpModifier(j);
                    }

                    EditorGUILayout.Space(6);

                    var clumpGroomModifier = furCreator.groomContainer.getActiveLayer().clumpModifiers[j];
                    var clumpsSpacing = furCreator.groomContainer.undoSlider(
                        new GUIContent("Clump Spacing:", "Sets the approximate distance between each clump."),
                        clumpGroomModifier.clumpsSpacing,
                        0.01f,
                        0.15f
                    );

                    EditorGUI.BeginChangeCheck();
                    var curve = EditorGUILayout.CurveField(
                        new GUIContent("Attraction Curve", "Sets the attraction intensity of the clump from root to tip."),
                        clumpGroomModifier.attractionCurve,
                        painterLayersUI.colors[furCreator.groomContainer.activeLayerIndex % 5], new Rect(0, -1, 1, 3));
                    if (EditorGUI.EndChangeCheck()) {
                        //FluffyUndo.safelyUndo(furCreator.groomContainer, furCreator.FurRenderer.furContainer);
                    }

                    var clumpModifierLayers =
                        furCreator.FurRenderer.furContainer.layerStrandsList[furCreator.groomContainer.activeLayerIndex].clumpsModifiers;
                    if (j < clumpModifierLayers.Length) {
                        var clumpModifierLayer = clumpModifierLayers[j];
                        clumpGroomModifier.attractionCurve = curve;
                        clumpModifierLayer.attractionCurve = curve;
                        var isSameCurve = clumpGroomModifier.isSameCurve(curve.getCurveSum());
                        if (!isSameCurve) {
                            furCreator.FurRenderer.furContainer.updateClumpAttrationCurveBuffer();
                        }
                    }

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (clumpsSpacing != clumpGroomModifier.clumpsSpacing || clumpGroomModifier.isDistanceBetweenClumpsUndoPerformed()) {
                        clumpGroomModifier.clumpsSpacing = clumpsSpacing;
                        clumpGroomModifier.TempClumpSpacing = clumpsSpacing;
                        furCreator.addStrands();
                    }

                    GUILayout.EndVertical();
                }

                var normalTextColor = Color.white;
                if (!EditorGUIUtility.isProSkin) {
                    normalTextColor = Color.black;
                }

                normalTextColor.a = 0.9f;
                painterLayersUI.buttonStyle.normal.textColor = normalTextColor;
                painterLayersUI.buttonStyle.hover.textColor = normalTextColor;
                painterLayersUI.buttonStyle.alignment = TextAnchor.MiddleCenter;
                if (GUILayout.Button(
                    new GUIContent("Add Clump Modifier", "Adds a clump modifier. This feature works best when using strand based rendering."),
                    painterLayersUI.buttonStyle)) {
                    furCreator.addClump();
                }

                GUILayout.EndVertical();

                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
            }
        }
    }
}