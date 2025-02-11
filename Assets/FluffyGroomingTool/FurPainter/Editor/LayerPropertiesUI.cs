using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class LayerPropertiesUI {
        private static readonly float MAX_SCALE_WIDTH = 0.5f;
        private static readonly float MAX_SCALE_HEIGHT = 10;
        private static readonly float MIN_STRANDS_DISTANCE = 0.0003f;
        private static readonly float MAX_STRANDS_DISTANCE = 0.015f;
        private static readonly int EDITOR_TIME = Shader.PropertyToID("_EditorTime");
        private bool isExpanded = true;
        private bool isProgressExpanded;

        private Material spinnerMat;
        private Texture2D emptyTexture;
        private static readonly int Expand = Shader.PropertyToID("expand");

        private bool isRunningHideAnimation;

        public void drawFurPropertiesUI(FurCreator furCreator, GUIStyle brushPropertiesPanelStyle) {
            EditorGUILayout.Space(7f);
            isExpanded = EditorGUILayout.Foldout(isExpanded, "Fur Properties");

            if (isExpanded) {
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                GUILayout.BeginVertical(brushPropertiesPanelStyle);
                drawProgressBar(furCreator);
                drawDistanceBetweenStrandsSlider(furCreator);
                drawWidthAndHeightSliders(furCreator);
                drawRotateAngRandomHeightSliders(furCreator);
                GUILayout.EndVertical();
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
            }
        }

        private void drawDistanceBetweenStrandsSlider(FurCreator furCreator) {
            var newSliderValue = furCreator.groomContainer.undoSlider(
                new GUIContent(getHairDistanceText(),
                    "Sets the minimum distance between each strand. Adjusting this slider will recalculate all the strands in the layer and may result in loss of precision in your groom."),
                furCreator.getActiveLayer().distanceBetweenStrands,
                MIN_STRANDS_DISTANCE,
                MAX_STRANDS_DISTANCE
            );
            if (hasSliderChanged(furCreator, newSliderValue) || furCreator.getActiveLayer().isDistanceBetweenStrandsUndoPerformed()) {
                furCreator.getActiveLayer().distanceBetweenStrands = newSliderValue;
                furCreator.getActiveLayer().TempDistanceBetweenStrands = newSliderValue;
                furCreator.addStrands();
            }
        }

        private static bool hasSliderChanged(FurCreator furCreator, float newSliderValue) {
            return Math.Abs(newSliderValue - furCreator.getActiveLayer().distanceBetweenStrands) > GroomLayer.FLOAT_COMPARISON_TOLERANCE;
        }

        private static void drawRotateAngRandomHeightSliders(FurCreator furCreator) {
            EditorGUI.BeginChangeCheck();
            furCreator.getActiveLayer().randomRotation = furCreator.groomContainer.undoSlider(
                new GUIContent("Randomize rotation:",
                    "Randomize the root rotation of each strand. Note that this will override the direction set using the Root Direction brush."),
                furCreator.getActiveLayer().randomRotation
            );

            furCreator.getActiveLayer().randomHeight = furCreator.groomContainer.undoSlider(
                new GUIContent("Randomize height:", "Randomize the height of each strand."),
                furCreator.getActiveLayer().randomHeight
            );

            if (EditorGUI.EndChangeCheck()) {
                furCreator.copyBuffersToNativeDelayed();
            }
        }

        private static void drawWidthAndHeightSliders(FurCreator furCreator) {
            float minWidth = furCreator.getActiveLayer().minWidth;
            float maxWidth = furCreator.getActiveLayer().maxWidth;
            float minHeight = furCreator.getActiveLayer().minHeight;
            float maxHeight = furCreator.getActiveLayer().maxHeight;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(
                new GUIContent(
                    "Width range:",
                    "The global minimum and maximum width of the strands. Painting the with to a magnitude of 0 will set the strands width to the minimum of this slider."
                ),
                ref minWidth,
                ref maxWidth,
                0.002f,
                MAX_SCALE_WIDTH
            );

            EditorGUILayout.MinMaxSlider(
                new GUIContent(
                    "Height range:",
                    "The global minimum and maximum height of the strands. Painting the height to a magnitude of 0 will set the strands height to the minimum of this slider."
                ),
                ref minHeight,
                ref maxHeight,
                0,
                MAX_SCALE_HEIGHT
            );
            if (EditorGUI.EndChangeCheck()) {
                FluffyUndo.safelyUndo(furCreator.groomContainer);
                furCreator.getActiveLayer().minWidth = minWidth;
                furCreator.getActiveLayer().maxWidth = maxWidth;
                furCreator.getActiveLayer().minHeight = minHeight;
                furCreator.getActiveLayer().maxHeight = maxHeight;
                furCreator.copyBuffersToNativeDelayed();
            }
        }

        private string getHairDistanceText() {
            if (isProgressExpanded) {
                return "Hang on, calculating..";
            }

            return "Hair strands spacing:";
        }


        private void drawProgressBar(FurCreator furCreator) {
            if (ReferenceEquals(spinnerMat, null) || spinnerMat == null) {
                spinnerMat = new Material(Shader.Find("Hidden/Fur-spinner"));
            }
            animateProgress(furCreator);
            GUILayout.Label(""); //Need an empty label just to calculate the height used for  GUILayoutUtility.GetLastRect()
            if (isProgressExpanded) {
                var lastRect = GUILayoutUtility.GetLastRect();
                var position = new Rect(lastRect.x + 152, lastRect.y, lastRect.width - 208, lastRect.height);
                drawSpinner(position);
            }

            EditorGUILayout.Space(-22);
        }

        public void drawSpinner(Rect position) {
            emptyTexture = emptyTexture == null ? PainterBrushTypeUI.createColorBackground(Color.black) : emptyTexture;
            spinnerMat.SetFloat(EDITOR_TIME, (float) EditorApplication.timeSinceStartup);
            EditorGUI.DrawPreviewTexture(
                position,
                emptyTexture,
                spinnerMat
            );
        }

        private void animateProgress(FurCreator furCreator) {
            if (!isProgressExpanded && furCreator.IsFurStrandsProgressVisible) {
                isProgressExpanded = true;
                isRunningHideAnimation = false;
                furCreator.StartCoroutine(animateProgressExpand(0.15f, false));
            }
            else if (!isRunningHideAnimation && isProgressExpanded && !furCreator.IsFurStrandsProgressVisible) {
                isRunningHideAnimation = true;
                furCreator.StartCoroutine(animateProgressExpand(0.1f, true));
            }
        }

        private IEnumerator animateProgressExpand(float duration, bool reverse) {
            var step = 1f / 60f;
            var elapsedTime = 0f;
            while (elapsedTime < duration) {
                elapsedTime += step;
                if (reverse) {
                    var value = 1f - elapsedTime / duration;
                    spinnerMat.SetFloat(Expand, value);
                }
                else {
                    spinnerMat.SetFloat(Expand, elapsedTime / duration);
                }

                yield return new WaitForSeconds(step);
            }

            if (reverse) {
                isProgressExpanded = false;
                isRunningHideAnimation = false;
            }
        }
    }
}