#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Rendering;

namespace Pinwheel.Griffin.Rendering
{
    public class GTreeRenderer3
    {
        private GStylizedTerrain terrain;

        public GStylizedTerrain Terrain
        {
            get { return terrain; }
            private set { terrain = value; }
        }

        private int quadTreeDepth;

        public int QuadTreeDepth
        {
            get { return quadTreeDepth; }
            set
            {
                Debug.Assert(value > 0);
                if (quadTreeDepth != value)
                {
                    quadTreeDepth = value;
                    OnDisable();
                    OnEnable();
                }
            }
        }

        #region Per-frame render settings

        private float treeDistance;
        private float billboardStart;
        private float lod1Start;
        private float lodTransitionDistance;
        private float cullVolumeBias;
        private GTreeCullMode cullMode;
        private Vector3 cameraPosition;
        private Vector3 terrainPosition;
        private Vector3 terrainSize;
        private LightProbeUsage lightProbeUsage;
        private int frameCount;

        #endregion

        #region Managed containers

        private Plane[] frustum = new Plane[6];
        private PrototypeRenderData[] prototypeRenderData;

        #endregion

        #region Native containers

        private NativeArray<Bounds> quadTreeNA;
        private NativeArray<Bounds> quadTreeTempNA;
        private NativeArray<byte> quadTreeCullResultsNA;
        private List<PerPrototypeInstanceData> perPrototypeInstanceData = new List<PerPrototypeInstanceData>();

        #endregion

        #region Job handles & dependencies & flags

        private JobHandle nativeDataPreparationJobsHandle = default;
        private JobHandle initInstanceDataJobHandle = default;
        private JobHandle optimizeQuadTreeJobHandle = default;
        private JobHandle cellCullingJobHandle = default;
        private JobHandle instanceCullingAndLODJobHande = default;
        private JobHandle computeBatchesJobHandle = default;

        private bool isQuadTreeOptimized = false;

        #endregion

        public GTreeRenderer3(GStylizedTerrain terrain, int quadTreeDepth)
        {
            this.terrain = terrain;
            this.quadTreeDepth = quadTreeDepth;
            frameCount = 0;
        }

        public void OnEnable()
        {
            frameCount = 0;
            InitQuadTree();
            InitPrototypeData();
            InitInstanceData();
            OptimizeQuadTreeAsync();
        }

        public void OnDisable()
        {
            optimizeQuadTreeJobHandle.Complete();
            nativeDataPreparationJobsHandle.Complete();
            initInstanceDataJobHandle.Complete();
            cellCullingJobHandle.Complete();
            instanceCullingAndLODJobHande.Complete();
            computeBatchesJobHandle.Complete();

            if (quadTreeNA.IsCreated)
            {
                quadTreeNA.Dispose();
            }

            if (quadTreeTempNA.IsCreated)
            {
                quadTreeTempNA.Dispose();
            }

            if (quadTreeCullResultsNA.IsCreated)
            {
                quadTreeCullResultsNA.Dispose();
            }

            foreach (PerPrototypeInstanceData d in perPrototypeInstanceData)
            {
                d.Dispose();
            }

            perPrototypeInstanceData.Clear();
        }

        public void OnTreeChanged()
        {
            OnDisable();
            OnEnable();
        }

        public void OnPrototypesChanged()
        {
            OnDisable();
            OnEnable();
        }

        public void OnPrototypeGroupChanged()
        {
            OnDisable();
            OnEnable();
        }

        public void OnGeometryChanged()
        {
            OnDisable();
            OnEnable();
        }

