using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace FluffyGroomingTool {
    public class ImportantButton {
        public Rect positionRect;
        public UnityAction clickAction { get; set; }
        public string resource;
        public string gradientResource = "important_button_gradient";
        public Color fillColor = Color.black;
        public bool isRippleEnabled = true;
        private Material material;
        private Texture2D texture;
        private Texture2D gradient;
        private float scrollingGradientTextureOffset;
        private Vector2 easedMousePosition = Vector2.zero;
        private Vector2 mousePosition = Vector2.zero;
        private const float circleAlphaFadeSpeed = 3;
        private const float easeMousePosSpeed = 10f;
        private const float gradientScrollSpeed = 0.2f;
        public bool disableCircle;
        public bool disableCircleAfterClick;

        public float getTextureWidth() {
            return texture ? texture.width : 0f;
        }

        public float getTextureHeight() {
            return texture ? texture.height : 0f;
        }

        public void draw() {
            loadResources();
            if (material == null || texture == null) return; //This may happen during builds.
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_Gradient", gradient);
            material.SetColor("_FillColor", fillColor);
            EditorGUI.DrawPreviewTexture(positionRect, texture, material);
            float mouseX = (Event.current.mousePosition.x - positionRect.x);
            float mouseY = (positionRect.y + positionRect.height - Event.current.mousePosition.y);
            if (!isClicked) material.SetVector("_MousePosition", new Vector2(mouseX, mouseY));
            material.SetVector("_RectSize", new Vector2(positionRect.width, positionRect.height));
            mousePosition = Event.current.mousePosition;
            setupButton();
        }

        private void loadResources() {
            if (ReferenceEquals(material, null) || material == null || texture == null) {
                material = new Material(Shader.Find("Hidden/Important-Button"));
                texture = Resources.Load<Texture2D>(resource);
                gradient = Resources.Load<Texture2D>(gradientResource);
            }
        }

        public float speedDiff;
        public float easedSpeedDiff;
        private bool isClicked;

        public void update(float deltaTime) {
            scrollingGradientTextureOffset -= gradientScrollSpeed * deltaTime;
            loadResources();
            material.SetFloat("_Offset", scrollingGradientTextureOffset);
            if (isClicked) {
                easedSpeedDiff += (targetExpand - easedSpeedDiff) * easeMousePosSpeed * deltaTime;
                if (circleAlpha == 0f) invokeClick();
                circleAlpha -= circleAlphaFadeSpeed * deltaTime;
            }
            else {
                circleAlpha += circleAlphaFadeSpeed * deltaTime;
                easedMousePosition += (mousePosition - easedMousePosition) * easeMousePosSpeed * deltaTime;
                speedDiff = Mathf.Clamp01(Vector3.Distance(easedMousePosition, mousePosition) / 100f);
                easedSpeedDiff += (speedDiff - easedSpeedDiff) * easeMousePosSpeed * deltaTime;
            }

            if (disableCircle) {
                circleAlpha = 0;
            }
            else {
                circleAlpha = Mathf.Clamp01(circleAlpha);
            }

            material.SetFloat("_circleAlpha", easeOutExpo(circleAlpha));
            material.SetFloat("_SpeedDiff", easedSpeedDiff);
        }
 

        private void invokeClick() {
            isClicked = false;
            easedSpeedDiff = 2;
            clickAction?.Invoke();
            if (disableCircleAfterClick) {
                disableCircle = true;
            }
        }

        private float targetExpand;
        private float circleAlpha = 1f;

        float easeOutExpo(float x) {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return x == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * x);
        }

        private void setupButton() {
            Color temp = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.0f);
            if (GUI.Button(positionRect, "") && !isClicked) {
                if (isRippleEnabled) {
                    isClicked = true;
                    float mouseDistanceToButtonCenter =
                        Vector2.Distance(mousePosition,
                            new Vector2(positionRect.x + positionRect.width / 2f, positionRect.y + positionRect.height / 2f));
                    float edgeDistanceToButtonCenter = Vector2.Distance(new Vector2(positionRect.x, positionRect.y),
                        new Vector2(positionRect.x + positionRect.width / 2f, positionRect.y + positionRect.height / 2f));
                    float distanceToCenterInPercent = mouseDistanceToButtonCenter / edgeDistanceToButtonCenter;
                    targetExpand = -6.3f * distanceToCenterInPercent;
                }
                else {
                    invokeClick();
                }
            }

            GUI.color = temp;
        }
    }
}