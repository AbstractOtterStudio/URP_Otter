using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace FluffyGroomingTool {
    [ExecuteAlways, RequireComponent(typeof(Renderer))]
    public class FurRenderer : MonoBehaviour {
        [Tooltip(
            "The distance to this camera will be used to calculate the LOD. If this is not set the Camera with the MainCamera tag will be used.")]
        public Camera lodCamera;

        public Material material;
        private Material windMaterial, motionVectorMaterial;
        public bool drawWindContribution;
        public ComputeShader computeShader;
        public FurContainer furContainer;
        public FurRendererSettings furRendererSettings;
        public bool motionVectors = true;
        public HeadersExpanded headerExpanded = new HeadersExpanded();
        public Renderer CurrentRenderer { get; set; }

        internal readonly FluffyRenderersController renderersController = new FluffyRenderersController();
        private ComputeBuffer emptyFloatBuffer;
        public MeshBaker meshBaker;
        private SDFColliderCommon sdfColliderCommon;
        private VerletSimulation verletSimulation;

        private int verticesCount, updateFurMeshKernel, updateClumpsKernel;

        public bool IsCreateMeshPass { get; set; }

        internal int currentFurContainerID;
        [SerializeField] int instanceID;

        public string materialPostfix = "Strands";


        public bool isHdrp, isUrp;

        //Only needed for the editor UI
        public string activeLodCameraName;
        public string activeLodName;

        private void OnValidate() {
            if (furContainer == null) {
                furContainer = ScriptableObject.CreateInstance<FurContainer>();
                currentFurContainerID = furContainer.id;
            }

            addRecreateAllListener();
#if UNITY_EDITOR
            if (isActiveAndEnabled && instanceID != 0 && instanceID != GetInstanceID()) {
                StartCoroutine(recreateOnObjectDuplication());
            }

            instanceID = GetInstanceID();
#endif
        }


        private IEnumerator recreateOnObjectDuplication() {
            yield return new WaitForEndOfFrame();
            renderersController.clearObjects();
            recreateAll();
        }

        private void addRecreateAllListener() {
            if (furContainer != null) {
                furContainer.recreateAll.RemoveListener(recreateAll);
                furContainer.recreateAll.AddListener(recreateAll);
            }
        }

        void OnEnable() {
#if UNITY_EDITOR
            addRecreateAllListener();
            if (Application.isPlaying && !ColliderHelper.collidersAssigned(sphereColliders, capsuleColliders)) {
                ErrorLogger.logNoColliders();
            }

            ErrorLogger.logMeshDistortionUponBuilds();
            DuplicateCleaner.checkDuplicates();
#endif
            RenderPipelineManager.beginFrameRendering += beginFrameRendering;
            Camera.onPreRender += cameraPreRender;
            initialize();
        }

        public CullAndSortController cullAndSortController;

        private void initialize() {
#if UNITY_EDITOR
            updateInEditModeTimeStamp = EditorApplication.timeSinceStartup + 3f;
#endif
            if (furRendererSettings == null) {
                furRendererSettings = new FurRendererSettings();
                furRendererSettings.verletSimulationSettings = new VerletSimulationSettings();
            }

            meshBaker = new MeshBaker(gameObject, Instantiate(Resources.Load<ComputeShader>(ShaderID.MESH_BAKER_CS_NAME)));
            ErrorLogger.checkGpuSkinning(meshBaker, this);
            sdfColliderCommon = new SDFColliderCommon(GetComponent<Renderer>(), meshBaker, furRendererSettings.verletSimulationSettings);

            loadDefaultMaterial();

            if (furContainer != null) {
                furContainer.recreateCardMeshes();
                computeShader = Instantiate(Resources.Load<ComputeShader>(ShaderID.FUR_RENDERER_CS_NAME));
                verticesCount = furContainer.getCombinedVerticesCount();
                verletSimulation = new VerletSimulation(verticesCount, furRendererSettings);
                checkAlphaSortingPerformaceWarning();
                currentFurContainerID = furContainer.id;
                furContainer.NeedsUpdate = true;
                updateFurMeshKernel = computeShader.FindKernel(ShaderID.UPDATE_FUR_MESH_KERNEL);
                updateClumpsKernel = computeShader.FindKernel(ShaderID.UPDATE_CLUMPS_POSITION_KERNEL);
                furContainer.recreateHairStrandsBuffers();
                furContainer.update();
                renderersController.createRendererObject(
                    isHdrp,
                    isUrp,
                    motionVectorMaterial,
                    verticesCount,
                    furContainer.TriangleIndexArray,
                    StrandRenderType.TRIANGLE_MESH
                );
                var furMeshBufferStride = renderersController.getVertexBufferStride();
                computeShader.SetInt(ShaderID.FUR_MESH_BUFFER_STRIDE, furMeshBufferStride);
                computeShader.SetBuffer(updateFurMeshKernel, ShaderID.FUR_MESH_BUFFER, renderersController.hairMeshBuffer);
                verletSimulation.setFurMeshBuffer(renderersController.hairMeshBuffer, furMeshBufferStride);
                createCullAndSortController();
/*
 * Without this the shader complains about the ComputeBuffers not being set even though they aren't used.
 * So instead of adding an extra pragma keyword for each clump layer, we set an empty buffer instead.
 */
                emptyFloatBuffer = new ComputeBuffer(1, sizeof(float) * 4);
                computeShader.SetBuffer(updateFurMeshKernel, ShaderID.CLUMP_POINTS_POSITION, emptyFloatBuffer);
                ColliderHelper.setupCollidersBuffer(ref colliderBuffer, ref collidersStruct, sphereColliders, capsuleColliders);
            }
        }

        private void loadDefaultMaterial() {
            DefaultMaterialLoader.loadDefaultMaterial(out isHdrp, out isUrp, ref material, out windMaterial, ref motionVectorMaterial,
                materialPostfix, CurrentRenderer ? CurrentRenderer : GetComponent<Renderer>());
        }


        private const int PerformanceWarningPolyCount = 128000;

        private void checkAlphaSortingPerformaceWarning() {
            if (furRendererSettings.isAlphaSortingEnabled && verticesCount > PerformanceWarningPolyCount) {
                Debug.LogWarning("This FurRenderer has a high polygon count and ALPHA SORTING enabled. This is a potential performance bottleneck.");
            }
        }

        private void createCullAndSortController() {
            if (renderersController.hairMesh.triangles.Length > 0) {
                var indexBuffer = renderersController.hairMesh.GetIndexBuffer();
                cullAndSortController = new CullAndSortController(
                    renderersController.getVertexBufferStride(),
                    indexBuffer,
                    renderersController.hairMeshBuffer,
                    getTrianglesCount(),
                    furRendererSettings
                );
            }
        }

        public void recreateMaterial() {
            material = null;
            loadDefaultMaterial();
        }


        void OnDisable() {
            furContainer.recreateAll.RemoveListener(recreateAll);
            RenderPipelineManager.beginFrameRendering -= beginFrameRendering;
            Camera.onPreRender -= cameraPreRender;
            clearResources();
        }

        private void clearResources() {
            meshBaker?.dispose();
            sdfColliderCommon?.dispose();
            verletSimulation?.dispose();
            cullAndSortController?.dispose();
            emptyFloatBuffer?.Dispose();
            colliderBuffer?.Dispose();
            renderersController.destroy();
            if (furContainer != null) furContainer.disposeBuffers();
            colliderBuffer = null;
        }

        private void beginFrameRendering(ScriptableRenderContext rc, Camera[] cameras) {
            if (Application.isPlaying) {
                updateAndRenderFur();
            }

            forceUpdateOnStart();
        }


        private void cameraPreRender(Camera cam) {
            if (lodCamera.getCamera() == cam && Application.isPlaying) {
                updateAndRenderFur();
            }

            forceUpdateOnStart();
        }

        //In edit mode we update the fur using lateupdate to avoid flickering
        private void LateUpdate() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                updateAndRenderFur();
            }
#endif
            if (isReadyToRender() && !cullAndSortController.IsCulled) {
                if (furRendererSettings.isAlphaSortingEnabled) {
                    var rp = new RenderParams(material) {
                        worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000000),
                        shadowCastingMode = ShadowCastingMode.TwoSided,
                        receiveShadows = true,
                        motionVectorMode = MotionVectorGenerationMode.Camera,
                        layer = gameObject.layer
                    };
                    Graphics.RenderMeshIndirect(rp, renderersController.hairMesh, cullAndSortController.RenderMeshArguments);
                    renderersController.disableRenderer();
                } else {
                    renderersController.enableRenderer();
                }
            } else if (cullAndSortController?.IsCulled == true) {
                renderersController.disableRenderer();
            }
        }


        internal void updateAndRenderFur() {
            checkForNewFurContainer();

            var mainCamera = lodCamera.getCamera();
            if (mainCamera == null) {
                ErrorLogger.logNoCamera();
            } else if (isReadyToRender() && isNotPaused() && !cullAndSortController.IsCulled) {
                meshBaker.bakeSkinnedMesh(IsCreateMeshPass);
                if (furRendererSettings.verletSimulationSettings.isVerletColliderEnabled() && atLeastOneLayerHasMotionEnabled()) {
                    sdfColliderCommon.createSDF(transform, verletSimulation.compute, verletSimulation.verletKernel);
                }

                furContainer.update();
                passCommonValuesToCs();
                furContainer.dispatchClumpsKernel(computeShader, updateClumpsKernel, this);
                dispatchStrandsKernel();
                renderersController.setupRenderers(IsCreateMeshPass, getMaterial(), transform.position, CurrentRenderer, isUrp, motionVectors);
                collideWithOtherSDFColliders();
            }


            activeLodCameraName = mainCamera ? mainCamera.name : "";
            activeLodName = cullAndSortController?.getCurrentLodName();
            cullAndSortController?.update(mainCamera, furContainer, meshBaker.getObjectPosition());

            forceUpdateOnStart();
        }


        private bool atLeastOneLayerHasMotionEnabled() {
            if (!furRendererSettings.perLayerMotionSettings) {
                return furRendererSettings.verletSimulationSettings.enableMovement;
            }

            foreach (var layer in furContainer.layerStrandsList) {
                if (layer.verletSimulationSettings.enableMovement) {
                    return true;
                }
            }

            return false;
        }

        private void collideWithOtherSDFColliders() {
            if (furRendererSettings.verletSimulationSettings.isSDFCollisionEnabled() && sdfColliders != null) {
                foreach (var sdfCollider in sdfColliders) {
                    if (sdfCollider != null) {
                        sdfCollider.collideWith(verletSimulation.verletNodesBuffer, renderersController.getRendererBounds(),
                            renderersController.hairMeshBuffer,
                            renderersController.getVertexBufferStride());
                    }
                }
            }
        }

        private static bool isNotPaused() {
            return Time.deltaTime > 0f;
        }

        private void passCommonValuesToCs() {
            if (computeShader == null) furContainer.recreateAll.Invoke();
            var thisTransform = transform;
            var localToWorldMatrix = thisTransform.localToWorldMatrix;

            var objectRotationMatrix = Matrix4x4.Rotate(thisTransform.rotation);
            if (IsCreateMeshPass) {
                localToWorldMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                objectRotationMatrix = Matrix4x4.Rotate(Quaternion.identity);
            }

            computeShader.SetBuffer(updateFurMeshKernel, ShaderID.SOURCE_MESH, meshBaker.bakedMesh);
            computeShader.SetMatrix(ShaderID.LOCAL_TO_WORLD_MATRIX, localToWorldMatrix);
            computeShader.SetMatrix(ShaderID.OBJECT_ROTATION_MATRIX, objectRotationMatrix);
            computeShader.SetFloat(ShaderID.DELTA_TIME, Time.deltaTime);
            var lossyScale = transform.lossyScale;
            computeShader.SetFloat(ShaderID.OBJECT_GLOBAL_SCALE, lossyScale.getValueFurthestFromOne());

            computeShader.SetFloat(ShaderID.EXTRA_SCALE, furContainer.furLods[cullAndSortController.getCurrentLodIndex()].strandsScale);
            verletSimulation.setupWind(this);
            ColliderHelper.setupColliderProperties(ref colliderBuffer, ref collidersStruct, sphereColliders, capsuleColliders,
                verletSimulation.compute, verletSimulation.getKernel());
        }

        private float getNormalPercent(HairStrandLayer layer) {
            if (furRendererSettings.perLayerNormals) return layer.sourceMeshNormalToStrandNormalPercent;
            return furRendererSettings.sourceMeshNormalToStrandNormalPercent;
        }

        private void dispatchStrandsKernel() {
            int layerVertexStartIndex = 0;
            foreach (var layer in furContainer.layerStrandsList) {
                computeShader.DisableKeyword(ShaderID.HAS_CLUMPS);
                if (layer.hasClumps() && layer.getLastClumpModifierBuffer() != null) {
                    computeShader.SetBuffer(updateFurMeshKernel, ShaderID.CLUMP_POINTS_POSITION, layer.getLastClumpModifierBuffer());
                    computeShader.SetBuffer(updateFurMeshKernel, ShaderID.CLUMP_ATTRACTION_CURVE, layer.getLastClumpAttractionBuffer());
                    computeShader.SetInt(ShaderID.CLUMP_Y_COORDINATES, layer.cardMeshProperties.getCardMeshVerticesY());
                    computeShader.EnableKeyword(ShaderID.HAS_CLUMPS);
                }

                layer.passCardMeshPropertiesToComputeShader(updateFurMeshKernel, computeShader);
                var layerVerticesCount = layer.hairStrandsBuffer.count * layer.CardMesh.vertexCount;
                computeShader.SetInt(ShaderID.LAYER_VERTEX_START_INDEX, layerVertexStartIndex);
                computeShader.SetInt(ShaderID.LAYER_VERTICES_COUNT, layerVerticesCount);

                var normalPercent = getNormalPercent(layer);
                computeShader.SetFloat(ShaderID.SOURCE_MESH_NORMAL_TO_STRAND_NORMAL_PERCENT, normalPercent);
                var currentVerletSettings = getCurrentVerletSettings(layer);
                verletSimulation.setupVerlet(computeShader, updateFurMeshKernel, currentVerletSettings);
                computeShader.Dispatch(updateFurMeshKernel, layerVerticesCount.toCsGroups(), 1, 1);
                verletSimulation.update(layer.hairStrandsBuffer.count, layer.cardMeshProperties.getCardMeshVerticesY(), layerVertexStartIndex / 2,
                    lodCamera.getCamera().transform.position, currentVerletSettings, normalPercent);
                layerVertexStartIndex += layerVerticesCount;
            }
        }

        private VerletSimulationSettings getCurrentVerletSettings(HairStrandLayer layer) {
            if (furRendererSettings.perLayerMotionSettings) return layer.verletSimulationSettings;
            return furRendererSettings.verletSimulationSettings;
        }

        private void checkForNewFurContainer() {
            if (currentFurContainerID != furContainer.id) {
                var existingFurCreator = GetComponent<FurCreator>();
                if (existingFurCreator != null) {
                    DestroyImmediate(existingFurCreator);
                }

                clearResources();
                initialize();
            }
        }

        private double updateInEditModeTimeStamp;
        public UnityAction UpdatedInEditModeAction { get; set; }

        void forceUpdateOnStart() {
#if UNITY_EDITOR
            if (!Application.isPlaying && EditorApplication.timeSinceStartup < updateInEditModeTimeStamp) {
                EditorApplication.QueuePlayerLoopUpdate();
                UpdatedInEditModeAction?.Invoke();
            }
#endif
        }

        public bool isReadyToRender() {
            if (CurrentRenderer == null) {
                CurrentRenderer = GetComponent<Renderer>();
                //renderer.isVisible will always return false on the first frame so we ignore it here.
                return areResourcesReadyToRender();
            }

            return (CurrentRenderer.isVisible || IsCreateMeshPass) && areResourcesReadyToRender();
        }

        private bool areResourcesReadyToRender() {
            return furContainer != null && furContainer.layerStrandsList.Length > 0 && furRendererSettings != null &&
                   renderersController.isReady() && cullAndSortController != null;
        }

        private Material getMaterial() {
            return drawWindContribution ? windMaterial : material;
        }

        private void recreateAll() {
            clearResources();
            initialize();
        }

        public int getTrianglesCount() {
            if (furContainer.TriangleIndexArray == null) return 0;
            return (int)(furContainer.TriangleIndexArray.Length / 3f);
        }

        public int getVerticesCount() {
            return verticesCount;
        }

        public SphereCollider[] sphereColliders;
        public CapsuleCollider[] capsuleColliders;
        public SDFCollider[] sdfColliders;
        private ComputeBuffer colliderBuffer;
        private ColliderStruct[] collidersStruct;


        public void updateLod() {
            cullAndSortController?.update(lodCamera.getCamera(), furContainer, meshBaker.getObjectPosition());
        }

        public void recreateSdfCollider() {
            sdfColliderCommon?.dispose();
            sdfColliderCommon = new SDFColliderCommon(GetComponent<Renderer>(), meshBaker, furRendererSettings.verletSimulationSettings);
        }
    }
}