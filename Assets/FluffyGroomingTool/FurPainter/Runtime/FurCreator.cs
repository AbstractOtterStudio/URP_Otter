using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace FluffyGroomingTool {
    [ExecuteInEditMode, RequireComponent(typeof(FurRenderer))]
    public class FurCreator : MonoBehaviour {
        [SerializeField] public GroomContainer groomContainer;

        [SerializeField] public UndoRecorder undoRecorder;
        [SerializeField] public PainterProperties painterProperties;
        public bool IsFirstLoad { get; set; }

        private ComputeShader computeShader;

        public bool IsFurStrandsProgressVisible { get; set; }

        private int paintGroomKernel;
        private int applySmoothingKernel;
        private int floodKernel;
        private AddStrandsCoroutine addStrandsCoroutine;
        private ComputeBuffer smoothIndicesAndFalloffs;
        private ComputeBuffer smoothSumBuffer;
        public bool needsUpdate;

        public GroomLayer getActiveLayer() {
            return groomContainer.getActiveLayer();
        }

        private void Reset() {
            StartCoroutine(addStrandsDelayed());
        }

        IEnumerator addStrandsDelayed() {
            yield return new WaitForSeconds(0.1f); //We need this delay since some member variable are initialized after Object.Instantiate
            if (groomContainer.getActiveLayer().strandsGroomOneToOne.Length == 0) {
                addStrands();
            }
        }

        private void OnValidate() {
#if UNITY_EDITOR
            if (!BuildPipeline.isBuildingPlayer) loadResources();
#endif
        }

        public FurRenderer FurRenderer { get; set; }

        private void loadResources() {
            FurRenderer = gameObject.GetComponent<FurRenderer>();

            if (computeShader == null) {
                computeShader = Instantiate(Resources.Load<ComputeShader>("FurCreatorCompute"));
                paintGroomKernel = computeShader.FindKernel("PaintGroomKernel");
                applySmoothingKernel = computeShader.FindKernel("ApplySmoothingKernel");
                floodKernel = computeShader.FindKernel("FloodKernel");
            }
        }

        public void addClump() {
            groomContainer.getActiveLayer().addClumpModifier();
            addStrands();
        }

        public void removeClumpModifier(int index) {
#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog("Delete The Clump Modifier",
                "This action cannot be undone. Are you sure you want to delete?", "Yes", "No")) {
                StartCoroutine(removeClumpModifierCoroutine(index));
            }
#endif
        }

        private IEnumerator removeClumpModifierCoroutine(int index) {
            yield return null;
            groomContainer.getActiveLayer().removeClumpModifier(index);
            FurRenderer.furContainer.removeClumpModifier(groomContainer.activeLayerIndex, index);
            addStrands();
        }

        public void addLayer() {
            updateSerializedFurProperties();
            groomContainer.addNewLayer();
            addStrands();
        }

        public void deleteLayer(int index) {
#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog("Delete the fur layer",
                "This action cannot be undone. Are you sure you want to delete the fur layer?", "Delete", "Cancel")) {
                StartCoroutine(deleteLayerCoroutine(index));
            }