        private void InitQuadTree()
        {
            cellCullingJobHandle.Complete();
            nativeDataPreparationJobsHandle.Complete();
            if (quadTreeNA.IsCreated)
            {
                quadTreeNA.Dispose();
            }

            if (quadTreeCullResultsNA.IsCreated)
            {
                quadTreeCullResultsNA.Dispose();
            }

            if (terrain.TerrainData == null)
                return;

            terrainPosition = Terrain.transform.position;
            terrainSize = Terrain.TerrainData.Geometry.Size;
            quadTreeNA = GQuadTree.Create<Bounds>(quadTreeDepth, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            quadTreeNA[0] = new Bounds(terrainPosition + terrainSize * 0.5f, terrainSize);
            InitQuadTreeJob j0 = new InitQuadTreeJob()
            {
                quadTree = quadTreeNA,
                startIndex = 0
            };
            j0.Run(1);

            for (int di = 1; di < quadTreeDepth; ++di)
            {
                InitQuadTreeJob j = new InitQuadTreeJob()
                {
                    quadTree = quadTreeNA,
                    startIndex = GQuadTree.GetFirstIndexAtDepthIndex(di)
                };

                JobHandle handle = j.Schedule(GQuadTree.GetSiblingCountAtDepthIndex(di), 32);
                handle.Complete();
            }

            quadTreeCullResultsNA = new NativeArray<byte>(quadTreeNA.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct InitQuadTreeJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<Bounds> quadTree;

            public int startIndex;

            public void Execute(int threadIndex)
            {
                int i = startIndex + threadIndex;
                int firstChildIndex = GQuadTree.GetFirstChildIndexOf(i);
                if (firstChildIndex >= quadTree.Length)
                    return;

                Bounds cell = quadTree[i];
                Vector3 min = cell.min;
                Vector3 max = cell.max;
                Vector3 center = cell.center;
                Vector3 size = cell.size;
                Vector3 halfSize = new Vector3(size.x * 0.5f, size.y, size.z * 0.5f);

                Bounds bottomLeftCell =
                    new Bounds(new Vector3((min.x + center.x) * 0.5f, center.y, (min.z + center.z) * 0.5f), halfSize);
                Bounds topLeftCell =
                    new Bounds(new Vector3((min.x + center.x) * 0.5f, center.y, (center.z + max.z) * 0.5f), halfSize);
                Bounds topRightCell =
                    new Bounds(new Vector3((center.x + max.x) * 0.5f, center.y, (center.z + max.z) * 0.5f), halfSize);
                Bounds bottomRightCell =
                    new Bounds(new Vector3((center.x + max.x) * 0.5f, center.y, (min.z + center.z) * 0.5f), halfSize);

                quadTree[firstChildIndex + 0] = bottomLeftCell;
                quadTree[firstChildIndex + 1] = topLeftCell;
                quadTree[firstChildIndex + 2] = topRightCell;
                quadTree[firstChildIndex + 3] = bottomRightCell;
            }
        }

        private void InitPrototypeData()
        {
            if (terrain.TerrainData == null ||
                terrain.TerrainData.Foliage.Trees == null)
                return;

            List<GTreePrototype> prototypes = Terrain.TerrainData.Foliage.Trees.Prototypes;
            prototypeRenderData = new PrototypeRenderData[prototypes.Count];
            perPrototypeInstanceData.Clear();
            for (int i = 0; i < prototypes.Count; ++i)
            {
                GTreePrototype p = prototypes[i];
                PrototypeRenderData prd = new PrototypeRenderData(p);
                prototypeRenderData[i] = prd;

                PerPrototypeInstanceData ppind = new PerPrototypeInstanceData();
                ppind.prototypeIndex = i;
                perPrototypeInstanceData.Add(ppind);
            }
        }

        class PerPrototypeInstanceData : System.IDisposable
        {
            public int prototypeIndex;

            public NativeList<Matrix4x4> instanceWorldTransformsNA;
            public NativeList<Bounds> instanceWorldBoundsNA;
            public NativeList<int> instanceCellIndicesNA;
            public NativeList<byte> instanceLodResultsNA;

            public NativeList<Matrix4x4> lod0WorldTransformNA;
            public NativeList<Matrix4x4> lod1WorldTransformNA;
            public NativeList<Matrix4x4> billboardWorldTransformNA;

            public List<GBatchTRS> lod0WorldTransformBatches;
            public int lod0BatchCount;
            public List<GBatchTRS> lod1WorldTransformBatches;
            public int lod1BatchCount;
            public List<GBatchTRS> billboardWorldTransformBatches;
            public int billboardBatchCount;

            public PerPrototypeInstanceData()
            {
                instanceWorldTransformsNA = new NativeList<Matrix4x4>(Allocator.Persistent);
                instanceWorldBoundsNA = new NativeList<Bounds>(Allocator.Persistent);
                instanceCellIndicesNA = new NativeList<int>(Allocator.Persistent);
                instanceLodResultsNA = new NativeList<byte>(Allocator.Persistent);

                lod0WorldTransformNA = new NativeList<Matrix4x4>(Allocator.Persistent);
                lod1WorldTransformNA = new NativeList<Matrix4x4>(Allocator.Persistent);
                billboardWorldTransformNA = new NativeList<Matrix4x4>(Allocator.Persistent);

                lod0WorldTransformBatches = new List<GBatchTRS>();
                lod1WorldTransformBatches = new List<GBatchTRS>();
                billboardWorldTransformBatches = new List<GBatchTRS>();
            }

            public void Dispose()
            {
                if (instanceWorldTransformsNA.IsCreated)
                {
                    instanceWorldTransformsNA.Dispose();
                }

                if (instanceWorldBoundsNA.IsCreated)
                {
                    instanceWorldBoundsNA.Dispose();
                }

                if (instanceCellIndicesNA.IsCreated)
                {
                    instanceCellIndicesNA.Dispose();
                }

                if (instanceLodResultsNA.IsCreated)
                {
                    instanceLodResultsNA.Dispose();
                }

                if (lod0WorldTransformNA.IsCreated)
                {
                    lod0WorldTransformNA.Dispose();
                }

                if (lod1WorldTransformNA.IsCreated)
                {
                    lod1WorldTransformNA.Dispose();
                }

                if (billboardWorldTransformNA.IsCreated)
                {
                    billboardWorldTransformNA.Dispose();
                }
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

                //Billboard
                startIndex = 0;
                naLength = billboardWorldTransformNA.Length;
                i = 0;
                while (startIndex < naLength)
                {
                    int batchLength = Mathf.Min(GBatchTRS.MAX_LENGTH, naLength - startIndex);
                    GBatchTRS batch = null;
                    if (i < billboardWorldTransformBatches.Count)
                    {
                        batch = billboardWorldTransformBatches[i];
                        if (batch == null)
                        {
                            batch = new GBatchTRS();
                            billboardWorldTransformBatches[i] = batch;
                        }
                    }
                    else
                    {
                        batch = new GBatchTRS();
                        billboardWorldTransformBatches.Add(batch);
                    }

                    unsafe
                    {
                        void* addOfSlice = UnsafeUtility.AddressOf(ref billboardWorldTransformNA.ElementAt(startIndex));
                        void* addOfMatrices = UnsafeUtility.AddressOf(ref batch.matrices[0]);
                        UnsafeUtility.MemCpy(addOfMatrices, addOfSlice, sizeof(Matrix4x4) * batchLength);
                    }

                    batch.length = batchLength;
                    startIndex += batchLength;
                    i += 1;
                }

                billboardBatchCount = i;
            }

            public void ClearAndEnsureContainerCapacity()
            {
                if (lod0WorldTransformNA.Capacity < instanceWorldBoundsNA.Length)
                {
                    lod0WorldTransformNA.Capacity = instanceWorldBoundsNA.Length;
                }
                if (lod1WorldTransformNA.Capacity < instanceWorldBoundsNA.Length)
                {
                    lod1WorldTransformNA.Capacity = instanceWorldBoundsNA.Length;
                }
                if (billboardWorldTransformNA.Capacity < instanceWorldBoundsNA.Length)
                {
                    billboardWorldTransformNA.Capacity = instanceWorldBoundsNA.Length;
                }

                lod0WorldTransformNA.Clear();
                lod1WorldTransformNA.Clear();
                billboardWorldTransformNA.Clear();
            }
        }

        private void InitInstanceData()
        {
            GTerrainData terrainData = Terrain.TerrainData;
            if (terrainData == null)
                return;

            GFoliage foliage = terrainData.Foliage;
            if (foliage.Trees == null)
                return;

            int instanceCount = foliage.TreeInstances.Count;
            if (instanceCount == 0)
                return;

            NativeArray<GTreeInstance> persistentInstancesNA = foliage.TreeInstances.ToNativeArray(Allocator.TempJob);

            for (int i = 0; i < perPrototypeInstanceData.Count; ++i)
            {
                PrototypeRenderData prototypeData = prototypeRenderData[i];
                PerPrototypeInstanceData ppind = perPrototypeInstanceData[i];
                InitPerPrototypeInstanceDataJob j = new InitPerPrototypeInstanceDataJob()
                {
                    persistentInstances = persistentInstancesNA,
                    quadTree = quadTreeNA,
                    prototypeIndex = i,
                    pivotOffset = prototypeData.pivotOffset,
                    baseBounds = prototypeData.baseBounds,
                    baseRotation = prototypeData.baseRotation,
                    baseScale = prototypeData.baseScale,
                    terrainPosition = terrain.transform.position,
                    terrainSize = terrainData.Geometry.Size,
                    quadTreeDepth = QuadTreeDepth,

                    instanceWorldTransforms = ppind.instanceWorldTransformsNA,
                    instanceWorldBounds = ppind.instanceWorldBoundsNA,
                    instanceCellIndices = ppind.instanceCellIndicesNA,
                    instanceLodResults = ppind.instanceLodResultsNA
                };
                JobHandle jhandle = j.Schedule();
                initInstanceDataJobHandle = JobHandle.CombineDependencies(initInstanceDataJobHandle, jhandle);
            }

            initInstanceDataJobHandle.Complete();
            persistentInstancesNA.Dispose(initInstanceDataJobHandle);
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct InitPerPrototypeInstanceDataJob : IJob
        {
            [ReadOnly] public NativeArray<GTreeInstance> persistentInstances;
            [ReadOnly] public NativeArray<Bounds> quadTree;

            public int prototypeIndex;
            public Bounds baseBounds;
            public float pivotOffset;
            public Quaternion baseRotation;
            public Vector3 baseScale;

            [WriteOnly] public NativeList<Matrix4x4> instanceWorldTransforms;
            [WriteOnly] public NativeList<Bounds> instanceWorldBounds;
            [WriteOnly] public NativeList<int> instanceCellIndices;
            [WriteOnly] public NativeList<byte> instanceLodResults;

            public Vector3 terrainPosition;
            public Vector3 terrainSize;
            public int quadTreeDepth;

            public void Execute()
            {
                for (int i = 0; i < persistentInstances.Length; ++i)
                {
                    GTreeInstance t = persistentInstances[i];
                    if (t.prototypeIndex != prototypeIndex)
                        continue;

                    Vector3 localPos = t.position;
                    localPos.Scale(terrainSize);
                    Vector3 worldPos = terrainPosition + localPos + Vector3.up * pivotOffset;

                    Quaternion worldRotation = baseRotation * t.rotation;
                    Vector3 worldScale = t.scale;
                    worldScale.Scale(baseScale);

                    Matrix4x4 trs = Matrix4x4.TRS(worldPos, worldRotation, worldScale);
                    instanceWorldTransforms.Add(trs);

                    Bounds b = baseBounds;
                    b.center = worldPos;
                    b.size = new Vector3(b.size.x * t.scale.x, b.size.y * t.scale.y, b.size.z * t.scale.z);
                    instanceWorldBounds.Add(b);
                    instanceLodResults.Add(InstanceLodFlags.CULLED);

                    instanceCellIndices.Add(CalculateCellIndex(i, worldPos));
                }
            }

            private int CalculateCellIndex(int index, Vector3 worldPos)
            {
                int currentCellIndex = 0;
                int currentDepthIndex = 0;
                while (currentDepthIndex < quadTreeDepth - 1)
                {
                    int firstChildIndex = GQuadTree.GetFirstChildIndexOf(currentCellIndex);
                    if (quadTree[firstChildIndex + 0].Contains(worldPos))
                    {
                        currentCellIndex = firstChildIndex + 0;
                    }
                    else if (quadTree[firstChildIndex + 1].Contains(worldPos))
                    {
                        currentCellIndex = firstChildIndex + 1;
                    }
                    else if (quadTree[firstChildIndex + 2].Contains(worldPos))
                    {
                        currentCellIndex = firstChildIndex + 2;
                    }
                    else if (quadTree[firstChildIndex + 3].Contains(worldPos))
                    {
                        currentCellIndex = firstChildIndex + 3;
                    }
                    else
                    {
                        break;
                    }

                    currentDepthIndex += 1;
                }

                return currentCellIndex;
            }
        }

        private void OptimizeQuadTreeAsync()
        {
            isQuadTreeOptimized = false;
            quadTreeTempNA =
                new NativeArray<Bounds>(quadTreeNA.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            OptimizeQuadTreeInitJob initJob = new OptimizeQuadTreeInitJob()
            {
                quadTree = quadTreeNA,
                quadTreeTemp = quadTreeTempNA
            };
            JobHandle initJobHandle = initJob.Schedule(quadTreeNA.Length, 1000, optimizeQuadTreeJobHandle);
            optimizeQuadTreeJobHandle = JobHandle.CombineDependencies(initJobHandle, optimizeQuadTreeJobHandle);

            foreach (PerPrototypeInstanceData ppid in perPrototypeInstanceData)
            {
                EncapsulateQuadTreeCellsToInstanceWorldBoundsJob job =
                    new EncapsulateQuadTreeCellsToInstanceWorldBoundsJob()
                    {
                        quadTreeTemp = quadTreeTempNA,
                        instanceWorldBounds = ppid.instanceWorldBoundsNA.AsArray(),
                        instanceCellIndices = ppid.instanceCellIndicesNA.AsArray()
                    };
                JobHandle jHandle = job.Schedule(optimizeQuadTreeJobHandle);
                optimizeQuadTreeJobHandle = JobHandle.CombineDependencies(optimizeQuadTreeJobHandle, jHandle);
            }

            EncapsulateQuadTreeCellsToChildrenCells encapsulateChildrenCellsJob = new EncapsulateQuadTreeCellsToChildrenCells()
            {
                quadTreeTemp = quadTreeTempNA,
                quadTreeDepth = quadTreeDepth
            };
            JobHandle encapsulateChildrenCellsJobHandle = encapsulateChildrenCellsJob.Schedule(optimizeQuadTreeJobHandle);
            optimizeQuadTreeJobHandle = JobHandle.CombineDependencies(encapsulateChildrenCellsJobHandle, optimizeQuadTreeJobHandle);
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct OptimizeQuadTreeInitJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Bounds> quadTree;
            [WriteOnly]
            public NativeArray<Bounds> quadTreeTemp;

            public void Execute(int index)
            {
                Bounds b = quadTree[index];
                b.size = Vector3.zero;
                quadTreeTemp[index] = b;
            }
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct EncapsulateQuadTreeCellsToInstanceWorldBoundsJob : IJob
        {
            [ReadOnly] public NativeArray<Bounds> instanceWorldBounds;
            [ReadOnly] public NativeArray<int> instanceCellIndices;

            public NativeArray<Bounds> quadTreeTemp;

            public void Execute()
            {
                for (int i = 0; i < instanceWorldBounds.Length; ++i)
                {
                    Bounds b = instanceWorldBounds[i];
                    int cellIndex = instanceCellIndices[i];
                    Bounds cell = quadTreeTemp[cellIndex];
                    if (cell.size == Vector3.zero)
                    {
                        quadTreeTemp[cellIndex] = b;
                    }
                    else
                    {
                        cell.Encapsulate(b);
                        quadTreeTemp[cellIndex] = cell;
                    }
                }
            }
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct EncapsulateQuadTreeCellsToChildrenCells : IJob
        {
            public NativeArray<Bounds> quadTreeTemp;
            public int quadTreeDepth;

            public void Execute()
            {
                NativeList<Bounds> boundsList = new NativeList<Bounds>(4, Allocator.Temp);
                for (int di = quadTreeDepth - 1 - 1; di >= 0; --di)
                {
                    int firstCellIndex = GQuadTree.GetFirstIndexAtDepthIndex(di);
                    int lastCellIndex = firstCellIndex + GQuadTree.GetSiblingCountAtDepthIndex(di) - 1;
                    for (int ci = firstCellIndex; ci <= lastCellIndex; ++ci)
                    {
                        int firstChildIndex = GQuadTree.GetFirstChildIndexOf(ci);
                        Bounds b0 = quadTreeTemp[firstChildIndex];
                        Bounds b1 = quadTreeTemp[firstChildIndex + 1];
                        Bounds b2 = quadTreeTemp[firstChildIndex + 2];
                        Bounds b3 = quadTreeTemp[firstChildIndex + 3];

                        boundsList.Clear();
                        if (b0.size != Vector3.zero) boundsList.AddNoResize(b0);
                        if (b1.size != Vector3.zero) boundsList.AddNoResize(b1);
                        if (b2.size != Vector3.zero) boundsList.AddNoResize(b2);
                        if (b3.size != Vector3.zero) boundsList.AddNoResize(b3);

                        if (boundsList.Length == 0)
                            continue;

                        Bounds b = boundsList[0];
                        for (int bi = 1; bi < boundsList.Length; ++bi)
                        {
                            b.Encapsulate(boundsList[bi]);
                        }
                        quadTreeTemp[ci] = b;
                    }
                }
                boundsList.Dispose();
            }
        }

        private void TryFinishingQuadTreeOptimizationJob()
        {
            if (isQuadTreeOptimized)
                return;

            if (optimizeQuadTreeJobHandle.IsCompleted &&
                quadTreeTempNA.IsCreated &&
                quadTreeTempNA.Length == quadTreeNA.Length)
            {
                optimizeQuadTreeJobHandle.Complete();
                cellCullingJobHandle.Complete();
                instanceCullingAndLODJobHande.Complete();
                quadTreeTempNA.CopyTo(quadTreeNA);
                isQuadTreeOptimized = true;
            }
        }

        public void Render(Camera cam)
        {
            if (TrySkipRenderingOnGeneralCases())
                return;
            TryFinishingQuadTreeOptimizationJob();

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
                //At the first frame there is no managed batch data just yet,
                //so we start calculating them right away and force jobs to be completed within the frame
                //to avoid flickering
                //Some actions will trigger renderer reset (OnDisable->OnEnable) such as changing or modifying prototype group asset, paint trees, etc.
                //When that happens, frameCount will be set to 0
                CullCells();
                CullInstanceAndComputeLOD();
                ComputeBatches();
                nativeDataPreparationJobsHandle.Complete();
                CopyNativeDataToManaged();
            }
            else
            {
                //At latter frames, just use the available managed batch data calculated from the last frame
                //If the data preps jobs scheduled from the last frame have been completed, 
                //we refresh managed data and start new data preps jobs
                //Batch data may not at 100% aligned with the current view, but the latency is more than acceptable.
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
                        CullInstanceAndComputeLOD();
                        ComputeBatches();
                    }
                }
            }

            DrawInstances(cam);
            frameCount += 1;
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
            if (!r.DrawTrees)
                return true;
            if (r.TreeDistance == 0)
                return true;

            GFoliage f = terrainData.Foliage;
            if (f.Trees == null)
                return true;
            if (f.Trees.Prototypes.Count == 0)
                return true;
            if (f.TreeInstances.Count == 0)
                return true;

            return false;
        }

        private void SetPerFrameRenderSettings()
        {
            GTerrainData terrainData = Terrain.TerrainData;
            GRendering r = terrainData.Rendering;
            GRuntimeSettings runtimeSettings = GRuntimeSettings.Instance;
            treeDistance = r.TreeDistance;
            billboardStart = r.BillboardStart;
            lod1Start = r.TreeLod1Start;
            lodTransitionDistance = runtimeSettings.renderingDefault.treeLodTransitionDistance;
            cullVolumeBias = runtimeSettings.renderingDefault.treeCullBias;
            cullMode = runtimeSettings.renderingDefault.treeCullMode;
            terrainPosition = Terrain.transform.position;
            lightProbeUsage = runtimeSettings.renderingDefault.treeLightProbeUsage;
        }

        private void SetView(Camera cam)
        {
            GFrustumUtilities.Calculate(cam, frustum, treeDistance);
            cameraPosition = cam.transform.position;
        }

        private void CopyNativeDataToManaged()
        {
            foreach (PerPrototypeInstanceData ppind in perPrototypeInstanceData)
            {
                ppind.CopyNativeDataToManaged();
            }
        }

        private void CullCells()
        {
            Bounds cell0 = quadTreeNA[0];
            cell0.Expand(cullVolumeBias);
            quadTreeCullResultsNA[0] = GCullUtils.TestFrustumAABB(ref frustum[0], ref frustum[1], ref frustum[2], ref frustum[3], ref frustum[4], ref frustum[5], ref cell0);

            CullQuadTreeJob j0 = new CullQuadTreeJob()
            {
                quadTree = quadTreeNA,
                quadTreeCullResult = quadTreeCullResultsNA,
                plane0 = frustum[0],
                plane1 = frustum[1],
                plane2 = frustum[2],
                plane3 = frustum[3],
                plane4 = frustum[4],
                plane5 = frustum[5],
                startIndex = 0,
                volumeBias = cullVolumeBias
            };
            j0.Run(1);

            for (int di = 0; di < quadTreeDepth; ++di)
            {
                CullQuadTreeJob j = new CullQuadTreeJob()
                {
                    quadTree = quadTreeNA,
                    quadTreeCullResult = quadTreeCullResultsNA,
                    plane0 = frustum[0],
                    plane1 = frustum[1],
                    plane2 = frustum[2],
                    plane3 = frustum[3],
                    plane4 = frustum[4],
                    plane5 = frustum[5],
                    startIndex = GQuadTree.GetFirstIndexAtDepthIndex(di),
                    volumeBias = cullVolumeBias
                };

                int elementCount = GQuadTree.GetSiblingCountAtDepthIndex(di);
                JobHandle handle = j.Schedule(elementCount, 1000, cellCullingJobHandle);
                cellCullingJobHandle = handle;
            }

            nativeDataPreparationJobsHandle =
                JobHandle.CombineDependencies(nativeDataPreparationJobsHandle, cellCullingJobHandle);
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct CullQuadTreeJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Bounds> quadTree;

            [NativeDisableParallelForRestriction] public NativeArray<byte> quadTreeCullResult;

            public Plane plane0, plane1, plane2, plane3, plane4, plane5;

            public int startIndex;
            public float volumeBias;

            public void Execute(int threadIndex)
            {
                int i = startIndex + threadIndex;

                int firstChildIndex = GQuadTree.GetFirstChildIndexOf(i);
                if (firstChildIndex >= quadTree.Length)
                    return;

                byte selfCullResult = quadTreeCullResult[i];
                if (selfCullResult == GCullFlags.CULLED)
                {
                    quadTreeCullResult[firstChildIndex + 0] = GCullFlags.CULLED;
                    quadTreeCullResult[firstChildIndex + 1] = GCullFlags.CULLED;
                    quadTreeCullResult[firstChildIndex + 2] = GCullFlags.CULLED;
                    quadTreeCullResult[firstChildIndex + 3] = GCullFlags.CULLED;
                }
                else if (selfCullResult == GCullFlags.VISIBLE)
                {
                    quadTreeCullResult[firstChildIndex + 0] = GCullFlags.VISIBLE;
                    quadTreeCullResult[firstChildIndex + 1] = GCullFlags.VISIBLE;
                    quadTreeCullResult[firstChildIndex + 2] = GCullFlags.VISIBLE;
                    quadTreeCullResult[firstChildIndex + 3] = GCullFlags.VISIBLE;
                }
                else
                {
                    quadTreeCullResult[firstChildIndex + 0] =
                        CullCell(quadTree[firstChildIndex + 0], volumeBias);
                    quadTreeCullResult[firstChildIndex + 1] =
                        CullCell(quadTree[firstChildIndex + 1], volumeBias);
                    quadTreeCullResult[firstChildIndex + 2] =
                        CullCell(quadTree[firstChildIndex + 2], volumeBias);
                    quadTreeCullResult[firstChildIndex + 3] =
                        CullCell(quadTree[firstChildIndex + 3], volumeBias);
                }
            }

            private byte CullCell(Bounds cell, float bias)
            {
                if (cell.size == Vector3.zero)
                {
                    return GCullFlags.CULLED;
                }
                else
                {
                    cell.Expand(bias);
                    return GCullUtils.TestFrustumAABB(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref cell);
                }
            }
        }

        private void CullInstanceAndComputeLOD()
        {
            for (int i = 0; i < perPrototypeInstanceData.Count; ++i)
            {
                PerPrototypeInstanceData ppind = perPrototypeInstanceData[i];
                PrototypeRenderData renderData = prototypeRenderData[i];
                float bbStart = billboardStart;
                float l1Start = lod1Start;

                InstanceCullingAndLODJob job = new InstanceCullingAndLODJob()
                {
                    quadTreeCullResults = quadTreeCullResultsNA,
                    plane0 = frustum[0],
                    plane1 = frustum[1],
                    plane2 = frustum[2],
                    plane3 = frustum[3],
                    plane4 = frustum[4],
                    plane5 = frustum[5],
                    instanceWorldBounds = ppind.instanceWorldBoundsNA.AsArray(),
                    instanceCellIndices = ppind.instanceCellIndicesNA.AsArray(),
                    instanceLodResults = ppind.instanceLodResultsNA.AsArray(),

                    cullVolumeBias = cullVolumeBias,
                    cameraPosition = cameraPosition,
                    lodTransitionDistance = lodTransitionDistance,
                    billboardStartDistance = bbStart,
                    lod1StartDistance = l1Start,
                    cullMode = cullMode,

                    hasLod1 = renderData.lod1 != null,
                    hasBillboard = renderData.billboard != null
                };

                JobHandle jhandle = job.Schedule(ppind.instanceWorldBoundsNA.Length, 10000,
                    nativeDataPreparationJobsHandle);
                instanceCullingAndLODJobHande = JobHandle.CombineDependencies(jhandle, instanceCullingAndLODJobHande);
            }

            nativeDataPreparationJobsHandle =
                JobHandle.CombineDependencies(nativeDataPreparationJobsHandle, instanceCullingAndLODJobHande);
        }

        struct InstanceLodFlags
        {
            public const byte CULLED = 0;
            public const byte LOD0 = 1;
            public const byte LOD1 = 2;
            public const byte BILLBOARD = 255;
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct InstanceCullingAndLODJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<byte> quadTreeCullResults;
            [ReadOnly] public NativeArray<Bounds> instanceWorldBounds;
            [ReadOnly] public NativeArray<int> instanceCellIndices;

            [WriteOnly] public NativeArray<byte> instanceLodResults;


            public Plane plane0, plane1, plane2, plane3, plane4, plane5;
            public float cullVolumeBias;
            public float lodTransitionDistance;
            public float lod1StartDistance;
            public float billboardStartDistance;
            public Vector3 cameraPosition;
            public GTreeCullMode cullMode;
            public bool hasLod1;
            public bool hasBillboard;

            public void Execute(int index)
            {
                Bounds instanceBounds = instanceWorldBounds[index];
                int cellIndex = instanceCellIndices[index];
                byte cellCullResult = quadTreeCullResults[cellIndex];
                bool isInstanceVisible = false;
                if (cellCullResult == GCullFlags.PARTIALLY_VISIBLE)
                {
                    if (cullMode == GTreeCullMode.Precise)
                    {
                        Bounds b = instanceBounds;
                        b.Expand(cullVolumeBias);
                        isInstanceVisible = GCullUtils.TestFrustumAABB(ref plane0, ref plane1, ref plane2, ref plane3, ref plane4, ref plane5, ref b) !=
                                            GCullFlags.CULLED;
                    }
                    else
                    {
                        isInstanceVisible = true;
                    }
                }
                else if (cellCullResult == GCullFlags.VISIBLE)
                {
                    isInstanceVisible = true;
                }

                byte lodFlag = InstanceLodFlags.CULLED;
                if (isInstanceVisible)
                {
                    Vector3 worldPos = instanceBounds.center;
                    float sqrDistanceToCamera = Vector3.SqrMagnitude(worldPos - cameraPosition);
                    if (sqrDistanceToCamera < lod1StartDistance * lod1StartDistance)
                    {
                        lodFlag = InstanceLodFlags.LOD0;
                    }
                    else if (sqrDistanceToCamera < billboardStartDistance * billboardStartDistance)
                    {
                        float d2cam = Mathf.Sqrt(sqrDistanceToCamera);
                        if (d2cam - lod1StartDistance > lodTransitionDistance)
                        {
                            lodFlag = InstanceLodFlags.LOD1;
                        }
                        else
                        {
                            float noise = Mathf.PerlinNoise(worldPos.x, worldPos.z);
                            float f = (d2cam - lod1StartDistance) / (lodTransitionDistance + 0.01f);
                            float v = Mathf.Clamp01((1 - f) + noise);
                            lodFlag = v > 0.95f ? InstanceLodFlags.LOD0 : InstanceLodFlags.LOD1;
                        }
                    }
                    else
                    {
                        float d2cam = Mathf.Sqrt(sqrDistanceToCamera);
                        if (d2cam - billboardStartDistance > lodTransitionDistance)
                        {
                            lodFlag = InstanceLodFlags.BILLBOARD;
                        }
                        else
                        {
                            float noise = Mathf.PerlinNoise(worldPos.x, worldPos.z);
                            float f = (d2cam - billboardStartDistance) / (lodTransitionDistance + 0.01f);
                            float v = Mathf.Clamp01((1 - f) + noise);
                            lodFlag = v > 0.95f ? InstanceLodFlags.LOD1 : InstanceLodFlags.BILLBOARD;
                        }
                    }
                }
                else
                {
                    lodFlag = InstanceLodFlags.CULLED;
                }

                if (lodFlag == InstanceLodFlags.BILLBOARD && !hasBillboard)
                {
                    if (hasLod1)
                        lodFlag = InstanceLodFlags.LOD1;
                    else
                        lodFlag = InstanceLodFlags.LOD0;
                }
                else if (lodFlag == InstanceLodFlags.LOD1 && !hasLod1)
                {
                    lodFlag = InstanceLodFlags.LOD0;
                }

                instanceLodResults[index] = lodFlag;
            }
        }

        private void ComputeBatches()
        {
            for (int i = 0; i < perPrototypeInstanceData.Count; ++i)
            {
                PerPrototypeInstanceData ppind = perPrototypeInstanceData[i];
                ppind.ClearAndEnsureContainerCapacity();

                ComputeBatchesJob job = new ComputeBatchesJob()
                {
                    instanceWorldTransforms = ppind.instanceWorldTransformsNA,
                    instanceLodResults = ppind.instanceLodResultsNA,

                    lod0WorldTransforms = ppind.lod0WorldTransformNA.AsParallelWriter(),
                    lod1WorldTransforms = ppind.lod1WorldTransformNA.AsParallelWriter(),
                    billboardWorldTransforms = ppind.billboardWorldTransformNA.AsParallelWriter()
                };

                JobHandle jhandle = job.Schedule(ppind.instanceWorldTransformsNA.Length, 10000,
                    nativeDataPreparationJobsHandle);
                computeBatchesJobHandle = JobHandle.CombineDependencies(jhandle, computeBatchesJobHandle);
            }

            nativeDataPreparationJobsHandle =
                JobHandle.CombineDependencies(nativeDataPreparationJobsHandle, computeBatchesJobHandle);
        }

#if GRIFFIN_BURST
        [BurstCompile(CompileSynchronously = true)]
#endif
        struct ComputeBatchesJob : IJobParallelFor
        {
            [ReadOnly] public NativeList<Matrix4x4> instanceWorldTransforms;
            [ReadOnly] public NativeList<byte> instanceLodResults;

            public NativeList<Matrix4x4>.ParallelWriter lod0WorldTransforms;
            public NativeList<Matrix4x4>.ParallelWriter lod1WorldTransforms;
            public NativeList<Matrix4x4>.ParallelWriter billboardWorldTransforms;

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
                else if (lod == InstanceLodFlags.BILLBOARD)
                {
                    billboardWorldTransforms.AddNoResize(trs);
                }
            }
        }

        public void DrawDebug(Camera cam)
        {
            if (frameCount == 0)
                return;

            #region draw quad tree
            //UnityEditor.Handles.EndGUI();
            //MaterialPropertyBlock debugPropertyBlock = new MaterialPropertyBlock();
            //for (int di = 0; di < quadTreeDepth; ++di)
            ////for (int di = quadTreeDepth - 1; di >= 0; --di)
            //{
            //    int targetDepth = (frameCount / 10) % quadTreeDepth;
            //    if (targetDepth != di)
            //        continue;

            //    int startIndex = GQuadTree.GetFirstIndexAtDepthIndex(di);
            //    int count = GQuadTree.GetSiblingCountAtDepthIndex(di);
            //    for (int i = 0; i < count; ++i)
            //    {
            //        byte cullResult = d_quadTreeCullResults[startIndex + i];
            //        //if (cullResult == CellCullFlags.CULLED)
            //        //    continue;

            //        Bounds cell = quadTreeNA[startIndex + i];

            //        Color color = cullResult == GCullFlags.VISIBLE ? Color.green :
            //                        cullResult == GCullFlags.PARTIALLY_VISIBLE ? Color.yellow : Color.red;
            //        color.a = 0.9f;
            //        UnityEditor.Handles.color = color;
            //        UnityEditor.Handles.zTest = CompareFunction.LessEqual;
            //        UnityEditor.Handles.DrawWireCube(cell.center, cell.size);
            //    }
            //}

            #endregion
        }

        private void DrawInstances(Camera cam)
        {
            for (int i = 0; i < perPrototypeInstanceData.Count; ++i)
            {
                PerPrototypeInstanceData ppid = perPrototypeInstanceData[i];
                PrototypeRenderData renderData = prototypeRenderData[i];
                PrototypeRenderData.LodInfo lod0 = renderData.lod0;
                if (lod0 != null)
                {
                    for (int bi = 0; bi < ppid.lod0BatchCount; ++bi)
                    {
                        GBatchTRS batch = ppid.lod0WorldTransformBatches[bi];
                        DrawBatch(cam, lod0.mesh, lod0.materials, batch, renderData, lod0);
                    }
                }

                PrototypeRenderData.LodInfo lod1 = renderData.lod1;
                if (lod1 != null)
                {
                    for (int bi = 0; bi < ppid.lod1BatchCount; ++bi)
                    {
                        GBatchTRS batch = ppid.lod1WorldTransformBatches[bi];
                        DrawBatch(cam, lod1.mesh, lod1.materials, batch, renderData, lod1);
                    }
                }


                PrototypeRenderData.LodInfo billboard = renderData.billboard;
                if (billboard != null)
                {
                    renderData.UpdateBillboardInfo(); //hack to get around null billboard mesh at startup
                    for (int bi = 0; bi < ppid.billboardBatchCount; ++bi)
                    {
                        GBatchTRS batch = ppid.billboardWorldTransformBatches[bi];
                        DrawBatch(cam, billboard.mesh, billboard.materials, batch, renderData, billboard);
                    }
                }
            }
        }

        private void DrawBatch(Camera cam, Mesh mesh, Material[] materials, GBatchTRS batch,
            PrototypeRenderData renderData, PrototypeRenderData.LodInfo lodInfo)
        {
            for (int mi = 0; mi < materials.Length; ++mi)
            {
                Material mat = materials[mi];
                if (mat.enableInstancing)
                {
                    Graphics.DrawMeshInstanced(
                        mesh, mi, mat,
                        batch.matrices, batch.length, null,
                        lodInfo.castShadow, lodInfo.receiveShadow,
                        renderData.layer, cam,
                        lightProbeUsage);
                }
                else
                {
                    for (int i = 0; i < batch.length; ++i)
                    {
                        Graphics.DrawMesh(
                            mesh, batch.matrices[i], mat,
                            renderData.layer, cam, mi, null,
                            lodInfo.castShadow, lodInfo.receiveShadow,
                            null,
                            lightProbeUsage,
                            null);
                    }
                }
            }
        }

        class PrototypeRenderData
        {
            public class LodInfo
            {
                public Mesh mesh;
                public Material[] materials;
                public ShadowCastingMode castShadow;
                public bool receiveShadow;
            }

            public Bounds baseBounds;
            public float pivotOffset;
            public Quaternion baseRotation;
            public Vector3 baseScale;
            public int layer;

            private BillboardAsset billboardAsset;

            public LodInfo lod0;
            public LodInfo lod1;
            public LodInfo billboard;

            private List<Vector4> billboardImageTexcoords = new List<Vector4>();

            public PrototypeRenderData(GTreePrototype p)
            {
                pivotOffset = p.PivotOffset;
                baseRotation = p.BaseRotation;
                baseScale = p.BaseScale;
                baseBounds = p.BaseBounds;
                layer = p.Layer;
                billboardAsset = p.Billboard;

                lod0 = GetLod0Info(p);
                lod1 = GetLod1Info(p);
                billboard = GetBillboardInfo(p);
            }

            private LodInfo GetLod0Info(GTreePrototype p)
            {
                if (!p.HasLod0)
                    return null;

                LodInfo info = new LodInfo();
                info.mesh = p.SharedMesh;
                info.materials = p.SharedMaterials;
                info.castShadow = p.ShadowCastingMode;
                info.receiveShadow = p.ReceiveShadow;
                return info;
            }

            private LodInfo GetLod1Info(GTreePrototype p)
            {
                if (!p.HasLod1)
                    return null;

                LodInfo info = new LodInfo();
                info.mesh = p.SharedMeshLod1;
                info.materials = p.SharedMaterialsLod1;
                info.castShadow = p.ShadowCastingMode;
                info.receiveShadow = p.ReceiveShadow;
                return info;
            }

            private LodInfo GetBillboardInfo(GTreePrototype p)
            {
                if (!p.IsValid)
                    return null;
                if (p.Billboard == null)
                    return null;

                Mesh bbMesh = GBillboardUtilities.GetMesh(p.Billboard);
                Material bbMat = p.Billboard.material;
                bbMat.SetVectorArray(MaterialProperties.BILLBOARD_IMAGE_TEXCOORDS, p.Billboard.GetImageTexCoords());
                bbMat.SetInt(MaterialProperties.BILLBOARD_IMAGE_COUNT, p.Billboard.imageCount);

                LodInfo info = new LodInfo();
                info.mesh = bbMesh;
                info.materials = new Material[] { bbMat };
                info.castShadow = p.BillboardShadowCastingMode;
                info.receiveShadow = p.BillboardReceiveShadow;

                return info;
            }

            public void UpdateBillboardInfo()
            {
                if (billboard != null)
                {
                    if (billboard.mesh == null)
                    {
                        billboard.mesh = GBillboardUtilities.GetMesh(billboardAsset);
                    }
                    Material bbMat = billboard.materials[0];
                    billboardAsset.GetImageTexCoords(billboardImageTexcoords);
                    bbMat.SetVectorArray(MaterialProperties.BILLBOARD_IMAGE_TEXCOORDS, billboardImageTexcoords);
                    bbMat.SetInt(MaterialProperties.BILLBOARD_IMAGE_COUNT, billboardAsset.imageCount);
                }
            }
        }

        struct MaterialProperties
        {
            public static readonly int BILLBOARD_IMAGE_TEXCOORDS = Shader.PropertyToID("_ImageTexcoords");
            public static readonly int BILLBOARD_IMAGE_COUNT = Shader.PropertyToID("_ImageCount");
        }
    }
}
#endif