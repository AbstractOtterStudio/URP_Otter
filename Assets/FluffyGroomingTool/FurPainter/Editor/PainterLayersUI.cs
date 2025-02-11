using System;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    [Serializable]
    public class PainterLayersUI {
        private static readonly string EDIT_LAYER_CONTROL_NAME = "EDIT_LAYER_CONTROL_NAME";
        private static readonly string EDIT_LAYER_CONTROL_NAME_UNFOCUSED = "EDIT_LAYER_CONTROL_NAME_UNFOCUSED";
        private GUIStyle[] headerStyles;
        private GUIStyle[] headerStylesTop;
        private GUIStyle _buttonStyle;
        private GUIStyle _buttonWithPaddingStyle;
        internal GUIStyle trashButtonStyle;
        internal GUIStyle duplicateButtonStyle;
        internal GUIStyle editNameButtonStyle;
        internal GUIStyle hideLayerOnStyle;
        internal GUIStyle hideLayerOffStyle;
        private GUIStyle headerText;
        private GUIStyle paddingStyle;
        private Texture2D arrowSide;
        private Texture2D arrowDown;
        internal Color32[] colors;
        private static Color layersUITextColor = new Color(1, 1, 1, 0.9f);
        private GUIStyle invisibleButtonStyle = new GUIStyle();

        public GUIStyle buttonStyle {
            get {
                createStyles();
                return _buttonStyle;
            }
        }

        public void createStyles() {
            if (_buttonStyle == null) {
                headerStyles = new GUIStyle[5];
                headerStyles[0] = createPanelStyle("bg_box_cyan");
                headerStyles[1] = createPanelStyle("bg_box_blue");
                headerStyles[2] = createPanelStyle("bg_box_pink");
                headerStyles[3] = createPanelStyle("bg_box_orange");
                headerStyles[4] = createPanelStyle("bg_box_green");

                headerStylesTop = new GUIStyle[5];
                headerStylesTop[0] = createTopPanelStyle("header_cyan");
                headerStylesTop[1] = createTopPanelStyle("header_blue");
                headerStylesTop[2] = createTopPanelStyle("header_pink");
                headerStylesTop[3] = createTopPanelStyle("header_orange");
                headerStylesTop[4] = createTopPanelStyle("header_green");

                _buttonStyle = createButtonStyle("bg_button", "bg_button_hover");
                _buttonStyle.richText = true;
                _buttonWithPaddingStyle = createButtonStyle("bg_button", "bg_button_hover");
                _buttonWithPaddingStyle.richText = true;
                _buttonWithPaddingStyle.padding = new RectOffset(32, 16, 8, 8);
                trashButtonStyle = createButtonStyle("ic_trash", "ic_trash_hover", 3);
                duplicateButtonStyle = createButtonStyle("duplicate", "duplicate_hoover");
                editNameButtonStyle = createButtonStyle("edit", "edit_hoover");
                hideLayerOnStyle = createButtonStyle("HideLayerOn", "HideLayerOnHover");
                hideLayerOffStyle = createButtonStyle("HideLayerOff", "HideLayerOffHover");

                headerText = new GUIStyle(GUI.skin.label) {
                    fontStyle = FontStyle.Bold, fontSize = 12, normal = {textColor = layersUITextColor},
                    richText = true
                };
                paddingStyle = new GUIStyle {padding = new RectOffset(15, 16, 0, 17), margin = new RectOffset(0, 0, -10, 0)};
                colors = new[] {
                    new Color32(90, 211, 172, 255),
                    new Color32(55, 210, 232, 255),
                    new Color32(207, 115, 229, 255),
                    new Color32(255, 133, 45, 255),
                    new Color32(153, 220, 81, 255)
                };
            }

            if (arrowSide == null) {
                arrowSide = Resources.Load<Texture2D>("ic_arrow_side");
                arrowDown = Resources.Load<Texture2D>("ic_arrow_down");
            }
        }

        public static GUIStyle createButtonStyle(string normal, string hover, int border = 5) {
            return new GUIStyle(GUI.skin.label) {
                border = new RectOffset(border, border, border, border),
                normal = {background = Resources.Load<Texture2D>(normal), textColor = layersUITextColor},
                hover = {background = Resources.Load<Texture2D>(hover), textColor = layersUITextColor},
                fontSize = 12,
                padding = new RectOffset(16, 16, 5, 7),
                margin = new RectOffset(17, 17, 0, 0),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        public GUIStyle createTopPanelStyle(String textureName) {
            var headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.border = new RectOffset(20, 20, 20, 10);
            headerStyle.padding = new RectOffset(28, 16, 6, 6);
            headerStyle.margin = new RectOffset(2, 2, -1, -2);
            headerStyle.normal.background = Resources.Load<Texture2D>(textureName);
            return headerStyle;
        }

        public GUIStyle createPanelStyle(String textureName) {
            var headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.border = new RectOffset(20, 20, 20, 20);
            headerStyle.padding = new RectOffset(0, 0, 0, 0);
            headerStyle.normal.background = Resources.Load<Texture2D>(textureName);
            return headerStyle;
        }

        private bool isExpanded = true;

        public bool isDrawHeader() {
            isExpanded = EditorGUILayout.Foldout(isExpanded, "Fur Layers");
            if (isExpanded) GUILayout.Space(4);
            return isExpanded;
        }

        private bool editLayerName;
        private bool editLayerNameUnfocused;

        public void drawLayerHeadingUI(FurCreator furCreator, int index, float width) {
            createStyles();

            bool isActiveLayer = furCreator.groomContainer.activeLayerIndex == index;
            var isHidden = furCreator.groomContainer.layers[index].isHidden;
            var hideLayerStyle = isHidden ? hideLayerOnStyle : hideLayerOffStyle;
            var layersLength = furCreator.groomContainer.layers.Length;
            if (isActiveLayer) {
                GUILayout.BeginVertical(headerStyles[index % 5]);
                GUILayout.BeginHorizontal(headerStylesTop[index % 5]);
                drawHeadersText(furCreator, index, isHidden);

                GUILayout.EndHorizontal();
                drawArrowTexture(arrowDown);

                var trashRect = createTrashRectFocused(layersLength);
                if (layersLength > 1) {
                    if (GUI.Button(trashRect, new GUIContent("", "Delete the layer."), trashButtonStyle)) {
                        furCreator.deleteLayer(index);
                    }
                }

                var toggleVisibilityRect = crateToggleVisibilityRect(trashRect);
                if (GUI.Button(toggleVisibilityRect, new GUIContent("", "Toggle layer hidden state."), hideLayerStyle)) {
                    furCreator.hideOrShowLayer(index);
                }

                var duplicateRect = crateDuplicateRect(toggleVisibilityRect);
                if (GUI.Button(duplicateRect, new GUIContent("", "Duplicate the layer."), duplicateButtonStyle)) {
                    furCreator.duplicateLayer(index);
                }

                var editNameRect = crateDuplicateRect(duplicateRect);
                if (GUI.Button(editNameRect, new GUIContent("", "Edit the layer name."), editNameButtonStyle)) {
                    editLayerName = true;
                    EditorGUI.FocusTextInControl(EDIT_LAYER_CONTROL_NAME);
                }

                GUILayout.BeginVertical(paddingStyle);
                GUILayout.Space(5);
            }
            else {
                GUILayout.Space(0); //We just need this in order for getLastRect to work.
                var currentColor = colors[index % 5];
                if (!EditorGUIUtility.isProSkin) {
                    currentColor = Color.black;
                    currentColor.a = 200;
                }

                _buttonWithPaddingStyle.normal.textColor = currentColor;
                _buttonWithPaddingStyle.hover.textColor = currentColor;
                _buttonWithPaddingStyle.alignment = TextAnchor.MiddleLeft;

                GUILayout.Space(0); //Needed in order to call  GUILayoutUtility.GetLastRect()
                //This is actually the trash can button, but since Unity UI passed the click events inverted we need a transparent button here.
                var invisibleButtonRect = createInvisibleButtonRect(width);

                if (layersLength > 1) {
                    if (GUI.Button(invisibleButtonRect, "", invisibleButtonStyle)) {
                        furCreator.deleteLayer(index);
                    }
                }

                invisibleButtonRect.position = new Vector2(invisibleButtonRect.position.x - 30f, invisibleButtonRect.position.y);
                if (GUI.Button(invisibleButtonRect, "", invisibleButtonStyle)) {
                    furCreator.hideOrShowLayer(index);
                }

                invisibleButtonRect.position = new Vector2(invisibleButtonRect.position.x - 30f, invisibleButtonRect.position.y);
                if (GUI.Button(invisibleButtonRect, "", invisibleButtonStyle)) {
                    furCreator.duplicateLayer(index);
                }

                invisibleButtonRect.position = new Vector2(invisibleButtonRect.position.x - 30f, invisibleButtonRect.position.y);
                if (GUI.Button(invisibleButtonRect, "", invisibleButtonStyle)) {
                    editLayerNameUnfocused = true;
                    GUI.FocusControl(EDIT_LAYER_CONTROL_NAME_UNFOCUSED);
                }

                initLayerName(furCreator, index);
                if (GUILayout.Button(getLayerNameText(furCreator, index, isHidden), _buttonWithPaddingStyle)) {
                    if (!editLayerNameUnfocused) {
                        furCreator.setActiveLayerIndex(index);
                    }
                }

                if (shouldHideLayerNameEdit()) {
                    editLayerNameUnfocused = false;
                }

                var lastRect = GUILayoutUtility.GetLastRect();
                GUI.SetNextControlName(EDIT_LAYER_CONTROL_NAME_UNFOCUSED);
                furCreator.FurRenderer.furContainer.layerStrandsList[index].layerName = GUI.TextField(
                    new Rect(lastRect.x + 28, lastRect.y + 7, editLayerNameUnfocused ? 250 : 0, 20),
                    furCreator.FurRenderer.furContainer.layerStrandsList[index].layerName);
                drawArrowTexture(arrowSide);
                var trashRect = createTrashRect();
                if (layersLength > 1) {
                    GUI.Button(trashRect, "", trashButtonStyle);
                }

                var toggleVisibilityRect = crateToggleVisibilityRect(trashRect);
                GUI.Button(toggleVisibilityRect, new GUIContent("", "Toggle layer hidden state."), hideLayerStyle);
                var duplicateRect = crateDuplicateRect(toggleVisibilityRect);
                GUI.Button(duplicateRect, new GUIContent("", "Duplicate the layer."), duplicateButtonStyle);
                var editNameRect = crateDuplicateRect(duplicateRect);
                GUI.Button(editNameRect, new GUIContent("", "Edit the layer name."), editNameButtonStyle);
            }

            GUILayout.Space(4);
        }

        private static string getLayerNameText(FurCreator furCreator, int index, bool isHidden) {
            return furCreator.FurRenderer.furContainer.layerStrandsList[index].layerName +
                   (isHidden ? " <color=#FF002E>(HIDDEN)</color>" : "");
        }

        private void drawHeadersText(FurCreator furCreator, int index, bool isHidden) {
            headerText.alignment = TextAnchor.MiddleLeft;
            if (shouldHideLayerNameEdit()) {
                editLayerName = false;
            }

            if (index < furCreator.FurRenderer.furContainer.layerStrandsList.Length) {
                initLayerName(furCreator, index);
                GUI.SetNextControlName(EDIT_LAYER_CONTROL_NAME);
                EditorGUI.BeginChangeCheck();
                furCreator.FurRenderer.furContainer.layerStrandsList[index].layerName = EditorGUILayout.TextField(
                    furCreator.FurRenderer.furContainer.layerStrandsList[index].layerName,
                    GUILayout.Width(editLayerName ? 250 : 0)
                );
                if (EditorGUI.EndChangeCheck()) {
                    EditorUtility.SetDirty(furCreator.FurRenderer.furContainer);
                }

                addLayerName(furCreator.FurRenderer.furContainer.layerStrandsList[index].layerName, isHidden);
            }
            else {
                addLayerName("Layer " + (index + 1), isHidden);
            }
        }

        private static void initLayerName(FurCreator furCreator, int index) {
            if (string.IsNullOrEmpty(furCreator.FurRenderer.furContainer.layerStrandsList[index].layerName)) {
                furCreator.FurRenderer.furContainer.layerStrandsList[index].layerName = "Layer " + (index + 1);
            }
        }

        private void addLayerName(string layerName, bool isHidden) {
            GUILayout.Label(
                layerName + " (Active)" +
                (isHidden ? " <color=#FF002E>(HIDDEN)</color>" : ""), headerText,
                GUILayout.Width(editLayerName ? 0 : 250));
        }

        private bool shouldHideLayerNameEdit() {
            return Event.current.type == EventType.MouseDown ||
                   Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return;
        }


        private static Rect crateToggleVisibilityRect(Rect currentRectPosition) {
            currentRectPosition.position = new Vector2(currentRectPosition.position.x - 27f, currentRectPosition.position.y + 1);
            currentRectPosition.width = 26;
            currentRectPosition.height = 26;
            return currentRectPosition;
        }

        private static Rect crateDuplicateRect(Rect currentRectPosition) {
            currentRectPosition.position = new Vector2(currentRectPosition.position.x - 30f, currentRectPosition.position.y);

            return currentRectPosition;
        }

        private static Rect createTrashRect() {
            var lastRect = GUILayoutUtility.GetLastRect();
            return new Rect(lastRect.width - 14, lastRect.y + 1, 28, 28);
        }

        private Rect createTrashRectFocused(int layersLength) {
            var lastRect = GUILayoutUtility.GetLastRect();

            return new Rect(layersLength == 1 ? lastRect.width + 18 : lastRect.width - 12, lastRect.y + 1, 28, 28);
        }

        private static Rect createInvisibleButtonRect(float width) {
            var lastRect = GUILayoutUtility.GetLastRect();
            return new Rect(width - 64, lastRect.y + 10, 32, 32);
        }

        public void drawAddLayerButton(FurCreator furCreator) {
            var normalTextColor = Color.white;
            if (!EditorGUIUtility.isProSkin) {
                normalTextColor = Color.black;
            }

            normalTextColor.a = 0.9f;
            _buttonStyle.normal.textColor = normalTextColor;
            _buttonStyle.hover.textColor = normalTextColor;
            _buttonStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Space(1);
            if (GUILayout.Button(new GUIContent("Add New Layer", "Add a new layer with a separate groom."), _buttonStyle)) {
                furCreator.addLayer();
            }
        }

        private void drawArrowTexture(Texture2D texture) {
            var lastRect = GUILayoutUtility.GetLastRect();
            Color guiColor = GUI.color;
            GUI.color = Color.clear;
            EditorGUI.DrawTextureTransparent(new Rect(lastRect.x + 15, lastRect.y + 10, 12, 12), texture);
            GUI.color = guiColor;
        }

        public void endLayout() {
            GUILayout.EndVertical();
            GUILayout.EndVertical();
            GUILayout.Space(10);
        }
    }
}