#endif
        }

        private IEnumerator deleteLayerCoroutine(int index) {
            yield return null;
            updateSerializedFurProperties();
            groomContainer.removeLayer(index);
            FurRenderer.furContainer.removeLayer(index);
            if (index <= groomContainer.activeLayerIndex) setActiveLayerIndex(groomContainer.activeLayerIndex - 1);
            groomContainer.invalidate();
            FurRenderer.furContainer.recreateAll.Invoke();
            needsUpdate = true;
        }

        public void setActiveLayerIndex(int index) {
            updateSerializedFurProperties();
            groomContainer.activeLayerIndex = index;
        }

        public void updateSerializedFurProperties() {
#if UNITY_EDITOR
            if (groomContainer != null && FurRenderer != null) {
                registerUndo();
                undoRecorder.appendUndo(groomContainer);
                groomContainer.copyValuesFromComputeBufferToNativeObject();
                FurRenderer.furContainer.copyValuesFromComputeBufferToNativeObject();
                FurRenderer.furContainer.copyClumpValuesFromComputeBufferToNativeObject();
            }
#endif
        }

        private void registerUndo() {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(this, "Changed config");
#endif
        }

        public void addStrands() {
            loadResources();
            if (needsToAddStrandsAutomatically()) {
                addStrandsCoroutine?.cancel();
                addStrandsCoroutine = new AddStrandsCoroutine {furCreator = this}.start();
            }
        }

        private bool needsToAddStrandsAutomatically() {
            return FurRenderer.meshBaker != null && FurRenderer.furContainer != null;
        }

        private FurMeshCreator furMeshCreator;

        //TODO: This need some love.
        public void addPreviewStrandAtPosition(RaycastHit hit) {
            loadResources();
            if (FurRenderer.meshBaker.sourceMesh) {
                var pointOnMesh = new PointOnMesh {
                    triangleIndex1 = FurRenderer.meshBaker.sourceMesh.triangles[hit.triangleIndex * 3 + 0],
                    triangleIndex2 = FurRenderer.meshBaker.sourceMesh.triangles[hit.triangleIndex * 3 + 1],
                    triangleIndex3 = FurRenderer.meshBaker.sourceMesh.triangles[hit.triangleIndex * 3 + 2]
                };
                pointOnMesh.vertex1 = FurRenderer.meshBaker.sourceMesh.vertices[pointOnMesh.triangleIndex1];
                pointOnMesh.vertex2 = FurRenderer.meshBaker.sourceMesh.vertices[pointOnMesh.triangleIndex2];
                pointOnMesh.vertex3 = FurRenderer.meshBaker.sourceMesh.vertices[pointOnMesh.triangleIndex3];
                pointOnMesh.pos = transform.InverseTransformPoint(hit.point);
                var createdStrand = createPointAtPosition(pointOnMesh, FurRenderer.meshBaker.sourceMesh.uv);
                var hairStrandsList = FurRenderer.furContainer.layerStrandsList[groomContainer.activeLayerIndex].layerHairStrands.ToList();
                hairStrandsList.Add(createdStrand);
                FurRenderer.furContainer.layerStrandsList[groomContainer.activeLayerIndex].layerHairStrands = hairStrandsList.ToArray();
                FurRenderer.furContainer.NeedsUpdate = true;
                FurRenderer.furContainer.recreateAll.Invoke();
            }
        }

        /**
         * This function can be called from a background thread, so it can't access Mesh data. Therefore we have to pass in the UV array instead.
         * UnityEngine Random.Range is also not allowed. 
         */
        public static HairStrand createPointAtPosition(PointOnMesh pointOnMesh, Vector2[] uvs) {
            var baryCentricPoint = pointOnMesh.creatBaryCentricMeshCoordinates();

            var rand = new Vector2(pointOnMesh.triangleIndex2, pointOnMesh.triangleIndex3).rand() * 100f;
            var rand2 = new Vector2(pointOnMesh.triangleIndex1, pointOnMesh.triangleIndex3 * 3).rand() * 100f;

            return new HairStrand {
                barycentricCoordinates = new Vector3(baryCentricPoint.u, baryCentricPoint.v, baryCentricPoint.w),
                uvOffset = new Vector2(rand > 50 ? 0.5f : 0, rand2 > 50 ? 0.5f : 0),
                scaleMatrix = Vector3.one,
                rootAndOrientationMatrix = new Matrix4x4(),
                uv = baryCentricPoint.interpolatedUv(uvs),
                triangles = new Vector3(pointOnMesh.triangleIndex1, pointOnMesh.triangleIndex2, pointOnMesh.triangleIndex3),
                bend = Vector4.one
            };
        }

        private void createComputeBuffers() {
            disposeBuffers();
            if (smoothIndicesAndFalloffs == null) smoothIndicesAndFalloffs = new ComputeBuffer(500000, sizeof(float) * 2, ComputeBufferType.Append);
            if (smoothSumBuffer == null) smoothSumBuffer = new ComputeBuffer(7, sizeof(int), ComputeBufferType.IndirectArguments);
            groomContainer.invalidate();
        }

        private bool shouldRecreateBuffers() {
            return !groomContainer.isFirstLayerStrandBufferInizialized() || smoothIndicesAndFalloffs == null;
        }

        private void disposeBuffers() {
            smoothIndicesAndFalloffs?.Dispose();
            smoothSumBuffer?.Dispose();
            smoothIndicesAndFalloffs = null;
            smoothSumBuffer = null;
            if (groomContainer != null) groomContainer.disposeBuffers();
        }

        private void Update() {
            groomContainer.update();
            updateKernels();
            if (shouldUpdateLayerStrands()) {
                needsUpdate = false;
                updateAllLayerStrands();
            }

            groomContainer.PainterProperties = painterProperties;
        }

        private bool shouldUpdateLayerStrands() {
            return needsUpdate && groomContainer.isFirstLayerStrandBufferInizialized();
        }

        public void updateKernels() {
            if (shouldRecreateBuffers()) createComputeBuffers();
        }

        public void flood() {
            if (painterProperties.isGroomAllLayerAtOnce) {
                for (int index = 0; index < groomContainer.layers.Length; index++) {
                    floodLayerWithIndex(index);
                }
            }
            else {
                floodLayerWithIndex(groomContainer.activeLayerIndex);
            }
        }

        private void floodLayerWithIndex(int index) {
            sendBuffersToKernel(floodKernel, FurRenderer.furContainer.getLayerStrandsBuffer(index), groomContainer.getLayerGroomBuffer(index));
            setLayerCount(index);
            sendTwistValuesToComputeShader();
            computeShader.SetInt("brushMenuType", getPainterProperties().type);
            computeShader.SetFloat("brushIntensity", getPainterProperties().getMagnitudeIntensity());
            computeShader.Dispatch(floodKernel, FurRenderer.furContainer.getLayerStrandsCount(index).toCsGroupsEditor(), 1, 1);

            computeShader.DisableKeyword("IS_HAIR_STRAND");
            var strandLayer = FurRenderer.furContainer.layerStrandsList[index];
            var groomLayer = groomContainer.layers[index];
            for (var i = 0; i < strandLayer.clumpsModifiers.Length; i++) {
                var clumpsModifier = strandLayer.clumpsModifiers[i];
                var clumpGroom = groomLayer.clumpModifiers[i];
                if (clumpsModifier.clumpsBuffer != null && clumpGroom.clumpGroomBuffer != null) {
                    computeShader.SetInt("currentLayerStrandsCount", clumpsModifier.clumpsBuffer.count);
                    sendBuffersToKernel(floodKernel, clumpsModifier.clumpsBuffer, clumpGroom.clumpGroomBuffer);
                    computeShader.Dispatch(floodKernel, clumpsModifier.clumpsBuffer.count.toCsGroupsEditor(), 1, 1);
                }
            }

            computeShader.EnableKeyword("IS_HAIR_STRAND");
        }

        private void setLayerCount(int layerIndex) {
            var currentLayerStrandsCount = FurRenderer.furContainer.getLayerStrandsCount(layerIndex);
            computeShader.SetInt("currentLayerStrandsCount", currentLayerStrandsCount);
        }

        private static readonly MeshProperties ZERO_RAY_HIT = MeshProperties.zero();

        public void updateAllLayerStrands() {
            computeShader.DisableKeyword("PAINT_GROOM");
            var emptyVec = Vector3.zero;
            for (var index = 0; index < groomContainer.layers.Length; index++) {
                if (groomContainer.isFirstLayerStrandBufferInizialized() && FurRenderer.meshBaker?.bakedMesh != null) {
                    doPaintStrandsPass(-1, ZERO_RAY_HIT, emptyVec, emptyVec, false, index);
                    if (!groomContainer.layers[index].isHidden) {
                        doPaintClumpsPass(-1, ZERO_RAY_HIT, emptyVec, emptyVec, false, index);
                    }
                }
            }

            computeShader.EnableKeyword("PAINT_GROOM");
        }

        public void paintGroom(int brushMenuType, MeshProperties mouseHitPoint, Vector3 previousMouseHitPoint, Vector3 clickStartHitPoint,
            bool isSmoothPass) {
            if (painterProperties.isGroomAllLayerAtOnce) {
                for (int i = 0; i < groomContainer.layers.Length; i++) {
                    paintGroomForLayerWithIndex(brushMenuType, mouseHitPoint, previousMouseHitPoint, clickStartHitPoint, isSmoothPass, i);
                }
            }
            else {
                paintGroomForLayerWithIndex(brushMenuType, mouseHitPoint, previousMouseHitPoint, clickStartHitPoint, isSmoothPass,
                    groomContainer.activeLayerIndex);
            }
        }

        private void paintGroomForLayerWithIndex(int brushMenuType, MeshProperties mouseHitPoint, Vector3 previousMouseHitPoint,
            Vector3 clickStartHitPoint,
            bool isSmoothPass, int layerIndex) {
            computeShader.EnableKeyword("PAINT_GROOM");
            if (groomContainer.isFirstLayerStrandBufferInizialized() && FurRenderer.meshBaker.bakedMesh != null) {
                doPaintStrandsPass(brushMenuType, mouseHitPoint, previousMouseHitPoint, clickStartHitPoint, isSmoothPass, layerIndex);
                doPaintClumpsPass(brushMenuType, mouseHitPoint, previousMouseHitPoint, clickStartHitPoint, isSmoothPass, layerIndex);
            }
        }

        private void doPaintStrandsPass(int brushMenuType, MeshProperties mouseHitPoint, Vector3 previousMouseHitPoint, Vector3 clickStartHitPoint,
            bool isSmoothPass, int layerIndex) {
            if (layerIndex >= FurRenderer.furContainer.layerStrandsList.Length) return;
            setLayerCount(layerIndex);
            paintGroomAndUpdateStrands(brushMenuType, mouseHitPoint, previousMouseHitPoint, clickStartHitPoint, isSmoothPass,
                FurRenderer.furContainer.getLayerStrandsBuffer(layerIndex), groomContainer.getLayerGroomBuffer(layerIndex), layerIndex);
        }

        private void doPaintClumpsPass(int brushMenuType, MeshProperties mouseHitPoint, Vector3 previousMouseHitPoint, Vector3 clickStartHitPoint,
            bool isSmoothPass, int layerIndex) {
            if (layerIndex < FurRenderer.furContainer.layerStrandsList.Length) {
                computeShader.DisableKeyword("IS_HAIR_STRAND");
                var strandLayer = FurRenderer.furContainer.layerStrandsList[layerIndex];
                var groomLayer = groomContainer.layers[layerIndex];
                for (var i = 0; i < strandLayer.clumpsModifiers.Length; i++) {
                    var clumpsModifier = strandLayer.clumpsModifiers[i];
                    var clumpGroom = groomLayer.clumpModifiers[i];
                    if (clumpsModifier.clumpsBuffer != null && clumpGroom.clumpGroomBuffer != null) {
                        computeShader.SetInt("currentLayerStrandsCount", clumpsModifier.clumpsBuffer.count);
                        paintGroomAndUpdateStrands(brushMenuType, mouseHitPoint, previousMouseHitPoint, clickStartHitPoint, isSmoothPass,
                            clumpsModifier.clumpsBuffer, clumpGroom.clumpGroomBuffer, layerIndex);
                    }
                }

                computeShader.EnableKeyword("IS_HAIR_STRAND");
            }
        }

        private void paintGroomAndUpdateStrands(int brushMenuType, MeshProperties mouseHitPoint, Vector3 previousMouseHitPoint,
            Vector3 clickStartHitPoint, bool isSmoothPass, ComputeBuffer hairStrandsBuffer, ComputeBuffer strandsGroomBuffer, int layerIndex) {
            if (strandsGroomBuffer == null) return;
            smoothIndicesAndFalloffs.SetCounterValue(0);
            sendBuffersToKernel(paintGroomKernel, hairStrandsBuffer, strandsGroomBuffer);
            computeShader.SetBuffer(paintGroomKernel, "smoothIndicesAndFalloffs", smoothIndicesAndFalloffs);
            sendCommonValuesToComputeShader(brushMenuType, mouseHitPoint, previousMouseHitPoint, clickStartHitPoint);
            sendLayerValuesToComputeShader(layerIndex);

            if (isSmoothPass) smoothSumBuffer.SetData(new int[7]);
            computeShader.Dispatch(paintGroomKernel, hairStrandsBuffer.count.toCsGroupsEditor(), 1, 1);

            if (isSmoothPass) {
                int[] args = new int[7];
                smoothSumBuffer.GetData(args);
                var count = args[0];
                //Something weird is happening here in some occasions count is some ridiculous large value. So for now we do this. Bug with interlockedAdd?    
                if (count < 1000000) {
                    sendBuffersToKernel(applySmoothingKernel, hairStrandsBuffer, strandsGroomBuffer);
                    computeShader.SetBuffer(applySmoothingKernel, "smoothIndicesAndFalloffsRead", smoothIndicesAndFalloffs);
                    computeShader.Dispatch(applySmoothingKernel, count.toCsGroupsEditor(), 1, 1);
                }
            }
        }

        private void sendLayerValuesToComputeShader(int layerIndex) {
            GroomLayer layer = groomContainer.layers[layerIndex];
            computeShader.SetFloat("worldScale", groomContainer.worldScale);
            computeShader.SetFloat("minScaleHeight", layer.minHeight);
            computeShader.SetFloat("maxScaleHeight", layer.maxHeight);
            computeShader.SetFloat("minScaleWidth", getInterpolatedWidth(layer.minWidth));
            computeShader.SetFloat("maxScaleWidth", getInterpolatedWidth(layer.maxWidth));
            computeShader.SetFloat("randomRotationMultiplier", layer.randomRotation);
            computeShader.SetFloat("randomHeightMultiplier", layer.randomHeight * layer.maxHeight);
            computeShader.SetBool("isLayerHidden", layer.isHidden);
        }

        public static float getInterpolatedWidth(float value, float threshold = 0.1f, float multiplier = 15f) {
            if (value > threshold) {
                return threshold + (value - threshold) * multiplier;
            }

            return value;
        }

        private void sendCommonValuesToComputeShader(int brushMenuType, MeshProperties mouseHitPoint, Vector3 previousMouseHitPoint,
            Vector3 clickStartHitPoint) {
            computeShader.SetInt("brushMenuType", brushMenuType);
            computeShader.SetVector("mouseHitPoint", mouseHitPoint.sourceVertex);
            computeShader.SetVector("mouseHitNormal", mouseHitPoint.sourceNormal);
            computeShader.SetVector("previousMouseHitPoint", previousMouseHitPoint);
            computeShader.SetVector("clickStartHitPoint", clickStartHitPoint);
            computeShader.SetBool("drawWindContribution", FurRenderer.drawWindContribution);
            computeShader.SetMatrix("localToWorldMatrix", transform.localToWorldMatrix);
            computeShader.SetMatrix("worldToLocalMatrix", transform.worldToLocalMatrix);
            computeShader.SetMatrix("localToWorldRotationMatrix", Matrix4x4.Rotate(transform.rotation));

            computeShader.SetFloat("brushSize", getPainterProperties().brushSize);
            computeShader.SetFloat("brushFalloff", getPainterProperties().brushFalloff);
            computeShader.SetFloat("brushOpacity", getPainterProperties().brushOpacity);
            computeShader.SetBool("ignoreNormal", getPainterProperties().isNormalIgnored);
            sendTwistValuesToComputeShader();
            computeShader.SetVector("overrideColor", getPainterProperties().overrideColor);
            computeShader.SetFloat("overrideIntensity", getPainterProperties().overrideIntensity);
            computeShader.SetFloat("brushIntensity", getPainterProperties().getMagnitudeIntensity());
            computeShader.SetFloat("maskErase", getPainterProperties().maskErase ? 1 : 0);
            computeShader.SetVector("resetValues", getPainterProperties().getResetValuesAsVector());
        }

        private void sendTwistValuesToComputeShader() {
            computeShader.SetBool("isClumpTwistSelected", getPainterProperties().isClumpTwistSelected);
            computeShader.SetFloat("twistSpread", getPainterProperties().twistSpread);
            computeShader.SetFloat("twistAmount", getPainterProperties().twistAmount);
        }

        private void sendBuffersToKernel(int kernel, ComputeBuffer hairStrandsBuffer, ComputeBuffer strandsGroomBuffer) {
            computeShader.SetBuffer(kernel, "smoothSumBuffer", smoothSumBuffer);
            computeShader.SetBuffer(kernel, "strandProperties", hairStrandsBuffer);
            computeShader.SetBuffer(kernel, "strandGroomsBuffer", strandsGroomBuffer);
            computeShader.SetBuffer(kernel, "sourceMesh", FurRenderer.meshBaker.bakedMesh);
        }

        private void OnEnable() {
#if UNITY_EDITOR
            if (groomContainer == null) {
                IsFirstLoad = true;
            }

            if (groomContainer == null) groomContainer = ScriptableObject.CreateInstance<GroomContainer>();
            if (painterProperties == null) painterProperties = ScriptableObject.CreateInstance<PainterProperties>();
            Undo.undoRedoPerformed += pushUndo;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            if (!BuildPipeline.isBuildingPlayer) {
                loadResources();
                FurRenderer.UpdatedInEditModeAction += updateAllLayerStrands;
            }
            else {
                ErrorLogger.logRemoveFurCreator();
            }
#endif
        }

        public void pushUndo() {
            groomContainer.invalidate();
            needsUpdate = true;
            undoRecorder.undoCallback(groomContainer);
        }

        private void OnDisable() {
#if UNITY_EDITOR
            if (!BuildPipeline.isBuildingPlayer) {
                Undo.undoRedoPerformed -= pushUndo;
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                updateSerializedFurProperties();
                FurRenderer.UpdatedInEditModeAction -= updateAllLayerStrands;
            }
#endif
            disposeBuffers();
        }


        public void cancelMeshCreation() {
            furMeshCreator = null;
        }

        public void createMesh() {
            furMeshCreator = new FurMeshCreator();
            furMeshCreator.createMesh(this);
        }

        public PainterProperties getPainterProperties() {
            return painterProperties;
        }

        public void setCardPreset(string postfix, bool isAlpha) {
            groomContainer.isUsingCardPreset = true;
            groomContainer.getActiveLayer().setCardPreset();
            FurRenderer.materialPostfix = postfix;
            FurRenderer.furRendererSettings.enableLod = false;
            FurRenderer.recreateMaterial();
            FurRenderer.furRendererSettings.isAlphaSortingEnabled = isAlpha;
            FurRenderer.furRendererSettings.isFrustumCullingEnabled = isAlpha;
        }

        public void permanentlyDeleteMaskedStrands(UnityAction endAction = null) {
#if UNITY_EDITOR
            if (groomContainer.hasHiddenLayers()) {
                EditorUtility.DisplayDialog(
                    "Hidden Layers",
                    "This operation requires all layers to be visible. Please show the layers or delete them.",
                    "Ok"
                );
                return;
            }

            if (permanentlyDeleteMaskedStrandsC == null) {
                if (permanentlyDeleteMaskedStrandsC != null) StopCoroutine(permanentlyDeleteMaskedStrandsC);
                permanentlyDeleteMaskedStrandsC = permanentlyDeleteMaskedStrandsCoroutine(endAction);
                StartCoroutine(permanentlyDeleteMaskedStrandsC);
                showToastMessage?.Invoke("Masked strands where deleted and the FurContainer has been optimized.");
            }
#endif
        }
