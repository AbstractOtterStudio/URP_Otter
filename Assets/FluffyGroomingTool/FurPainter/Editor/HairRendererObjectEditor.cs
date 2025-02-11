using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    [CustomEditor(typeof(HairRenderer))]
    public class HairRendererObjectEditor : Editor {
        private GeneralSettingsUI generalSettingsUI;
        private MotionSettingsUI motionSettingsUI;
        private WindSettingUI windSettingUI;

        private HairRenderer hairRenderer;
        private Styles styles = new();
        private NormalUI normalUI;
        private CollidersUI collidersUI;
        private StrandShapeUI strandShapeUI;
        private HairCullingLodUI cullUi;
        private SerializedProperty isHqLineRenderingEnabled;


        private void initialize() {
            hairRenderer = serializedObject.targetObject as HairRenderer;
            isHqLineRenderingEnabled = serializedObject.FindProperty("enableHqLineRendering");

            generalSettingsUI = new GeneralSettingsUI(serializedObject, "Main Settings", "isMainExpanded") { styles = styles };
            motionSettingsUI = new MotionSettingsUI(serializedObject, "Movement", "isMovementExpanded", "settings") {
                styles = styles
            };
            windSettingUI = new WindSettingUI(serializedObject, "Wind", "isWindExpanded", "settings") { styles = styles };
            normalUI = new NormalUI(serializedObject, "Normals", "isNormalExpanded", "settings") { styles = styles };
            cullUi = new HairCullingLodUI(serializedObject, "Culling", "isCullingExpanded") { styles = styles };
            strandShapeUI = new StrandShapeUI(serializedObject, "Strands Shape", "isStrandShapeExpanded") {
                styles = styles,
                hairRenderer = hairRenderer
            };
            collidersUI = new CollidersUI(serializedObject, "Colliders", "isColliderExpanded", "settings") {
                styles = styles,
                furRenderer = null,
                hairRenderer = hairRenderer
            };
            EditorUtility.SetSelectedRenderState((serializedObject.targetObject as MonoBehaviour)?.GetComponent<Renderer>(),
                EditorSelectedRenderState.Hidden);
        }

        public override void OnInspectorGUI() {
            if (generalSettingsUI == null) initialize();
            serializedObject.Update();
            generalSettingsUI.drawUI();
#if HAS_PACKAGE_UNITY_HDRP_15_0_7
            if (isHqLineRenderingEnabled != null) {
                strandShapeUI.isHqLineRenderingEnabled = isHqLineRenderingEnabled.boolValue;
                normalUI.isHqLineRenderingEnabled = isHqLineRenderingEnabled.boolValue;
            }
#endif
            strandShapeUI.drawUI();
            if (generalSettingsUI.hasNoHairContainer()) {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            motionSettingsUI.drawUI();
            if (hairRenderer.CurrentRenderer != null) {
                normalUI.hasRenderer = hairRenderer.CurrentRenderer != null;
                normalUI.drawUI();
            }

            cullUi.drawUI();
            windSettingUI.drawUI();
            collidersUI.drawUI();
            generalSettingsUI.drawMaterialUI();
            serializedObject.ApplyModifiedProperties();
        }
    }

    class HairCullingLodUI : HeaderUI {
        private readonly SerializedProperty MinVal;
        private readonly SerializedProperty MaxVal;
        private readonly SerializedProperty MinCullAmount;
        private readonly SerializedProperty MaxCullAmount;
        private readonly SerializedProperty cam;
        private readonly SerializedProperty disObject;
        private readonly SerializedProperty cameraName;
        private readonly SerializedProperty extraStrandWidth;

        public HairCullingLodUI(SerializedObject serializedObject, string header, string headerProperty) :
            base(serializedObject, header, headerProperty) {
            MinVal = serializedObject.FindProperty("cullSettings.MinVal");
            MaxVal = serializedObject.FindProperty("cullSettings.MaxVal");
            MinCullAmount = serializedObject.FindProperty("cullSettings.MinCullAmount");
            MaxCullAmount = serializedObject.FindProperty("cullSettings.MaxCullAmount");
            cam = serializedObject.FindProperty("cullSettings.camera");
            disObject = serializedObject.FindProperty("cullSettings.distanceCheckTargetObject");
            cameraName = serializedObject.FindProperty("currentCullingCameraName");
            extraStrandWidth = serializedObject.FindProperty("cullSettings.extraStrandWidth");
        }

        public override void drawContent() {
            GUILayout.BeginVertical(styles.PanelStyle);
            var minValFloatValue = MinVal.floatValue;
            var maxValFloatValue = MaxVal.floatValue;
            EditorGUILayout.MinMaxSlider("Lod Min/Max Camera Distance", ref minValFloatValue, ref maxValFloatValue, 0, CullSettings.MAX_CAM_DIST);
            MinVal.floatValue = minValFloatValue;
            MaxVal.floatValue = maxValFloatValue;
            EditorGUILayout.LabelField("Culling starts at distance: "+ minValFloatValue.ToString("f1") + " and ends at distance: "+maxValFloatValue.ToString("f1"));
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);

            var minCullAmountFloatValue = (float)MinCullAmount.intValue;
            var maxCullAmountFloatValue = (float)MaxCullAmount.intValue;
            EditorGUILayout.MinMaxSlider("Skip every (x) strands", ref minCullAmountFloatValue, ref maxCullAmountFloatValue, 0, CullSettings.MAX_SKIP_AMOUNT);
            MinCullAmount.intValue = (int)minCullAmountFloatValue;
            MaxCullAmount.intValue = (int)maxCullAmountFloatValue;
            EditorGUILayout.LabelField("Minimum every ("+MinCullAmount.intValue + ") and maximum (" +MaxCullAmount.intValue +") strands will be culled");
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);

            EditorGUILayout.PropertyField(cam);
            EditorGUILayout.LabelField("Currently checking distance to the Camera: "+cameraName.stringValue);
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);

            EditorGUILayout.PropertyField(disObject);
            
            EditorGUILayout.PropertyField(extraStrandWidth);
            
            GUILayout.EndVertical();
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }
    }

    class StrandShapeUI : HeaderUI {
        public HairRenderer hairRenderer;
        private Color32 color = new(55, 210, 232, 255);

        public StrandShapeUI(SerializedObject serializedObject, string header, string headerProperty) : base(
            serializedObject, header, headerProperty) { }

        public bool isHqLineRenderingEnabled;

        public override void drawContent() {
            GUILayout.BeginVertical(styles.PanelStyle);
            EditorGUI.BeginChangeCheck();
            if (!isHqLineRenderingEnabled) {
                EditorGUILayout.CurveField(new GUIContent("Strand Shape Curve", "The shape curve of each strand from root to tip."),
                    hairRenderer.hairContainer.shapeCurve,
                    color,
                    new Rect(0f, 0f, 1, 1f)
                );

                EditorGUILayout.LabelField("Strands Width:");
                var lastRect = GUILayoutUtility.GetLastRect();
                lastRect.y += 23;
                lastRect.height = 18;
                hairRenderer.hairContainer.strandsWidth =
                    GUI.HorizontalSlider(lastRect, hairRenderer.hairContainer.strandsWidth, 0.00007f, 0.01f);

                if (EditorGUI.EndChangeCheck()) {
#if UNITY_EDITOR
                    EditorUtility.SetDirty(hairRenderer.hairContainer);
                    hairRenderer.rebuildShapeBuffer();
#endif
                }

                GUILayout.Space(35);
                var panelSize = MeshCardPropertiesUI.PANEL_SIZE;
                GUILayout.BeginHorizontal(styles.PanelStyle, GUILayout.MaxWidth(panelSize), GUILayout.MinHeight(panelSize));
                EditorGUILayout.Space(panelSize);
                GUILayout.EndHorizontal();
                lastRect = GUILayoutUtility.GetLastRect();
                lastRect.width = MeshCardPropertiesUI.PREVIEW_HEIGHT;
                MeshCardPropertiesUI.drawGrid(hairRenderer.hairContainer.shapeCurve, hairRenderer.hairContainer.pointsPerStrand, lastRect, color);
            } else {
                var style = GUI.skin.label;
                style.wordWrap = true;
                style.alignment = TextAnchor.UpperLeft;
                EditorGUILayout.LabelField("Strand width is handled in the Material/Shader when using the HQ Line Renderer.", style);
            }

            GUILayout.EndVertical();
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }
    }
}