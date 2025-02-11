using System;
using UnityEditor;
using UnityEngine;

namespace FluffyGroomingTool {
    [CustomEditor(typeof(FurRenderer))]
    public class FurRendererObjectEditor : Editor {
        private GeneralSettingsUI generalSettingsUI;
        private MotionSettingsUI motionSettingsUI;
        private WindSettingUI windSettingUI;
        private CollidersUI collidersUI;
        private NormalUI normalUI;
        private LodUi lodUI;
        private Styles styles = new Styles();
        private FurRenderer furRenderer;

        private void initialize() {
            furRenderer = serializedObject.targetObject as FurRenderer;
            generalSettingsUI = new GeneralSettingsUI(serializedObject, "Main Settings", "isMainExpanded") { styles = styles };
            collidersUI = new CollidersUI(serializedObject, "Colliders", "isColliderExpanded") {
                styles = styles,
                furRenderer = furRenderer
            };
            lodUI = new LodUi(serializedObject, "LOD & Culling", "isLodExpanded") {
                styles = styles,
                furRenderer = furRenderer
            };
            motionSettingsUI = new MotionSettingsUI(serializedObject, "Movement", "isMovementExpanded") {
                styles = styles,
                furRenderer = furRenderer
            };
            windSettingUI = new WindSettingUI(serializedObject, "Wind", "isWindExpanded") { styles = styles };
            normalUI = new NormalUI(serializedObject, "Normals", "isNormalExpanded") {
                styles = styles,
                furRenderer = furRenderer
            };
            EditorUtility.SetSelectedRenderState((serializedObject.targetObject as MonoBehaviour)?.GetComponent<Renderer>(),
                EditorSelectedRenderState.Hidden);
        }

        public override void OnInspectorGUI() {
            if (generalSettingsUI == null) initialize();
            serializedObject.Update();

            generalSettingsUI.drawUI();
            if (generalSettingsUI.hasNoFurContainer()) {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            motionSettingsUI.drawUI();
            normalUI.drawUI();
            windSettingUI.drawUI();
            collidersUI.drawUI();
            lodUI.drawUI();
            generalSettingsUI.drawMaterialUI();
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed) {
                furRenderer.updateLod();
            }
        }
    }

    public class Styles {
        private GUIStyle _panelStyle;

        public GUIStyle PanelStyle {
            get {
                if (_panelStyle == null) _panelStyle = createDefaultPanelStyle();
                return _panelStyle;
            }
        }

        private static GUIStyle createDefaultPanelStyle() {
            return new GUIStyle(GUI.skin.label) {
                border = new RectOffset(20, 20, 20, 20), padding = new RectOffset(15, 16, 15, 17),
                normal = { background = Resources.Load<Texture2D>("bg_box") }
            };
        }

        public bool addColapsibleHeader(bool isExpanded, string text) {
            EditorGUILayout.BeginVertical();
            isExpanded = EditorGUILayout.Foldout(isExpanded, text);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
            return isExpanded;
        }
    }

    public class GeneralSettingsUI : HeaderUI {
        readonly SerializedProperty furContainer;
        readonly SerializedProperty hairContainer;
        readonly SerializedProperty material;
        private readonly SerializedProperty isMotionVectorEnabled;
        private readonly SerializedProperty isHqLineRenderingEnabled;
        private readonly SerializedProperty strandRenderType;
        private readonly SerializedProperty isUrp;
        MaterialEditor matEditor;
        private GUIStyle buttonStyle;
        private string materialName;
        private StrandRenderType renderType;

        public GeneralSettingsUI(SerializedObject serializedObject, string header, string headerProperty) : base(serializedObject, header,
            headerProperty) {
            furContainer = serializedObject.FindProperty("furContainer");
            hairContainer = serializedObject.FindProperty("hairContainer");
            material = serializedObject.FindProperty("material");
            isMotionVectorEnabled = serializedObject.FindProperty("motionVectors");
            isHqLineRenderingEnabled = serializedObject.FindProperty("enableHqLineRendering");
            strandRenderType = serializedObject.FindProperty("strandRenderType");

            isUrp = serializedObject.FindProperty("isUrp");
            buttonStyle = PainterLayersUI.createButtonStyle("bg_button", "bg_button_hover");
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            materialName = serializedObject.targetObject.name + "FluffyMaterial";
        }