#if UNITY_EDITOR
        private IEnumerator permanentlyDeleteMaskedStrandsC;
#endif
        public UnityAction<String> showToastMessage;

        private IEnumerator permanentlyDeleteMaskedStrandsCoroutine(UnityAction endAction = null) {
            FurRenderer.furContainer.copyValuesFromComputeBufferToNativeObject();
            groomContainer.copyValuesFromComputeBufferToNativeObject();
            yield return new WaitForSeconds(1f); //Give the async copyFromNativeFunctions some time to finish. 

            for (var layerIndex = 0; layerIndex < groomContainer.layers.Length; layerIndex++) {
                var layerStrands = FurRenderer.furContainer.layerStrandsList[layerIndex].layerHairStrands;
                var groomStrands = groomContainer.layers[layerIndex].strandsGroomOneToOne;
                var unMaskedGrooms = new List<StrandGroom>();
                var unMaskedStrands = new List<HairStrand>();

                var maskedGrooms = groomContainer.layers[layerIndex].getPermanentlyDeletedGrooms();
                var maskedStrands = groomContainer.layers[layerIndex].getPermanentlyDeletedStrands();

                for (var i = 0; i < layerStrands.Length; i++) {
                    var strand = layerStrands[i];
                    if (strand.scaleMatrix.x > 0) {
                        unMaskedGrooms.Add(groomStrands[i]);
                        unMaskedStrands.Add(strand);
                    }
                    else {
                        maskedGrooms.Add(groomStrands[i]);
                        maskedStrands.Add(strand);
                    }
                }

                FurRenderer.furContainer.layerStrandsList[layerIndex].layerHairStrands = unMaskedStrands.ToArray();
                groomContainer.layers[layerIndex].strandsGroomOneToOne = unMaskedGrooms.ToArray();
                groomContainer.layers[layerIndex].permanentlyDeletedGrooms = maskedGrooms.ToArray();
                groomContainer.layers[layerIndex].permanentlyDeletedStrands = maskedStrands.ToArray();
            }

            groomContainer.invalidate();
            FurRenderer.furContainer.recreateAll.Invoke();
            needsUpdate = true;
            yield return new WaitForSeconds(0.2f);
            endAction?.Invoke();
#if UNITY_EDITOR
            permanentlyDeleteMaskedStrandsC = null;
#endif
        }

        public void hideOrShowLayer(int index) {
            groomContainer.layers[index].isHidden = !groomContainer.layers[index].isHidden;
            updateAllLayerStrands();
#if UNITY_EDITOR
            groomContainer.setDirty();
            FurRenderer.furContainer.copyValuesFromComputeBufferToNativeObject();
#endif
        }

        public void duplicateLayer(int index) {
#if UNITY_EDITOR
            registerUndo();
            groomContainer.setDirty();
#endif
            undoRecorder.appendUndo(groomContainer);
            copyBuffersToNative();
            StartCoroutine(createLayerDuplicate(index));
        }

        public void copyBuffersToNative() {
            groomContainer.copyValuesFromComputeBufferToNativeObject();
            FurRenderer.furContainer.copyValuesFromComputeBufferToNativeObject();
            FurRenderer.furContainer.copyClumpValuesFromComputeBufferToNativeObject();
        }

        IEnumerator createLayerDuplicate(int index) {
            showToastMessage?.Invoke("Layer Duplicated. You may need to groom to see the new Layer!");
            yield return new WaitForSeconds(0.5f);
            groomContainer.duplicateLayer(index);
            FurRenderer.furContainer.duplicateLayer(index);
            groomContainer.invalidate();
            groomContainer.getActiveLayer().clearPermanentlyDeletedBackup();
            FurRenderer.furContainer.recreateAll.Invoke();
            needsUpdate = true;
        }

        private IEnumerator copyToNativeDelayed;

        public void copyBuffersToNativeDelayed() {
            if (copyToNativeDelayed != null) {
                StopCoroutine(copyToNativeDelayed);
            }

            copyToNativeDelayed = copyBuffersToNativeDelayedCoroutine();
            StartCoroutine(copyToNativeDelayed);
        }

        IEnumerator copyBuffersToNativeDelayedCoroutine() {
            yield return new WaitForSeconds(1f);
            copyBuffersToNative();
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(gameObject, "Copy Fluffy To Native");
#endif
        }
    }
}