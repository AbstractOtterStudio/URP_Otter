using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class ToastMessage {
        private GUIStyle style;
        private GUIStyle textStyle;

        private EditorDeltaTime deltaTime = new EditorDeltaTime();
        public string messageText = "";
        private float showAmount = 0.001f;
        private Color textCol = Color.white;
        private Texture2D texture;

        public bool show;
        public bool isFinished;
        public bool isCollapsed;
        private float hideTimeStamp = -1f;

        private static readonly float DEFAULT_SHOW_DURATION = 5f;
        public int fixedColorIndex = -1;

        private Color32[] colors = new Color32[] {
            PainterBrushTypeUI.pink,
            PainterBrushTypeUI.orange,
            PainterBrushTypeUI.green,
            PainterBrushTypeUI.cyan,
            PainterBrushTypeUI.blue
        };

        public void drawMessage(float width) {
            checkIfToastIsFinished();
            createResources();
            updateShowAmount();
            drawUI(width);
        }

        private void createResources() {
            if (textStyle == null || texture == null) {
                textStyle = new GUIStyle(GUI.skin.label) {
                    fontStyle = FontStyle.Bold,
                    fontSize = 14,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true,
                };
                textStyle.normal.textColor = textCol;
                var colorIndex = fixedColorIndex == -1 ? Random.Range(0, colors.Length - 1) : fixedColorIndex;
                texture = PainterBrushTypeUI.createColorBackground(colors[colorIndex]);
                style = new GUIStyle {normal = {background = texture}};
            }
        }

        private void updateShowAmount() {
            deltaTime.Update();
            if (deltaTime.deltaTime > 0) {
                var speed = 12f;
                if (show) {
                    showAmount += (1f - showAmount) * speed * deltaTime.deltaTime;
                }
                else {
                    showAmount += -showAmount * speed * deltaTime.deltaTime;
                }
            }

            showAmount = Mathf.Clamp01(showAmount);
            textCol.a = showAmount;
        }

        private void checkIfToastIsFinished() {
            if (hideTimeStamp > -1f && EditorApplication.timeSinceStartup > hideTimeStamp) {
                show = false;
                if (showAmount < 0.001f) {
                    isFinished = true;
                }
            }
        }

        private void drawUI(float width) {
            if (Event.current.type == EventType.Layout) {
                if (!show && showAmount < 0.01f) {
                    isCollapsed = true;
                }
                else if (show) {
                    isCollapsed = false;
                }
            }

            if (!isCollapsed) {
                var size = textStyle.CalcHeight(new GUIContent(messageText), width);

                GUILayout.BeginVertical(style, GUILayout.Height(size * 1.2f * showAmount));
                GUILayout.Space(5 * showAmount);
                GUILayout.Space((-size * 0.1f + size * 0.1f * showAmount));
                GUILayout.Label(messageText, textStyle, GUILayout.Height(size * showAmount));
                GUILayout.Space(5 * showAmount);
                GUILayout.EndVertical();
            }
        }

        public void drawFixedMessage(bool doShow, float width) {
            show = doShow;
            drawMessage(width);
        }

        public ToastMessage autoHide() {
            hideTimeStamp = (float) (EditorApplication.timeSinceStartup + DEFAULT_SHOW_DURATION);
            return this;
        }
    }
}