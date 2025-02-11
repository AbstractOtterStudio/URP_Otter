using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyGroomingTool {
    public class FluffyWindow : EditorWindow {
        private bool isChangingBrushValue;
        private bool isPainting;
        private bool isPaintingDisabledDueToAltDrag;

        internal float brushMoveDistance;

        private Vector2 mousePosition = Vector2.zero;
        private Vector2 lastMousePosition = Vector2.zero;
        internal Vector2 scroll;

        internal MeshProperties mirroredRayCastHitMp;
        internal MeshProperties rayCastHitMp;
        internal Vector3 previousRayHit;
        internal Vector3 previousMirrorRayHit;

        internal Vector3? clickStartHitPoint;
        internal Vector3? mirrorClickStartHitPoint;

        internal FurCreator furCreator;
        internal GameObject activeObject;

        internal readonly PainterBrushTypeUI brushTypeUI = new PainterBrushTypeUI();
        private readonly PainterResetAndSmoothUI painterResetAndSmoothUI = new PainterResetAndSmoothUI();
        private readonly PainterMagnitudeUI painterMagnitudeUI = new PainterMagnitudeUI();
        private readonly PainterClumpMaskUI painterPainterClumpMaskUI = new PainterClumpMaskUI();
        private readonly PainterTwistUI painterTwistUI = new PainterTwistUI();

        private readonly PainterMaskUI painterMaskUI = new PainterMaskUI();
        internal readonly PainterAddSpacingUI painterAddSpacingUI = new PainterAddSpacingUI();
        internal readonly BrushPropertiesUI brushPropertiesUI = new BrushPropertiesUI();
        readonly FirstLoadSpinnerUI firstLoadUI = new FirstLoadSpinnerUI();
        readonly ClumpModifierUI clumpModifierUI = new ClumpModifierUI();
        internal readonly PainterLayersUI painterLayersUI = new PainterLayersUI();
        private readonly PainterColorOverrideUI painterColorOverrideUI = new PainterColorOverrideUI();
        private readonly RateAndReviewUI rateAndReviewUi = new RateAndReviewUI();

        internal readonly LayerPropertiesUI layerPropertiesUI = new LayerPropertiesUI();
        readonly MeshCardPropertiesUI meshCardPropertiesUI = new MeshCardPropertiesUI();
        readonly ExportMeshUI exportMeshUI = new ExportMeshUI();
        private readonly ExportFurContainerUI _exportFurContainerUI = new ExportFurContainerUI();
        private readonly AddFurCreatorUI addFurCreatorUI = new AddFurCreatorUI();
        private WelcomeTextUI welcomeTextUI = new WelcomeTextUI();

        private bool hasMeshFilterOrSkinnedMesh;

        [MenuItem("Tools/Fluffy Grooming Tool/Launch Fluffy Window", false, 1)]
        public static FluffyWindow launchFurPainter() {
            var window = GetWindow<FluffyWindow>();
            window.titleContent = new GUIContent("Fluffy Grooming Tool");
            window.Show();
            window.OnSelectionChange();
            return window;
        }

        public void showWelcomeTextUI() {
            welcomeTextUI.isEnabled = true;
            welcomeTextUI.startTimeStamp = (float) EditorApplication.timeSinceStartup;
        }

        private void OnEnable() {
            brushPropertiesUI.init();
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged -= playModeStateChanged;
            EditorApplication.playModeStateChanged += playModeStateChanged;
            sceneDisk = SceneDisk.createDisk("SceneDisk");
            mirroredSceneDisk = SceneDisk.createDisk("MirroredSceneDisk");
            OnSelectionChange();
        }

        private SceneDisk sceneDisk;
        private SceneDisk mirroredSceneDisk;

        private void playModeStateChanged(PlayModeStateChange state) {
            OnSelectionChange();
        }

        private void OnDisable() {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.playModeStateChanged -= playModeStateChanged;
            if (sceneDisk != null) DestroyImmediate(sceneDisk.gameObject);
            if (mirroredSceneDisk != null) DestroyImmediate(mirroredSceneDisk.gameObject);
        }

        internal void OnSelectionChange() {
            if (shouldLockSelectionToCurrent()) {
                if (!furCreator.gameObject.activeInHierarchy) {
                    furCreator.painterProperties.isSelectionLocked = false;
                }
                else {
                    Selection.activeGameObject = furCreator.gameObject;
                    return;
                }
            }

            if (furCreator != null) furCreator.showToastMessage -= showTaost;
            furCreator = null;
            activeObject = null;
            hasMeshFilterOrSkinnedMesh = false;
            if (Selection.activeGameObject != null) {
                furCreator = Selection.activeGameObject.GetComponent<FurCreator>();
                if (furCreator != null) furCreator.showToastMessage += showTaost;
                var activeGameObject = Selection.activeGameObject;
                hasMeshFilterOrSkinnedMesh = activeGameObject.GetComponent<MeshFilter>() != null ||
                                             activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
                if (hasMeshFilterOrSkinnedMesh) {
                    activeObject = activeGameObject;
                }
            }

            Repaint();
        }

        private void showTaost(string message) {
            toastMessages.Add(
                new ToastMessage {
                    messageText = message,
                    show = true
                }.autoHide()
            );
        }

        private bool shouldLockSelectionToCurrent() {
            return activeObject != null && furCreator != null && furCreator.getPainterProperties().isSelectionLocked &&
                   Selection.activeGameObject != activeObject;
        }

        private void OnGUI() {
            drawToastMessages();

            if (hasFurCreator() && furCreator.IsFirstLoad) {
                firstLoadUI.drawFirstLoadProgressbar(this);
                refreshAll();
            }
            else {
                firstLoadUI.resetIndex();
                scroll = GUILayout.BeginScrollView(scroll, false, false);
                GUILayout.BeginVertical(new GUIStyle() {padding = new RectOffset(4, 4, 0, 0)});
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
                if (furCreator != null) {
                    if (!furCreator.isActiveAndEnabled || !furCreator.FurRenderer.isActiveAndEnabled) {
                        drawFurCreatorDisabledUI();
                    }
                    else {
                        drawPainterUI();
                    }
                }
                else {
                    addFurCreatorUI.drawFurCreatorUI(this, hasMeshFilterOrSkinnedMesh);
                }

                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }

            welcomeTextUI.drawWelcomeText(this);
        }

        private void drawToastMessages() {
            foreach (var message in toastMessages) {
                message.drawMessage(position.width);
            }

            if (Event.current.type == EventType.Layout) {
                toastMessages = toastMessages.Where(tm => !tm.isFinished).ToList();
            }

            var isSelectionLocked = furCreator != null && furCreator.getPainterProperties().isSelectionLocked;
            if (isSelectionLocked) selectionLockedMessage.messageText = "Selection is locked to (" + activeObject.name + ")";
            selectionLockedMessage.drawFixedMessage(isSelectionLocked, position.width);
            var isGroomAllLayerAtOnce = furCreator != null && furCreator.getPainterProperties().isGroomAllLayerAtOnce;
            allLayerModeMessage.drawFixedMessage(isGroomAllLayerAtOnce, position.width);
        }

        private ToastMessage selectionLockedMessage = new ToastMessage();
        private ToastMessage allLayerModeMessage = new ToastMessage {messageText = "Groom all layers simultaneously is enabled"};
        private List<ToastMessage> toastMessages = new List<ToastMessage>();

        private bool hasFurCreator() {
            return furCreator != null && furCreator.isActiveAndEnabled && furCreator.FurRenderer.isActiveAndEnabled;
        }

        private void drawPainterUI() {
            Tools.current = Tool.None;
            brushTypeUI.drawBrushTypeUI(this, furCreator);
            painterResetAndSmoothUI.drawResetAndSmoothUI(brushTypeUI, furCreator);
            painterMagnitudeUI.drawMagnitudeUI(brushTypeUI, furCreator);
            painterPainterClumpMaskUI.drawClumpMaskUI(brushTypeUI, furCreator);
            painterTwistUI.drawTwistUI(brushTypeUI, furCreator);
            painterMaskUI.drawMaskUI(this, painterLayersUI.buttonStyle, furCreator);
            painterColorOverrideUI.drawColorOverrideUI(brushTypeUI, furCreator);
            painterAddSpacingUI.drawAddFurSpacingUI(brushTypeUI, furCreator.getPainterProperties());
            brushTypeUI.endLayout();
            brushPropertiesUI.drawBrushPropertiesUI(furCreator, brushTypeUI, painterLayersUI.buttonStyle);

            if (painterLayersUI.isDrawHeader()) {
                GUILayout.BeginVertical(brushPropertiesUI.PanelStyle);

                for (int i = 0; i < furCreator.groomContainer.layers.Length; i++) {
                    painterLayersUI.drawLayerHeadingUI(furCreator, i, position.width);

                    if (i == furCreator.groomContainer.activeLayerIndex) {
                        layerPropertiesUI.drawFurPropertiesUI(furCreator, brushPropertiesUI.PanelStyle);
                        clumpModifierUI.drawClumpingUI(furCreator, brushPropertiesUI.PanelStyle, painterLayersUI);
                        meshCardPropertiesUI.drawCardMeshPropertiesUI(furCreator, brushPropertiesUI.PanelStyle, painterLayersUI);
                        painterLayersUI.endLayout();
                    }
                }

                painterLayersUI.drawAddLayerButton(furCreator);
                GUILayout.EndVertical();
                EditorGUILayout.Space(PainterResetAndSmoothUI.DEFAULT_MARGIN_TOP);
            }

            _exportFurContainerUI.drawExportFurContainerUI(furCreator, brushPropertiesUI.PanelStyle, this);
            exportMeshUI.drawExportMeshUI(furCreator, this);
            rateAndReviewUi.drawRateAndReviewUI(this);
            refreshAll();
            if (GUI.changed) furCreator.updateAllLayerStrands();
        }

        private void refreshAll() {
            updateSceneView();
            Repaint();
        }

        private void drawFurCreatorDisabledUI() {
            GUILayout.BeginVertical(brushTypeUI.BrushDetailsStyle);
            var furCreatorOrRenderer = furCreator.isActiveAndEnabled ? "(FurRenderer)" : "(FurCreator)";
            EditorGUILayout.LabelField("Please enable " + furCreator.name + furCreatorOrRenderer + " to edit the fur.");
            GUILayout.Space(5);
            if (GUILayout.Button("Enable", painterLayersUI.buttonStyle)) {
                furCreator.enabled = true;
                GameObject gameObject;
                (gameObject = furCreator.gameObject).SetActive(true);
                furCreator.FurRenderer.enabled = true;
                var existingObject = PainterUtils.findExistingFurObject(
                    FurMeshCreator.getFurObjectName(gameObject),
                    furCreator.transform.parent
                );
                if (existingObject != null) existingObject.gameObject.SetActive(false);
            }

            GUILayout.EndVertical();
        }


        void OnSceneGUI(SceneView sceneView) {
#if UNITY_EDITOR
            if (BuildPipeline.isBuildingPlayer) return;
#endif
            Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePosition);
            if (hasFurCreator() && furCreator.FurRenderer.meshBaker.sourceMesh != null) {
                if (sceneDisk == null || sceneDisk.gameObject == null) sceneDisk = SceneDisk.createDisk("SceneDisk");
                if (mirroredSceneDisk == null || mirroredSceneDisk.gameObject == null) mirroredSceneDisk = SceneDisk.createDisk("MirroredSceneDisk");
                var hitMp = furCreator.FurRenderer.meshBaker.rayCast(worldRay);

                var isHit = hitMp != null;
                if (isHit && !isChangingBrushValue) {
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    rayCastHitMp = (MeshProperties) hitMp;
                    mirroredRayCastHitMp = createInverseHit(rayCastHitMp);

                    if (isPainting && furCreator.isActiveAndEnabled && !isChangingBrushValue) {
                        this.paintVertex();
                        updateSceneView();
                    }
                }

                if (isHit || isChangingBrushValue) {
                    sceneDisk.gameObject.SetActive(true);
                    drawSceneDisk(rayCastHitMp, sceneDisk);
                    if (furCreator.getPainterProperties().isMirrorMode) {
                        mirroredSceneDisk.gameObject.SetActive(true);
                        Handles.zTest = CompareFunction.LessEqual;
                        drawSceneDisk(mirroredRayCastHitMp, mirroredSceneDisk);
                        Handles.zTest = CompareFunction.Greater;
                    }
                    else {
                        mirroredSceneDisk.gameObject.SetActive(false);
                    }
                }
                else {
                    sceneDisk.gameObject.SetActive(false);
                    mirroredSceneDisk.gameObject.SetActive(false);
                }
            }


            processInputs();
            sceneView.Repaint();
            if (GUI.changed) furCreator.updateAllLayerStrands();
        }


        private MeshProperties createInverseHit(MeshProperties currentHit) {
            var thisTransform = furCreator.transform;
            var targetTransform = getTargetMirrorTransform(thisTransform);

            var inverseTransformPoint = targetTransform.InverseTransformPoint(currentHit.sourceVertex);
            var inverseNormalDirection = targetTransform.InverseTransformDirection(currentHit.sourceNormal);
            bool mirrorOnXAxis = furCreator.getPainterProperties().mirrorAxisTab == 0;

            Vector3 mirrorNormal;
            if (mirrorOnXAxis) {
                inverseTransformPoint.x *= -1f;
                mirrorNormal = new Vector3(-inverseNormalDirection.x, inverseNormalDirection.y, inverseNormalDirection.z);
            }
            else {
                inverseTransformPoint.z *= -1f;
                mirrorNormal = new Vector3(inverseNormalDirection.x, inverseNormalDirection.y, -inverseNormalDirection.z);
            }

            var mirroredPoint = targetTransform.TransformPoint(inverseTransformPoint);
            mirrorNormal = targetTransform.TransformDirection(mirrorNormal);

            return new MeshProperties {
                sourceNormal = mirrorNormal,
                sourceVertex = mirroredPoint,
                sourceTangent = currentHit.sourceTangent
            };
        }

        private Transform getTargetMirrorTransform(Transform thisTransform) {
            Transform transform;
            return furCreator.FurRenderer.meshBaker.isSkinnedMesh() &&
                   (transform = furCreator.transform).parent != null
                // ReSharper disable once Unity.InefficientPropertyAccess
                ? transform.parent
                : thisTransform;
        }

        private static readonly float OUTLINE_THICKNESS = 1.5f;

        private void drawSceneDisk(MeshProperties raycastHit, SceneDisk disk) {
            var type = furCreator.getPainterProperties().type;
            Handles.color = Color.white;
            Handles.DrawWireDisc(raycastHit.sourceVertex, raycastHit.sourceNormal,
                brushPropertiesUI.getBrushDiscSize(type, painterAddSpacingUI, furCreator.getPainterProperties()), OUTLINE_THICKNESS);
            Handles.DrawWireDisc(raycastHit.sourceVertex, raycastHit.sourceNormal,
                brushPropertiesUI.getFalloffBrushDiscSize(type, furCreator.getPainterProperties()), OUTLINE_THICKNESS);
            disk.setup(
                raycastHit,
                brushPropertiesUI.getBrushDiscSize(type, painterAddSpacingUI, furCreator.getPainterProperties()),
                brushPropertiesUI.getFalloffBrushDiscSize(type, furCreator.getPainterProperties()),
                brushPropertiesUI.getCircleFillColor(furCreator.getPainterProperties())
            );
        }

        private void updateSceneView() {
            if (furCreator != null) {
                furCreator.FurRenderer.drawWindContribution = furCreator.getPainterProperties().type == (int) PaintType.WIND_MAX_DISTANCE;
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }

        void processInputs() {
            if (hasFurCreator()) {
                Event e = Event.current;

                mousePosition = e.mousePosition;

                if (e.rawType == EventType.MouseUp) onMouseUp();

                if (lastMousePosition == mousePosition) isPainting = false;

                if (e.type == EventType.MouseDown) {
                    reset(e.alt);
                    brushPropertiesUI.resetMouseMove(e.button == 0);
                }

                if (!isPaintingDisabledDueToAltDrag) {
                    isChangingBrushValue = brushPropertiesUI.handleBrushShortcuts(e, furCreator.getPainterProperties()) || isChangingBrushValue;
                    handleRegularPainting(e);
                }

                if (e.rawType == EventType.MouseEnterWindow &&
                    !brushPropertiesUI.handleBrushShortcuts(e, furCreator.getPainterProperties())) onMouseUp();
                lastMousePosition = mousePosition;
            }
        }

        private void handleRegularPainting(Event e) {
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && !e.getControlButton() && e.button == 0 && !e.shift) {
                isPainting = true;
                if (e.type == EventType.MouseDown) brushMoveDistance = 0;
            }
        }

        private void onMouseUp() {
            if (previousRayHit != Vector3.zero) furCreator.updateSerializedFurProperties();
            reset(false);
            brushPropertiesUI.isLeftMousePressed = false;
        }

        private void reset(bool eAlt) {
            isChangingBrushValue = false;
            isPainting = false;
            previousRayHit = Vector3.zero;
            previousMirrorRayHit = Vector3.zero;
            isPaintingDisabledDueToAltDrag = eAlt;
            clickStartHitPoint = null;
            mirrorClickStartHitPoint = null;
        }
    }
}