using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace FluffyGroomingTool {
    public class RippleButton {
        public Rect positionRect;
        public string resource;
        public string text;
        public UnityAction clickAction;
        private Material material;
        private Texture2D texture;
        private Texture2D gradient;
        private Vector2 mousePosition = Vector2.zero;

        private float rippleExpand;
        private bool isClicked;
        private GUIStyle textStyle;
        private bool isHoover;
        private float hooverAlpha;
        private Color white50Alpha = new Color(1, 1, 1, 0.5f);

        private float circleAlpha = 1f;
        public bool enabled = true;
        private const float rippleClickedExpandSpeed = 8;
        private const float rippleClickedAlphaFadeSpeed = 4;

        public void draw() {
            initResources();

            EditorGUI.DrawPreviewTexture(positionRect, texture, material);
            float mouseX = (Event.current.mousePosition.x - positionRect.x);
            float mouseY = (positionRect.y + positionRect.height - Event.current.mousePosition.y);
            if (!isClicked) material.SetVector("_MousePosition", new Vector2(mouseX, mouseY));
            material.SetVector("_RectSize", new Vector2(positionRect.width, positionRect.height));
            material.SetVector("_TextureSize", new Vector2(texture.width, texture.height));

            if (enabled) material.SetFloat("_GlobalAlpha", 1);
            else material.SetFloat("_GlobalAlpha", 0.5f);

            mousePosition = Event.current.mousePosition;
            textStyle.normal.textColor = enabled ? Color.white : white50Alpha;

            GUI.Label(positionRect, text, textStyle);
            setupButton();
        }

        private void initResources() {
            if (material == null || texture == null || textStyle == null) {
                material = new Material(Shader.Find("Hidden/Ripple-Button"));
                texture = Resources.Load<Texture2D>(resource);
                material.SetTexture("_MainTex", texture);
                textStyle = new GUIStyle();
                textStyle.fontSize = 14;
                textStyle.normal.textColor = Color.white;
                textStyle.alignment = TextAnchor.MiddleCenter;
            }
        }

        public void update(float deltaTime) {
            if (material == null) {
                material = new Material(Shader.Find("Hidden/Ripple-Button"));
            }

            if (isClicked) {
                rippleExpand += (1f - rippleExpand) * rippleClickedExpandSpeed * deltaTime;
                if (circleAlpha == 0f) invokeClick();
                circleAlpha -= rippleClickedAlphaFadeSpeed * deltaTime;
            }
            else {
                circleAlpha = 1f;
                rippleExpand = 0;
            }

            circleAlpha = Mathf.Clamp01(circleAlpha);
            material.SetFloat("_circleAlpha", easeOutQuad(circleAlpha));
            material.SetFloat("_SpeedDiff", rippleExpand);
            if (isHoover) hooverAlpha += rippleClickedExpandSpeed * deltaTime;
            else hooverAlpha -= rippleClickedExpandSpeed * deltaTime;
            hooverAlpha = Mathf.Clamp01(hooverAlpha);
            material.SetFloat("_hoverAlpha", hooverAlpha);
            material.SetFloat("_RippleExpand", rippleExpand);
        }

        private void invokeClick() {
            isClicked = false;
            clickAction?.Invoke();
        }

        float easeOutQuad(float x) {
            return 1f - (1f - x) * (1f - x);
        }

        private void setupButton() {
            Color temp = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.0f);
            if (GUI.Button(positionRect, new GUIContent("", text)) && !isClicked) {
                isClicked = true;
                float mouseDistanceToButtonCenter =
                    Vector2.Distance(mousePosition, new Vector2(positionRect.x + positionRect.width / 2f, positionRect.y + positionRect.height / 2f));
                float edgeDistanceToButtonCenter = Vector2.Distance(new Vector2(positionRect.x, positionRect.y),
                    new Vector2(positionRect.x + positionRect.width / 2f, positionRect.y + positionRect.height / 2f));
                float distanceToCenterInPercent = mouseDistanceToButtonCenter / edgeDistanceToButtonCenter;
            }

            isHoover = GUI.tooltip == text;
            GUI.color = temp;
        }
    }
}