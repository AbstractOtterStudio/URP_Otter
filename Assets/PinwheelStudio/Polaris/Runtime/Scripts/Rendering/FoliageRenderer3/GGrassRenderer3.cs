#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

namespace Pinwheel.Griffin.Rendering
{
    public class GGrassRenderer3
    {
        private GStylizedTerrain terrain;

        #region Per-frame render settings

        private float lod1Start;
        private float grassDistance;
        private float grassFadeStart;
        private float grassLodTransitionDistance;
        private float cullVolumeBias;
        private bool enableInteractiveGrass;
        private Vector3 cameraPosition;
        private Vector3 terrainPosition;
        private Vector3 terrainSize;
        private LightProbeUsage lightProbeUsage;
        private bool unloadInactiveCells;
        private int frameCountToUnloadInactiveCells;
        private int frameCount;

        #endregion

        #region Native containers

        private NativeArray<Bounds> cellWorldBoundsNA;
        private NativeArray<byte> cellCullResultsNA;

        #endregion

        #region Managed containers

        private Plane[] frustum = new Plane[6];
        /// <summary>
        /// Number of instances for each prototype, count upon loaded visible cells
        /// </summary>
        private int[] perPrototypeInstanceCount;
        /// <summary>
        /// Cached settings of grass prototypes
        /// </summary>
        private PrototypeRenderData[] prototypeRenderData;
        /// <summary>
        /// Per prototype instance data of each grass patch/cell, loading state
        /// </summary>
        private PatchRenderData[] patchRenderData;
        private byte[] cellCullResults;
        private int[] cellLastVisibleAtFrameFlags;
        /// <summary>
        /// TRS batches for rendering, per prototype
        /// </summary>
        private PerPrototypeBatchData[] perPrototypeBatchData;
        private MaterialPropertyBlock[] perPrototypeMaterialPropertyBlocks;

        //for debugging
        //private Bounds[] d_cellWorldBounds;

        #endregion

        #region Job handles & dependencies & flags

        private JobHandle nativeDataPreparationJobsHandle = default;

        #endregion

        public GStylizedTerrain Terrain
        {
            get { return terrain; }
            private set { terrain = value; }
        }

        public GGrassRenderer3(GStylizedTerrain terrain)
        {
            this.terrain = terrain;
        }

        public void OnEnable()
        {
            frameCount = 0;
            InitCells();
            InitPrototypeRenderData();
            InitPatchRenderData();
        }

        public void OnDisable()
        {
            nativeDataPreparationJobsHandle.Complete();

            if (cellWorldBoundsNA.IsCreated)
            {
                cellWorldBoundsNA.Dispose();
            }
            if (cellCullResultsNA.IsCreated)
            {
                cellCullResultsNA.Dispose();
            }

            if (patchRenderData != null)
            {
                foreach (PatchRenderData prd in patchRenderData)
                {
                    if (prd.State != PatchRenderData.LoadState.Unloaded)
                    {
                        prd.Unload();
                    }
                }
            }

            if (perPrototypeBatchData != null)
            {
                foreach (PerPrototypeBatchData batchData in perPrototypeBatchData)
                {
                    batchData.Dispose();
                }
            }
        }

        public void OnCellChanged(int cellIndex)
        {
            if (patchRenderData != null)
            {
                nativeDataPreparationJobsHandle.Complete();
                patchRenderData[cellIndex].Unload();

                Vector3 terrainSize = terrain.TerrainData.Geometry.Size;
                Bounds b = patchRenderData[cellIndex].GetPatchBounds();
                Vector3 size = b.size;
                size.Scale(terrainSize);
                b.size = size;

                Vector3 center = b.center;
                center.Scale(terrainSize);
                b.center = center + terrain.transform.position;

                cellWorldBoundsNA[cellIndex] = b;
            }
        }

        public void OnPatchGridSizeChanged()
        {
            OnDisable();
            OnEnable();
        }

        public void OnPrototypeGroupChanged()
        {
            OnDisable();
            OnEnable();
        }

        public void OnPrototypesChanged()
        {
            OnDisable();
            OnEnable();
        }

        public void OnGeometryChanged()
        {
            OnDisable();
            OnEnable();
        }

