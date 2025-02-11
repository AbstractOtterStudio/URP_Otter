using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class WelcomeTextUI {
        private static readonly float FONT_SIZE_IN_PERCENT_OF_WIDTH = 0.045f;
        private static readonly float LABEL_HEIGHT = 0.0675f;
        private static readonly float SCROLL_AMOUNT = 0.018f;
        private static readonly int ALPHA = Shader.PropertyToID("_Alpha");

        private Material colorFadeMaterial;
        private GUIStyle percentTextStyle;
        private GUIStyle headingTextStyle;
        private readonly Color pink = new Color32(207, 115, 229, 255);
        private Texture2D emptyTexture;

        public float startTimeStamp;
        public bool isEnabled;

        public void drawWelcomeText(EditorWindow window) {
            if (isEnabled) {
                initResources();
                percentTextStyle.fontSize = (int) (window.position.width * FONT_SIZE_IN_PERCENT_OF_WIDTH);
                GUILayout.BeginVertical();

                float time = (float) EditorApplication.timeSinceStartup - startTimeStamp;

                setupMaterial(time);
                drawBackgroundTexture(window);
                drawText(window, time);

                window.Repaint();
                GUILayout.EndVertical();
            }
        }

        private void drawBackgroundTexture(EditorWindow window) {
            EditorGUI.DrawPreviewTexture(
                new Rect(0, 0, window.position.width, window.position.height),
                emptyTexture,
                colorFadeMaterial
            );
        }

        private void drawText(EditorWindow window, float time) {
            var originalColor = GUI.color;
            var white = Color.white;

            float labelHeight = window.position.width * LABEL_HEIGHT;
            float offsetY = time * window.position.height * SCROLL_AMOUNT;

            GUI.color = color(time: time, fadeInStartTime: 0, color: white, fadeOutStartTime: 3);
            var position1 = new Rect(0f, window.position.height / 2 - labelHeight / 2 - labelHeight - offsetY, window.position.width, labelHeight);
            GUI.Label(position1, "Hello!", percentTextStyle);

            GUI.color = color(time: time, fadeInStartTime: 1, color: white, fadeOutStartTime: 4);
            var position2 = new Rect(0f, window.position.height / 2 - labelHeight / 2 - offsetY, window.position.width, labelHeight);
            GUI.Label(position2, "I'm your Fluffy window.", percentTextStyle);

            GUI.color = color(time: time, fadeInStartTime: 2, color: white, fadeOutStartTime: 5);
            var position3 = new Rect(0f, window.position.height / 2 + labelHeight / 2 - offsetY, window.position.width, labelHeight);
            GUI.Label(position3, "Go ahead and get me docked", percentTextStyle);

            GUI.color = color(time: time, fadeInStartTime: 3, color: white, fadeOutStartTime: 6);
            var position4 = new Rect(0f, window.position.height / 2 + labelHeight / 2 + labelHeight - offsetY, window.position.width, labelHeight);
            GUI.Label(position4, "and let's get started :)", percentTextStyle);

            GUI.color = originalColor;
        }

        private void setupMaterial(float time) {
            colorFadeMaterial.SetFloat(FirstLoadSpinnerUI.EDITOR_TIME, (float) EditorApplication.timeSinceStartup * 0.6f);
            if (time > 7 && time <= 7.5) {
                colorFadeMaterial.SetFloat(ALPHA, 1f - (time - 7f) * 2f);
            }
            else if (time > 7.5) {
                colorFadeMaterial.SetFloat(ALPHA, 0);
                if (Event.current.type != EventType.Repaint) {
                    destroy();
                }
            }
            else {
                colorFadeMaterial.SetFloat(ALPHA, 1f);
            }
        }

        private static Color color(float time, float fadeInStartTime, Color color, float fadeOutStartTime) {
            if (time > fadeInStartTime && time < fadeInStartTime + 1) {
                color.a = time - fadeInStartTime;
            }
            else if (time >= fadeOutStartTime && time <= fadeOutStartTime + 1) {
                color.a = 1f - (time - fadeOutStartTime);
            }
            else if (time > fadeInStartTime && time < fadeOutStartTime) {
                color.a = 1;
            }
            else {
                color.a = 0;
            }

            return color;
        }


        private void initResources() {
            if (headingTextStyle == null || ReferenceEquals(colorFadeMaterial, null) || colorFadeMaterial == null) {
                colorFadeMaterial = new Material(Shader.Find("Hidden/Color-Fade"));
                emptyTexture = PainterBrushTypeUI.createColorBackground(Color.black);
                percentTextStyle = createUIStyle();
                headingTextStyle = createUIStyle();
                headingTextStyle.normal.textColor = pink;
            }
        }

        private GUIStyle createUIStyle() {
            var guiStyle = new GUIStyle(GUI.skin.label) {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            return guiStyle;
        }

        private void destroy() {
            isEnabled = false;
            Object.DestroyImmediate(colorFadeMaterial);
            Object.DestroyImmediate(emptyTexture);
        }
    }
}