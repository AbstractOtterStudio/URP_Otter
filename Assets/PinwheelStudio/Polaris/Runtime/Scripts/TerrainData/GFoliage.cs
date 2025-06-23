#if GRIFFIN
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Collections;
using Pinwheel.Griffin.Rendering;
using Pinwheel.Griffin.Compression;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// Contains information about a terrain foliage prototypes, instances, snapping mode, etc.
    /// This should not be used alone without a parent terrain data. In most cases you don't instantiate it with ScriptableObject.Create(). The correct way is to accessing it with GTerrainData.Foliage.
    /// </summary>
    public class GFoliage : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        private GTerrainData terrainData;
        /// <summary>
        /// The parent terrain data this object belongs to.
        /// </summary>
        public GTerrainData TerrainData
        {
            get
            {
                return terrainData;
            }
            internal set
            {
                terrainData = value;
            }
        }

        [SerializeField]
        private GTreePrototypeGroup trees;
        /// <summary>
        /// The tree prototype group assigned to this.
        /// </summary>
        public GTreePrototypeGroup Trees
        {
            get
            {
                return trees;
            }
            set
            {
                GTreePrototypeGroup oldValue = trees;
                GTreePrototypeGroup newValue = value;
                trees = value;
                if (oldValue != newValue)
                {
                    if (TerrainData != null)
                    {
                        TerrainData.InvokeTreePrototypeGroupChanged();
                    }
                }
            }
        }

        [SerializeField]
        private List<GTreeInstance> treeInstances;
        /// <summary>
        /// Direct reference to the tree instances list.
        /// Don't add tree to this list directly, use AddTreeInstances() instead. 
        /// </summary>
        public List<GTreeInstance> TreeInstances
        {
            get
            {
                if (treeInstances == null)
                    treeInstances = new List<GTreeInstance>();
                return treeInstances;
            }
            set
            {
                treeInstances = value;
            }
        }

        [SerializeField]
        private GSnapMode treeSnapMode;
        /// <summary>
        /// The behavior when perform snapping tree onto surface below.
        /// </summary>
        public GSnapMode TreeSnapMode
        {
            get
            {
                return treeSnapMode;
            }
            set
            {
                treeSnapMode = value;
            }
        }

        [SerializeField]
        private LayerMask treeSnapLayerMask;
        /// <summary>
        /// Collider layers to use when tree snapping mode set to World.
        /// </summary>
        public LayerMask TreeSnapLayerMask
        {
            get
            {
                return treeSnapLayerMask;
            }
            set
            {
                treeSnapLayerMask = value;
            }
        }

        [SerializeField]
        private GGrassPrototypeGroup grasses;
        /// <summary>
        /// The grass prototype group assigned to this.
        /// </summary>
        public GGrassPrototypeGroup Grasses
        {
            get
            {
                return grasses;
            }
            set
            {
                GGrassPrototypeGroup oldValue = grasses;
                GGrassPrototypeGroup newValue = value;
                grasses = newValue;
                if (oldValue != newValue)
                {
                    if (TerrainData != null)
                    {
                        TerrainData.InvokeGrassPrototypeGroupChanged();
                    }
                }
            }
        }

        [SerializeField]
        private GSnapMode grassSnapMode;
        /// <summary>
        /// The behavior when performing snapping grass onto surface below.
        /// </summary>
        public GSnapMode GrassSnapMode
        {
            get
            {
                return grassSnapMode;
            }
            set
            {
                grassSnapMode = value;
            }
        }

        [SerializeField]
        private LayerMask grassSnapLayerMask;
        /// <summary>
        /// Collider layers to use when grass snapping mode set to World.
        /// </summary>
        public LayerMask GrassSnapLayerMask
        {
            get
            {
                return grassSnapLayerMask;
            }
            set
            {
                grassSnapLayerMask = value;
            }
        }

        [SerializeField]
        private int patchGridSize;
        /// <summary>
        /// The terrain divides grass to several patches. This value determine size of the patch grid.
        /// Ex: PatchGridSize = 16 -> 16x16=256 patches.
        /// </summary>
        public int PatchGridSize
        {
            get
            {
                return patchGridSize;
            }
            set
            {
                int oldValue = patchGridSize;
                int newValue = Mathf.Clamp(value, 1, 20);

                patchGridSize = newValue;
                if (oldValue != newValue)
                {
                    if (grassPatches != null)
                    {
                        ResampleGrassPatches();
                    }
                    if (TerrainData != null)
                    {
                        TerrainData.InvokeGrassPatchGridSizeChange();
                    }
                }
            }
        }

        [SerializeField]
        private GGrassPatch[] grassPatches;
        /// <summary>
        /// Direct reference to the grass patches array, which contains grass instances within an area.
        /// </summary>
        public GGrassPatch[] GrassPatches
        {
            get
            {
                if (grassPatches == null)
                {
                    grassPatches = new GGrassPatch[PatchGridSize * PatchGridSize];
                    for (int x = 0; x < PatchGridSize; ++x)
                    {
                        for (int z = 0; z < patchGridSize; ++z)
                        {
                            GGrassPatch patch = new GGrassPatch(this, x, z);
                            grassPatches[GUtilities.To1DIndex(x, z, PatchGridSize)] = patch;
                        }
                    }
                }
                if (grassPatches.Length != PatchGridSize * PatchGridSize)
                {
                    ResampleGrassPatches();
                }
                return grassPatches;
            }
        }

        private List<Rect> treeDirtyRegions;
        private List<Rect> TreeDirtyRegions
        {
            get
            {
                if (treeDirtyRegions == null)
                {
                    treeDirtyRegions = new List<Rect>();
                }
                return treeDirtyRegions;
            }
            set
            {
                treeDirtyRegions = value;
            }
        }

        private List<Rect> grassDirtyRegions;
        private List<Rect> GrassDirtyRegions
        {
            get
            {
                if (grassDirtyRegions == null)
                {
                    grassDirtyRegions = new List<Rect>();
                }
                return grassDirtyRegions;
            }
            set
            {
                grassDirtyRegions = value;
            }
        }

        [SerializeField]
        private bool enableInteractiveGrass;
        /// <summary>
        /// Turn this on if you want grass to react to a game object with GInteractiveGrassAgent component attached.
        /// </summary>
        public bool EnableInteractiveGrass
        {
            get
            {
                return enableInteractiveGrass;
            }
            set
            {
                enableInteractiveGrass = value;
            }
        }

        [SerializeField]
        private int vectorFieldMapResolution;
        /// <summary>
        /// Size of the texture used for interactive grass.
        /// </summary>
        public int VectorFieldMapResolution
        {
            get
            {
                return vectorFieldMapResolution;
            }
            set
            {
                vectorFieldMapResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(value), GCommon.TEXTURE_SIZE_MIN, GCommon.TEXTURE_SIZE_MAX);
            }
        }

        [SerializeField]
        private float bendSensitive;
        /// <summary>
        /// How fast grass bend when a GInteractiveGrassAgent object is nearby.
        /// </summary>
        public float BendSensitive
        {
            get
            {
                return bendSensitive;
            }
            set
            {
                bendSensitive = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float restoreSensitive;
        /// <summary>
        /// How fast grass restore to their original orientation when a GInteractiveGrassAgent goes away.
        /// </summary>
        public float RestoreSensitive
        {
            get
            {
                return restoreSensitive;
            }
            set
            {
                restoreSensitive = Mathf.Clamp01(value);
            }
        }

        /// <summary>
        /// Get the total number of grass instances.
        /// </summary>
        public int GrassInstanceCount
        {
            get
            {
                GGrassPatch[] patches = GrassPatches;
                int count = 0;
                for (int i = 0; i < patches.Length; ++i)
                {
                    count += patches[i].Instances.Count;
                }
                return count;
            }
        }

        [ExcludeFromDoc]
        public float grassVersion;
        [ExcludeFromDoc]
        public const float GRASS_VERSION_COMPRESSED = 2020.1f;

        [ExcludeFromDoc]
        public void Reset()
        {
            name = "Foliage";
            TreeSnapMode = GRuntimeSettings.Instance.foliageDefault.treeSnapMode;
            TreeSnapLayerMask = GRuntimeSettings.Instance.foliageDefault.treeSnapLayerMask;
            GrassSnapMode = GRuntimeSettings.Instance.foliageDefault.grassSnapMode;
            GrassSnapLayerMask = GRuntimeSettings.Instance.foliageDefault.grassSnapLayerMask;
            PatchGridSize = GRuntimeSettings.Instance.foliageDefault.patchGridSize;
            EnableInteractiveGrass = GRuntimeSettings.Instance.foliageDefault.enableInteractiveGrass;
            VectorFieldMapResolution = GRuntimeSettings.Instance.foliageDefault.vectorFieldMapResolution;
            BendSensitive = GRuntimeSettings.Instance.foliageDefault.bendSensitive;
            RestoreSensitive = GRuntimeSettings.Instance.foliageDefault.restoreSensitive;
            ClearGrassInstances();
            ClearTreeInstances();

            grassVersion = GVersionInfo.Number;
        }

        [ExcludeFromDoc]
        public void ResetFull()
        {
            Reset();
        }

        [ExcludeFromDoc]
        public void Refresh()
        {
            //if (Trees != null)
            //{
            //    List<GTreePrototype> prototypes = Trees.Prototypes;
            //    //for (int i = 0; i < prototypes.Count; ++i)
            //    //{
            //    //    prototypes[i].Refresh();
            //    //}
            //    RemoveTreeInstances(t => t.PrototypeIndex < 0 || t.PrototypeIndex >= Trees.Prototypes.Count);
            //}
            //if (Grasses != null)
            //{
            //    for (int i = 0; i < GrassPatches.Length; ++i)
            //    {
            //        GrassPatches[i].RemoveInstances(g => g.PrototypeIndex < 0 || g.PrototypeIndex >= Grasses.Prototypes.Count);
            //    }
            //}
        }

        private void ResampleGrassPatches()
        {
            List<GGrassInstance> grassInstances = new List<GGrassInstance>();
            for (int i = 0; i < grassPatches.Length; ++i)
            {
                grassInstances.AddRange(grassPatches[i].Instances);
            }

            grassPatches = new GGrassPatch[PatchGridSize * PatchGridSize];
            for (int x = 0; x < PatchGridSize; ++x)
            {
                for (int z = 0; z < patchGridSize; ++z)
                {
                    int index = GUtilities.To1DIndex(x, z, PatchGridSize);
                    GGrassPatch patch = new GGrassPatch(this, x, z);
                    grassPatches[index] = patch;
                }
            }

            AddGrassInstances(grassInstances);
        }

        /// <summary>
        /// Add some grass instances to the terrain and refresh the renderer.
        /// </summary>
        /// <param name="instances">List to instances to add.</param>
        public void AddGrassInstances(List<GGrassInstance> instances)
        {
            Rect[] uvRects = new Rect[GrassPatches.Length];
            for (int r = 0; r < uvRects.Length; ++r)
            {
                uvRects[r] = GrassPatches[r].GetUvRange();
            }

            bool[] dirty = new bool[GrassPatches.Length];
            for (int i = 0; i < instances.Count; ++i)
            {
                GGrassInstance grass = instances[i];
                for (int r = 0; r < uvRects.Length; ++r)
                {
                    if (uvRects[r].Contains(new Vector2(grass.position.x, grass.position.z)))
                    {
                        grassPatches[r].Instances.Add(grass);
                        dirty[r] = true;
                        break;
                    }
                }
            }

            for (int i = 0; i < dirty.Length; ++i)
            {
                if (dirty[i] == true)
                {
                    GrassPatches[i].RecalculateBounds();
                    GrassPatches[i].Changed();
                }
            }
        }

        /// <summary>
        /// Get a copy of all grass instances in this terrain.
        /// Very slow!
        /// </summary>
        /// <returns>A collection of all grass instances.</returns>
        public List<GGrassInstance> GetGrassInstances()
        {
            List<GGrassInstance> instances = new List<GGrassInstance>();
            for (int i = 0; i < GrassPatches.Length; ++i)
            {
                instances.AddRange(GrassPatches[i].Instances);
            }
            return instances;
        }

        /// <summary>
        /// Erase all grass instances from the terrain.
        /// </summary>
        public void ClearGrassInstances()
        {
            for (int i = 0; i < GrassPatches.Length; ++i)
            {
                GrassPatches[i].ClearInstances();
            }
        }

        /// <summary>
        /// Mark all tree in this region as modified to perform related actions such as tree snapping.
        /// You should call this function after modifying the tree list or terrain geometry.
        /// </summary>
        /// <param name="uvRect">A rect in [0-1] space indicate the modified region.</param>
        public void SetTreeRegionDirty(Rect uvRect)
        {
            TreeDirtyRegions.Add(uvRect);
        }

        [ExcludeFromDoc]
        public void SetTreeRegionDirty(IEnumerable<Rect> uvRects)
        {
            TreeDirtyRegions.AddRange(uvRects);
        }

        /// <summary>
        /// Get a collection of regions where trees have been modified.
        /// Region value in [0-1] range.
        /// </summary>
        /// <returns>An array of regions.</returns>
        public Rect[] GetTreeDirtyRegions()
        {
            return TreeDirtyRegions.ToArray();
        }

        /// <summary>
        /// Clear all regions marked with SetTreeDirtyRegions().
        /// Call this function when you've done tree snapping action or similar.
        /// </summary>
        public void ClearTreeDirtyRegions()
        {
            TreeDirtyRegions.Clear();
        }

        /// <summary>
        /// Mark all grass in this region as modified to perform related actions such as grass snapping.
        /// You should call this function after modifying the grass list or terrain geometry.
        /// </summary>
        /// <param name="uvRect">A rect in [0-1] space indicate the modified region.</param>
        public void SetGrassRegionDirty(Rect uvRect)
        {
            GrassDirtyRegions.Add(uvRect);
        }

        [ExcludeFromDoc]
        public void SetGrassRegionDirty(IEnumerable<Rect> uvRects)
        {
            GrassDirtyRegions.AddRange(uvRects);
        }

        /// <summary>
        /// Get a collection of regions where grass have been modified.
        /// Region value in [0-1] range.
        /// </summary>
        /// <returns>An array of regions.</returns>
        public Rect[] GetGrassDirtyRegions()
        {
            return GrassDirtyRegions.ToArray();
        }

        /// <summary>
        /// Clear all regions marked with SetGrassDirtyRegions().
        /// Call this function when you've done grass snapping action or similar.
        /// </summary>
        public void ClearGrassDirtyRegions()
        {
            GrassDirtyRegions.Clear();
        }

        /// <summary>
        /// Copy numeric value to other object.
        /// This doesn't copy tree & grass instances.
        /// </summary>
        /// <param name="des">The other object</param>
        public void CopyTo(GFoliage des)
        {
            des.Trees = Trees;
            des.TreeSnapMode = TreeSnapMode;
            des.TreeSnapLayerMask = TreeSnapLayerMask;
            des.Grasses = Grasses;
            des.GrassSnapMode = GrassSnapMode;
            des.GrassSnapLayerMask = GrassSnapLayerMask;
            des.PatchGridSize = PatchGridSize;
        }

        [ExcludeFromDoc]
        public void OnBeforeSerialize()
        {
            for (int i = 0; i < GrassPatches.Length; ++i)
            {
                GrassPatches[i].Serialize();
            }
            GCompressor.CleanUp();
        }

        [ExcludeFromDoc]
        public void OnAfterDeserialize()
        {
            for (int i = 0; i < GrassPatches.Length; ++i)
            {
                GrassPatches[i].Deserialize();
            }
            GCompressor.CleanUp();
        }

        [ExcludeFromDoc]
        public void Internal_UpgradeGrassSerializeVersion()
        {
            for (int i = 0; i < GrassPatches.Length; ++i)
            {
                GrassPatches[i].UpgradeSerializeVersion();
            }
            grassVersion = GVersionInfo.Number;
            Debug.Log("Successfully upgrade grass serialize to newer version!");
        }

        /// <summary>
        /// Call this function to invoke an event and refresh grass renderer when you've modify some grass instances.
        /// </summary>
        public void GrassAllChanged()
        {
            for (int i = 0; i < GrassPatches.Length; ++i)
            {
                GrassPatches[i].Changed();
            }
        }

        /// <summary>
        /// Call this function to invoke an event and refresh tree renderer when you've modify some tree instances.
        /// </summary>
        public void TreeAllChanged()
        {
            if (TerrainData != null)
            {
                TerrainData.InvokeTreeChanged();
            }
        }

        /// <summary>
        /// Add some tree instances to this terrain.
        /// </summary>
        /// <param name="newInstances">A collection of new instances.</param>
        public void AddTreeInstances(IEnumerable<GTreeInstance> newInstances)
        {
            TreeInstances.AddRange(newInstances);
            TreeAllChanged();
        }

        /// <summary>
        /// Erase tree instances that match a condition.
        /// </summary>
        /// <param name="condition">The condition to apply. An instances will be erased if condition returns true.</param>
        public void RemoveTreeInstances(System.Predicate<GTreeInstance> condition)
        {
            int removedCount = TreeInstances.RemoveAll(condition);
            if (removedCount > 0)
            {
                TreeAllChanged();
            }
        }

        /// <summary>
        /// Erase all tree instances from this terrain.
        /// </summary>
        public void ClearTreeInstances()
        {
            TreeInstances.Clear();
            TreeAllChanged();
        }

        [ExcludeFromDoc]
        public int GetTreeMemStats()
        {
            return TreeInstances.Count * GTreeInstance.GetStructSize();
        }

        [ExcludeFromDoc]
        public int GetGrassMemStats()
        {
            int memory = 0;
            if (grassPatches != null)
            {
                for (int i = 0; i < grassPatches.Length; ++i)
                {
                    if (grassPatches[i] != null)
                    {
                        memory += grassPatches[i].GetMemStats();
                    }
                }
            }
            return memory;
        }

        [ExcludeFromDoc]
        public NativeArray<Vector2> GetTreesPositionArray(Allocator allocator = Allocator.TempJob)
        {
            List<GTreeInstance> trees = TreeInstances;
            int treeCount = trees.Count;
            NativeArray<Vector2> positions = new NativeArray<Vector2>(treeCount, allocator, NativeArrayOptions.UninitializedMemory);
            Vector2 pos = Vector2.zero;

            for (int i = 0; i < treeCount; ++i)
            {
                GTreeInstance t = trees[i];
                pos.Set(t.position.x, t.position.z);
                positions[i] = pos;
            }

            return positions;
        }
    }
}
#endif
