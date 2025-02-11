using System;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class PainterBrushTypeUI {
        const int ITEM_SIZE = 50;
        public bool isBrushMenuExpanded = true;
        private GUIStyle _brushDetailsStyle;

        public GUIStyle BrushDetailsStyle {
            get {
                loadResources();
                return _brushDetailsStyle;
            }
        }

        FurMenuItem[] items;
        Texture2D emptyBackground;
        GUIStyle iconStyle, textStyle;
        Rect selectedButtonRect;
        GUIStyle buttonRowGUIStyle;
        GUIStyle outlineStyle;
        private int selectedIndex = 0;
        private GUIStyle selectedLabelUIStyle;

        public static Color32 pink = new Color32(207, 115, 229, 255);
        public static Color32 orange = new Color32(255, 133, 45, 255);
        public static Color32 green = new Color32(153, 220, 81, 255);
        public static Color32 cyan = new Color32(90, 211, 172, 255);
        public static Color32 blue = new Color32(55, 210, 232, 255);

        public void initStyles() {
            emptyBackground = createColorBackground(Color.white);
            iconStyle = createIconStyle();
            textStyle = createTextStyle();
            buttonRowGUIStyle = new GUIStyle() {padding = new RectOffset(0, 0, 9, 0)};
            outlineStyle = new GUIStyle {border = new RectOffset(20, 20, 20, 20)};
            selectedLabelUIStyle = new GUIStyle {fontStyle = FontStyle.Bold, padding = new RectOffset(14, 0, 0, 0)};
            _brushDetailsStyle = new GUIStyle(GUI.skin.label) {
                border = new RectOffset(20, 20, 20, 20),
                padding = new RectOffset(15, 16, 15, 17),
            };
        }

        public void drawBrushTypeUI(FluffyWindow editorWindow, FurCreator furCreator) {
            loadResources();
            addColapsibleHeader();
            var widthCounter = 0;
            EditorGUILayout.BeginVertical(editorWindow.brushPropertiesUI.PanelStyle);
            if (isBrushMenuExpanded) {
                EditorGUILayout.BeginHorizontal(buttonRowGUIStyle);
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.clear;
                for (int i = 0; i < items.Length; i++) {
                    var furMenuItem = items[i];
                    addButton(furMenuItem, furCreator);
                    widthCounter = incrementWidthCounter(editorWindow, widthCounter);
                    if ((int) furMenuItem.brushType == furCreator.painterProperties.type) selectedIndex = i;
                }

                GUI.backgroundColor = originalColor;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (Event.current.type == EventType.Repaint) {
                    outlineStyle.Draw(selectedButtonRect, false, false, false, false);
                }

                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
            }
            else {
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                selectedLabelUIStyle.normal.textColor = items[selectedIndex].color;
                GUILayout.Label(items[selectedIndex].text, selectedLabelUIStyle);
                EditorGUILayout.Space(5);
            }
        }

        private void addButton(FurMenuItem furMenuItem, FurCreator furCreator) {
            EditorGUILayout.BeginVertical();
            iconStyle.normal.background = emptyBackground;
            if (GUILayout.Button(new GUIContent(furMenuItem.icon, furMenuItem.tooltip), iconStyle, GUILayout.Width(ITEM_SIZE),
                GUILayout.Height(ITEM_SIZE))) {
                FluffyUndo.safelyUndo(furCreator.groomContainer.PainterProperties);
                furCreator.painterProperties.type = (int) furMenuItem.brushType;
            }

            if ((int) furMenuItem.brushType == furCreator.painterProperties.type) {
                selectedButtonRect = GUILayoutUtility.GetLastRect();
                selectedButtonRect.x += 1;
                outlineStyle.normal.background = furMenuItem.outline;
                BrushDetailsStyle.normal.background = furMenuItem.detailsBoxBg;
                BrushDetailsStyle.active.background = furMenuItem.detailsBoxBg;
            }

            textStyle.normal.background = emptyBackground;
            if (GUILayout.Button(new GUIContent(furMenuItem.text, furMenuItem.tooltip), textStyle, GUILayout.Width(ITEM_SIZE))) {
                furCreator.painterProperties.type = (int) furMenuItem.brushType;
            }

            EditorGUILayout.EndVertical();
        }

        private void addColapsibleHeader() {
            EditorGUILayout.BeginVertical();
            isBrushMenuExpanded = EditorGUILayout.Foldout(isBrushMenuExpanded, "Brush Type:");
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private int incrementWidthCounter(EditorWindow editorWindow, int widthCounter) {
            widthCounter += ITEM_SIZE;
            if (widthCounter > editorWindow.position.width - 140) {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal(buttonRowGUIStyle);
                widthCounter = 0;
            }

            return widthCounter;
        }

        private GUIStyle createIconStyle() {
            GUIStyle centeredTextStyle = new GUIStyle(GUI.skin.button);
            centeredTextStyle.border.bottom = 0;
            centeredTextStyle.border.top = 0;
            centeredTextStyle.border.left = 0;
            centeredTextStyle.border.right = 0;
            centeredTextStyle.padding = new RectOffset(0, 0, -5, 5);
            centeredTextStyle.normal.background = emptyBackground;
            return centeredTextStyle;
        }

        private GUIStyle createTextStyle() {
            GUIStyle centeredTextStyle = new GUIStyle(GUI.skin.button);
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;
            centeredTextStyle.wordWrap = true;
            centeredTextStyle.normal.background = emptyBackground;
            centeredTextStyle.padding = new RectOffset(0, 0, 0, 0);
            return centeredTextStyle;
        }

        private void loadResources() {
            if (items == null) {
                initStyles();
                var outlinePink = Resources.Load<Texture2D>("outline_pink");
                var outlineOrange = Resources.Load<Texture2D>("outline_orange");
                var outlineGreen = Resources.Load<Texture2D>("outline_green");
                var outlineCyan = Resources.Load<Texture2D>("outline_cyan");
                var outlineBlue = Resources.Load<Texture2D>("outline_blue");

                var bgBoxCyan = Resources.Load<Texture2D>("bg_box_cyan");
                var bgBoxGreen = Resources.Load<Texture2D>("bg_box_green");
                var bgBoxOrange = Resources.Load<Texture2D>("bg_box_orange");
                var bgBoxPink = Resources.Load<Texture2D>("bg_box_pink");
                var bgBoxBlue = Resources.Load<Texture2D>("bg_box_blue");
                items = new[] {
                    new FurMenuItem(Resources.Load<Texture2D>("ic_length"), "Length", PaintType.HEIGHT, outlineCyan, bgBoxCyan, cyan,
                        FluffyTooltips.Length),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_width"), "Width", PaintType.WIDTH, outlineCyan, bgBoxCyan, cyan,
                        FluffyTooltips.Width),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_root"), "Rotate Root", PaintType.DIRECTION_ROOT, outlineBlue, bgBoxPink, blue,
                        FluffyTooltips.RotateRoot),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_bend"), "Bend", PaintType.DIRECTION_BEND, outlineBlue, bgBoxPink, blue,
                        FluffyTooltips.Bend),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_orient"), "Orient", PaintType.DIRECTION_ORIENTATION, outlineBlue, bgBoxPink, blue,
                        FluffyTooltips.Orient),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_twist"), "Twist", PaintType.TWIST, outlineBlue, bgBoxBlue, blue,
                        FluffyTooltips.Twist),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_attract"), "Attract", PaintType.ATTRACT, outlineBlue, bgBoxPink, blue,
                        FluffyTooltips.Attract),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_smooth"), "Smooth", PaintType.SMOOTH, outlinePink, bgBoxPink, pink,
                        FluffyTooltips.Smooth),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_reset"), "Reset", PaintType.RESET, outlinePink, bgBoxPink, pink,
                        FluffyTooltips.Reset),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_mask"), "Mask (Delete)", PaintType.MASK, outlineGreen, bgBoxGreen, green,
                        FluffyTooltips.Mask),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_clumping_mask"), "Clump Mask", PaintType.CLUMPING_MASK, outlineGreen, bgBoxGreen,
                        green, FluffyTooltips.ClumpingMask),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_raise"), "Raise", PaintType.RAISE, outlineCyan, bgBoxCyan, cyan,
                        FluffyTooltips.Raise),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_wind"), "Motion & Wind", PaintType.WIND_MAX_DISTANCE, outlineCyan, bgBoxCyan, cyan,
                        FluffyTooltips.Wind),
                    new FurMenuItem(Resources.Load<Texture2D>("ic_color_override"), "Color Override", PaintType.COLOR_OVERRIDE, outlineOrange,
                        bgBoxOrange, orange, FluffyTooltips.OverrideColor)
                };
            }
        }

        public static Texture2D createColorBackground(Color color) {
            var background = new Texture2D(1, 1);
            Color[] pix = {color, color};
            background.SetPixels(pix);
            background.Apply();
            return background;
        }

        public void endLayout() {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }
    }

    class FurMenuItem {
        public Texture2D icon;
        public Texture2D outline;
        public Texture2D detailsBoxBg;
        public string text;
        public PaintType brushType;
        public Color color;
        public string tooltip;

        public FurMenuItem(Texture2D icon, string text, PaintType brushType, Texture2D outline, Texture2D detailsBoxBg, Color color, string tooltip) {
            this.icon = icon;
            this.text = text;
            this.brushType = brushType;
            this.outline = outline;
            this.detailsBoxBg = detailsBoxBg;
            this.color = color;
            this.tooltip = tooltip;
        }
    }
}