        public override void drawContent() {
            GUILayout.BeginVertical(styles.PanelStyle);
            EditorGUILayout.PropertyField(material);
            if (!AssetDatabase.Contains(material.objectReferenceValue)) {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Save Material To Disk", buttonStyle)) {
                    var path = EditorUtility.SaveFilePanel("Save Material", "Assets/", materialName, "mat");
                    if (!string.IsNullOrEmpty(path)) {
                        path = FileUtil.GetProjectRelativePath(path);
                        AssetDatabase.CreateAsset(material.objectReferenceValue, path);
                    }
                }

                EditorGUILayout.Space(15);
            }

            if (hairContainer != null) EditorGUILayout.PropertyField(hairContainer);
            if (furContainer != null) EditorGUILayout.PropertyField(furContainer);


#if HAS_PACKAGE_UNITY_HDRP_15_0_7
            if (isHqLineRenderingEnabled != null) {
                EditorGUILayout.PropertyField(isHqLineRenderingEnabled);
            }

            if (!isUrp.boolValue && isHqLineRenderingEnabled is not { boolValue: true }) {
                EditorGUILayout.PropertyField(isMotionVectorEnabled);
                if (strandRenderType != null) {
                    EditorGUILayout.PropertyField(strandRenderType);
                }
            }
#else
            if (!isUrp.boolValue) {
                EditorGUILayout.PropertyField(isMotionVectorEnabled);
                if (strandRenderType != null) {
                    EditorGUILayout.PropertyField(strandRenderType);
                }
            }
#endif

            GUILayout.EndVertical();
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }

        public void drawMaterialUI() {
            Material mat = material.objectReferenceValue as Material;
            if (material.objectReferenceValue != null) {
                if (matEditor == null || matEditor.target != mat) {
                    matEditor = (MaterialEditor)Editor.CreateEditor(mat);
                }

                matEditor.DrawHeader();
                matEditor.OnInspectorGUI();
            }
        }

        public bool hasNoFurContainer() {
            return furContainer.objectReferenceValue == null;
        }

