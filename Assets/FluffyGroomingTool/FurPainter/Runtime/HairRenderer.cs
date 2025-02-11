using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyGroomingTool {
    [ExecuteAlways]
    public class HairRenderer : MonoBehaviour {
        public HairContainer hairContainer;
        public Camera lodCamera;
        public Material material;
        public FurRendererSettings settings;
        public SphereCollider[] sphereColliders;
        public CapsuleCollider[] capsuleColliders;
        public SDFCollider[] sdfColliders; //TODO

        public bool motionVectors = true;

        private ComputeBuffer hairStrandPointsBuffer, verletNodesBuffer, strandShapeBuffer;
        private ComputeShader hairRendererCompute;
        private int allInOneKernel;
        private Material motionVectorMaterial;
        private MeshBaker meshBaker;

        private SDFColliderCommon sdfColliderCommon;
        private ComputeBuffer colliderBuffer;
        private ColliderStruct[] collidersStruct;
        internal readonly FluffyRenderersController fluffyRenderersController = new FluffyRenderersController();
        public bool isUrp;
        public HeadersExpanded headerExpanded = new HeadersExpanded();
        public bool enableHqLineRendering;
        public CullSettings cullSettings = new CullSettings();
        public string currentCullingCameraName;

        private void OnEnable() { 
            if (Application.isPlaying && !ColliderHelper.collidersAssigned(sphereColliders, capsuleColliders)) {
                ErrorLogger.logNoColliders();
            }

            RenderPipelineManager.beginContextRendering += beginFrameRendering;
            Camera.onPreRender += cameraPreRender;
            DuplicateCleaner.checkDuplicates();
        }

        internal int hairContainerID;

        private void checkForHairContainer() {
            if (hairContainer != null && hairContainerID != hairContainer.id) {
                var existingFurCreator = GetComponent<FurCreator>();
                if (existingFurCreator != null) {
                    DestroyImmediate(existingFurCreator);
                }

                clearResources();
                initialize();
            }
        }

        private void checkForChangedTopology() {
            if (enableHqLineRendering && strandRenderType != StrandRenderType.LINE) {
                strandRenderType = StrandRenderType.LINE;
            }

            if (strandRenderType != fluffyRenderersController.currentStrandRenderType) {
                var existingFurCreator = GetComponent<FurCreator>();
                if (existingFurCreator != null) {
                    DestroyImmediate(existingFurCreator);
                }

                clearResources();
                initialize();
            }
        }

        private void loadDefaultMaterial() {
            CurrentRenderer = GetComponent<Renderer>();

            DefaultMaterialLoader.loadDefaultMaterial(
                out _, out isUrp, ref material, out _, ref motionVectorMaterial, "Strands", CurrentRenderer, true
            );
        }

        public StrandRenderType strandRenderType = StrandRenderType.TRIANGLE_MESH;

        private void initialize() {
            if (hairContainer != null) {
                isInitializing = true;
                if (settings == null) {
                    settings = new FurRendererSettings {
                        verletSimulationSettings = new VerletSimulationSettings(),
                        sourceMeshNormalToStrandNormalPercent = 0.9f
                    };
                }

                hairContainerID = hairContainer.id;
                if (hairContainer.isSkinned) {
                    meshBaker = new MeshBaker(gameObject, Instantiate(Resources.Load<ComputeShader>(ShaderID.MESH_BAKER_CS_NAME)));
                    sdfColliderCommon = new SDFColliderCommon(GetComponent<Renderer>(), meshBaker, settings.verletSimulationSettings);
                }

                loadDefaultMaterial();
                var pointsCount = hairContainer.hairStrandPoints.Length;
                var verticesCount = pointsCount * (strandRenderType != StrandRenderType.LINE ? 2 : 1);
                verletNodesBuffer = new ComputeBuffer(
                    hairContainer.hairStrandPoints.Length,
                    sizeof(float) * 15 + sizeof(int),
                    ComputeBufferType.Default
                );


                hairRendererCompute = Instantiate(Resources.Load<ComputeShader>("HairRenderer"));

                allInOneKernel = hairRendererCompute.FindKernel("AllInOneKernel");
                hairRendererCompute.SetInt("strandPointsCount", hairContainer.pointsPerStrand);
                ColliderHelper.setupCollidersBuffer(ref colliderBuffer, ref collidersStruct, sphereColliders, capsuleColliders);
                var cardSubDivisionsYTriangle = hairContainer.pointsPerStrand - 1;
                var cardSubDivisionsLine = (hairContainer.pointsPerStrand - 1) * 2;

                var isLine = strandRenderType == StrandRenderType.LINE;
                var strandsCount = pointsCount / hairContainer.pointsPerStrand;
                var triIndices = new int[(isLine ? cardSubDivisionsLine : cardSubDivisionsYTriangle * 6) * strandsCount];

                fluffyRenderersController.createRendererObject(
                    DefaultMaterialLoader.isHdrp(),
                    isUrp,
                    motionVectorMaterial,
                    verticesCount,
                    triIndices,
                    strandRenderType
                );
                var furMeshBufferStride = fluffyRenderersController.getVertexBufferStride();
                hairRendererCompute.SetInt(ShaderID.FUR_MESH_BUFFER_STRIDE, furMeshBufferStride);
                hairRendererCompute.SetBuffer(allInOneKernel, ShaderID.FUR_MESH_BUFFER, fluffyRenderersController.hairMeshBuffer);

                createHairStrandPointsBuffer();

                hairRendererCompute.EnableKeyword("INITIALIZE_VERLET_NODES");
                if (strandRenderType != StrandRenderType.LINE) {
                    strandShapeBuffer = hairContainer.createShapeBuffer(null);
                }

                var initializeIndicesKernel =
                    hairRendererCompute.FindKernel(isLine ? "LinesIndicesKernel" : "TriangleIndicesKernel");
                hairRendererCompute.SetInt(ShaderID.FUR_MESH_BUFFER_STRIDE, furMeshBufferStride);
                var triangleIndexBuffer = fluffyRenderersController.hairMesh.GetIndexBuffer();
                hairRendererCompute.SetBuffer(initializeIndicesKernel, ShaderID.MESH_INDEX_BUFFER, triangleIndexBuffer);
                hairRendererCompute.SetInt("cardSubdivisionsY", isLine ? cardSubDivisionsLine : cardSubDivisionsYTriangle);

                if (isLine) {
                    hairRendererCompute.Dispatch(initializeIndicesKernel, strandsCount.toCsGroups(), 1, 1);
                } else {
                    hairRendererCompute.Dispatch(initializeIndicesKernel, (cardSubDivisionsYTriangle * strandsCount).toCsGroups(), 1, 1);
                }

                triangleIndexBuffer.Dispose();
            }
        }

        private bool isInitializing;

        private void createHairStrandPointsBuffer() {
            hairStrandPointsBuffer = new ComputeBuffer(hairContainer.hairStrandPoints.Length, Marshal.SizeOf<HairStrandPointStruct>(),
                ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            var hairStrandPoints = hairStrandPointsBuffer.BeginWrite<HairStrandPointStruct>(0, hairContainer.hairStrandPoints.Length);
            var mainThreadContext = SynchronizationContext.Current;

            createAssetsThread = new Thread(() => {
                var count = hairContainer.hairStrandPoints.Length;
                var structs = hairContainer.hairStrandPoints.ToList().Select(it => it.convertToStruct()).ToArray();
                doIt(structs, hairStrandPoints);
                mainThreadContext.Post(_ => {
                    if (hairStrandPointsBuffer == null || !hairStrandPoints.IsCreated) return;

                    hairStrandPointsBuffer.EndWrite<HairStrandPointStruct>(count);
                    hairRendererCompute.SetBuffer(allInOneKernel, "hairStrandPoints", hairStrandPointsBuffer);
                    if (strandRenderType != StrandRenderType.LINE) {
                        rebuildShapeBuffer();
                        hairRendererCompute.SetBuffer(allInOneKernel, "strandShapeBuffer", strandShapeBuffer);
                    }

                    isInitializing = false;
                }, null);
            });
            createAssetsThread.Start();
        }

        private Thread createAssetsThread;

        unsafe void doIt(HairStrandPointStruct[] structs, NativeArray<HairStrandPointStruct> hairStrandPoints) {
            fixed (void* structsPointer = structs) {
                // ...and use memcpy to copy the Vector3[] into a NativeArray<floar3> without casting. whould be fast!
                UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(hairStrandPoints),
                    structsPointer, structs.Length * (long)UnsafeUtility.SizeOf<HairStrandPointStruct>());
            }
        }

        private void Update() {
            checkForHairContainer();
            checkForChangedTopology();
        }

        private void updateAndRenderHair() {
            if (hairContainer != null && hairContainerID != hairContainer.id || strandRenderType != fluffyRenderersController.currentStrandRenderType) {
                return; //We have changes upcoming in FixedUpdate. It has to be done in Update to avoid a Destroyimmediate issue on older Unit versions.
            }

            if (hairContainer != null && !isInitializing) {
                fluffyRenderersController.setupRenderers(false, material, transform.position, CurrentRenderer, isUrp, motionVectors);
                fluffyRenderersController.setHqLineRenderingEnabled(enableHqLineRendering);
                var thisTransform = transform;
                if (hairContainer.isSkinned) {
                    if (meshBaker == null) {
                        initialize();
                        return;
                    }

                    meshBaker.bakeSkinnedMesh(false);
                    if (settings.verletSimulationSettings.isVerletColliderEnabled()) {
                        sdfColliderCommon.createSDF(thisTransform, hairRendererCompute, allInOneKernel);
                    }

                    hairRendererCompute.SetBuffer(allInOneKernel, ShaderID.SOURCE_MESH, meshBaker.bakedMesh);
                }


                var rotation = thisTransform.rotation;
                hairRendererCompute.SetMatrix(ShaderID.LOCAL_TO_WORLD_MATRIX, thisTransform.localToWorldMatrix);
                hairRendererCompute.SetMatrix(ShaderID.OBJECT_ROTATION_MATRIX, Matrix4x4.Rotate(rotation));

                hairRendererCompute.SetBuffer(allInOneKernel, "verletNodes", verletNodesBuffer);


                var strandNodesCount = hairContainer.pointsPerStrand; //Get from layer
                int nearestPow = CullAndSortController.nextPowerOf2(strandNodesCount);
                var nodesCount = hairContainer.hairStrandPoints.Length;
                var strandsCount = nodesCount / strandNodesCount;
                int dispatchCount = nearestPow * strandsCount;
                var layerNodeStartIndex = 0;

                //This should be done in a common function in VerletSimulation.cs
                hairRendererCompute.SetInt("nearestPow", nearestPow);
                hairRendererCompute.SetInt("strandPointsCount", strandNodesCount);
                hairRendererCompute.SetVector("worldSpaceCameraPos", lodCamera.getCamera().transform.position);
                hairRendererCompute.SetFloat("sourceMeshNormalToStrandNormalPercent", settings.sourceMeshNormalToStrandNormalPercent);
                hairRendererCompute.SetInt("verletNodesCount", nodesCount);
                hairRendererCompute.SetInt("layerVertexStartIndex", layerNodeStartIndex);
                hairRendererCompute.SetFloat("shapeConstraintRoot", settings.verletSimulationSettings.stiffnessRoot);
                hairRendererCompute.SetFloat("keepShapeStrength", settings.verletSimulationSettings.keepShapeStrength);
                hairRendererCompute.SetFloat("shapeConstraintTip", settings.verletSimulationSettings.stiffnessTip);
                hairRendererCompute.SetInt("numberOfFixedNodesInStrand", settings.verletSimulationSettings.isFirstNodeFixed ? 1 : 0);
                hairRendererCompute.setKeywordEnabled("IS_SKINNED", hairContainer.isSkinned);
                hairRendererCompute.setKeywordEnabled("STRIP_TOPOLOGY", strandRenderType != StrandRenderType.LINE);
                var currentCamera = cullSettings.camera.getCamera();
                float lodExtraStrandWidth = 0;

                var objectPosition = cullSettings.distanceCheckTargetObject != null
                    ? cullSettings.distanceCheckTargetObject.position
                    : meshBaker?.getObjectPosition() ?? transform.position;
                if (currentCamera != null) {
                    currentCullingCameraName = currentCamera.name;
                    lodExtraStrandWidth = cullSettings.getExtraWidthAmount(Vector3.Distance(currentCamera.transform.position, objectPosition));
                    if (material != null) {
                        material.SetFloat("_CullingExtraStrandWidth", lodExtraStrandWidth * 100);
                    }
                }

                var cullAmount = currentCamera != null ? cullSettings.getCullAmount(Vector3.Distance(currentCamera.transform.position, objectPosition)) : 0;
                hairRendererCompute.SetInt(ShaderID.LOD_SKIP_STRANDS_COUNT, cullAmount);
                hairRendererCompute.SetInt(ShaderID.LOD_MAX_SKIP_STRANDS_COUNT, cullSettings.MaxCullAmount + 1);
                hairRendererCompute.SetFloat("killTheVert", Single.NaN);
                hairRendererCompute.SetFloat("strandsWidth", getStrandsWidth() + lodExtraStrandWidth);
                hairRendererCompute.SetVector("_Gravity", settings.verletSimulationSettings.gravity);
                hairRendererCompute.SetFloat("deltaTime", Time.smoothDeltaTime);

                hairRendererCompute.SetFloat("_Decay", 1f - settings.verletSimulationSettings.drag);
                hairRendererCompute.SetFloat("stepSize", 1f / settings.verletSimulationSettings.constraintIterations);
                hairRendererCompute.SetInt("solverIterations", settings.verletSimulationSettings.constraintIterations);
                ColliderHelper.setupColliderProperties(ref colliderBuffer, ref collidersStruct, sphereColliders, capsuleColliders,
                    hairRendererCompute,
                    allInOneKernel);

                VerletSimulation.setupWind(hairRendererCompute, allInOneKernel, settings);
                hairRendererCompute.setKeywordEnabled("VERLET_ENABLED", Application.isPlaying && settings.verletSimulationSettings.enableMovement);


                hairRendererCompute.setKeywordEnabled("COLLIDE_WITH_SOURCE_MESH",
                    settings.verletSimulationSettings.isVerletColliderEnabled() && hairContainer.isSkinned);
                hairRendererCompute.setKeywordEnabled("USE_FORWARD_COLLISION", settings.verletSimulationSettings.useForwardCollision);
                var extraScale = 1f + (thisTransform.lossyScale.x - hairContainer.objectScaleAtSkinning) / hairContainer.objectScaleAtSkinning;
                hairRendererCompute.SetFloat(ShaderID.EXTRA_SCALE, extraScale);
                hairRendererCompute.Dispatch(allInOneKernel, dispatchCount.toCsGroups(), 1, 1);
                hairRendererCompute.DisableKeyword("INITIALIZE_VERLET_NODES");
                collideWithOtherSDFColliders();
            }
        }


        private float getStrandsWidth() {
            return FurCreator.getInterpolatedWidth(hairContainer.strandsWidth, 0.0025f);
        }

        private void collideWithOtherSDFColliders() {
            if (settings.verletSimulationSettings.isSDFCollisionEnabled() && sdfColliders != null) {
                foreach (var sdfCollider in sdfColliders) {
                    if (sdfCollider != null) {
                        sdfCollider.collideWith(verletNodesBuffer, fluffyRenderersController.getRendererBounds(),
                            fluffyRenderersController.hairMeshBuffer,
                            fluffyRenderersController.getVertexBufferStride());
                    }
                }
            }
        }

        private void beginFrameRendering(ScriptableRenderContext ctx, List<Camera> cameras) {
            updateAndRenderHair();
        }

        private void cameraPreRender(Camera cam) {
            if (lodCamera.getCamera() == cam) {
                updateAndRenderHair();
            }
        }

        private void OnDisable() {
            clearResources();
            RenderPipelineManager.beginContextRendering -= beginFrameRendering;
            Camera.onPreRender -= cameraPreRender;
            hairContainerID = -1;
        }

        private void clearResources() {
            createAssetsThread?.Abort();
            meshBaker?.dispose();
            fluffyRenderersController.destroy();
            hairStrandPointsBuffer?.Dispose();
            hairStrandPointsBuffer = null;
            verletNodesBuffer?.Dispose();
            sdfColliderCommon?.dispose();
            strandShapeBuffer?.Dispose();
        }

        public Renderer CurrentRenderer { get; set; }

        public void recreateSdfCollider() {
            if (hairContainer.isSkinned) {
                sdfColliderCommon?.dispose();
                sdfColliderCommon = new SDFColliderCommon(GetComponent<Renderer>(), meshBaker, settings.verletSimulationSettings);
            }
        }

        public void recreate() {
            clearResources();
            initialize();
        }

        public void rebuildShapeBuffer() {
            if (strandRenderType != StrandRenderType.LINE) {
                hairContainer.createShapeBuffer(strandShapeBuffer);
            }
        }
    }
}

[Serializable]
public class CullSettings {
    public float MinVal = 1;
    public float MaxVal = 10;
    public int MinCullAmount;
    public int MaxCullAmount;
    public const float MAX_CAM_DIST = 20;
    public const float MAX_SKIP_AMOUNT = 50;
    public Camera camera;
    public Transform distanceCheckTargetObject;
    [Range(0f, 0.01f)] public float extraStrandWidth;

    public int getCullAmount(float distToCam) {
        var clamped = Mathf.Clamp(Mathf.Abs(distToCam), MinVal, MaxVal);
        var lerpAmount = (clamped - MinVal) / (MaxVal - MinVal);
        return (int)Mathf.Lerp(MinCullAmount, MaxCullAmount, lerpAmount);
    }

    public float getExtraWidthAmount(float distToCam) {
        var clamped = Mathf.Clamp(Mathf.Abs(distToCam), MinVal, MaxVal);
        var lerpAmount = (clamped - MinVal) / (MaxVal - MinVal);
        return lerpAmount * extraStrandWidth;
    }
}

public enum StrandRenderType {
    LINE,
    TRIANGLE_MESH
}