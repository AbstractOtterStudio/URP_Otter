using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    public class BrushPropertiesUI {
        static readonly float CHANGE_BRUSH_SENSITIVITY = 0.005f;
        static readonly float SMALL_CHANGE_BRUSH_SENSITIVITY = 0.0001f;
        static readonly float BRUSH_FILL_OPACITY = 0.75f;
        static readonly float MIN_BRUSH_SIZE = 0.001f;
        static readonly float MAX_BRUSH_SIZE = 10f;
        private static readonly string HAS_IGNORE_NORMAL_TIP_BEEN_SHOWN = "hasIgnoreNormalTipBeenShown";

        private GUIStyle _panelStyle;
        private BindPoseHelper bindPoseHelper = new BindPoseHelper();
        private static bool hasIgnoreNormalBeenShown;

        public void init() {
            hasIgnoreNormalBeenShown = PerProjectPreferences.getInt(HAS_IGNORE_NORMAL_TIP_BEEN_SHOWN, -1) != -1;
            PerProjectPreferences.setInt(HAS_IGNORE_NORMAL_TIP_BEEN_SHOWN, 1);
        }

        public GUIStyle PanelStyle {
            get {
                if (_panelStyle == null) _panelStyle = createDefaultPanelStyle();
                return _panelStyle;
            }
        }


        public static GUIStyle createDefaultPanelStyle(string bg = "bg_box") {
            return new GUIStyle(GUI.skin.label) {
                border = new RectOffset(20, 20, 20, 20), padding = new RectOffset(15, 16, 15, 17),
                normal = {background = Resources.Load<Texture2D>(bg)}
            };
        }

        private GUIStyle mirrorPanelStyle;
        private GUIStyle learnMoreStyle;

        public void drawBrushPropertiesUI(FurCreator furCreator, PainterBrushTypeUI brushMenu, GUIStyle buttonStyle) {
            PainterProperties properties = furCreator.getPainterProperties();
            var ctrl = getControlText();
            properties.isBrushMenuExpanded = EditorGUILayout.Foldout(properties.isBrushMenuExpanded, "Brush Properties");

            if (properties.isBrushMenuExpanded) {
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                GUILayout.BeginVertical(PanelStyle);
                properties.brushSize = furCreator.groomContainer.PainterProperties.undoSlider(
                    new GUIContent("Brush Size:", "Shortcut: Left mouse button + " + ctrl + ". Changes the size of the brush."), properties.brushSize,
                    MIN_BRUSH_SIZE, MAX_BRUSH_SIZE);
                addIgnoreNormalTooltipClick();
                properties.brushOpacity = furCreator.groomContainer.PainterProperties.undoSlider(
                    new GUIContent("Brush Opacity:", "Shortcut: Left mouse button + Shift. Changes the intensity of the brush."),
                    properties.brushOpacity);
                properties.brushFalloffPercentOfBrushSize = furCreator.groomContainer.PainterProperties.undoSlider(
                    new GUIContent("Brush Falloff:",
                        "Shortcut: Left mouse button + Shift & " + ctrl +
                        ". The falloff is the intensity of the brush from the center towards its borders."),
                    properties.brushFalloffPercentOfBrushSize);
                properties.brushFalloff = properties.brushFalloffPercentOfBrushSize * properties.brushSize;
                properties.isNormalIgnored = furCreator.toggle(
                    new GUIContent("Ignore Normal Slope",
                        "When this is off painting will only affect areas with similar curvature as the center of the brush." +
                        " A good example is when painting a thin surface like a rabbits ear, when Ignore Normal Slope is checked - both the back and front side of the ear will be affected by the brush."),
                    properties.isNormalIgnored
                );
                drawIgnoreNormalTooltip();
                properties.isSelectionLocked = furCreator.toggle(
                    new GUIContent("Lock Selection", "Locks selection to the current selected object."),
                    properties.isSelectionLocked
                );
                properties.isGroomAllLayerAtOnce = furCreator.toggle(
                    new GUIContent("Groom All Layers Mode", "All Layer will be affected by the brush stroke. Not only the current active."),
                    properties.isGroomAllLayerAtOnce
                );
                if (properties.isMirrorMode) {
                    GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                    if (mirrorPanelStyle == null) {
                        mirrorPanelStyle = new GUIStyle(GUI.skin.label) {
                            border = new RectOffset(20, 20, 20, 20),
                            padding = new RectOffset(15, 16, 15, 17),
                            normal = {background = Resources.Load<Texture2D>("bg_box_cyan")}
                        };
                    }

                    GUILayout.BeginVertical(mirrorPanelStyle);
                }

                var mirrorMode = furCreator.toggle(
                    new GUIContent("Mirror Mode", "Mirrors the strokes along x or z axis."),
                    properties.isMirrorMode
                );
                if (properties.isMirrorMode) {
                    GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                    properties.mirrorAxisTab = fluffyToolbar.drawToolbar(
                        properties.mirrorAxisTab,
                        new[] {"Mirror X-Axis", "Mirror Z-Axis"},
                        furCreator
                    );
                    bindPoseHelper.updateBindPoseState(furCreator);
                    if (!bindPoseHelper.isInBindPose) {
                        GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                        if (GUILayout.Button("Restore Bind Pose", buttonStyle)) {
                            bindPoseHelper.restoreBindPose(furCreator);
                        }
                    }
                }
                else {
                    EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                    if (helpStyle == null) helpStyle = getHelpTextStyle();
                    //Footer
                    GUILayout.Label(
                        "Opacity: Left mouse button + Shift\nSize: Left mouse button + " + ctrl + "\nFalloff: Left mouse button + Shift & " + ctrl,
                        helpStyle
                    );
                }

                if (properties.isMirrorMode) GUILayout.EndVertical();
                properties.isMirrorMode = mirrorMode;
                GUILayout.EndVertical();
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
            }
        }

        private GUIStyle invisibleButtonStyle = new GUIStyle();

        private void addIgnoreNormalTooltipClick() {
            if (!hasIgnoreNormalBeenShown) {
                var lastRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(new Rect(lastRect.x, lastRect.y, 110, 50), "", invisibleButtonStyle)) {
                    Application.OpenURL("https://danielzeller427.gitbook.io/fluffy-grooming-tool/brush-properties-1/ingore-normal-slope");
                    hasIgnoreNormalBeenShown = true;
                }
            }
        }

        private readonly ImportantButton myObjectHelpText = new ImportantButton() {
            positionRect = new Rect(),
            resource = "learn_more_frame",
            fillColor = new Color(0.0f, 0.0f, 0.0f, 0.85f),
            isRippleEnabled = false,
            clickAction = delegate { hasIgnoreNormalBeenShown = true; }
        };

        private EditorDeltaTime editorDeltaTime = new EditorDeltaTime();

        private void drawIgnoreNormalTooltip() {
            if (!hasIgnoreNormalBeenShown) {
                var lastRect = GUILayoutUtility.GetLastRect();
                myObjectHelpText.positionRect = new Rect(
                    lastRect.x,
                    lastRect.y - myObjectHelpText.getTextureHeight() / 2f,
                    myObjectHelpText.getTextureWidth() / 2f,
                    myObjectHelpText.getTextureHeight() / 2f
                );
                editorDeltaTime.Update();
                myObjectHelpText.update(editorDeltaTime.deltaTime);
                myObjectHelpText.draw();

                if (learnMoreStyle == null)
                    learnMoreStyle = new GUIStyle(GUI.skin.label) {
                        alignment = TextAnchor.MiddleLeft
                    };

                GUI.Label(new Rect(lastRect.x + 11, lastRect.y - 82, myObjectHelpText.getTextureWidth(), 50),
                    "This fella will be you best friend\nwhenever you can't reach some strands.", learnMoreStyle);
            }
        }

        private FluffyToolbar fluffyToolbar = new FluffyToolbar();

        private GUIStyle helpStyle;

        private string getControlText() {
            return Application.platform == RuntimePlatform.OSXEditor ? "Cmnd" : "Ctrl";
        }

        public float getFalloffBrushDiscSize(int type, PainterProperties p) {
            if (type == (int) PaintType.ADD_FUR) return 0;
            return p.brushFalloffPercentOfBrushSize * p.brushSize;
        }


        public float getBrushDiscSize(int type, PainterAddSpacingUI painterAddSpacingUI, PainterProperties p) {
            if (type == (int) PaintType.ADD_FUR) return painterAddSpacingUI.paintDistanceBetweenStrands;
            return p.brushSize;
        }


        public static GUIStyle getHelpTextStyle() {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.fontSize = 10;
            style.wordWrap = true;
            return style;
        }

        void changeBrushSize(float deltaX, PainterProperties p) {
            p.brushSize += deltaX * getChangeBrushSizeSpeed(p);
            p.brushSize = Mathf.Clamp(p.brushSize, MIN_BRUSH_SIZE, MAX_BRUSH_SIZE);
            p.brushFalloff = Mathf.Clamp(p.brushFalloffPercentOfBrushSize * p.brushSize, MIN_BRUSH_SIZE, p.brushSize);
        }

        private static float getChangeBrushSizeSpeed(PainterProperties painterProperties) {
            float lerpRange = 0.165f - MIN_BRUSH_SIZE;
            float lerpPercent = Mathf.Clamp01((painterProperties.brushSize - MIN_BRUSH_SIZE) / lerpRange);
            return Mathf.Lerp(SMALL_CHANGE_BRUSH_SENSITIVITY, CHANGE_BRUSH_SENSITIVITY, lerpPercent);
        }

        void changeBrushOpacity(float deltaX, PainterProperties p) {
            p.brushOpacity += deltaX * CHANGE_BRUSH_SENSITIVITY;
            p.brushOpacity = Mathf.Clamp01(p.brushOpacity);
        }

        void changeFalloff(float deltaX, PainterProperties p) {
            p.brushFalloffPercentOfBrushSize += deltaX * CHANGE_BRUSH_SENSITIVITY;
            p.brushFalloffPercentOfBrushSize = Mathf.Clamp01(p.brushFalloffPercentOfBrushSize);
        }

        public float getCircleFillColor(PainterProperties p) {
            return p.brushOpacity * BRUSH_FILL_OPACITY;
        }

        public bool handleBrushShortcuts(Event e, PainterProperties p) {
            prevMousePosition ??= e.mousePosition;
            var delta = e.mousePosition.x - ((Vector2) prevMousePosition).x;
            prevMousePosition = e.mousePosition;
            if (Mathf.Abs(delta) > 0 && isLeftMousePressed && e.getControlButton() && !e.shift) {
                changeBrushSize(delta, p);
                return true;
            }

            if (Mathf.Abs(delta) > 0 && isLeftMousePressed && !e.getControlButton() && e.shift) {
                changeBrushOpacity(delta, p);
                return true;
            }

            if (Mathf.Abs(delta) > 0 && isLeftMousePressed && e.getControlButton() && e.shift) {
                changeFalloff(delta, p);
                return true;
            }


            return false;
        }

        public void resetBindPoseHelper() {
            bindPoseHelper.reset();
        }

        private Vector2? prevMousePosition;
        internal bool isLeftMousePressed;

        public void resetMouseMove(bool isPressed) {
            prevMousePosition = null;
            isLeftMousePressed = isPressed;
        }
    }
}