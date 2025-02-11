using System;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class BannerImage {
        private Material material;
        private Texture2D texttureOn;
        private Texture2D texttureOff;
        private Boolean isOn;
        private float onAmount;
        private Rect positionRect = new Rect(0, 0, 650, 650 * (9f / 16f));
        private const float hooverFadeSpeed = 0.03f;

        public void draw(EditorWindow window) {
            if (!(Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseUp ||
                  Event.current.type == EventType.MouseDown)) return;
            setupTexture();
            window.Repaint();
            setupButton();
        }

        private void setupTexture() {
            if (texttureOn == null) {
                texttureOn = Resources.Load<Texture2D>("GettingStartedOn");
                texttureOff = Resources.Load<Texture2D>("GettingStartedOff");
            }

            material = material == null ? new Material(Shader.Find("Hidden/Start-Banner")) : material;
            material.SetTexture("_MainTex2", texttureOff);
            material.SetFloat("_onAmount", onAmount);
            material.SetVector("_RectSize", new Vector2(positionRect.width, positionRect.height));
            material.SetVector("_TextureSize", new Vector2(texttureOn.width, texttureOn.height));
            material.SetFloat("iTime", (float) EditorApplication.timeSinceStartup * 2);
            EditorGUI.DrawPreviewTexture(
                new Rect(0, 0, 650, 650 * (9f / 16f)),
                texttureOn,
                material, ScaleMode.StretchToFill, (9f / 16f)
            );
        }

        private void setupButton() {
            Color temp = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.0f);
            if (GUI.Button(positionRect, new GUIContent("", "Open Videos"))) {
                Application.OpenURL("https://youtube.com/playlist?list=PLWjAK4pigjfGqQ7MZlHrnhrJeVzxcLF-V");
            }

            GUI.color = temp;
            isOn = GUI.tooltip == "Open Videos";
            if (isOn) onAmount += hooverFadeSpeed;
            else onAmount -= hooverFadeSpeed;
            onAmount = Mathf.Clamp01(onAmount);
        }
    }
}