        private void InitCells()
        {
            if (terrain.TerrainData == null)
                return;
            GGrassPatch[] patches = terrain.TerrainData.Foliage.GrassPatches;
            cellWorldBoundsNA = new NativeArray<Bounds>(patches.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            cellCullResultsNA = new NativeArray<byte>(patches.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            cellCullResults = new byte[patches.Length];
            cellLastVisibleAtFrameFlags = new int[patches.Length];

            Vector3 terrainSize = terrain.TerrainData.Geometry.Size;
            for (int i = 0; i < patches.Length; ++i)
            {
                Bounds b = patches[i].Bounds;
                Vector3 size = b.size;
                size.Scale(terrainSize);
                b.size = size;

                Vector3 center = b.center;
                center.Scale(terrainSize);
                b.center = center + terrain.transform.position;

                cellWorldBoundsNA[i] = b;
                cellLastVisibleAtFrameFlags[i] = -1;
            }
        }

        private void InitPrototypeRenderData()
        {
            if (terrain.TerrainData == null)
                return;
            if (terrain.TerrainData.Foliage.Grasses == null)
                return;
            List<GGrassPrototype> prototypes = terrain.TerrainData.Foliage.Grasses.Prototypes;
            prototypeRenderData = new PrototypeRenderData[prototypes.Count];
            for (int i = 0; i < prototypes.Count; ++i)
            {
                prototypeRenderData[i] = new PrototypeRenderData(prototypes[i]);
            }

            perPrototypeInstanceCount = new int[prototypes.Count];
            perPrototypeBatchData = new PerPrototypeBatchData[prototypes.Count];
            perPrototypeMaterialPropertyBlocks = new MaterialPropertyBlock[prototypes.Count];
            for (int i = 0; i < prototypes.Count; ++i)
            {
                perPrototypeBatchData[i] = new PerPrototypeBatchData();
                perPrototypeMaterialPropertyBlocks[i] = new MaterialPropertyBlock();
            }
        }

        private void InitPatchRenderData()
        {
            if (terrain.TerrainData == null)
                return;
            if (terrain.TerrainData.Foliage.Grasses == null)
                return;
            GGrassPatch[] patches = terrain.TerrainData.Foliage.GrassPatches;
            patchRenderData = new PatchRenderData[patches.Length];
            for (int i = 0; i < patches.Length; ++i)
            {
                patchRenderData[i] = new PatchRenderData(patches[i], prototypeRenderData);
            }
        }

        public void Render(Camera cam)
        {
            if (TrySkipRenderingOnGeneralCases())
                return;

            SetPerFrameRenderSettings();
            if (GRuntimeSettings.Instance.renderingDefault.drawFoliageOnMainCameraOnly)
            {
                SetView(Camera.main);
            }
            else
            {
                SetView(cam);
            }

            if (frameCount == 0)
            {
                CullCells();
                CullInstancesAndComputeLOD();
                ComputeBatches();
                nativeDataPreparationJobsHandle.Complete();
                CopyNativeDataToManaged();
            }
            else
            {
                if (nativeDataPreparationJobsHandle.IsCompleted)
                {
                    nativeDataPreparationJobsHandle.Complete();

                    //We use culling & batch data from the last frame if the Frame Debugger is enabled,
                    //otherwise the FD will flickering
#if (DEVELOPMENT_BUILD || UNITY_EDITOR ) && UNITY_2021_2_OR_NEWER
                    if (!FrameDebugger.enabled)
#endif
                    {
                        CopyNativeDataToManaged();
                        CullCells();
                        CullInstancesAndComputeLOD();
                        ComputeBatches();
                        UnloadInactiveCells();
                    }
                }
            }

            ConfigureMaterialProperties();
            DrawInstances(cam);
            frameCount += 1;
        }

        public void DrawDebug(Camera cam)
        {
#if UNITY_EDITOR
            //nativeDataPreparationJobsHandle.Complete();
            //UnityEditor.Handles.EndGUI();
            //Bounds[] d_cellWorldBounds = cellWorldBoundsNA.ToArray();
            //for (int i = 0; i < d_cellWorldBounds.Length; ++i)
            //{
            //    PatchRenderData prd = patchRenderData[i];
            //    Bounds b = d_cellWorldBounds[i];
            //    byte c = cellCullResults[i];
            //    UnityEditor.Handles.color =
            //        prd.State == PatchRenderData.LoadState.Loaded ? Color.green : Color.red;

            //    //if (b.size != Vector3.zero)
            //    //    UnityEditor.Handles.DrawWireCube(b.center, b.size);
            //    //else
            //    //    UnityEditor.Handles.DrawSolidDisc(b.center, Vector3.up, 5);
            //    if (c == GCullFlags.CULLED)
            //        UnityEditor.Handles.color = Color.red;
            //    if (c == GCullFlags.PARTIALLY_VISIBLE)
            //        UnityEditor.Handles.color = Color.yellow;
            //    if (c == GCullFlags.VISIBLE)
            //        UnityEditor.Handles.color = Color.green;

            //    UnityEditor.Handles.DrawWireCube(b.center, b.size);
            //}
#endif
        }

        private bool TrySkipRenderingOnGeneralCases()
        {
            if (Terrain.transform.rotation != Quaternion.identity)
                return true;

            if (Terrain.transform.lossyScale != Vector3.one)
                return true;

            if (GRuntimeSettings.Instance.isEditingGeometry)
                return true;

            GTerrainData terrainData = Terrain.TerrainData;
            if (terrainData == null)
                return true;

            GRendering r = terrainData.Rendering;
            if (!r.DrawGrasses)
                return true;
            if (r.GrassDistance == 0)
                return true;

            GFoliage f = terrainData.Foliage;
            if (f.Grasses == null)
                return true;
            if (f.Grasses.Prototypes.Count == 0)
                return true;
            if (f.GrassInstanceCount == 0)
                return true;

            return false;
        }

        private void SetPerFrameRenderSettings()
        {
            GTerrainData terrainData = Terrain.TerrainData;
            GRendering r = terrainData.Rendering;
            GFoliage f = terrainData.Foliage;
            GRuntimeSettings runtimeSettings = GRuntimeSettings.Instance;
            terrainPosition = Terrain.transform.position;
            terrainSize = Terrain.TerrainData.Geometry.Size;
            lod1Start = r.GrassLod1Start;
            grassDistance = r.GrassDistance;
            grassLodTransitionDistance = runtimeSettings.renderingDefault.grassLodTransitionDistance;
            grassFadeStart = r.GrassFadeStart;
            cullVolumeBias = runtimeSettings.renderingDefault.grassCullBias;
            enableInteractiveGrass = f.EnableInteractiveGrass;
            lightProbeUsage = runtimeSettings.renderingDefault.grassLightProbeUsage;
            unloadInactiveCells = runtimeSettings.renderingDefault.grassUnloadInactiveCells;
            frameCountToUnloadInactiveCells = runtimeSettings.renderingDefault.grassFrameCountToUnloadInactiveCells;
        }

        private void SetView(Camera cam)
        {
            //float fov = cam.fieldOfView;
            //cam.fieldOfView += cullVolumeBias*0.5f;
            GFrustumUtilities.Calculate(cam, frustum, grassDistance);
            cameraPosition = cam.transform.position;
            //cam.fieldOfView = fov;
        }

        private void CopyNativeDataToManaged()
        {
            cellCullResultsNA.CopyTo(cellCullResults);
            foreach (PerPrototypeBatchData ppbd in perPrototypeBatchData)
            {
                ppbd.CopyNativeDataToManaged();
            }

            //d_cellWorldBounds = cellWorldBoundsNA.ToArray();
        }

        private void CullCells()
        {
            CullCellsJob j = new CullCellsJob()
            {
                cellWorldBounds = cellWorldBoundsNA,
                cellCullResults = cellCullResultsNA,
                plane0 = frustum[0],
                plane1 = frustum[1],
                plane2 = frustum[2],
                plane3 = frustum[3],
                plane4 = frustum[4],
                plane5 = frustum[5],
                volumeBias = cullVolumeBias
            };
            JobHandle jhandle = j.Schedule(cellWorldBoundsNA.Length, 100);
            if (frameCount == 0)
            {
                jhandle.Complete();
                cellCullResultsNA.CopyTo(cellCullResults);
            }

            nativeDataPreparationJobsHandle = jhandle;
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct CullCellsJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Bounds> cellWorldBounds;
            [WriteOnly]
            public NativeArray<byte> cellCullResults;

            public Plane plane0, plane1, plane2, plane3, plane4, plane5;
            public float volumeBias;

            public void Execute(int index)
            {
                Bounds b = cellWorldBounds[index];
                b.Expand(volumeBias);
                byte cullResult = GCullUtils.TestFrustumAABB(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref b);
                cellCullResults[index] = cullResult;
            }
        }

        private void CullInstancesAndComputeLOD()
        {
            for (int ci = 0; ci < patchRenderData.Length; ++ci)
            {
                if (cellCullResults[ci] == GCullFlags.CULLED)
                    continue;

                PatchRenderData prd = patchRenderData[ci];
                if (prd.State == PatchRenderData.LoadState.Unloaded)
                {
                    prd.Load(terrainPosition, terrainSize);
                }

                for (int pi = 0; pi < prd.perPrototypeInstanceData.Length; ++pi)
                {
                    PerPrototypeInstanceData ppid = prd.perPrototypeInstanceData[pi];
                    PrototypeRenderData prototypeData = prototypeRenderData[pi];

                    InstanceCullingAndLODJob job = new InstanceCullingAndLODJob()
                    {
                        cellLodFlag = cellCullResults[ci],
                        instanceWorldTransform = ppid.instanceWorldTransformsNA,
                        instanceLodResults = ppid.instanceLodResultsNA,
                        plane0 = frustum[0],
                        plane1 = frustum[1],
                        plane2 = frustum[2],
                        plane3 = frustum[3],
                        plane4 = frustum[4],
                        plane5 = frustum[5],
                        cullVolumeBias = cullVolumeBias,
                        lodTransitionDistance = grassLodTransitionDistance,
                        lod1StartDistance = lod1Start,
                        grassDistance = grassDistance,
                        cameraPosition = cameraPosition,
                        hasLod1 = prototypeData.lod1 != null
                    };

                    nativeDataPreparationJobsHandle = job.Schedule(prd.perPrototypeInstanceCount[pi], 1000, nativeDataPreparationJobsHandle);
                }
            }
        }


#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct InstanceCullingAndLODJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeList<Matrix4x4> instanceWorldTransform;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<byte> instanceLodResults;

            public byte cellLodFlag;
            public Plane plane0, plane1, plane2, plane3, plane4, plane5;
            public float cullVolumeBias;
            public float lodTransitionDistance;
            public float lod1StartDistance;
            public float grassDistance;
            public Vector3 cameraPosition;
            public bool hasLod1;

            public void Execute(int index)
            {
                Matrix4x4 trs = instanceWorldTransform[index];
                Vector3 instanceWorldPos = trs.GetColumn(3);
                bool isInstanceInFrustum = false;
                if (cellLodFlag == GCullFlags.VISIBLE)
                {
                    isInstanceInFrustum = true;
                }
                else if (cellLodFlag == GCullFlags.PARTIALLY_VISIBLE)
                {
                    if (GCullUtils.IsPointInsideFrustum(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref instanceWorldPos))
                    {
                        isInstanceInFrustum = true;
                    }
                    else
                    {
                        Vector3 boundSize = Vector3.one * cullVolumeBias * 2;
                        Bounds b = new Bounds(instanceWorldPos, boundSize);
                        isInstanceInFrustum = GCullUtils.TestFrustumAABB(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref b) !=
                                            GCullFlags.CULLED;
                    }
                }

                byte lodFlag = InstanceLodFlags.CULLED;
                if (isInstanceInFrustum)
                {
                    float sqrDistanceToCamera = Vector3.SqrMagnitude(instanceWorldPos - cameraPosition);
                    if (sqrDistanceToCamera < lod1StartDistance * lod1StartDistance)
                    {
                        lodFlag = InstanceLodFlags.LOD0;
                    }
                    else
                    {
                        float d2cam = Mathf.Sqrt(sqrDistanceToCamera);
                        if (d2cam - lod1StartDistance > lodTransitionDistance)
                        {
                            lodFlag = InstanceLodFlags.LOD1;
                        }
                        else
                        {
                            float noise = Mathf.PerlinNoise(instanceWorldPos.x, instanceWorldPos.z);
                            float f = (d2cam - lod1StartDistance) / (lodTransitionDistance + 0.01f);
                            float v = Mathf.Clamp01((1 - f) + noise);
                            lodFlag = v > 0.95f ? InstanceLodFlags.LOD0 : InstanceLodFlags.LOD1;
                        }
                    }
                }

                if (lodFlag == InstanceLodFlags.LOD1 && !hasLod1)
                {
                    lodFlag = InstanceLodFlags.LOD0;
                }

                instanceLodResults[index] = lodFlag;
            }
        }

        private void ComputeBatches()
        {
            UpdatePerPrototypeInstanceCount();
            for (int bi = 0; bi < perPrototypeBatchData.Length; ++bi)
            {
                int capacity = perPrototypeInstanceCount[bi];
                perPrototypeBatchData[bi].ClearAndEnsureContainerCapacity(capacity);
            }

            for (int ci = 0; ci < patchRenderData.Length; ++ci)
            {
                if (cellCullResults[ci] == GCullFlags.CULLED)
                    continue;
                PatchRenderData prd = patchRenderData[ci];
                if (prd.State == PatchRenderData.LoadState.Unloaded)
                    continue;

                for (int pi = 0; pi < prd.perPrototypeInstanceData.Length; ++pi)
                {
                    PerPrototypeInstanceData ppid = prd.perPrototypeInstanceData[pi];
                    PerPrototypeBatchData ppbd = perPrototypeBatchData[pi];
                    ComputeBatchesJob job = new ComputeBatchesJob()
                    {
                        instanceWorldTransforms = ppid.instanceWorldTransformsNA,
                        instanceLodResults = ppid.instanceLodResultsNA,
                        lod0WorldTransforms = ppbd.lod0WorldTransformNA,
                        lod1WorldTransforms = ppbd.lod1WorldTransformNA
                    };

                    nativeDataPreparationJobsHandle = job.Schedule(prd.perPrototypeInstanceCount[pi], 10000, nativeDataPreparationJobsHandle);
                }
            }
        }

        private void UpdatePerPrototypeInstanceCount()
        {
            for (int i = 0; i < perPrototypeInstanceCount.Length; ++i)
            {
                perPrototypeInstanceCount[i] = 0;
            }
            for (int i = 0; i < patchRenderData.Length; ++i)
            {
                PatchRenderData prd = patchRenderData[i];
                if (prd.State == PatchRenderData.LoadState.Unloaded)
                    continue;

                if (cellCullResults[i] == GCullFlags.CULLED)
                    continue;

                for (int pi = 0; pi < prd.perPrototypeInstanceData.Length; ++pi)
                {
                    int c = prd.perPrototypeInstanceCount[pi];
                    perPrototypeInstanceCount[pi] += c;
                }
            }
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct ComputeBatchesJob : IJobParallelFor
        {
            [ReadOnly] public NativeList<Matrix4x4> instanceWorldTransforms;
            [ReadOnly] public NativeList<byte> instanceLodResults;

            [NativeDisableContainerSafetyRestriction] public NativeList<Matrix4x4> lod0WorldTransforms;
            [NativeDisableContainerSafetyRestriction] public NativeList<Matrix4x4> lod1WorldTransforms;

            public void Execute(int index)
            {
                Matrix4x4 trs = instanceWorldTransforms[index];
                byte lod = instanceLodResults[index];
                if (lod == InstanceLodFlags.LOD0)
                {
                    lod0WorldTransforms.AddNoResize(trs);
                }
                else if (lod == InstanceLodFlags.LOD1)
                {
                    lod1WorldTransforms.AddNoResize(trs);
                }
            }
        }

        private void ConfigureMaterialProperties()
        {
            Vector4 windParams = GWindZone.WindParams;
            Texture2D windNoiseTexture = GRuntimeSettings.Instance.foliageRendering.windNoiseTexture;

            for (int i = 0; i < prototypeRenderData.Length; ++i)
            {
                if (perPrototypeInstanceCount[i] == 0)
                    continue;

                PrototypeRenderData proto = prototypeRenderData[i];
                MaterialPropertyBlock propertyBlock = perPrototypeMaterialPropertyBlocks[i];
                propertyBlock.Clear();

                propertyBlock.SetVector(MaterialProperties.WIND, windParams);
                propertyBlock.SetTexture(MaterialProperties.NOISE_TEX, windNoiseTexture);

                propertyBlock.SetColor(MaterialProperties.COLOR, proto.color);
                if (proto.shape != GGrassShape.DetailObject && proto.texture != null)
                    propertyBlock.SetTexture(MaterialProperties.MAIN_TEX, proto.texture);
                propertyBlock.SetFloat(MaterialProperties.BEND_FACTOR, proto.bendFactor);

                float fadeMaxDistance = grassDistance;
                float fadeMinDistance = Mathf.Clamp(grassFadeStart, 0f, 0.99f) * fadeMaxDistance;
                propertyBlock.SetFloat(MaterialProperties.FADE_MIN_DISTANCE, fadeMinDistance);
                propertyBlock.SetFloat(MaterialProperties.FADE_MAX_DISTANCE, fadeMaxDistance);
            }

            if (terrain.TerrainData.Foliage.EnableInteractiveGrass)
            {
                RenderTexture vectorFieldRT = terrain.GetGrassVectorFieldRenderTexture();
                Matrix4x4 worldToNormalizedMatrix = terrain.GetWorldToNormalizedMatrix();
                for (int i = 0; i < prototypeRenderData.Length; ++i)
                {
                    if (perPrototypeInstanceCount[i] == 0)
                        continue;
                    MaterialPropertyBlock propertyBlock = perPrototypeMaterialPropertyBlocks[i];
                    propertyBlock.SetTexture(MaterialProperties.VECTOR_FIELD, vectorFieldRT);
                    propertyBlock.SetMatrix(MaterialProperties.WORLD_TO_NORMALIZED, worldToNormalizedMatrix);
                }
            }
        }

        struct MaterialProperties
        {
            public static readonly int COLOR = Shader.PropertyToID("_Color");
            public static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
            public static readonly int NOISE_TEX = Shader.PropertyToID("_NoiseTex");
            public static readonly int WIND = Shader.PropertyToID("_Wind");
            public static readonly int BEND_FACTOR = Shader.PropertyToID("_BendFactor");
            public static readonly int VECTOR_FIELD = Shader.PropertyToID("_VectorField");
            public static readonly int WORLD_TO_NORMALIZED = Shader.PropertyToID("_WorldToNormalized");
            public static readonly int FADE_MIN_DISTANCE = Shader.PropertyToID("_FadeMinDistance");
            public static readonly int FADE_MAX_DISTANCE = Shader.PropertyToID("_FadeMaxDistance");
        }

        private void DrawInstances(Camera cam)
        {
            for (int i = 0; i < perPrototypeBatchData.Length; ++i)
            {
                PerPrototypeBatchData ppbd = perPrototypeBatchData[i];
                PrototypeRenderData prototypeData = prototypeRenderData[i];
                MaterialPropertyBlock propertyBlock = perPrototypeMaterialPropertyBlocks[i];

                PrototypeRenderData.LodInfo lod0 = prototypeData.lod0;
                if (lod0 != null)
                {
                    for (int bi = 0; bi < ppbd.lod0BatchCount; ++bi)
                    {
                        GBatchTRS batch = ppbd.lod0WorldTransformBatches[bi];
                        DrawBatch(cam, lod0.mesh, lod0.GetMaterials(prototypeData.shape, enableInteractiveGrass), batch, propertyBlock, prototypeData, lod0);
                    }
                }

                PrototypeRenderData.LodInfo lod1 = prototypeData.lod1;
                if (lod1 != null)
                {
                    for (int bi = 0; bi < ppbd.lod1BatchCount; ++bi)
                    {
                        GBatchTRS batch = ppbd.lod1WorldTransformBatches[bi];
                        DrawBatch(cam, lod1.mesh, lod1.GetMaterials(prototypeData.shape, enableInteractiveGrass), batch, propertyBlock, prototypeData, lod1);
                    }
                }
            }
        }

        private void DrawBatch(Camera cam, Mesh mesh, Material[] materials, GBatchTRS batch, MaterialPropertyBlock propertyBlock,
            PrototypeRenderData renderData, PrototypeRenderData.LodInfo lodInfo)
        {
            if (mesh == null)
                return;
            for (int mi = 0; mi < materials.Length; ++mi)
            {
                Material mat = materials[mi];
                Graphics.DrawMeshInstanced(
                    mesh, mi, mat,
                    batch.matrices, batch.length, propertyBlock,
                    lodInfo.castShadow, lodInfo.receiveShadow,
                    renderData.layer, cam,
                    lightProbeUsage);
            }
        }

        private void UnloadInactiveCells()
        {
            if (!unloadInactiveCells)
                return;

            for (int i = 0; i < cellCullResults.Length; ++i)
            {
                byte c = cellCullResults[i];
                if (c == GCullFlags.CULLED)
                    continue;
                cellLastVisibleAtFrameFlags[i] = frameCount;
            }

            for (int i = 0; i < patchRenderData.Length; ++i)
            {
                int d = frameCount - cellLastVisibleAtFrameFlags[i];
                if (d > frameCountToUnloadInactiveCells)
                {
                    if (patchRenderData[i].State == PatchRenderData.LoadState.Loaded)
                    {
                        patchRenderData[i].Unload(nativeDataPreparationJobsHandle);
                        break;
                    }
                }
            }
        }

        class PatchRenderData
        {
            public PerPrototypeInstanceData[] perPrototypeInstanceData;
            public int[] perPrototypeInstanceCount;
            private JobHandle initInstanceDataJobHandle;

            public enum LoadState
            {
                Unloaded,
                Loaded
            }

            private LoadState state;
            public LoadState State
            {
                get
                {
                    return state;
                }
            }

            private GGrassPatch grassPatch;
            private PrototypeRenderData[] grassPrototypes;

            public PatchRenderData(GGrassPatch p, PrototypeRenderData[] prototypes)
            {
                state = LoadState.Unloaded;
                grassPatch = p;
                grassPrototypes = prototypes;
            }

            public void Load(Vector3 terrainPosition, Vector3 terrainSize)
            {
                if (state == LoadState.Loaded)
                    return;

                perPrototypeInstanceCount = new int[grassPrototypes.Length];
                perPrototypeInstanceData = new PerPrototypeInstanceData[grassPrototypes.Length];
                for (int i = 0; i < perPrototypeInstanceData.Length; ++i)
                {
                    perPrototypeInstanceData[i] = new PerPrototypeInstanceData();
                }

                if (grassPatch.InstanceCount == 0)
                    return;

                NativeArray<GGrassInstance> persistentInstancesNA = grassPatch.Instances.ToNativeArray(Allocator.TempJob);
                for (int i = 0; i < perPrototypeInstanceData.Length; ++i)
                {
                    PrototypeRenderData prototypeData = grassPrototypes[i];
                    PerPrototypeInstanceData ppind = perPrototypeInstanceData[i];
                    ppind.Create();

                    InitPerPrototypeInstanceDataJob j = new InitPerPrototypeInstanceDataJob()
                    {
                        persistentInstances = persistentInstancesNA,
                        prototypeIndex = i,
                        pivotOffset = prototypeData.pivotOffset,
                        baseScale = prototypeData.baseScale,
                        terrainPosition = terrainPosition,
                        terrainSize = terrainSize,

                        instanceWorldTransforms = ppind.instanceWorldTransformsNA,
                        instanceLodResults = ppind.instanceLodResultsNA
                    };
                    JobHandle jhandle = j.Schedule();
                    initInstanceDataJobHandle = JobHandle.CombineDependencies(initInstanceDataJobHandle, jhandle);
                }

                initInstanceDataJobHandle.Complete();
                persistentInstancesNA.Dispose();

                for (int i = 0; i < perPrototypeInstanceData.Length; ++i)
                {
                    perPrototypeInstanceCount[i] = perPrototypeInstanceData[i].instanceWorldTransformsNA.Length;
                }

                state = LoadState.Loaded;
            }

            public void Unload(JobHandle dependency = default)
            {
                if (state == LoadState.Unloaded)
                    return;

                foreach (PerPrototypeInstanceData ppid in perPrototypeInstanceData)
                {
                    ppid.Dispose(dependency);
                }
                perPrototypeInstanceData = null;
                perPrototypeInstanceCount = null;
                state = LoadState.Unloaded;
            }

            public Bounds GetPatchBounds()
            {
                return grassPatch.Bounds;
            }
        }

        class PerPrototypeInstanceData
        {
            public NativeList<Matrix4x4> instanceWorldTransformsNA;
            public NativeList<byte> instanceLodResultsNA;

            public PerPrototypeInstanceData()
            {

            }

            public void Create()
            {
                if (!instanceWorldTransformsNA.IsCreated)
                {
                    instanceWorldTransformsNA = new NativeList<Matrix4x4>(Allocator.Persistent);
                }
                if (!instanceLodResultsNA.IsCreated)
                {
                    instanceLodResultsNA = new NativeList<byte>(Allocator.Persistent);
                }
            }

            public void Dispose(JobHandle dependency = default)
            {
                if (instanceWorldTransformsNA.IsCreated)
                {
                    instanceWorldTransformsNA.Dispose(dependency);
                }
                if (instanceLodResultsNA.IsCreated)
                {
                    instanceLodResultsNA.Dispose(dependency);
                }
            }
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct InitPerPrototypeInstanceDataJob : IJob
        {
            [ReadOnly]
            public NativeArray<GGrassInstance> persistentInstances;

            public int prototypeIndex;
            public float pivotOffset;
            public Vector3 baseScale;

            [WriteOnly]
            public NativeList<Matrix4x4> instanceWorldTransforms;
            [WriteOnly]
            public NativeList<byte> instanceLodResults;

            public Vector3 terrainPosition;
            public Vector3 terrainSize;

            public void Execute()
            {
                for (int i = 0; i < persistentInstances.Length; ++i)
                {
                    GGrassInstance t = persistentInstances[i];
                    if (t.prototypeIndex != prototypeIndex)
                        continue;

                    Vector3 localPos = t.position;
                    localPos.Scale(terrainSize);
                    Vector3 worldPos = terrainPosition + localPos + Vector3.up * pivotOffset;

                    Quaternion worldRotation = t.rotation;
                    Vector3 worldScale = t.scale;
                    worldScale.Scale(baseScale);

                    Matrix4x4 trs = Matrix4x4.TRS(worldPos, worldRotation, worldScale);
                    instanceWorldTransforms.Add(trs);
                    instanceLodResults.Add(InstanceLodFlags.CULLED);
                }
            }
        }

        struct InstanceLodFlags
        {
            public const byte CULLED = 0;
            public const byte LOD0 = 1;
            public const byte LOD1 = 2;
        }

        class PrototypeRenderData
        {
            public class LodInfo
            {
                public Mesh mesh;
                public Material[] materialGrass;
                public Material[] materialGrassInteractive;
                public Material[] materialsDetailObject;
                public ShadowCastingMode castShadow;
                public bool receiveShadow;

                public Material[] GetMaterials(GGrassShape shape, bool enableInteractive)
                {
                    if (shape == GGrassShape.DetailObject)
                        return materialsDetailObject;
                    else if (enableInteractive)
                        return materialGrassInteractive;
                    else
                        return materialGrass;
                }
            }

            public Color color;
            public float pivotOffset;
            public Vector3 baseScale;
            public int layer;
            public float bendFactor;
            public Texture2D texture;
            public GGrassShape shape;

            public LodInfo lod0;
            public LodInfo lod1;

            public PrototypeRenderData(GGrassPrototype p)
            {
                pivotOffset = p.pivotOffset;
                baseScale = p.size;
                layer = p.layer;
                color = p.Color;
                texture = p.Texture;
                shape = p.Shape;
                bendFactor = p.BendFactor;

                lod0 = GetLod0Info(p);
                lod1 = GetLod1Info(p);
            }

            private LodInfo GetLod0Info(GGrassPrototype p)
            {
                if (!p.HasLod0)
                    return null;

                LodInfo info = new LodInfo();
                info.castShadow = p.ShadowCastingMode;
                info.receiveShadow = p.ReceiveShadow;

                info.mesh = p.GetBaseMesh();
                if (shape == GGrassShape.DetailObject)
                {
                    info.materialsDetailObject = p.DetailMaterials;
                }
                else
                {
                    info.materialGrass = new Material[] { GGrassMaterialProvider.GetMaterial(false, p.IsBillboard) };
                    info.materialGrassInteractive = new Material[] { GGrassMaterialProvider.GetMaterial(true, p.IsBillboard) };
                }

                return info;
            }

            private LodInfo GetLod1Info(GGrassPrototype p)
            {
                if (!p.HasLod1)
                    return null;

                LodInfo info = new LodInfo();
                info.castShadow = ShadowCastingMode.Off;
                info.receiveShadow = false;

                info.mesh = p.GetBaseMeshLod1();
                if (shape == GGrassShape.DetailObject)
                {
                    info.materialsDetailObject = p.DetailMaterialsLod1;
                }
                else
                {
                    info.materialGrass = new Material[] { GGrassMaterialProvider.GetMaterialLod1() };
                    info.materialGrassInteractive = new Material[] { GGrassMaterialProvider.GetMaterialLod1() };
                }

                return info;
            }

        }

        class PerPrototypeBatchData
        {
            public NativeList<Matrix4x4> lod0WorldTransformNA;
            public NativeList<Matrix4x4> lod1WorldTransformNA;

            public List<GBatchTRS> lod0WorldTransformBatches;
            public int lod0BatchCount;
            public List<GBatchTRS> lod1WorldTransformBatches;
            public int lod1BatchCount;

            public PerPrototypeBatchData()
            {
                lod0WorldTransformNA = new NativeList<Matrix4x4>(Allocator.Persistent);
                lod1WorldTransformNA = new NativeList<Matrix4x4>(Allocator.Persistent);
                lod0WorldTransformBatches = new List<GBatchTRS>();
                lod1WorldTransformBatches = new List<GBatchTRS>();
            }

            public void Dispose()
            {
                if (lod0WorldTransformNA.IsCreated)
                {
                    lod0WorldTransformNA.Dispose();
                }

                if (lod1WorldTransformNA.IsCreated)
                {
                    lod1WorldTransformNA.Dispose();
                }
            }

            public void ClearAndEnsureContainerCapacity(int capacity)
            {
                if (lod0WorldTransformNA.Capacity < capacity)
                {
                    lod0WorldTransformNA.Capacity = capacity;
                }
                if (lod1WorldTransformNA.Capacity < capacity)
                {
                    lod1WorldTransformNA.Capacity = capacity;
                }

                lod0WorldTransformNA.Clear();
                lod1WorldTransformNA.Clear();
            }

            public void CopyNativeDataToManaged()
            {
                //LOD0
                int startIndex = 0;
                int naLength = lod0WorldTransformNA.Length;
                int i = 0;
                while (startIndex < naLength)
                {
                    int batchLength = Mathf.Min(GBatchTRS.MAX_LENGTH, naLength - startIndex);
                    GBatchTRS batch = null;
                    if (i < lod0WorldTransformBatches.Count)
                    {
                        batch = lod0WorldTransformBatches[i];
                        if (batch == null)
                        {
                            batch = new GBatchTRS();
                            lod0WorldTransformBatches[i] = batch;
                        }
                    }
                    else
                    {
                        batch = new GBatchTRS();
                        lod0WorldTransformBatches.Add(batch);
                    }

                    unsafe
                    {
                        void* addOfSlice = UnsafeUtility.AddressOf(ref lod0WorldTransformNA.ElementAt(startIndex));
                        void* addOfMatrices = UnsafeUtility.AddressOf(ref batch.matrices[0]);
                        UnsafeUtility.MemCpy(addOfMatrices, addOfSlice, sizeof(Matrix4x4) * batchLength);
                    }

                    batch.length = batchLength;
                    startIndex += batchLength;
                    i += 1;
                }

                lod0BatchCount = i;

                //LOD1
                startIndex = 0;
                naLength = lod1WorldTransformNA.Length;
                i = 0;
                while (startIndex < naLength)
                {
                    int batchLength = Mathf.Min(GBatchTRS.MAX_LENGTH, naLength - startIndex);
                    GBatchTRS batch = null;
                    if (i < lod1WorldTransformBatches.Count)
                    {
                        batch = lod1WorldTransformBatches[i];
                        if (batch == null)
                        {
                            batch = new GBatchTRS();
                            lod1WorldTransformBatches[i] = batch;
                        }
                    }
                    else
                    {
                        batch = new GBatchTRS();
                        lod1WorldTransformBatches.Add(batch);
                    }

                    unsafe
                    {
                        void* addOfSlice = UnsafeUtility.AddressOf(ref lod1WorldTransformNA.ElementAt(startIndex));
                        void* addOfMatrices = UnsafeUtility.AddressOf(ref batch.matrices[0]);
                        UnsafeUtility.MemCpy(addOfMatrices, addOfSlice, sizeof(Matrix4x4) * batchLength);
                    }

                    batch.length = batchLength;
                    startIndex += batchLength;
                    i += 1;
                }

                lod1BatchCount = i;
            }
        }
    }
}
#endif
