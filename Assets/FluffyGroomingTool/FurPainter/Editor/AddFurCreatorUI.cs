using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace FluffyGroomingTool {
    public class AddFurCreatorUI {
        private GUIStyle textStyle;
        private GUIStyle headingTextStyle;
        private GUIStyle panelStyle;
        private GUIStyle buttonStyle;

        private int currentPickerWindow = -1;

        public void drawFurCreatorUI(FluffyWindow window, bool hasMeshFilterOrSkinnedMesh) {
            if (textStyle == null) {
                textStyle = createTextStyle();
                headingTextStyle = createTextHeadingStyle();
                panelStyle = window.brushPropertiesUI.PanelStyle;
                buttonStyle = window.painterLayersUI.buttonStyle;
                buttonStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (!hasMeshFilterOrSkinnedMesh) {
                drawSelectObjectUI(window);
                window.Repaint();
            } else if (window.activeObject != null) {
                var mesh = PainterUtils.getMeshWithoutTransform(window.activeObject);
                var existingFurRenderer = window.activeObject.GetComponent<FurRenderer>();
                if (!mesh) return;
                if (isMeshUnreadableOrIndexFormat16(mesh)) {
                    draw32IndexFormatWarning(mesh, panelStyle, headingTextStyle, buttonStyle);
                } else if (isNewGroomContainerDetekted(existingFurRenderer)) {
                    drawRestoreSavedGroomContainerUI(window, existingFurRenderer);
                } else {
                    drawAddFurUi(window);
                }
            }
        }

        public static bool isMeshUnreadableOrIndexFormat16(Mesh mesh) {
            return !mesh.isReadable || mesh.indexFormat != IndexFormat.UInt32;
        }

        private static bool isNewGroomContainerDetekted(FurRenderer existingFurRenderer) {
            return existingFurRenderer != null && existingFurRenderer.furContainer != null &&
                   existingFurRenderer.furContainer.groomContainerGuid != null;
        }

        private void drawAddFurUi(FluffyWindow fcw) {
            EditorGUILayout.BeginVertical(panelStyle);
            EditorGUILayout.LabelField("Add Fur Using:", headingTextStyle);
            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
            if (GUILayout.Button("Strand Preset", buttonStyle)) {
                addFurCheckingExistingRenderer(fcw);
            }

            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
            if (GUILayout.Button("Cutout Card Preset", buttonStyle)) {
                addFurCheckingExistingRenderer(fcw);
                fcw.activeObject.GetComponent<FurCreator>()?.setCardPreset("Card", false);
            }

            GUILayout.Space(PainterResetAndSmoothUI.DEFAULT_CHILD_VERTICAL_MARGIN);
            if (GUILayout.Button("Alpha Card Preset", buttonStyle)) {
                addFurCheckingExistingRenderer(fcw);
                fcw.activeObject.GetComponent<FurCreator>()?.setCardPreset("Alpha", true);
            }

            EditorGUILayout.EndVertical();
        }

        private void drawRestoreSavedGroomContainerUI(FluffyWindow fcw, FurRenderer existingFurRenderer) {
            EditorGUILayout.BeginVertical(panelStyle);
            EditorGUILayout.LabelField(
                "This Fur Container has a saved groom attached to it.\nWould you like to enable grooming?",
                headingTextStyle
            );
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Enable Grooming", buttonStyle)) {
                var groomPath = AssetDatabase.GUIDToAssetPath(existingFurRenderer.furContainer.groomContainerGuid);

                var loadAssetAtPath = AssetDatabase.LoadAssetAtPath<GroomContainer>(groomPath);
                if (loadAssetAtPath != null) {
                    var furCreator = Undo.AddComponent<FurCreator>(fcw.activeObject);
                    furCreator.groomContainer = loadAssetAtPath;
                    furCreator.IsFirstLoad = false;
                    fcw.OnSelectionChange();
                } else {
                    EditorUtility.DisplayDialog("GroomContainer not found",
                        "Could not find the belonging GroomContainer, please select it manually", "ok");
                    currentPickerWindow = GUIUtility.GetControlID(FocusType.Passive) + 100;
                    EditorGUIUtility.ShowObjectPicker<GroomContainer>(null, false, "GroomContainer", currentPickerWindow);
                }
            }

            if (EditorGUIUtility.GetObjectPickerControlID() == currentPickerWindow &&
                Event.current.commandName == "ObjectSelectorUpdated") {
                var container = EditorGUIUtility.GetObjectPickerObject();
                currentPickerWindow = -1;
                if (container != null) {
                    Undo.AddComponent<FurCreator>(fcw.activeObject).groomContainer = (GroomContainer)container;
                    fcw.OnSelectionChange();
                }
            }

            EditorGUILayout.EndVertical();
        }

        public static bool draw32IndexFormatWarning(Mesh mesh, GUIStyle panelStyle, GUIStyle textStyle, GUIStyle buttonStyle) {
            var returnValue = false;
            EditorGUILayout.BeginVertical(panelStyle);
            EditorGUILayout.LabelField(
                "Fluffy requires your asset to have Read/Write enabled and IndexFormat set to 32.\n\n" +
                "It is also recommended to disable Optimize Mesh and Mesh Compression to prevent errors in builds.",
                textStyle
            );
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Would you like Fluffy to fix this for you?");

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Yes Please", buttonStyle)) {
                mesh.fixRwAndIndexFormat();
                returnValue = true;
            }

            EditorGUILayout.EndVertical();
            return returnValue;
        }

        private FurCreator[] furCreatorsInScene;
        private Texture2D helpTexture;

        private readonly ImportantButton myObjectHelpText = new ImportantButton() {
            positionRect = new Rect(),
            resource = "help_my_character",
            fillColor = new Color(144 / 256f, 165 / 256f, 204 / 256f, 1f),
            clickAction = delegate { Debug.Log("You found an easter egg <3. Very special super duper skill unlocked!"); }
        };

        private EditorDeltaTime editorDeltaTime = new EditorDeltaTime();

        private static readonly float NARROW_WINDOW_SIZE = 400;

        private void drawSelectObjectUI(FluffyWindow window) {
            var isNarrowLayout = window.position.width < NARROW_WINDOW_SIZE;
            startNarrowOrWideLayout(isNarrowLayout);
            drawHierarchyImage();
            GUILayout.Space(6);
            drawSelectARendererText();
            endNarrowOrWideLayout(isNarrowLayout);
            EditorGUILayout.Space(20);
            getFurCreatorsInScene();

            if (furCreatorsInScene.Length > 0 && !BuildPipeline.isBuildingPlayer && !EditorApplication.isCompiling) {
                EditorGUILayout.BeginVertical(panelStyle);
                drawWeFoundObjectsText();
                drawSelectFluffyObjectUI();
                EditorGUILayout.EndVertical();
            }
        }

        private void drawSelectARendererText() {
            EditorGUILayout.LabelField(
                "Please select a GameObject with a SkinnedMeshRenderer or a MeshRenderer in the Hierarchy in order to add fur.",
                headingTextStyle
            );
        }

        private void drawWeFoundObjectsText() {
            if (furCreatorsInScene.Length > 1) {
                EditorGUILayout.LabelField("We found some Fluffy GameObjects in your scene. Select one of them to start grooming.", headingTextStyle);
            } else {
                EditorGUILayout.LabelField("We found a Fluffy GameObject in your scene. Select it to start grooming.", headingTextStyle);
            }
        }

        private void drawSelectFluffyObjectUI() {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(panelStyle);
            EditorGUILayout.LabelField("Select:", textStyle);
            EditorGUILayout.Space(10);
            foreach (var furCreator in furCreatorsInScene) {
                if (GUILayout.Button(furCreator.name, buttonStyle)) {
                    EditorGUIUtility.PingObject(furCreator);
                    Selection.activeObject = furCreator;
                }

                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
        }

        private static void endNarrowOrWideLayout(bool isNarrowLayout) {
            if (isNarrowLayout) {
                EditorGUILayout.EndVertical();
            } else {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void startNarrowOrWideLayout(bool isNarrowLayout) {
            if (isNarrowLayout) {
                EditorGUILayout.BeginVertical(panelStyle);
            } else {
                EditorGUILayout.BeginHorizontal(panelStyle);
            }
        }

        private static readonly float MY_OBJECT_POS_IN_PERCENT_OG_BG_WIDTH = 0.099f;
        private static readonly float MY_OBJECT_POS_IN_PERCENT_OG_BG_HEIGHT = 0.3175f;

        private void drawHierarchyImage() {
            if (helpTexture == null) helpTexture = Resources.Load<Texture2D>("help_hierarchy");
            if (helpTexture != null) {
                //This can happen during builds.
                GUILayout.Box(helpTexture, new GUIStyle(), GUILayout.Width(helpTexture.width / 2f), GUILayout.Height(helpTexture.height / 2f));
                var lastRect = GUILayoutUtility.GetLastRect();
                myObjectHelpText.positionRect = new Rect(
                    lastRect.x + helpTexture.width * MY_OBJECT_POS_IN_PERCENT_OG_BG_WIDTH,
                    lastRect.y + helpTexture.height * MY_OBJECT_POS_IN_PERCENT_OG_BG_HEIGHT,
                    myObjectHelpText.getTextureWidth() / 2f,
                    myObjectHelpText.getTextureHeight() / 2f
                );
                editorDeltaTime.Update();
                myObjectHelpText.update(editorDeltaTime.deltaTime);
                myObjectHelpText.draw();
            }
        }

        private float retrieveFurCreatorsTimeStamp = -1;
        private static readonly int MIN_DELAY_TO_RETRIEVE_FUR_CREATORS = 30;

        private void getFurCreatorsInScene() {
            var shouldRefreshFurCreators = retrieveFurCreatorsTimeStamp + MIN_DELAY_TO_RETRIEVE_FUR_CREATORS < EditorApplication.timeSinceStartup;
            if (furCreatorsInScene == null || shouldRefreshFurCreators && Event.current.type == EventType.Layout) {
#if UNITY_2023_1_OR_NEWER
                furCreatorsInScene = Object.FindObjectsByType<FurCreator>(FindObjectsSortMode.InstanceID);
#else
                furCreatorsInScene = Object.FindObjectsOfType<FurCreator>();
#endif
                retrieveFurCreatorsTimeStamp = (float)EditorApplication.timeSinceStartup;
            }

            if (BuildPipeline.isBuildingPlayer) retrieveFurCreatorsTimeStamp = -1;

            var needsRecreation = false;
            foreach (var furCreator in furCreatorsInScene) {
                needsRecreation = furCreator == null || needsRecreation;
            }

            if (needsRecreation) { 
#if UNITY_2023_1_OR_NEWER
                furCreatorsInScene = Object.FindObjectsByType<FurCreator>(FindObjectsSortMode.InstanceID);
#else
                furCreatorsInScene = Object.FindObjectsOfType<FurCreator>();
#endif
            }
        }

        private void addFurCheckingExistingRenderer(FluffyWindow fcw) {
            var scale = fcw.activeObject.transform.lossyScale;
            var isScaleOk = isScaleWithinAcceptableRange(scale);
            if (!isScaleOk && !EditorUtility.DisplayDialog("Scale notification",
                    $"This Object has a lossyScale of {scale}. Would you like Fluffy to assume this is the default scale for this character?",
                    "Yes", "Cancel")) {
                return;
            }

            var existingProceduralFurRenderer = fcw.activeObject.GetComponent<FurRenderer>();
            if (existingProceduralFurRenderer != null) {
                if (EditorUtility.DisplayDialog("Are you sure?",
                        "This will remove the current Fur Renderer and create a new one.", "Proceed", "Cancel")) {
                    Object.DestroyImmediate(existingProceduralFurRenderer);
                    addFurRenderer(fcw);
                }
            } else {
                addFurRenderer(fcw);
            }
        }

        private static readonly float MIN_ACCEPTABLE_SCALE = 0.7f;
        private static readonly float MAX_ACCEPTABLE_SCALE = 1.5f;

        private static Boolean isScaleWithinAcceptableRange(Vector3 scale) {
            if (scale.x < MIN_ACCEPTABLE_SCALE || scale.x > MAX_ACCEPTABLE_SCALE ||
                scale.y < MIN_ACCEPTABLE_SCALE || scale.y > MAX_ACCEPTABLE_SCALE ||
                scale.z < MIN_ACCEPTABLE_SCALE || scale.z > MAX_ACCEPTABLE_SCALE) {
                return false;
            }

            return true;
        }

        private void addFurRenderer(FluffyWindow fcw) {
            FurCreator furCreator = Undo.AddComponent<FurCreator>(fcw.activeObject);
            furCreator.groomContainer.worldScale = fcw.activeObject.transform.lossyScale.getValueFurthestFromOne();
            furCreator.FurRenderer.furContainer.worldScale = furCreator.groomContainer.worldScale;
            fcw.OnSelectionChange();
            getFurCreatorsInScene();
        }

        private GUIStyle createTextStyle() {
            var guiStyle = new GUIStyle(EditorStyles.label) {
                wordWrap = true
            };
            return guiStyle;
        }

        private GUIStyle createTextHeadingStyle() {
            var guiStyle = new GUIStyle(EditorStyles.label) {
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                padding = new RectOffset(4, 0, 0, 0)
            };
            return guiStyle;
        }
    }
}