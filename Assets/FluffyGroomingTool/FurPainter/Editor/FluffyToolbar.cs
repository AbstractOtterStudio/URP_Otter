using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class FluffyToolbar {
        public Color activatedColor = new Color32(90, 211, 172, 255);
        public Color activatedColorHover = new Color32(90, 211, 172, 200);
        private const float height = 25f;
        private GUIStyle left;
        private GUIStyle middle;
        private GUIStyle right;
        private GUIStyle leftOn;
        private GUIStyle middleOn;
        private GUIStyle rightOn;
        private Rect rect;
        private static Color layersUITextColor = new Color(1, 1, 1, 1f);
        private List<Texture2D> textures = new List<Texture2D>(); //We need a local ref to avoid them from getting garbage collected

        private GUIStyle createButtonStyle(string textureName, Color onColor, Color offColor) {
            return new GUIStyle(GUI.skin.label) {
                border = new RectOffset(5, 5, 5, 5),
                normal = {background = createTexture(textureName, onColor), textColor = layersUITextColor},
                hover = {background = createTexture(textureName, offColor), textColor = layersUITextColor},
                fontSize = 12,
                padding = new RectOffset(0, 0, 4, 4),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private Texture2D createTexture(string textureName, Color color) {
            var texture = Resources.Load<Texture2D>(textureName);
            var pixels = texture.GetPixels();
            for (var i = 0; i < pixels.Length; ++i) {
                pixels[i] = pixels[i] * color;
            }

            Texture2D tex = new Texture2D(texture.width, texture.height);
            tex.SetPixels(pixels);
            tex.Apply();
            textures.Add(tex);
            return tex;
        }

        public int drawToolbar(int currentItem, string[] items, FurCreator furCreator) {
            if (leftOn == null) {
                var offColor = new Color(0.5f, 0.5f, 0.5f, 0.37f);
                var onColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

                left = createButtonStyle("tab_left", onColor, offColor);
                middle = createButtonStyle("tab_middle", onColor, offColor);
                right = createButtonStyle("tab_right", onColor, offColor);
                leftOn = createButtonStyle("tab_left", activatedColorHover, activatedColor);
                middleOn = createButtonStyle("tab_middle", activatedColorHover, activatedColor);
                rightOn = createButtonStyle("tab_right", activatedColorHover, activatedColor);
            }

            GUILayout.Label("    ", GUILayout.MaxHeight(0));
            if (Event.current.type == EventType.Repaint) {
                rect = GUILayoutUtility.GetLastRect();
                rect.x += 2;
            }

            GUILayout.BeginHorizontal();
            var count = items.Length;
            for (var index = 0; index < count; index++) { 
                var style = getStyle(index, count, index == currentItem);
                var item = items[index];
                var width = (rect.width - 4) / count;
                if (GUI.Button(new Rect(new Vector2(rect.position.x + width * index, rect.position.y), new Vector2(width, height)), item, style)) {
                    if (furCreator != null && currentItem != index) {
                        Undo.RegisterCompleteObjectUndo(furCreator.getPainterProperties(), "Fluffy");
                    }

                    currentItem = index;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(height);
            return currentItem;
        }

        private GUIStyle getStyle(int index, int count, bool isSelected) {
            if (index == 0) return isSelected ? leftOn : left;
            if (index == count - 1) return isSelected ? rightOn : right;
            return isSelected ? middleOn : middle;
        }
    }
}