        public bool hasNoHairContainer() {
            return hairContainer.objectReferenceValue == null;
        }
    }

    class MotionSettingsUI : HeaderUI {
        private SerializedProperty enablePhysics;
        private SerializedProperty perLayerMotionSettings;
        private SerializedProperty constraintIterations;
        private SerializedProperty gravity;
        private SerializedProperty drag;
        private SerializedProperty stiffnessRoot;
        private SerializedProperty stiffnessTip;
        private SerializedProperty keepShapeStrength;
        private SerializedProperty isFirstNodeFixed;
        internal FurRenderer furRenderer;

        public MotionSettingsUI(
            SerializedObject serializedObject,
            string header,
            string headerProperty,
            string settingsName = "furRendererSettings"
        ) :
            base(serializedObject, header, headerProperty) {
            enablePhysics = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.enableMovement");
            perLayerMotionSettings = serializedObject.FindProperty(settingsName + ".perLayerMotionSettings");
            constraintIterations = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.constraintIterations");
            gravity = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.gravity");
            drag = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.drag");
            stiffnessRoot = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.stiffnessRoot");
            stiffnessTip = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.stiffnessTip");
            isFirstNodeFixed = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.isFirstNodeFixed");
            if (!settingsName.Equals("furRendererSettings")) {
                keepShapeStrength = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.keepShapeStrength");
            }
        }

        private FluffyToolbar fluffyToolbar = new FluffyToolbar() {
            activatedColor = new Color32(55, 210, 232, 255),
            activatedColorHover = new Color32(55, 210, 232, 200)
        };


        private int selectedLayerIndex;

        public override void drawContent() {
            GUILayout.BeginVertical(styles.PanelStyle);

            if (furRenderer != null) {
                EditorGUILayout.PropertyField(perLayerMotionSettings);
                if (perLayerMotionSettings.boolValue) {
                    GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                    selectedLayerIndex = Math.Min(furRenderer.furContainer.layerStrandsList.Length - 1, selectedLayerIndex);
                    selectedLayerIndex = fluffyToolbar.drawToolbar(selectedLayerIndex, furRenderer.getLayersList(), null);

                    if (furRenderer.furContainer.layerStrandsList[selectedLayerIndex].verletSimulationSettings == null) {
                        initializeLayerSettings();
                    }

                    var verletSettings = furRenderer.furContainer.layerStrandsList[selectedLayerIndex].verletSimulationSettings;

                    EditorGUILayout.Space(-5);
                    GUILayout.BeginVertical(styles.PanelStyle);
                    EditorGUI.BeginChangeCheck();
                    verletSettings?.also(settings => {
                        settings.enableMovement = EditorGUILayout.Toggle("Enable Movement", settings.enableMovement);
                        if (settings.enableMovement) {
                            settings.drag = EditorGUILayout.Slider("Drag", settings.drag, 0f, 1f);
                            settings.gravity = EditorGUILayout.Vector3Field("Gravity", settings.gravity);
                            settings.stiffnessRoot = EditorGUILayout.Slider("Stiffness Root", settings.stiffnessRoot, 0, 200);
                            settings.stiffnessTip = EditorGUILayout.Slider("Stiffness Tip", settings.stiffnessTip, 0, 200);
                            settings.constraintIterations = EditorGUILayout.IntSlider("Constraint Iterations", settings.constraintIterations, 4, 32);
                            settings.isFirstNodeFixed = EditorGUILayout.Toggle("First Node Is Fixed", settings.isFirstNodeFixed);
                        }
                    });
                    if (EditorGUI.EndChangeCheck()) {
                        EditorUtility.SetDirty(furRenderer.furContainer);
                    }

                    GUILayout.EndVertical();
                } else {
                    EditorGUILayout.PropertyField(enablePhysics);
                }
            } else {
                EditorGUILayout.PropertyField(enablePhysics);
            }

            if (enablePhysics.boolValue && !perLayerMotionSettings.boolValue) {
                GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
                EditorGUILayout.PropertyField(drag);
                EditorGUILayout.PropertyField(gravity);
                EditorGUILayout.PropertyField(stiffnessRoot);
                EditorGUILayout.PropertyField(stiffnessTip);
                EditorGUILayout.PropertyField(constraintIterations);
                EditorGUILayout.PropertyField(isFirstNodeFixed);
                if (keepShapeStrength != null) EditorGUILayout.PropertyField(keepShapeStrength);
            }

            GUILayout.EndVertical();
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }

        private void initializeLayerSettings() {
            foreach (var layer in furRenderer.furContainer.layerStrandsList) {
                layer.verletSimulationSettings = (VerletSimulationSettings)furRenderer.furRendererSettings.verletSimulationSettings.Clone();
            }
        }
    }

    static class Extensions {
        public static string[] getLayersList(this FurRenderer furRenderer) {
            var layerNames = new string[furRenderer.furContainer.layerStrandsList.Length];
            for (var i = 0; i < layerNames.Length; i++) {
                if (string.IsNullOrEmpty(furRenderer.furContainer.layerStrandsList[i].layerName)) {
                    furRenderer.furContainer.layerStrandsList[i].layerName = "Layer " + i;
                    EditorUtility.SetDirty(furRenderer.furContainer);
                }

                layerNames[i] = furRenderer.furContainer.layerStrandsList[i].layerName;
            }

            return layerNames;
        }
    }

    class WindSettingUI : HeaderUI {
        private readonly SerializedProperty gustFrequency;
        private readonly SerializedProperty windStrength;
        private readonly SerializedProperty windDirectionDegree;

        public WindSettingUI(SerializedObject serializedObject, string header, string headerProperty, string settingsName = "furRendererSettings") :
            base(serializedObject, header, headerProperty) {
            gustFrequency = serializedObject.FindProperty(settingsName + ".windProperties.gustFrequency");
            windStrength = serializedObject.FindProperty(settingsName + ".windProperties.windStrength");
            windDirectionDegree = serializedObject.FindProperty(settingsName + ".windProperties.windDirectionDegree");
        }

        public override void drawContent() {
            GUILayout.BeginVertical(styles.PanelStyle);
            EditorGUILayout.PropertyField(gustFrequency);
            EditorGUILayout.PropertyField(windStrength);
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
            EditorGUILayout.PropertyField(windDirectionDegree);
            GUILayout.EndVertical();
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }
    }

    class CollidersUI : HeaderUI {
        private readonly SerializedProperty sdfColliderResolution;
        private readonly SerializedProperty useForwardCollision;
        private readonly SerializedProperty sphereColliders;
        private readonly SerializedProperty sdfColliders;
        private readonly SerializedProperty capsuleColliders;
        private GUIStyle leftPaddingStyle;
        private readonly SerializedProperty collideWithSourceMesh;
        private readonly SerializedProperty colliderSkinWidth;
        public FurRenderer furRenderer;
        public HairRenderer hairRenderer;

        public CollidersUI(
            SerializedObject serializedObject,
            string header,
            string headerProperty,
            string settingsName = "furRendererSettings"
        ) : base(serializedObject, header, headerProperty) {
            sphereColliders = serializedObject.FindProperty("sphereColliders");
            capsuleColliders = serializedObject.FindProperty("capsuleColliders");
            sdfColliders = serializedObject.FindProperty("sdfColliders");
            collideWithSourceMesh = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.collideWithSourceMesh");
            colliderSkinWidth = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.colliderSkinWidth");
            sdfColliderResolution = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.sdfColliderResolution");
            useForwardCollision = serializedObject.FindProperty(settingsName + ".verletSimulationSettings.useForwardCollision");
        }

        private static GUIStyle extraLeftPadding() {
            return new GUIStyle(GUI.skin.label) {
                border = new RectOffset(0, 0, 0, 0), padding = new RectOffset(15, 0, 0, 0)
            };
        }

        public override void drawContent() {
            if (leftPaddingStyle == null) {
                leftPaddingStyle = extraLeftPadding();
            }

            GUILayout.BeginVertical(styles.PanelStyle);

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
                EditorGUILayout.LabelField("SDF Colliders are currently not supported on Android.");
                if (furRenderer != null) furRenderer.furRendererSettings.verletSimulationSettings.isUnsupportedSDFPlatform = true;
                if (hairRenderer != null) hairRenderer.settings.verletSimulationSettings.isUnsupportedSDFPlatform = true;
            } else {
                EditorGUILayout.PropertyField(collideWithSourceMesh, new GUIContent("Collide With Source Mesh(SDF)"));
                if (furRenderer != null) furRenderer.furRendererSettings.verletSimulationSettings.isUnsupportedSDFPlatform = false;
                if (hairRenderer != null) hairRenderer.settings.verletSimulationSettings.isUnsupportedSDFPlatform = false;
                if (collideWithSourceMesh.boolValue) {
                    GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);

                    GUILayout.BeginVertical(styles.PanelStyle);
                    EditorGUILayout.PropertyField(colliderSkinWidth);

                    var res = sdfColliderResolution.intValue;
                    EditorGUILayout.PropertyField(sdfColliderResolution);
                    if (res != sdfColliderResolution.intValue) {
                        if (furRenderer != null) furRenderer.recreateSdfCollider();
                        if (hairRenderer != null) hairRenderer.recreateSdfCollider();
                    }

                    EditorGUILayout.PropertyField(useForwardCollision);
                    GUILayout.EndVertical();
                }
            }

            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);

            GUILayout.BeginVertical(styles.PanelStyle);
            GUILayout.BeginVertical(leftPaddingStyle);
            EditorGUILayout.PropertyField(sphereColliders);
            EditorGUILayout.PropertyField(capsuleColliders);
            if (furRenderer != null && !furRenderer.furRendererSettings.verletSimulationSettings.isUnsupportedSDFPlatform) {
                EditorGUILayout.PropertyField(sdfColliders);
            } else if (hairRenderer != null && !hairRenderer.settings.verletSimulationSettings.isUnsupportedSDFPlatform) {
                EditorGUILayout.PropertyField(sdfColliders);
            }


            GUILayout.EndVertical();
            GUILayout.EndVertical();
            GUILayout.EndVertical();
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }
    }

    public abstract class HeaderUI {
        readonly SerializedProperty isExpanded;
        private string _header;
        public Styles styles;

        public HeaderUI(SerializedObject serializedObject, string header, string headerProperty) {
            isExpanded = serializedObject.FindProperty("headerExpanded." + headerProperty);
            if (isExpanded == null) Debug.Log("header" + headerProperty);
            _header = header;
        }

        public void drawUI() {
            if (isExpanded == null) Debug.Log(isExpanded.displayName);
            isExpanded.boolValue = styles.addColapsibleHeader(isExpanded.boolValue, _header);
            if (isExpanded.boolValue) {
                drawContent();
            }
        }

        public abstract void drawContent();
    }

    class NormalUI : HeaderUI {
        readonly SerializedProperty sourceMeshNormalToStrandNormalPercent;
        readonly SerializedProperty perLayerNormals;
        private Rect boundsRect;
        public bool hasRenderer = true;
        internal FurRenderer furRenderer;

        public NormalUI(SerializedObject serializedObject, string header, string headerProperty, string settingsName = "furRendererSettings") : base(
            serializedObject, header, headerProperty) {
            sourceMeshNormalToStrandNormalPercent = serializedObject.FindProperty(settingsName + ".sourceMeshNormalToStrandNormalPercent");
            perLayerNormals = serializedObject.FindProperty(settingsName + ".perLayerNormals");
        }

        private FluffyToolbar fluffyToolbar = new FluffyToolbar() {
            activatedColor = new Color32(55, 210, 232, 255),
            activatedColorHover = new Color32(55, 210, 232, 200)
        };


        private int selectedLayerIndex;
        public bool isHqLineRenderingEnabled;

        public override void drawContent() {
            if (hasRenderer) {
                GUILayout.BeginVertical(styles.PanelStyle);
                if (isHqLineRenderingEnabled) {
                    var style = GUI.skin.label;
                    style.wordWrap = true;
                    style.alignment = TextAnchor.UpperLeft;
                    EditorGUILayout.LabelField("Normals are automatically handled by the HQ Line Renderer.", style);
                } else {
                    if (furRenderer != null) {
                        EditorGUILayout.PropertyField(perLayerNormals);
                    }

                    GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);

                    if (perLayerNormals.boolValue) {
                        GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);

                        selectedLayerIndex = fluffyToolbar.drawToolbar(selectedLayerIndex, furRenderer.getLayersList(), null);

                        if (getCurrentLayerNormalPercent() < 0) initializeLayerNormalSettings();

                        EditorGUILayout.Space(-5);
                        GUILayout.BeginVertical(styles.PanelStyle);
                        EditorGUI.BeginChangeCheck();

                        drawNormalLabels();
                        furRenderer.furContainer.layerStrandsList[selectedLayerIndex].sourceMeshNormalToStrandNormalPercent =
                            GUI.HorizontalSlider(boundsRect, getCurrentLayerNormalPercent(), 0, 1);
                        if (EditorGUI.EndChangeCheck()) {
                            EditorUtility.SetDirty(furRenderer.furContainer);
                        }

                        GUILayout.EndVertical();
                    } else {
                        drawNormalLabels();
                        if (boundsRect != null) {
                            sourceMeshNormalToStrandNormalPercent.floatValue =
                                GUI.HorizontalSlider(boundsRect, sourceMeshNormalToStrandNormalPercent.floatValue, 0, 1);
                        }
                    }
                }

                GUILayout.EndVertical();
                GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
            }
        }

        private float getCurrentLayerNormalPercent() {
            return furRenderer.furContainer.layerStrandsList[selectedLayerIndex].sourceMeshNormalToStrandNormalPercent;
        }

        private void drawNormalLabels() {
            EditorGUILayout.LabelField("Source Mesh Normal");
            var lastRect = GUILayoutUtility.GetLastRect();
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUI.Label(lastRect, "Card Normal");
            EditorGUILayout.Space(18);
            lastRect = GUILayoutUtility.GetLastRect();
            if (lastRect.x != 0) {
                boundsRect = lastRect;
                boundsRect.height = 18;
            }
        }

        private void initializeLayerNormalSettings() {
            foreach (var layer in furRenderer.furContainer.layerStrandsList) {
                if (layer.sourceMeshNormalToStrandNormalPercent < 0) {
                    layer.sourceMeshNormalToStrandNormalPercent = furRenderer.furRendererSettings.sourceMeshNormalToStrandNormalPercent;
                }
            }
        }
    }

    internal class LodUi : HeaderUI {
        private readonly SerializedProperty isLodEnabled;
        private readonly SerializedProperty isLodEnabledInEditMode;
        private readonly SerializedProperty isFrustumCullingEnabled;
        private readonly SerializedProperty isAlphaSortingEnabled;
        private readonly SerializedProperty isHdrp;
        private readonly SerializedProperty activeLodCameraName;
        private readonly SerializedProperty activeLodName;
        private readonly SerializedProperty lodCamera;
        private Rect boundsRect;
        private readonly SerializedObject serializedObject;
        private GUIStyle helpStyle;
        internal FurRenderer furRenderer;

        public LodUi(SerializedObject serializedObject, string header, string headerProperty) : base(serializedObject, header, headerProperty) {
            this.serializedObject = serializedObject;
            isLodEnabled = serializedObject.FindProperty("furRendererSettings.enableLod");
            isLodEnabledInEditMode = serializedObject.FindProperty("furRendererSettings.enableLodInEditMode");
            isFrustumCullingEnabled = serializedObject.FindProperty("furRendererSettings.isFrustumCullingEnabled");
            activeLodCameraName = serializedObject.FindProperty("activeLodCameraName");
            activeLodName = serializedObject.FindProperty("activeLodName");
            isAlphaSortingEnabled = serializedObject.FindProperty("furRendererSettings.isAlphaSortingEnabled");
            lodCamera = serializedObject.FindProperty("lodCamera");
            isHdrp = serializedObject.FindProperty("isHdrp");
        }

        private int lodTab;
        private GUIStyle infoStyle;

        private FluffyToolbar fluffyToolbar = new FluffyToolbar() {
            activatedColor = new Color32(55, 210, 232, 255),
            activatedColorHover = new Color32(55, 210, 232, 200)
        };

        public override void drawContent() {
            GUILayout.BeginVertical(styles.PanelStyle);
            EditorGUILayout.PropertyField(isLodEnabled);

            if (isLodEnabled.boolValue) {
                var furContainerProperty = serializedObject.FindProperty("furContainer").objectReferenceValue;
                if (furContainerProperty != null) {
                    EditorGUILayout.PropertyField(lodCamera);
                    drawLodEnabledInEditModeUi();
                    EditorGUILayout.Space(5);
                    lodTab = fluffyToolbar.drawToolbar(lodTab, new[] { "Lod 1", "Lod 2", "Lod 3", "Culled" }, null);
                    drawLodTabContentUi(furContainerProperty as FurContainer);
                }
            }

            drawAlphaSortingAndCullingUi();
            GUILayout.EndVertical();
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
        }

        private void drawAlphaSortingAndCullingUi() {
            EditorGUILayout.PropertyField(isAlphaSortingEnabled,
                new GUIContent("Alpha sorting",
                    " Should Alpha sorting be enabled. Needed when using transparent materials. This can have a huge " +
                    "negative impact on performance if the " +
                    "fur mesh has a high triangle count. But it works pretty well for moderate triangle counts."));
            if (isAlphaSortingEnabled.boolValue && isHdrp.boolValue) {
                if (helpStyle == null) {
                    helpStyle = BrushPropertiesUI.getHelpTextStyle();
                }

                EditorGUILayout.LabelField(
                    "Performance waring: Alpha sorting should only be used when using card based rendering with a transparent material. Currently not supported with ray tracing.",
                    helpStyle
                );
                GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
            }

            EditorGUILayout.PropertyField(isFrustumCullingEnabled,
                new GUIContent("Frustum culling per triangle",
                    "Culls away the triangles that are outside of the camera frustum. This may improve performance since it only renders " +
                    "visible triangles, but can in some cases lead to shadow artifacts."));
        }

        private void drawLodEnabledInEditModeUi() {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(isLodEnabledInEditMode, new GUIContent("LOD & Culling In Edit Mode",
                "This will enable LOD in edit mode as well. Make sure you disable it whenever you want to groom some changes."));
            if (EditorGUI.EndChangeCheck() && !isLodEnabledInEditMode.boolValue) {
                furRenderer.cullAndSortController.setLodIndex(0, furRenderer.furContainer);
                furRenderer.cullAndSortController.dispatchFrustumAndLodOnlyKernel();
            }

            if (isLodEnabledInEditMode.boolValue) {
                infoStyle ??= BrushPropertiesUI.getHelpTextStyle();
                infoStyle.richText = true;
                EditorGUILayout.LabelField(
                    "When the <color=#37d2e8>FurRenderer is focused in edit mode,</color> the Scene View Camera is used for LOD.\n" +
                    $"Active LOD Camera: <color=#37d2e8>{activeLodCameraName.stringValue}\n" +
                    $"</color>Active LOD: <color=#37d2e8>{activeLodName.stringValue}</color>",
                    infoStyle
                );
            }
        }

        private void drawLodTabContentUi(FurContainer furContainer) {
            EditorGUILayout.Space(-5);
            GUILayout.BeginVertical(styles.PanelStyle);

            if (lodTab == 3) {
                EditorGUI.BeginChangeCheck();
                furContainer.culledDistance = EditorGUILayout.FloatField(
                    new GUIContent("Culled Distance", "Distance to camera before hiding the fur."),
                    furContainer.culledDistance
                );
                if (EditorGUI.EndChangeCheck()) {
                    EditorUtility.SetDirty(furContainer);
                }
            } else {
                var selectedLOD = furContainer.furLods[lodTab];
                if (lodTab != 0) {
                    var newLODStartDistance =
                        EditorGUILayout.FloatField(new GUIContent("LOD Start Distance", "Distance to camera before switching to this LOD."),
                            selectedLOD.startDistance);
                    if (newLODStartDistance != selectedLOD.startDistance) {
                        var lod2 = furContainer.furLods[2];
                        if (lodTab == 1 && newLODStartDistance >= lod2.startDistance) {
                            lod2.startDistance = newLODStartDistance + 0.5f;
                        }

                        selectedLOD.startDistance = newLODStartDistance;
                        EditorUtility.SetDirty(furContainer);
                    }
                }

                var newValue = EditorGUILayout.IntSlider(
                    new GUIContent("Draw every (x) strand", "How many strands to cull away when rendering at this LOD."),
                    selectedLOD.skipStrandsCount, 1, 10);
                var newScale = EditorGUILayout.Slider(
                    new GUIContent("Strands Width Scale", "Scale of each strand when rendering at this LOD"),
                    selectedLOD.strandsScale, 1, 10);

                if (newValue != selectedLOD.skipStrandsCount || newScale != selectedLOD.strandsScale) {
                    selectedLOD.skipStrandsCount = newValue;
                    selectedLOD.strandsScale = newScale;
                    EditorUtility.SetDirty(furContainer);
                }
            }

            GUILayout.EndVertical();
        }
    }
}