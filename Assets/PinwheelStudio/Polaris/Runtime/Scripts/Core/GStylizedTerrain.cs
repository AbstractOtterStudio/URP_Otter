#if GRIFFIN
using UnityEngine;
using System.Collections.Generic;
using Pinwheel.Griffin.TextureTool;
using UnityEngine.Rendering;
using Pinwheel.Griffin.Rendering;
using Unity.Collections;
using Unity.Jobs;
using System.Collections;
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if __MICROSPLAT_POLARIS__
using JBooth.MicroSplat;
#endif
#if UNITY_EDITOR && GRIFFIN_EDITOR_COROUTINES
using Unity.EditorCoroutines.Editor;
#endif

namespace Pinwheel.Griffin
{
    [System.Serializable]
    [ExecuteInEditMode]
    public class GStylizedTerrain : MonoBehaviour
    {
        /// <summary>
        /// The layer that Polaris performs raycast on, used for tools such as painter & splines.
        /// </summary>
        public const string RAYCAST_LAYER = "POLARIS RAYCAST";

#pragma warning disable CS0067 // Event is never used
        [ExcludeFromDoc]
        [System.Obsolete]
        public delegate void HeightMapProcessCallback(Texture2D heightmap);
        [ExcludeFromDoc]
        [System.Obsolete] public event HeightMapProcessCallback PreProcessHeightMap;
        [ExcludeFromDoc]
        [System.Obsolete] public event HeightMapProcessCallback PostProcessHeightMap;
#pragma warning restore

        private static HashSet<GStylizedTerrain> activeTerrainSet;

        private static HashSet<GStylizedTerrain> ActiveTerrainSet
        {
            get
            {
                if (activeTerrainSet == null)
                    activeTerrainSet = new HashSet<GStylizedTerrain>();
                return activeTerrainSet;
            }
        }

        /// <summary>
        /// Get a collection of all active Polaris terrain in the scene.
        /// </summary>
        public static IEnumerable<GStylizedTerrain> ActiveTerrains
        {
            get { return ActiveTerrainSet; }
        }

        [SerializeField] internal GTerrainData terrainData;

        /// <summary>
        /// The terrain data object assigned to this terrain. This object contains all terrain settings as well as height map, textures and foliage data.
        /// </summary>
        public GTerrainData TerrainData
        {
            get { return terrainData; }
            set
            {
                GTerrainData oldData = terrainData;
                GTerrainData newData = value;
                terrainData = newData;

                if (oldData == null && newData != null)
                {
                    newData.Dirty += OnTerrainDataDirty;
                    newData.GrassPatchChanged += OnGrassPatchChanged;
                    newData.GrassPatchGridSizeChanged += OnGrassPatchGridSizeChanged;
                    newData.TreeChanged += OnTreeChanged;
                    newData.TreePrototypeGroupChanged += OnTreePrototypeGroupChanged;
                    newData.GrassPrototypeGroupChanged += OnGrassPrototypeGroupChanged;

                    if (newData.Geometry.StorageMode == GGeometry.GStorageMode.SaveToAsset)
                    {
                        newData.Geometry.SetRegionDirty(GCommon.UnitRect);
                        newData.Foliage.SetTreeRegionDirty(GCommon.UnitRect);
                        newData.Foliage.SetGrassRegionDirty(GCommon.UnitRect);
                        OnTerrainDataDirty(GTerrainData.DirtyFlags.All);
                    }
                    else
                    {
                        newData.Geometry.SetRegionDirty(GCommon.UnitRect);
                        OnTerrainDataDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                    }
                }
                else if (oldData != null && newData != null && oldData != newData)
                {
                    oldData.Dirty -= OnTerrainDataDirty;
                    oldData.GrassPatchChanged -= OnGrassPatchChanged;
                    oldData.GrassPatchGridSizeChanged -= OnGrassPatchGridSizeChanged;
                    oldData.TreeChanged -= OnTreeChanged;
                    oldData.TreePrototypeGroupChanged -= OnTreePrototypeGroupChanged;
                    oldData.GrassPrototypeGroupChanged -= OnGrassPrototypeGroupChanged;

                    newData.Dirty += OnTerrainDataDirty;
                    newData.GrassPatchChanged += OnGrassPatchChanged;
                    newData.GrassPatchGridSizeChanged += OnGrassPatchGridSizeChanged;
                    newData.TreeChanged += OnTreeChanged;
                    newData.TreePrototypeGroupChanged += OnTreePrototypeGroupChanged;
                    newData.GrassPrototypeGroupChanged += OnGrassPrototypeGroupChanged;

                    if (newData.Geometry.StorageMode == GGeometry.GStorageMode.SaveToAsset)
                    {
                        newData.Geometry.SetRegionDirty(GCommon.UnitRect);
                        newData.Foliage.SetTreeRegionDirty(GCommon.UnitRect);
                        newData.Foliage.SetGrassRegionDirty(GCommon.UnitRect);
                        OnTerrainDataDirty(GTerrainData.DirtyFlags.All);
                    }
                    else
                    {
                        newData.Geometry.SetRegionDirty(GCommon.UnitRect);
                        OnTerrainDataDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                    }
                }
                else if (oldData != null && newData == null)
                {
                    oldData.Dirty -= OnTerrainDataDirty;
                    oldData.GrassPatchChanged -= OnGrassPatchChanged;
                    oldData.GrassPatchGridSizeChanged -= OnGrassPatchGridSizeChanged;
                    oldData.TreeChanged -= OnTreeChanged;
                    oldData.TreePrototypeGroupChanged -= OnTreePrototypeGroupChanged;
                    oldData.GrassPrototypeGroupChanged -= OnGrassPrototypeGroupChanged;

                    Transform root = GetChunkRoot();
                    if (root != null)
                    {
                        GUtilities.DestroyGameobject(root.gameObject);
                    }
                }
            }
        }

        [SerializeField]
        private GStylizedTerrain topNeighbor;

        /// <summary>
        /// The neighbor terrain at positive-Z direction.
        /// </summary>
        public GStylizedTerrain TopNeighbor
        {
            get { return topNeighbor; }
            set
            {
                topNeighbor = value;
                if (topNeighbor != null)
                {
                    topTerrainData = topNeighbor.TerrainData;
                }
            }
        }

        [SerializeField]
        private GTerrainData topTerrainData;

        [ExcludeFromDoc]
        public GTerrainData TopTerrainData
        {
            get { return topTerrainData; }
        }

        [SerializeField]
        private GStylizedTerrain bottomNeighbor;
        /// <summary>
        /// The neighbor terrain at negative-Z direction
        /// </summary>
        public GStylizedTerrain BottomNeighbor
        {
            get { return bottomNeighbor; }
            set
            {
                bottomNeighbor = value;
                if (bottomNeighbor != null)
                {
                    bottomTerrainData = bottomNeighbor.TerrainData;
                }
            }
        }

        [SerializeField]
        private GTerrainData bottomTerrainData;
        [ExcludeFromDoc]
        public GTerrainData BottomTerrainData
        {
            get { return bottomTerrainData; }
        }

        [SerializeField]
        private GStylizedTerrain leftNeighbor;

        /// <summary>
        /// The neighbor terrain at negative-X direction.
        /// </summary>
        public GStylizedTerrain LeftNeighbor
        {
            get { return leftNeighbor; }
            set
            {
                leftNeighbor = value;
                if (leftNeighbor != null)
                {
                    leftTerrainData = leftNeighbor.TerrainData;
                }
            }
        }

        [SerializeField]
        private GTerrainData leftTerrainData;
        [ExcludeFromDoc]
        public GTerrainData LeftTerrainData
        {
            get { return leftTerrainData; }
        }

        [SerializeField]
        private GStylizedTerrain rightNeighbor;
        /// <summary>
        /// The neightbor terrain at positive-X direction.
        /// </summary>
        public GStylizedTerrain RightNeighbor
        {
            get { return rightNeighbor; }
            set
            {
                rightNeighbor = value;
                if (rightNeighbor != null)
                {
                    rightTerrainData = rightNeighbor.TerrainData;
                }
            }
        }

        [SerializeField]
        private GTerrainData rightTerrainData;
        [ExcludeFromDoc]
        public GTerrainData RightTerrainData
        {
            get { return rightTerrainData; }
        }

        [SerializeField]
        private int groupId;
        /// <summary>
        /// The group that this terrain belongs to. Terrain tools can use this id to selectively modify terrains. This also be used in seam matching, only terrains in the same group match their seams.
        /// </summary>
        public int GroupId
        {
            get { return groupId; }
            set { groupId = Mathf.Max(0, value); }
        }

        [SerializeField]
        private bool autoConnect = true;
        /// <summary>
        /// If on, terrains in the same group will find their neighbors in some occasions.
        /// </summary>
        public bool AutoConnect
        {
            get { return autoConnect; }
            set { autoConnect = value; }
        }

        [ExcludeFromDoc]
        [SerializeField]
        public float geometryVersion;
        [ExcludeFromDoc]
        public const float GEOMETRY_VERSION_CHUNK_POSITION_AT_CHUNK_CENTER = 245;

        /// <summary>
        /// The bounding box of this terrain in world space.
        /// </summary>
        public Bounds Bounds
        {
            get
            {
                Bounds b = new Bounds();
                Vector3 size = terrainData != null
                    ? new Vector3(terrainData.Geometry.width, terrainData.Geometry.height, terrainData.Geometry.length)
                    : Vector3.zero;
                b.size = size;
                b.center = transform.position + size * 0.5f;
                return b;
            }
        }

        /// <summary>
        /// The bounding rect (on XZ plane) of this terrain in world space.
        /// </summary>
        public Rect Rect
        {
            get
            {
                Rect r = new Rect();
                r.size = TerrainData != null
                    ? new Vector2(TerrainData.Geometry.Width, TerrainData.Geometry.Length)
                    : Vector2.zero;
                r.center = new Vector2(transform.position.x + r.size.x * 0.5f, transform.position.z + r.size.y * 0.5f);
                return r;
            }
        }

        private RenderTexture heightMap;
        private RenderTexture normalMap;
        private RenderTexture normalMapInterpolated;
        private RenderTexture normalMapPerPixel;
        private RenderTexture grassVectorFieldMap;
        private RenderTexture grassVectorFieldMapTmp;

        private Dictionary<Camera, GTreeRenderer3> perCameraTreeRendererMap;
        private Dictionary<Camera, GGrassRenderer3> perCameraGrassRendererMap;

        [SerializeField] private Vector3 lastPosition;

        internal GGeometry geometryShortcut;
        internal GTerrainGeneratedData geometryDataShortcut;
        internal GShading shadingShortcut;
        internal GMask maskShortcut;

        /// <summary>
        /// Get the root transform of all terrain chunk meshes. This is not the terrain transform itself, but a child object of it named "~Geometry".
        /// </summary>
        /// <returns>The root transform object, can be null.</returns>
        public Transform GetChunkRoot()
        {
            Transform root = transform.Find(GCommon.CHUNK_ROOT_NAME);
            return root;
        }

        /// <summary>
        /// Get the root transform of all terrain chunk meshes. This is not the terrain transform itself, but a child object of it named "~Geometry". 
        /// </summary>
        /// <returns>The root transform object, never be null.</returns>
        public Transform GetOrCreateChunkRoot()
        {
            Transform root = GUtilities.GetOrCreateChildTransform(transform, GCommon.CHUNK_ROOT_NAME);
            GUtilities.ResetTransform(root, transform);

#if UNITY_EDITOR
            root.hideFlags = GEditorSettings.Instance.general.showGeometryChunkInHierarchy
                ? HideFlags.None
                : HideFlags.HideInHierarchy;
#if !UNITY_2022_2_OR_NEWER
            StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(root.gameObject);
            GameObjectUtility.SetStaticEditorFlags(root.gameObject, staticFlags | StaticEditorFlags.NavigationStatic);
#endif
#endif
            return root;
        }

        /// <summary>
        /// Connect all terrains to their adjacent neighbor. There is no seam matching happens, just references assign.
        /// </summary>
        public static void ConnectAdjacentTiles()
        {
            IEnumerator<GStylizedTerrain> terrains = ActiveTerrains.GetEnumerator();
            while (terrains.MoveNext())
            {
                GStylizedTerrain t = terrains.Current;
                t.ConnectNeighbor();
            }
        }

        /// <summary>
        /// Find and assign the references to adjacent terrains.
        /// </summary>
        public void ConnectNeighbor()
        {
            if (TerrainData == null)
                return;
            if (!AutoConnect)
                return;
            Vector2 sizeXZ = new Vector2(
                TerrainData.Geometry.Width,
                TerrainData.Geometry.Length);
            Vector2 posXZ = new Vector2(
                transform.position.x,
                transform.position.z);

            IEnumerator<GStylizedTerrain> terrains = ActiveTerrains.GetEnumerator();
            while (terrains.MoveNext())
            {
                GStylizedTerrain t = terrains.Current;
                if (t.TerrainData == null)
                    return;
                if (GroupId != t.GroupId)
                    continue;

                Vector2 neighborSizeXZ = new Vector2(
                    t.TerrainData.Geometry.Width,
                    t.TerrainData.Geometry.Length);
                Vector2 neighborPosXZ = new Vector2(
                    t.transform.position.x,
                    t.transform.position.z);
                Vector2 neighborCenter = neighborPosXZ + neighborSizeXZ * 0.5f;

                if (LeftNeighbor == null)
                {
                    Rect r = new Rect();
                    r.size = sizeXZ;
                    r.position = new Vector2(posXZ.x - sizeXZ.x, posXZ.y);
                    if (r.Contains(neighborCenter))
                    {
                        LeftNeighbor = t;
                        t.RightNeighbor = this;
                    }
                }

                if (TopNeighbor == null)
                {
                    Rect r = new Rect();
                    r.size = sizeXZ;
                    r.position = new Vector2(posXZ.x, posXZ.y + sizeXZ.y);
                    if (r.Contains(neighborCenter))
                    {
                        TopNeighbor = t;
                        t.BottomNeighbor = this;
                    }
                }

                if (RightNeighbor == null)
                {
                    Rect r = new Rect();
                    r.size = sizeXZ;
                    r.position = new Vector2(posXZ.x + sizeXZ.x, posXZ.y);
                    if (r.Contains(neighborCenter))
                    {
                        RightNeighbor = t;
                        t.LeftNeighbor = this;
                    }
                }

                if (BottomNeighbor == null)
                {
                    Rect r = new Rect();
                    r.size = sizeXZ;
                    r.position = new Vector2(posXZ.x, posXZ.y - sizeXZ.y);
                    if (r.Contains(neighborCenter))
                    {
                        BottomNeighbor = t;
                        t.TopNeighbor = this;
                    }
                }
            }
        }

        private void OnEnable()
        {
            ActiveTerrainSet.Add(this);
            //GStylizedTerrain.ConnectAdjacentTiles();
            ConnectNeighbor();
            GCommon.RegisterBeginRender(OnBeginCameraRendering);
            GCommon.RegisterBeginRenderSRP(OnBeginCameraRenderingSRP);
            GCommon.RegisterEndRender(OnEndCameraRendering);

#if __MICROSPLAT_POLARIS__
            PushControlTexturesToMicroSplat();
#endif

            if (TerrainData != null)
            {
                TerrainData.Dirty += OnTerrainDataDirty;
                TerrainData.GrassPatchChanged += OnGrassPatchChanged;
                TerrainData.GrassPatchGridSizeChanged += OnGrassPatchGridSizeChanged;
                TerrainData.TreeChanged += OnTreeChanged;
                TerrainData.TreePrototypeGroupChanged += OnTreePrototypeGroupChanged;
                TerrainData.GrassPrototypeGroupChanged += OnGrassPrototypeGroupChanged;
            }

            if (TerrainData != null)
            {
                TerrainData.Shading.UpdateMaterials();
            }

            if (TerrainData != null && TerrainData.Geometry.StorageMode == GGeometry.GStorageMode.GenerateOnEnable)
            {
                TerrainData.Geometry.SetRegionDirty(GCommon.UnitRect);
                OnTerrainDataDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                TerrainData.Geometry.ClearDirtyRegions();
            }

            perCameraTreeRendererMap = new Dictionary<Camera, GTreeRenderer3>();
            foreach (GTreeRenderer3 renderer in perCameraTreeRendererMap.Values)
            {
                renderer.OnEnable();
            }

            perCameraGrassRendererMap = new Dictionary<Camera, GGrassRenderer3>();
            foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
            {
                renderer.OnEnable();
            }

            GTreePrototypeGroup.Changed += OnTreePrototypesChanged;
            GGrassPrototypeGroup.Changed += OnGrassPrototypesChanged;
        }

        private void OnDisable()
        {
            ReleaseTemporaryTextures();
            ActiveTerrainSet.Remove(this);
            //GStylizedTerrain.ConnectAdjacentTiles();
            CleanUpChunks();

            GCommon.UnregisterBeginRender(OnBeginCameraRendering);
            GCommon.UnregisterBeginRenderSRP(OnBeginCameraRenderingSRP);
            GCommon.UnregisterEndRender(OnEndCameraRendering);

            if (TerrainData != null)
            {
                TerrainData.Dirty -= OnTerrainDataDirty;
                TerrainData.GrassPatchChanged -= OnGrassPatchChanged;
                TerrainData.GrassPatchGridSizeChanged -= OnGrassPatchGridSizeChanged;
                TerrainData.TreeChanged -= OnTreeChanged;
                TerrainData.TreePrototypeGroupChanged -= OnTreePrototypeGroupChanged;
                TerrainData.GrassPrototypeGroupChanged -= OnGrassPrototypeGroupChanged;
                TerrainData.CleanUp();
            }

            foreach (GTreeRenderer3 renderer in perCameraTreeRendererMap.Values)
            {
                renderer.OnDisable();
            }

            foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
            {
                renderer.OnDisable();
            }

            GTreePrototypeGroup.Changed -= OnTreePrototypesChanged;
            GGrassPrototypeGroup.Changed -= OnGrassPrototypesChanged;
        }

        /// <summary>
        /// Sent when a prototype's settings has been modified, via GTreePrototypeGroup.NotifyChanged()
        /// </summary>
        /// <param name="sender"></param>
        private void OnTreePrototypesChanged(GTreePrototypeGroup sender)
        {
            if (TerrainData == null)
                return;
            if (TerrainData.Foliage.Trees == sender)
            {
                foreach (GTreeRenderer3 renderer in perCameraTreeRendererMap.Values)
                {
                    renderer.OnPrototypesChanged();
                }
            }
        }

        /// <summary>
        /// Sent when a prototype's settings has been modified, via GGrassPrototypeGroup.NotifyChanged()
        /// </summary>
        /// <param name="sender"></param>
        private void OnGrassPrototypesChanged(GGrassPrototypeGroup sender)
        {
            if (TerrainData == null)
                return;
            if (TerrainData.Foliage.Grasses == sender)
            {
                foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
                {
                    renderer.OnPrototypesChanged();
                }
            }
        }

        private void OnDestroy()
        {
            ReleaseTemporaryTextures();
            CleanUpChunks();
        }

        private void Reset()
        {
            geometryVersion = GVersionInfo.Number;
        }

        private void ReleaseTemporaryTextures()
        {
            if (heightMap != null)
            {
                heightMap.Release();
                GUtilities.DestroyObject(heightMap);
            }

            if (normalMap != null)
            {
                normalMap.Release();
                GUtilities.DestroyObject(normalMap);
            }

            if (normalMapInterpolated != null)
            {
                normalMapInterpolated.Release();
                GUtilities.DestroyObject(normalMapInterpolated);
            }

            if (normalMapPerPixel != null)
            {
                normalMapPerPixel.Release();
                GUtilities.DestroyObject(normalMapPerPixel);
            }
        }

        private void CleanUpChunks()
        {
            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].CleanUpNonSerializedMeshes();
            }
        }

        private void OnGrassPatchChanged(int index)
        {
            foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
            {
                renderer.OnCellChanged(index);
            }
        }

        private void OnGrassPatchGridSizeChanged()
        {
            foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
            {
                renderer.OnPatchGridSizeChanged();
            }
        }

        private void OnTreeChanged()
        {
            foreach (GTreeRenderer3 renderer in perCameraTreeRendererMap.Values)
            {
                renderer.OnTreeChanged();
            }
        }

        /// <summary>
        /// Sent when user assign a new prototype group to the terrain
        /// </summary>
        private void OnTreePrototypeGroupChanged()
        {
            foreach (GTreeRenderer3 renderer in perCameraTreeRendererMap.Values)
            {
                renderer.OnPrototypeGroupChanged();
            }
        }

        private void OnGrassPrototypeGroupChanged()
        {
            foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
            {
                renderer.OnPrototypeGroupChanged();
            }
        }

        private void OnBeginCameraRendering(Camera cam)
        {
            if (perCameraTreeRendererMap == null)
                return;

            if (transform.position != lastPosition)
            {
                if (TerrainData.Rendering.DrawTrees)
                {
                    foreach (GTreeRenderer3 renderer in perCameraTreeRendererMap.Values)
                    {
                        renderer.OnGeometryChanged();
                    }
                }

                if (TerrainData.Rendering.DrawGrasses)
                {
                    foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
                    {
                        renderer.OnGeometryChanged();
                    }
                }
                lastPosition = transform.position;
            }

            if (TerrainData != null &&
                transform.rotation == Quaternion.identity &&
                transform.localScale == Vector3.one &&
                GUtilities.IsSceneViewOrGameCamera(cam) &&
                !GUtilities.IsPrefabCamera(cam))
            {
                if (TerrainData.Rendering.DrawTrees)
                {
                    GTreeRenderer3 treeRenderer = null;
                    if (!perCameraTreeRendererMap.TryGetValue(cam, out treeRenderer))
                    {
                        treeRenderer = new GTreeRenderer3(this, 6);
                        treeRenderer.OnEnable();
                        perCameraTreeRendererMap.Add(cam, treeRenderer);
                    }

                    treeRenderer.QuadTreeDepth = GRuntimeSettings.Instance.renderingDefault.treeCullQuadTreeDepth;
                    treeRenderer.Render(cam);
                }

                if (TerrainData.Rendering.DrawGrasses)
                {
                    GGrassRenderer3 grassRenderer = null;
                    if (!perCameraGrassRendererMap.TryGetValue(cam, out grassRenderer))
                    {
                        grassRenderer = new GGrassRenderer3(this);
                        grassRenderer.OnEnable();
                        perCameraGrassRendererMap.Add(cam, grassRenderer);
                    }

                    grassRenderer.Render(cam);
                }
            }

#if UNITY_EDITOR
            if (GEditorSettings.Instance.general.debugMode)
            {
                DrawChunkUpdateDebug(cam);
            }

            if (GEditorSettings.Instance.topographic.enable)
            {
                DrawChunkTopographic(cam);
            }
#endif
        }

        private void OnBeginCameraRenderingSRP(ScriptableRenderContext context, Camera cam)
        {
            OnBeginCameraRendering(cam);
        }

        private void OnEndCameraRendering(Camera cam)
        {
        }

        private void OnTerrainDataDirty(GTerrainData.DirtyFlags dirtyFlag)
        {
            if ((dirtyFlag & GTerrainData.DirtyFlags.Geometry) == GTerrainData.DirtyFlags.Geometry)
            {
                OnGeometryDirty();
            }
            else if ((dirtyFlag & GTerrainData.DirtyFlags.GeometryTimeSliced) ==
                     GTerrainData.DirtyFlags.GeometryTimeSliced)
            {
                if (TerrainData.Geometry.AllowTimeSlicedGeneration)
                {
                    OnGeometryTimeSlicedDirty();
                }
                else
                {
                    OnGeometryDirty();
                }
            }

            if ((dirtyFlag & GTerrainData.DirtyFlags.Shading) == GTerrainData.DirtyFlags.Shading)
            {
                OnShadingDirty();
            }

            if ((dirtyFlag & GTerrainData.DirtyFlags.Rendering) == GTerrainData.DirtyFlags.Rendering)
            {
                OnRenderingDirty();
            }

            if ((dirtyFlag & GTerrainData.DirtyFlags.Foliage) == GTerrainData.DirtyFlags.Foliage)
            {
                OnFoliageDirty();
            }

            GUtilities.MarkCurrentSceneDirty();
        }

        private void OnGeometryDirty()
        {
            geometryVersion = GVersionInfo.Number;
            geometryShortcut = TerrainData.Geometry;
            geometryDataShortcut = TerrainData.GeometryData;
            shadingShortcut = TerrainData.Shading;
            maskShortcut = terrainData.Mask;

            ReleaseTemporaryTextures();
            GTerrainChunk[] chunks = InitChunks();
            List<GTerrainChunk> chunksToUpdate = ExtractDirtyChunks(chunks);

            if (chunksToUpdate.Count == 0)
                return;

            int lodCount = geometryShortcut.LODCount;
            for (int lod = 0; lod < lodCount; ++lod)
            {
                GenerateChunks(chunksToUpdate, lod);
            }

            foreach (GTreeRenderer3 renderer in perCameraTreeRendererMap.Values)
            {
                renderer.OnGeometryChanged();
            }

            foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
            {
                renderer.OnGeometryChanged();
            }
        }

        private void OnGeometryTimeSlicedDirty()
        {
            geometryVersion = GVersionInfo.Number;
            geometryShortcut = TerrainData.Geometry;
            geometryDataShortcut = TerrainData.GeometryData;
            shadingShortcut = TerrainData.Shading;
            maskShortcut = TerrainData.Mask;

            GTerrainChunk[] chunks = InitChunks();
            List<GTerrainChunk> chunksToUpdate = ExtractDirtyChunks(chunks);
            if (chunksToUpdate.Count == 0)
                return;

            SortChunkByDistance(
                Camera.main ? Camera.main.transform.position : Vector3.zero,
                chunksToUpdate);

            if (Application.isPlaying)
            {
                StartCoroutine(GenerateChunksTimeSliced(chunksToUpdate));
            }
#if UNITY_EDITOR
            else if (!Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
#if GRIFFIN_EDITOR_COROUTINES
                EditorCoroutineUtility.StartCoroutine(GenerateChunksTimeSliced(chunksToUpdate), this);
#else
                int lodCount = geometryShortcut.LODCount;
                for (int lod = 0; lod < lodCount; ++lod)
                {
                    GenerateChunks(chunksToUpdate, lod);
                }
#endif
            }
#endif
        }

        private void GenerateChunks(List<GTerrainChunk> chunksToUpdate, int lod)
        {
            int meshBaseResolution = geometryShortcut.MeshBaseResolution;
            int meshResolution = geometryShortcut.MeshResolution;

            InitGeneration(chunksToUpdate, meshResolution);
            CreateBaseSubdivTree(chunksToUpdate, meshBaseResolution, meshResolution, lod);
            SplitBaseTreeForDynamicPolygon(chunksToUpdate, meshBaseResolution, meshResolution, lod);
            if (lod == 0)
            {
                if (meshBaseResolution != meshResolution)
                {
                    StitchSeam(chunksToUpdate, meshBaseResolution, meshResolution);
                }
            }
            else
            {
                StitchSeamLOD(chunksToUpdate, meshBaseResolution, meshResolution, lod);
            }

            CountLeafNode(chunksToUpdate, meshBaseResolution, meshResolution, lod);
            CreateVertex(chunksToUpdate, meshBaseResolution, meshResolution, lod);
            UpdateMesh(chunksToUpdate, lod);
            if (lod == 0)
            {
                BakeCollisionMesh(chunksToUpdate);
                UpdateCollisionMesh(chunksToUpdate);
            }

            CleanUpGeneration(chunksToUpdate);

            geometryShortcut.ClearDirtyRegions();
        }

        private void InitGeneration(List<GTerrainChunk> chunksToUpdate, int meshResolution)
        {
            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                chunksToUpdate[i].InitSubdivTreeNativeContainers(meshResolution);
            }
        }

        private void CreateBaseSubdivTree(
            List<GTerrainChunk> chunksToUpdate,
            int meshBaseResolution,
            int meshResolution,
            int lod)
        {
            JobHandle[] jobHandles = new JobHandle[chunksToUpdate.Count];
            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                GCreateBaseTreeJob j = chunksToUpdate[i].GetCreateBaseSubdivTreeJob(
                    meshBaseResolution,
                    meshResolution,
                    lod);
                jobHandles[i] = j.Schedule();
            }

            GJobUtilities.CompleteAll(jobHandles);
        }

        private void SplitBaseTreeForDynamicPolygon(
            List<GTerrainChunk> chunksToUpdate,
            int meshBaseResolution,
            int meshResolution,
            int lod)
        {
            JobHandle[] jobHandles = new JobHandle[chunksToUpdate.Count];
            geometryShortcut.Internal_CreateNewSubDivisionMap();
            GTextureNativeDataDescriptor<Color32> subdivMap =
                new GTextureNativeDataDescriptor<Color32>(geometryShortcut.Internal_SubDivisionMap);

            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                GSplitBaseTreeForDynamicPolygonJob j = chunksToUpdate[i].GetSplitBaseTreeForDynamicPolygonJob(
                    meshBaseResolution,
                    meshResolution,
                    lod,
                    subdivMap);
                jobHandles[i] = j.Schedule();
            }

            GJobUtilities.CompleteAll(jobHandles);
        }

        private void StitchSeam(
            List<GTerrainChunk> chunksToUpdate,
            int meshBaseResolution,
            int meshResolution)
        {
            JobHandle[] jobHandles = new JobHandle[chunksToUpdate.Count];
            int stitchSeamIteration = 0;
            int stitchSeamMaxIteration = GRuntimeSettings.Instance.geometryGeneration.triangulateIteration;
            bool newVertexCreated = true;
            List<NativeArray<bool>> markers = new List<NativeArray<bool>>();

            while (newVertexCreated && stitchSeamIteration <= stitchSeamMaxIteration)
            {
                GStitchSeamJob[] stitchJobs = new GStitchSeamJob[chunksToUpdate.Count];
                for (int i = 0; i < chunksToUpdate.Count; ++i)
                {
                    GTerrainChunk c = chunksToUpdate[i];

                    GTerrainChunk leftChunk = GetLeftNeighborChunk(c);
                    bool hasLeftMarkers = leftChunk != null;
                    NativeArray<bool> leftMarkers = hasLeftMarkers
                        ? leftChunk.GetVertexMarker()
                        : new NativeArray<bool>(1, Allocator.TempJob);
                    markers.Add(leftMarkers);

                    GTerrainChunk topChunk = GetTopNeighborChunk(c);
                    bool hasTopMarkers = topChunk != null;
                    NativeArray<bool> topMarkers = hasTopMarkers
                        ? topChunk.GetVertexMarker()
                        : new NativeArray<bool>(1, Allocator.TempJob);
                    markers.Add(topMarkers);

                    GTerrainChunk rightChunk = GetRightNeighborChunk(c);
                    bool hasRightMarkers = rightChunk != null;
                    NativeArray<bool> rightMarkers = hasRightMarkers
                        ? rightChunk.GetVertexMarker()
                        : new NativeArray<bool>(1, Allocator.TempJob);
                    markers.Add(rightMarkers);

                    GTerrainChunk bottomChunk = GetBottomNeighborChunk(c);
                    bool hasBottomMarkers = bottomChunk != null;
                    NativeArray<bool> bottomMarkers = hasBottomMarkers
                        ? bottomChunk.GetVertexMarker()
                        : new NativeArray<bool>(1, Allocator.TempJob);
                    markers.Add(bottomMarkers);

                    GStitchSeamJob j = c.GetStitchSeamJob(
                        meshBaseResolution,
                        meshResolution,
                        hasLeftMarkers, leftMarkers,
                        hasTopMarkers, topMarkers,
                        hasRightMarkers, rightMarkers,
                        hasBottomMarkers, bottomMarkers
                    );
                    stitchJobs[i] = j;
                }

                for (int i = 0; i < stitchJobs.Length; ++i)
                {
                    jobHandles[i] = stitchJobs[i].Schedule();
                }

                GJobUtilities.CompleteAll(jobHandles);

                stitchSeamIteration += 1;
                int tmp = 0;
                for (int i = 0; i < chunksToUpdate.Count; ++i)
                {
                    tmp += chunksToUpdate[i].GetGenerationMetadata(GGeometryJobUtilities.METADATA_NEW_VERTEX_CREATED);
                }

                newVertexCreated = tmp > 0;
            }

            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                chunksToUpdate[i].CacheVertexMarker();
            }

            for (int i = 0; i < markers.Count; ++i)
            {
                GNativeArrayUtilities.Dispose(markers[i]);
            }
        }

        private void StitchSeamLOD(
            List<GTerrainChunk> chunksToUpdate,
            int meshBaseResolution,
            int meshResolution,
            int lod)
        {
            JobHandle[] jobHandles = new JobHandle[chunksToUpdate.Count];
            List<NativeArray<bool>> markers = new List<NativeArray<bool>>();
            int stitchSeamIteration = 0;
            int stitchSeamMaxIteration = GRuntimeSettings.Instance.geometryGeneration.triangulateIteration;
            bool newVertexCreated = true;

            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                GTerrainChunk c = chunksToUpdate[i];
                NativeArray<bool> markerLOD0 = c.GetVertexMarker();
                markers.Add(markerLOD0);
            }

            while (newVertexCreated && stitchSeamIteration <= stitchSeamMaxIteration)
            {
                for (int i = 0; i < chunksToUpdate.Count; ++i)
                {
                    GTerrainChunk c = chunksToUpdate[i];
                    GStitchSeamLODJob j = c.GetStitchSeamLODJob(
                        meshBaseResolution,
                        meshResolution,
                        lod,
                        markers[i]);
                    jobHandles[i] = j.Schedule();
                }

                GJobUtilities.CompleteAll(jobHandles);

                stitchSeamIteration += 1;
                int tmp = 0;
                for (int i = 0; i < chunksToUpdate.Count; ++i)
                {
                    tmp += chunksToUpdate[i].GetGenerationMetadata(GGeometryJobUtilities.METADATA_NEW_VERTEX_CREATED);
                }

                newVertexCreated = tmp > 0;
            }

            for (int i = 0; i < markers.Count; ++i)
            {
                GNativeArrayUtilities.Dispose(markers[i]);
            }
        }

        private void CountLeafNode(
            List<GTerrainChunk> chunksToUpdate,
            int meshBaseResolution,
            int meshResolution,
            int lod)
        {
            JobHandle[] jobHandles = new JobHandle[chunksToUpdate.Count];
            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                GCountLeafNodeJob j = chunksToUpdate[i].GetCountLeafNodeJob(meshBaseResolution, meshResolution, lod);
                jobHandles[i] = j.Schedule();
            }

            GJobUtilities.CompleteAll(jobHandles);
        }

        private void CreateVertex(
            List<GTerrainChunk> chunksToUpdate,
            int meshBaseResolution,
            int meshResolution,
            int lod)
        {
            JobHandle[] jobHandles = new JobHandle[chunksToUpdate.Count];
            int displacementSeed = geometryShortcut.DisplacementSeed;
            float displacementStrength = geometryShortcut.DisplacementStrength;
            bool smoothNormal = geometryShortcut.SmoothNormal;
            bool useSmoothNormalMask = geometryShortcut.UseSmoothNormalMask;
            bool mergeUv = geometryShortcut.MergeUv;
            GTextureNativeDataDescriptor<Color32> maskMap =
                new GTextureNativeDataDescriptor<Color32>((smoothNormal && useSmoothNormalMask)
                    ? maskShortcut.MaskMapOrDefault
                    : null);
            GTextureNativeDataDescriptor<Color32>[] heightMapGrid = GetHeightMapGrid();
            GAlbedoToVertexColorMode albedoToVertexColor = geometryShortcut.AlbedoToVertexColorMode;
            GTextureNativeDataDescriptor<Color32> albedoMap = new GTextureNativeDataDescriptor<Color32>(
                albedoToVertexColor != GAlbedoToVertexColorMode.None ? shadingShortcut.AlbedoMapOrDefault : null);
            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                GCreateVertexJob j = chunksToUpdate[i].GetCreateVertexJob(
                    meshBaseResolution,
                    meshResolution,
                    lod,
                    displacementSeed,
                    displacementStrength,
                    smoothNormal,
                    useSmoothNormalMask,
                    mergeUv,
                    maskMap,
                    albedoToVertexColor,
                    albedoMap,
                    heightMapGrid);

                jobHandles[i] = j.Schedule();
            }

            GJobUtilities.CompleteAll(jobHandles);

            for (int i = 0; i < heightMapGrid.Length; ++i)
            {
                heightMapGrid[i].Dispose();
            }
            if (!smoothNormal || !useSmoothNormalMask)
            {
                maskMap.Dispose();
            }

            if (albedoToVertexColor == GAlbedoToVertexColorMode.None)
            {
                albedoMap.Dispose();
            }
        }

        private void UpdateMesh(
            List<GTerrainChunk> chunksToUpdate,
            int lod)
        {
            GAlbedoToVertexColorMode albedoToVertexColor = geometryShortcut.AlbedoToVertexColorMode;
            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                chunksToUpdate[i].UpdateMesh(lod, albedoToVertexColor);
                chunksToUpdate[i].Refresh();
            }
        }

        private void BakeCollisionMesh(List<GTerrainChunk> chunksToUpdate)
        {
            NativeArray<int> meshLod0 = new NativeArray<int>(chunksToUpdate.Count, Allocator.TempJob);
            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                Mesh m = chunksToUpdate[i].GetMesh(0);
                if (m != null)
                {
                    meshLod0[i] = m.GetInstanceID();
                }
            }

            GBakeCollisionMeshJob job = new GBakeCollisionMeshJob();
            job.instanceIds = meshLod0;

            JobHandle h = job.Schedule(meshLod0.Length, 1);
            h.Complete();

            meshLod0.Dispose();
        }

        private void UpdateCollisionMesh(
            List<GTerrainChunk> chunksToUpdate)
        {
            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                chunksToUpdate[i].UpdateCollisionMesh();
            }
        }

        private void CleanUpGeneration(List<GTerrainChunk> chunksToUpdate)
        {
            for (int i = 0; i < chunksToUpdate.Count; ++i)
            {
                chunksToUpdate[i].CleanUpNativeArrays();
            }
        }

        private IEnumerator GenerateChunksTimeSliced(List<GTerrainChunk> chunksToUpdate)
        {
            yield return null;
            ForceLOD(0);

            int coreCount = 1; //SystemInfo.processorCount - 1;
            List<GTerrainChunk> chunks = new List<GTerrainChunk>();

            int step = (chunksToUpdate.Count + coreCount - 1) / coreCount;
            for (int repeat = 0; repeat < 2; ++repeat) //generate 2 times for seams to stitch up
            {
                for (int i = 0; i < step; ++i)
                {
                    chunks.Clear();
                    for (int j = 0; j < coreCount; ++j)
                    {
                        int index = i * coreCount + j;
                        if (index < chunksToUpdate.Count)
                        {
                            chunks.Add(chunksToUpdate[index]);
                        }
                    }

                    GenerateChunks(chunks, 0);

                    yield return null;
                }
            }

            int lodCount = TerrainData.Geometry.LODCount;
            if (lodCount == 1)
                yield break;

            for (int i = 0; i < step; ++i)
            {
                chunks.Clear();
                for (int j = 0; j < coreCount; ++j)
                {
                    int index = i * coreCount + j;
                    if (index < chunksToUpdate.Count)
                    {
                        chunks.Add(chunksToUpdate[index]);
                    }
                }

                for (int lod = 1; lod < lodCount; ++lod)
                {
                    GenerateChunks(chunks, lod);
                }

                yield return null;
            }

            BakeCollisionMesh(chunksToUpdate);
            UpdateCollisionMesh(chunksToUpdate);
            ForceLOD(-1);

            foreach (GTreeRenderer3 renderer in perCameraTreeRendererMap.Values)
            {
                renderer.OnGeometryChanged();
            }

            foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
            {
                renderer.OnGeometryChanged();
            }
        }

        private List<GTerrainChunk> ExtractDirtyChunks(GTerrainChunk[] chunks)
        {
            List<GTerrainChunk> dirtyChunks = new List<GTerrainChunk>(chunks.Length);
            for (int i = 0; i < chunks.Length; ++i)
            {
                Rect uvRect = chunks[i].GetUvRange();
                if (geometryShortcut.DirtyRegionOverlapTest(uvRect))
                {
                    dirtyChunks.Add(chunks[i]);
                }
            }

            return dirtyChunks;
        }

        private GTextureNativeDataDescriptor<Color32>[] GetHeightMapGrid()
        {
            GTextureNativeDataDescriptor<Color32>[] descriptors = new GTextureNativeDataDescriptor<Color32>[9];

            //center
            if (TerrainData != null)
            {
                descriptors[4] = new GTextureNativeDataDescriptor<Color32>(TerrainData.Geometry.HeightMap);
            }
            else
            {
                descriptors[4] = new GTextureNativeDataDescriptor<Color32>(null);
            }

            //left - top - right - bottom
            //if (LeftNeighbor != null && LeftNeighbor.gameObject.activeInHierarchy && LeftNeighbor.TerrainData != null)
            if (LeftTerrainData != null)
            {
                descriptors[3] = new GTextureNativeDataDescriptor<Color32>(LeftTerrainData.Geometry.HeightMap);
            }
            else
            {
                descriptors[3] = new GTextureNativeDataDescriptor<Color32>(null);
            }

            if (TopTerrainData != null)
            {
                descriptors[7] = new GTextureNativeDataDescriptor<Color32>(TopTerrainData.Geometry.HeightMap);
            }
            else
            {
                descriptors[7] = new GTextureNativeDataDescriptor<Color32>(null);
            }

            if (RightTerrainData != null)
            {
                descriptors[5] = new GTextureNativeDataDescriptor<Color32>(RightTerrainData.Geometry.HeightMap);
            }
            else
            {
                descriptors[5] = new GTextureNativeDataDescriptor<Color32>(null);
            }

            if (BottomTerrainData != null)
            {
                descriptors[1] = new GTextureNativeDataDescriptor<Color32>(BottomTerrainData.Geometry.HeightMap);
            }
            else
            {
                descriptors[1] = new GTextureNativeDataDescriptor<Color32>(null);
            }

            //BL
            if (BottomNeighbor != null && BottomNeighbor.LeftNeighbor != null)
            {
                if (BottomNeighbor.LeftNeighbor.TerrainData != null &&
                    BottomNeighbor.LeftNeighbor.gameObject.activeInHierarchy)
                {
                    descriptors[0] =
                        new GTextureNativeDataDescriptor<Color32>(BottomNeighbor.LeftNeighbor.TerrainData.Geometry
                            .HeightMap);
                }
                else
                {
                    descriptors[0] = new GTextureNativeDataDescriptor<Color32>(null);
                }
            }
            else if (LeftNeighbor != null && LeftNeighbor.BottomNeighbor != null)
            {
                if (LeftNeighbor.BottomNeighbor.TerrainData != null &&
                    LeftNeighbor.BottomNeighbor.gameObject.activeInHierarchy)
                {
                    descriptors[0] =
                        new GTextureNativeDataDescriptor<Color32>(LeftNeighbor.BottomNeighbor.TerrainData.Geometry
                            .HeightMap);
                }
                else
                {
                    descriptors[0] = new GTextureNativeDataDescriptor<Color32>(null);
                }
            }
            else
            {
                descriptors[0] = new GTextureNativeDataDescriptor<Color32>(null);
            }

            //TL
            if (TopNeighbor != null && TopNeighbor.LeftNeighbor != null)
            {
                if (TopNeighbor.LeftNeighbor.TerrainData != null &&
                    TopNeighbor.LeftNeighbor.gameObject.activeInHierarchy)
                {
                    descriptors[6] =
                        new GTextureNativeDataDescriptor<Color32>(TopNeighbor.LeftNeighbor.TerrainData.Geometry
                            .HeightMap);
                }
                else
                {
                    descriptors[6] = new GTextureNativeDataDescriptor<Color32>(null);
                }
            }
            else if (LeftNeighbor != null && LeftNeighbor.TopNeighbor != null)
            {
                if (LeftNeighbor.TopNeighbor.TerrainData != null &&
                    LeftNeighbor.TopNeighbor.gameObject.activeInHierarchy)
                {
                    descriptors[6] =
                        new GTextureNativeDataDescriptor<Color32>(LeftNeighbor.TopNeighbor.TerrainData.Geometry
                            .HeightMap);
                }
                else
                {
                    descriptors[6] = new GTextureNativeDataDescriptor<Color32>(null);
                }
            }
            else
            {
                descriptors[6] = new GTextureNativeDataDescriptor<Color32>(null);
            }

            //TR
            if (TopNeighbor != null && TopNeighbor.RightNeighbor != null)
            {
                if (TopNeighbor.RightNeighbor.TerrainData != null &&
                    TopNeighbor.RightNeighbor.gameObject.activeInHierarchy)
                {
                    descriptors[8] =
                        new GTextureNativeDataDescriptor<Color32>(TopNeighbor.RightNeighbor.TerrainData.Geometry
                            .HeightMap);
                }
                else
                {
                    descriptors[8] = new GTextureNativeDataDescriptor<Color32>(null);
                }
            }
            else if (RightNeighbor != null && RightNeighbor.TopNeighbor != null)
            {
                if (RightNeighbor.TopNeighbor.TerrainData != null &&
                    RightNeighbor.TopNeighbor.gameObject.activeInHierarchy)
                {
                    descriptors[8] =
                        new GTextureNativeDataDescriptor<Color32>(RightNeighbor.TopNeighbor.TerrainData.Geometry
                            .HeightMap);
                }
                else
                {
                    descriptors[8] = new GTextureNativeDataDescriptor<Color32>(null);
                }
            }
            else
            {
                descriptors[8] = new GTextureNativeDataDescriptor<Color32>(null);
            }

            //BR
            if (BottomNeighbor != null && BottomNeighbor.RightNeighbor != null)
            {
                if (BottomNeighbor.RightNeighbor.TerrainData != null &&
                    BottomNeighbor.RightNeighbor.gameObject.activeInHierarchy)
                {
                    descriptors[2] =
                        new GTextureNativeDataDescriptor<Color32>(BottomNeighbor.RightNeighbor.TerrainData.Geometry
                            .HeightMap);
                }
                else
                {
                    descriptors[2] = new GTextureNativeDataDescriptor<Color32>(null);
                }
            }
            else if (RightNeighbor != null && RightNeighbor.BottomNeighbor != null)
            {
                if (RightNeighbor.BottomNeighbor.TerrainData != null &&
                    RightNeighbor.BottomNeighbor.gameObject.activeInHierarchy)
                {
                    descriptors[2] =
                        new GTextureNativeDataDescriptor<Color32>(RightNeighbor.BottomNeighbor.TerrainData.Geometry
                            .HeightMap);
                }
                else
                {
                    descriptors[2] = new GTextureNativeDataDescriptor<Color32>(null);
                }
            }
            else
            {
                descriptors[2] = new GTextureNativeDataDescriptor<Color32>(null);
            }

            return descriptors;
        }

        private GTerrainChunk GetLeftNeighborChunk(GTerrainChunk c)
        {
            int maxIndex = geometryShortcut.ChunkGridSize - 1;
            Vector2 index = c.Index;
            if (index.x > 0)
            {
                return c.Internal_NeighborChunks[0];
            }

            if (LeftNeighbor == null)
                return null;
            if (!LeftNeighbor.gameObject.activeInHierarchy)
                return null;

            List<GTerrainChunk> leftTerrainChunks = new List<GTerrainChunk>(LeftNeighbor.GetChunks());
            index.x = maxIndex;
            GTerrainChunk neighborChunk = leftTerrainChunks.Find(c0 => c0.Index == index);
            return neighborChunk;
        }

        private GTerrainChunk GetTopNeighborChunk(GTerrainChunk c)
        {
            int maxIndex = geometryShortcut.ChunkGridSize - 1;
            Vector2 index = c.Index;
            if (index.y < maxIndex)
            {
                return c.Internal_NeighborChunks[1];
            }

            if (TopNeighbor == null)
                return null;
            if (!TopNeighbor.gameObject.activeInHierarchy)
                return null;

            List<GTerrainChunk> topTerrainChunks = new List<GTerrainChunk>(TopNeighbor.GetChunks());
            index.y = 0;
            GTerrainChunk neighborChunk = topTerrainChunks.Find(c0 => c0.Index == index);
            return neighborChunk;
        }

        private GTerrainChunk GetRightNeighborChunk(GTerrainChunk c)
        {
            int maxIndex = geometryShortcut.ChunkGridSize - 1;
            Vector2 index = c.Index;
            if (index.x < maxIndex)
            {
                return c.Internal_NeighborChunks[2];
            }

            if (RightNeighbor == null)
                return null;
            if (!RightNeighbor.gameObject.activeInHierarchy)
                return null;

            List<GTerrainChunk> rightTerrainChunks = new List<GTerrainChunk>(RightNeighbor.GetChunks());
            index.x = 0;
            GTerrainChunk neighborChunk = rightTerrainChunks.Find(c0 => c0.Index == index);
            return neighborChunk;
        }

        private GTerrainChunk GetBottomNeighborChunk(GTerrainChunk c)
        {
            int maxIndex = geometryShortcut.ChunkGridSize - 1;
            Vector2 index = c.Index;
            if (index.y > 0)
            {
                return c.Internal_NeighborChunks[3];
            }

            if (BottomNeighbor == null)
                return null;
            if (!BottomNeighbor.gameObject.activeInHierarchy)
                return null;

            List<GTerrainChunk> bottomTerrainChunks = new List<GTerrainChunk>(BottomNeighbor.GetChunks());
            index.y = maxIndex;
            GTerrainChunk neighborChunk = bottomTerrainChunks.Find(c0 => c0.Index == index);
            return neighborChunk;
        }

        private void OnRenderingDirty()
        {
            geometryShortcut = TerrainData.Geometry;
            InitChunks();
            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].Internal_UpdateRenderer();
            }
        }

        private void OnShadingDirty()
        {
            geometryShortcut = TerrainData.Geometry;
            shadingShortcut = TerrainData.Shading;

#if __MICROSPLAT_POLARIS__
            if (TerrainData.Shading.ShadingSystem == GShadingSystem.MicroSplat)
            {
                //PullMaterialAndSplatMapsFromMicroSplat();
                PushControlTexturesToMicroSplat();
            }
#endif

            if (TerrainData.Shading.ShadingSystem != GShadingSystem.Polaris)
                return;
            InitChunks();
            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].Internal_UpdateMaterial();
            }
        }

#if __MICROSPLAT_POLARIS__
        public void PushControlTexturesToMicroSplat()
        {
            if (TerrainData == null)
                return;
            MicroSplatPolarisMesh pm = gameObject.GetComponent<MicroSplatPolarisMesh>();
            if (pm == null)
                return;
            Texture2D[] controls = new Texture2D[TerrainData.Shading.SplatControlMapCount];
            for (int i = 0; i < controls.Length; ++i)
            {
                controls[i] = TerrainData.Shading.GetSplatControl(i);
            }
            pm.controlTextures = controls;
            pm.Sync();
        }

        public void PullMaterialAndSplatMapsFromMicroSplat()
        {
            if (TerrainData == null)
                return;
            MicroSplatPolarisMesh pm = gameObject.GetComponent<MicroSplatPolarisMesh>();
            if (pm == null)
                return;
            TerrainData.Shading.CustomMaterial = pm.matInstance;

            TextureArrayConfig config = TerrainData.Shading.MicroSplatTextureArrayConfig;
            if (config == null)
                return;
            GSplatPrototypeGroup protoGroup = TerrainData.Shading.Splats;
            if (protoGroup == null)
                return;

            List<TextureArrayConfig.TextureEntry> entries = config.sourceTextures;
            List<GSplatPrototype> prototypes = new List<GSplatPrototype>();
            for (int i = 0; i < entries.Count; ++i)
            {
                GSplatPrototype p = new GSplatPrototype();
                p.Texture = entries[i].diffuse;
                prototypes.Add(p);
            }

            protoGroup.Prototypes = prototypes;
        }
#endif

        private void OnFoliageDirty()
        {
            TerrainData.Foliage.Refresh();
        }

        private GTerrainChunk[] InitChunks()
        {
            int gridSize = geometryShortcut.ChunkGridSize;
            Vector2 chunkPhysicalSize = new Vector2(geometryShortcut.Width, geometryShortcut.Length) / gridSize;
            Transform root = GetChunkRoot();
            GTerrainChunk[] chunks = null;
            if (root == null)
            {
                root = GetOrCreateChunkRoot();
                chunks = CreateNewChunks(root);
                for (int i = 0; i < chunks.Length; ++i)
                {
                    InitSingleChunk(chunks, i, chunkPhysicalSize);
                }
            }
            else
            {
                chunks = root.GetComponentsInChildren<GTerrainChunk>();
                if (chunks.Length != gridSize * gridSize)
                {
                    DestroyImmediate(root.gameObject);
                    root = GetOrCreateChunkRoot();
                    chunks = CreateNewChunks(root);
                    for (int i = 0; i < chunks.Length; ++i)
                    {
                        InitSingleChunk(chunks, i, chunkPhysicalSize);
                    }
                }
                else
                {
                    for (int i = 0; i < chunks.Length; ++i)
                    {
                        InitSingleChunk(chunks, i, chunkPhysicalSize);
                    }
                }
            }

            GUtilities.MarkCurrentSceneDirty();
            return chunks;
        }

        private void InitSingleChunk(GTerrainChunk[] chunks, int chunkIndex, Vector3 chunkPhysicalSize)
        {
            GTerrainChunk currentChunk = chunks[chunkIndex];
            currentChunk.SetupLODGroup(geometryShortcut.LODCount);

            GUtilities.Fill(currentChunk.Internal_NeighborChunks, null);
            for (int j = 0; j < chunks.Length; ++j)
            {
                GTerrainChunk otherChunk = chunks[j];
                if (otherChunk.Index == currentChunk.Index + Vector2.left)
                {
                    currentChunk.Internal_NeighborChunks[0] = otherChunk;
                }

                if (otherChunk.Index == currentChunk.Index + Vector2.up)
                {
                    currentChunk.Internal_NeighborChunks[1] = otherChunk;
                }

                if (otherChunk.Index == currentChunk.Index + Vector2.right)
                {
                    currentChunk.Internal_NeighborChunks[2] = otherChunk;
                }

                if (otherChunk.Index == currentChunk.Index + Vector2.down)
                {
                    currentChunk.Internal_NeighborChunks[3] = otherChunk;
                }
            }
#if UNITY_EDITOR && !UNITY_2022_2_OR_NEWER
            StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(currentChunk.gameObject);
            GameObjectUtility.SetStaticEditorFlags(currentChunk.gameObject, staticFlags | StaticEditorFlags.NavigationStatic);
#endif
        }

        private GTerrainChunk[] CreateNewChunks(Transform root)
        {
            int gridSize = TerrainData.Geometry.ChunkGridSize;
            GTerrainChunk[] chunks = new GTerrainChunk[gridSize * gridSize];
            for (int z = 0; z < gridSize; ++z)
            {
                for (int x = 0; x < gridSize; ++x)
                {
                    Transform chunkTransform = new GameObject($"C({x},{z})").transform;
                    chunkTransform.parent = root;
                    GTerrainChunk chunk = chunkTransform.gameObject.AddComponent<GTerrainChunk>();
                    chunk.Index = new Vector2(x, z);
                    chunk.Terrain = this;
                    chunk.Internal_UpdateRenderer();
                    chunk.Internal_UpdateMaterial();
                    chunks[GUtilities.To1DIndex(x, z, gridSize)] = chunk;
                }
            }

            return chunks;
        }

        /// <summary>
        /// Get all the children chunks of this terrain. Chunks are just smaller unit of a terrain containing surface mesh & renderer & collider that render one small part of the terrain.
        /// </summary>
        /// <returns>Array of children chunks.</returns>
        public GTerrainChunk[] GetChunks()
        {
            Transform root = GetChunkRoot();
            if (root != null)
            {
                return root.GetComponentsInChildren<GTerrainChunk>();
            }
            else
            {
                return new GTerrainChunk[0];
            }
        }

        private List<GTerrainChunk> GetChunksSortedByDistance(Vector3 origin)
        {
            List<GTerrainChunk> chunks = new List<GTerrainChunk>(GetChunks());
            SortChunkByDistance(origin, chunks);
            return chunks;
        }

        private void SortChunkByDistance(Vector3 origin, List<GTerrainChunk> chunks)
        {
            chunks.Sort((c1, c2) =>
            {
                Vector3 center1 = c1.MeshColliderComponent.bounds.center;
                Vector3 center2 = c2.MeshColliderComponent.bounds.center;
                float d1 = Vector3.Distance(origin, center1);
                float d2 = Vector3.Distance(origin, center2);
                return d1.CompareTo(d2);
            });
        }

        /// <summary>
        /// Perform physics raycast against this terrain.
        /// This is just a convenient function, you can use Physics.Raycast as usual.
        /// </summary>
        /// <param name="ray">The ray to cast against this terrian.</param>
        /// <param name="hit">Will contains hit point information.</param>
        /// <param name="distance">Length of the ray.</param>
        /// <returns>True if ray was hit the terrain.</returns>
        public bool Raycast(Ray ray, out RaycastHit hit, float distance)
        {
            List<GTerrainChunk> chunks = GetChunksSortedByDistance(ray.origin);

            for (int i = 0; i < chunks.Count; ++i)
            {
                if (chunks[i].MeshColliderComponent.Raycast(ray, out hit, distance))
                {
                    return true;
                }
            }

            hit = new RaycastHit();
            return false;
        }

        /// <summary>
        /// Perform a top-down raycast against this terrain.
        /// This is just a convenient function, you can use Physics.Raycast as usual.
        /// </summary>
        /// <param name="normalizePoint">Ray position in terrain's normalized [0-1] space. Y component will be omitted.</param>
        /// <param name="hit">Will contains information about the hit point.</param>
        /// <returns>True if ray hits the terrain.</returns>
        public bool Raycast(Vector3 normalizePoint, out RaycastHit hit)
        {
            Ray r = new Ray();
            Vector3 origin = NormalizedToWorldPoint(normalizePoint);
            origin.y = 10000;
            r.origin = origin;
            r.direction = Vector3.down;

            return Raycast(r, out hit, float.MaxValue);
        }

        /// <summary>
        /// Perform raycast against all terrain within a group.
        /// This is a convenient function, you can use Physics.Raycast as usual.
        /// </summary>
        /// <param name="ray">The ray to cast.</param>
        /// <param name="hit">Will contains information about the hit point.</param>
        /// <param name="distance">Length of the ray.</param>
        /// <param name="groupId">Terrain group id.</param>
        /// <returns>True if ray hits any terrain.</returns>
        public static bool Raycast(Ray ray, out RaycastHit hit, float distance, int groupId)
        {
            List<RaycastHit> hitInfo = new List<RaycastHit>();
            IEnumerator<GStylizedTerrain> terrain = ActiveTerrains.GetEnumerator();
            while (terrain.MoveNext())
            {
                if (terrain.Current.GroupId != groupId && groupId >= 0)
                    continue;
                RaycastHit h;
                if (terrain.Current.Raycast(ray, out h, distance))
                {
                    hitInfo.Add(h);
                }
            }

            if (hitInfo.Count == 0)
            {
                hit = new RaycastHit();
                return false;
            }
            else
            {
                hitInfo.Sort((h0, h1) =>
                    Vector3.SqrMagnitude(h0.point - ray.origin)
                        .CompareTo(Vector3.SqrMagnitude(h1.point - ray.origin)));
                hit = hitInfo[0];
                return true;
            }
        }

        /// <summary>
        /// Convert a point in world space to terrain UV [0-1] space.
        /// </summary>
        /// <param name="point">World position, Y component will be omitted.</param>
        /// <returns>The UV position for texture sampling.</returns>
        public Vector2 WorldPointToUV(Vector3 point)
        {
            if (TerrainData == null)
                return Vector2.zero;
            Vector3 localPoint = transform.InverseTransformPoint(point);
            Vector3 terrainSize = new Vector3(TerrainData.Geometry.Width, TerrainData.Geometry.Height,
                TerrainData.Geometry.Length);
            Vector2 uv = new Vector2(
                GUtilities.InverseLerpUnclamped(0, terrainSize.x, localPoint.x),
                GUtilities.InverseLerpUnclamped(0, terrainSize.z, localPoint.z));
            return uv;
        }

        /// <summary>
        /// Convert a point in world space to terrain normalized [0-1] space.
        /// </summary>
        /// <param name="point">World position.</param>
        /// <returns>Position in normalized [0-1] space.</returns>
        public Vector3 WorldPointToNormalized(Vector3 point)
        {
            if (TerrainData == null)
                return Vector2.zero;
            Vector3 localPoint = transform.InverseTransformPoint(point);
            Vector3 terrainSize = new Vector3(TerrainData.Geometry.Width, TerrainData.Geometry.Height,
                TerrainData.Geometry.Length);
            Vector3 normalized = new Vector3(
                GUtilities.InverseLerpUnclamped(0, terrainSize.x, localPoint.x),
                GUtilities.InverseLerpUnclamped(0, terrainSize.y, localPoint.y),
                GUtilities.InverseLerpUnclamped(0, terrainSize.z, localPoint.z));
            return normalized;
        }

        /// <summary>
        /// Convert a point in normalized [0-1] space to world space.
        /// </summary>
        /// <param name="normalizedPoint">A point in normalized [0-1] space.</param>
        /// <returns>A point in world space.</returns>
        public Vector3 NormalizedToWorldPoint(Vector3 normalizedPoint)
        {
            if (TerrainData == null)
            {
                return normalizedPoint;
            }
            else
            {
                Matrix4x4 matrix = Matrix4x4.TRS(
                    transform.position,
                    transform.rotation,
                    new Vector3(
                        transform.lossyScale.x * TerrainData.Geometry.Width,
                        transform.lossyScale.y * TerrainData.Geometry.Height,
                        transform.lossyScale.z * TerrainData.Geometry.Length));
                return matrix.MultiplyPoint(normalizedPoint);
            }
        }

        /// <summary>
        /// Get a matrix to transform a point from world to normalzied [0-1] space.
        /// Use this matrix with Matrix4x4.MultiplyPoint()
        /// </summary>
        /// <returns>The world to normalized matrix.</returns>
        public Matrix4x4 GetWorldToNormalizedMatrix()
        {
            Vector3 terrainSize = TerrainData.Geometry.Size;
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Matrix4x4 localToNormalized =
                Matrix4x4.Scale(new Vector3(1 / terrainSize.x, 1 / terrainSize.y, 1 / terrainSize.z));
            Matrix4x4 worldToNormalized = localToNormalized * worldToLocal;
            return worldToNormalized;
        }

        /// <summary>
        /// Force the terrain to be rendered at a specified LOD. 
        /// </summary>
        /// <param name="level">The LOD to use. Use -1 to render at standard LOD processing.</param>
        public void ForceLOD(int level)
        {
            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].LodGroupComponent.ForceLOD(level);
            }
        }

        [ExcludeFromDoc]
        public Vector4 GetHeightMapSample(Vector2 uv)
        {
            if (TerrainData == null)
                return Vector4.zero;
            Vector4 sample = TerrainData.Geometry.GetDecodedHeightMapSample(uv);
            return sample;
        }

        [ExcludeFromDoc]
        private static Vector4 GetHeightMapSample(GStylizedTerrain t, Vector2 uv)
        {
            if (t == null || !t.isActiveAndEnabled)
                return Vector4.zero;
            else
                return t.GetHeightMapSample(uv);
        }

        [ExcludeFromDoc]
        public Vector4 GetInterpolatedHeightMapSample(Vector2 uv)
        {
            int count = 1;
            Vector4 sample = Vector4.zero;
            if (uv.x == 0 && uv.y == 0) //bottom left
            {
                sample += GetHeightMapSample(uv);
                sample += GetHeightMapSample(LeftNeighbor, new Vector2(1, 0));
                sample += GetHeightMapSample(BottomNeighbor, new Vector2(0, 1));
                sample +=
                    LeftNeighbor != null ? GetHeightMapSample(LeftNeighbor.BottomNeighbor, new Vector2(1, 1)) :
                    BottomNeighbor != null ? GetHeightMapSample(BottomNeighbor.LeftNeighbor, new Vector2(1, 1)) :
                    Vector4.zero;

                count += LeftNeighbor != null ? 1 : 0;
                count += BottomNeighbor != null ? 1 : 0;
                count += LeftNeighbor != null && LeftNeighbor.BottomNeighbor != null ? 1 :
                    BottomNeighbor != null && BottomNeighbor.LeftNeighbor != null ? 1 : 0;
            }
            else if (uv.x == 0 && uv.y == 1) //top left
            {
                sample += GetHeightMapSample(uv);
                sample += GetHeightMapSample(LeftNeighbor, new Vector2(1, 1));
                sample += GetHeightMapSample(TopNeighbor, new Vector2(0, 0));
                sample +=
                    LeftNeighbor != null ? GetHeightMapSample(LeftNeighbor.TopNeighbor, new Vector2(1, 0)) :
                    TopNeighbor != null ? GetHeightMapSample(TopNeighbor.LeftNeighbor, new Vector2(1, 0)) :
                    Vector4.zero;

                count += LeftNeighbor != null ? 1 : 0;
                count += TopNeighbor != null ? 1 : 0;
                count += LeftNeighbor != null && LeftNeighbor.TopNeighbor != null ? 1 :
                    TopNeighbor != null && TopNeighbor.LeftNeighbor != null ? 1 : 0;
            }
            else if (uv.x == 1 && uv.y == 1) //top right
            {
                sample += GetHeightMapSample(uv);
                sample += GetHeightMapSample(RightNeighbor, new Vector2(0, 1));
                sample += GetHeightMapSample(TopNeighbor, new Vector2(1, 0));
                sample +=
                    RightNeighbor != null ? GetHeightMapSample(RightNeighbor.TopNeighbor, new Vector2(0, 0)) :
                    TopNeighbor != null ? GetHeightMapSample(TopNeighbor.RightNeighbor, new Vector2(0, 0)) :
                    Vector4.zero;

                count += RightNeighbor != null ? 1 : 0;
                count += TopNeighbor != null ? 1 : 0;
                count += RightNeighbor != null && RightNeighbor.TopNeighbor != null ? 1 :
                    TopNeighbor != null && TopNeighbor.RightNeighbor != null ? 1 : 0;
            }
            else if (uv.x == 1 && uv.y == 0) //bottom right
            {
                sample += GetHeightMapSample(uv);
                sample += GetHeightMapSample(RightNeighbor, new Vector2(0, 0));
                sample += GetHeightMapSample(BottomNeighbor, new Vector2(1, 1));
                sample +=
                    RightNeighbor != null ? GetHeightMapSample(RightNeighbor.BottomNeighbor, new Vector2(0, 1)) :
                    BottomNeighbor != null ? GetHeightMapSample(BottomNeighbor.RightNeighbor, new Vector2(0, 1)) :
                    Vector4.zero;

                count += RightNeighbor != null ? 1 : 0;
                count += BottomNeighbor != null ? 1 : 0;
                count += RightNeighbor != null && RightNeighbor.BottomNeighbor != null ? 1 :
                    BottomNeighbor != null && BottomNeighbor.RightNeighbor != null ? 1 : 0;
            }
            else if (uv.x == 0 && uv.y != 0 && uv.y != 1) //left edge
            {
                sample += GetHeightMapSample(uv);
                sample += GetHeightMapSample(LeftNeighbor, new Vector2(1, uv.y));

                count += LeftNeighbor != null ? 1 : 0;
            }
            else if (uv.x == 1 && uv.y != 0 && uv.y != 1) //right edge
            {
                sample += GetHeightMapSample(uv);
                sample += GetHeightMapSample(RightNeighbor, new Vector2(0, uv.y));

                count += RightNeighbor != null ? 1 : 0;
            }
            else if (uv.x != 0 && uv.x != 1 && uv.y == 0) //bottom edge
            {
                sample += GetHeightMapSample(uv);
                sample += GetHeightMapSample(BottomNeighbor, new Vector2(uv.x, 1));

                count += BottomNeighbor != null ? 1 : 0;
            }
            else if (uv.x != 0 && uv.x != 1 && uv.y == 1) //top edge
            {
                sample += GetHeightMapSample(uv);
                sample += GetHeightMapSample(TopNeighbor, new Vector2(uv.x, 0));

                count += TopNeighbor != null ? 1 : 0;
            }
            else
            {
                sample = GetHeightMapSample(uv);
            }

            return sample * 1.0f / count;
        }

        /// <summary>
        /// Match the height map and meshes at the edges of all terrains in a group.
        /// </summary>
        /// <param name="groupId">The terrain group.</param>
        /// <param name="updateMesh">Indicate if you want to regenerate the surface meshes too. Default is true.</param>
        public static void MatchEdges(int groupId, bool updateMesh = true)
        {
            GCommon.ForEachTerrain(groupId, (t) => { t.MatchEdges(false); });
            if (updateMesh)
            {
                GCommon.ForEachTerrain(groupId, (t) =>
                {
                    if (t.TerrainData != null)
                    {
                        t.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                    }
                });
            }
        }

        /// <summary>
        /// Match the height map and meshes at the edges of this terrain and its neighbors.
        /// </summary>
        /// <param name="updateMesh">Indicate if you want to regenerate the surface meshes too. Default is true.</param>
        public void MatchEdges(bool updateMesh = true)
        {
            if (TerrainData == null)
                return;
            MatchEdgesLeft(updateMesh);
            MatchEdgesRight(updateMesh);
            MatchEdgesTop(updateMesh);
            MatchEdgesBottom(updateMesh);

            MatchEdgeLeftTop(updateMesh);
            MatchEdgeTopRight(updateMesh);
            MatchEdgeRightBottom(updateMesh);
            MatchEdgeBottomLeft(updateMesh);
        }

        private void MatchEdgesLeft(bool updateMesh)
        {
            if (LeftNeighbor == null)
                return;
            if (LeftNeighbor.TerrainData == null)
                return;
            int res = TerrainData.Geometry.HeightMapResolution;
            if (LeftNeighbor.TerrainData.Geometry.HeightMapResolution != res)
            {
                Debug.LogWarning("Can't match terrain edges, different height map resolution.");
                return;
            }

            Texture2D selfHm = TerrainData.Geometry.HeightMap;
            Texture2D otherHm = LeftNeighbor.TerrainData.Geometry.HeightMap;

            Color[] selfData = selfHm.GetPixels(0, 1, 1, res - 1, 0);
            Color[] otherData = otherHm.GetPixels(res - 1, 1, 1, res - 1, 0);
            for (int i = 0; i < selfData.Length; ++i)
            {
                Color avg = GUtilities.MatchHeightMapSample(2, selfData[i], otherData[i]);
                selfData[i] = avg;
                otherData[i] = avg;
            }

            selfHm.SetPixels(0, 1, 1, res - 1, selfData, 0);
            selfHm.Apply();
            otherHm.SetPixels(res - 1, 1, 1, res - 1, otherData, 0);
            otherHm.Apply();

            TerrainData.Geometry.SetRegionDirty(GUtilities.GetLeftDirtyRect());
            LeftNeighbor.TerrainData.Geometry.SetRegionDirty(GUtilities.GetRightDirtyRect());

            if (updateMesh)
            {
                TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                LeftNeighbor.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
            }
        }

        private void MatchEdgesRight(bool updateMesh)
        {
            if (RightNeighbor == null)
                return;
            if (RightNeighbor.TerrainData == null)
                return;
            int res = TerrainData.Geometry.HeightMapResolution;
            if (RightNeighbor.TerrainData.Geometry.HeightMapResolution != res)
            {
                Debug.LogWarning("Can't match terrain edges, different height map resolution.");
                return;
            }

            Texture2D selfHm = TerrainData.Geometry.HeightMap;
            Texture2D otherHm = RightNeighbor.TerrainData.Geometry.HeightMap;

            Color[] selfData = selfHm.GetPixels(res - 1, 1, 1, res - 1, 0);
            Color[] otherData = otherHm.GetPixels(0, 1, 1, res - 1, 0);
            for (int i = 0; i < selfData.Length; ++i)
            {
                Color avg = GUtilities.MatchHeightMapSample(2, selfData[i], otherData[i]);
                selfData[i] = avg;
                otherData[i] = avg;
            }

            selfHm.SetPixels(res - 1, 1, 1, res - 1, otherData, 0);
            selfHm.Apply();
            otherHm.SetPixels(0, 1, 1, res - 1, selfData, 0);
            otherHm.Apply();

            TerrainData.Geometry.SetRegionDirty(GUtilities.GetRightDirtyRect());
            RightNeighbor.TerrainData.Geometry.SetRegionDirty(GUtilities.GetLeftDirtyRect());

            if (updateMesh)
            {
                TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                RightNeighbor.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
            }
        }

        private void MatchEdgesTop(bool updateMesh)
        {
            if (TopNeighbor == null)
                return;
            if (TopNeighbor.TerrainData == null)
                return;
            int res = TerrainData.Geometry.HeightMapResolution;
            if (TopNeighbor.TerrainData.Geometry.HeightMapResolution != res)
            {
                Debug.LogWarning("Can't match terrain edges, different height map resolution.");
                return;
            }

            Texture2D selfHm = TerrainData.Geometry.HeightMap;
            Texture2D otherHm = TopNeighbor.TerrainData.Geometry.HeightMap;

            Color[] selfData = selfHm.GetPixels(1, res - 1, res - 1, 1, 0);
            Color[] otherData = otherHm.GetPixels(1, 0, res - 1, 1, 0);
            for (int i = 0; i < selfData.Length; ++i)
            {
                Color avg = GUtilities.MatchHeightMapSample(2, selfData[i], otherData[i]);
                selfData[i] = avg;
                otherData[i] = avg;
            }

            selfHm.SetPixels(1, res - 1, res - 1, 1, selfData, 0);
            selfHm.Apply();
            otherHm.SetPixels(1, 0, res - 1, 1, otherData, 0);
            otherHm.Apply();

            TerrainData.Geometry.SetRegionDirty(GUtilities.GetTopDirtyRect());
            TopNeighbor.TerrainData.Geometry.SetRegionDirty(GUtilities.GetBottomDirtyRect());

            if (updateMesh)
            {
                TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                TopNeighbor.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
            }
        }

        private void MatchEdgesBottom(bool updateMesh)
        {
            if (BottomNeighbor == null)
                return;
            if (BottomNeighbor.TerrainData == null)
                return;
            int res = TerrainData.Geometry.HeightMapResolution;
            if (BottomNeighbor.TerrainData.Geometry.HeightMapResolution != res)
            {
                Debug.LogWarning("Can't match terrain edges, different height map resolution.");
                return;
            }

            Texture2D selfHm = TerrainData.Geometry.HeightMap;
            Texture2D otherHm = BottomNeighbor.TerrainData.Geometry.HeightMap;

            Color[] selfData = selfHm.GetPixels(1, 0, res - 1, 1, 0);
            Color[] otherData = otherHm.GetPixels(1, res - 1, res - 1, 1, 0);
            for (int i = 0; i < selfData.Length; ++i)
            {
                Color avg = GUtilities.MatchHeightMapSample(2, selfData[i], otherData[i]);
                selfData[i] = avg;
                otherData[i] = avg;
            }

            selfHm.SetPixels(1, 0, res - 1, 1, otherData, 0);
            selfHm.Apply();
            otherHm.SetPixels(1, res - 1, res - 1, 1, selfData, 0);
            otherHm.Apply();

            TerrainData.Geometry.SetRegionDirty(GUtilities.GetBottomDirtyRect());
            BottomNeighbor.TerrainData.Geometry.SetRegionDirty(GUtilities.GetTopDirtyRect());

            if (updateMesh)
            {
                TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                BottomNeighbor.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
            }
        }

        private void MatchEdgeLeftTop(bool updateMesh)
        {
            int res = TerrainData.Geometry.HeightMapResolution;
            GStylizedTerrain left = null;
            if (LeftNeighbor != null &&
                LeftNeighbor.TerrainData != null &&
                LeftNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                left = LeftNeighbor;
            }

            GStylizedTerrain top = null;
            if (TopNeighbor != null &&
                TopNeighbor.TerrainData != null &&
                TopNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                top = TopNeighbor;
            }

            GStylizedTerrain topLeft = null;
            if (TopNeighbor != null &&
                TopNeighbor.LeftNeighbor != null &&
                TopNeighbor.LeftNeighbor.TerrainData != null &&
                TopNeighbor.LeftNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                topLeft = TopNeighbor.LeftNeighbor;
            }

            if (topLeft == null)
            {
                if (LeftNeighbor != null &&
                    LeftNeighbor.TopNeighbor != null &&
                    LeftNeighbor.TopNeighbor.TerrainData != null &&
                    LeftNeighbor.TopNeighbor.TerrainData.Geometry.HeightMapResolution == res)
                {
                    topLeft = LeftNeighbor.TopNeighbor;
                }
            }

            Color data = TerrainData.Geometry.HeightMap.GetPixel(0, res - 1);
            int c = 1;

            if (left != null)
            {
                data += left.TerrainData.Geometry.HeightMap.GetPixel(res - 1, res - 1);
                c += 1;
            }

            if (top != null)
            {
                data += top.TerrainData.Geometry.HeightMap.GetPixel(0, 0);
                c += 1;
            }

            if (topLeft != null)
            {
                data += topLeft.TerrainData.Geometry.HeightMap.GetPixel(res - 1, 0);
                c += 1;
            }

            Color avg = data / c;
            TerrainData.Geometry.HeightMap.SetPixel(0, res - 1, avg);
            TerrainData.Geometry.HeightMap.Apply();
            TerrainData.Geometry.SetRegionDirty(GUtilities.GetLeftTopDirtyRect());
            if (updateMesh)
            {
                TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
            }

            if (left != null)
            {
                left.TerrainData.Geometry.HeightMap.SetPixel(res - 1, res - 1, avg);
                left.TerrainData.Geometry.HeightMap.Apply();
                left.TerrainData.Geometry.SetRegionDirty(GUtilities.GetTopRightDirtyRect());
                if (updateMesh)
                {
                    left.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }

            if (top != null)
            {
                top.TerrainData.Geometry.HeightMap.SetPixel(0, 0, avg);
                top.TerrainData.Geometry.HeightMap.Apply();
                top.TerrainData.Geometry.SetRegionDirty(GUtilities.GetBottomLeftDirtyRect());
                if (updateMesh)
                {
                    top.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }

            if (topLeft != null)
            {
                topLeft.TerrainData.Geometry.HeightMap.SetPixel(res - 1, 0, avg);
                topLeft.TerrainData.Geometry.HeightMap.Apply();
                topLeft.TerrainData.Geometry.SetRegionDirty(GUtilities.GetRightBottomDirtyRect());
                if (updateMesh)
                {
                    topLeft.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }
        }

        private void MatchEdgeTopRight(bool updateMesh)
        {
            int res = TerrainData.Geometry.HeightMapResolution;
            GStylizedTerrain right = null;
            if (RightNeighbor != null &&
                RightNeighbor.TerrainData != null &&
                RightNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                right = RightNeighbor;
            }

            GStylizedTerrain top = null;
            if (TopNeighbor != null &&
                TopNeighbor.TerrainData != null &&
                TopNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                top = TopNeighbor;
            }

            GStylizedTerrain topRight = null;
            if (TopNeighbor != null &&
                TopNeighbor.RightNeighbor != null &&
                TopNeighbor.RightNeighbor.TerrainData != null &&
                TopNeighbor.RightNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                topRight = TopNeighbor.RightNeighbor;
            }

            if (topRight == null)
            {
                if (RightNeighbor != null &&
                    RightNeighbor.TopNeighbor != null &&
                    RightNeighbor.TopNeighbor.TerrainData != null &&
                    RightNeighbor.TopNeighbor.TerrainData.Geometry.HeightMapResolution == res)
                {
                    topRight = RightNeighbor.TopNeighbor;
                }
            }

            Color data = TerrainData.Geometry.HeightMap.GetPixel(res - 1, res - 1);
            int c = 1;

            if (right != null)
            {
                data += right.TerrainData.Geometry.HeightMap.GetPixel(0, res - 1);
                c += 1;
            }

            if (top != null)
            {
                data += top.TerrainData.Geometry.HeightMap.GetPixel(res - 1, 0);
                c += 1;
            }

            if (topRight != null)
            {
                data += topRight.TerrainData.Geometry.HeightMap.GetPixel(0, 0);
                c += 1;
            }

            Color avg = data / c;
            TerrainData.Geometry.HeightMap.SetPixel(res - 1, res - 1, avg);
            TerrainData.Geometry.HeightMap.Apply();
            TerrainData.Geometry.SetRegionDirty(GUtilities.GetTopRightDirtyRect());
            if (updateMesh)
            {
                TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
            }

            if (right != null)
            {
                right.TerrainData.Geometry.HeightMap.SetPixel(0, res - 1, avg);
                right.TerrainData.Geometry.HeightMap.Apply();
                right.TerrainData.Geometry.SetRegionDirty(GUtilities.GetLeftTopDirtyRect());
                if (updateMesh)
                {
                    right.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }

            if (top != null)
            {
                top.TerrainData.Geometry.HeightMap.SetPixel(res - 1, 0, avg);
                top.TerrainData.Geometry.HeightMap.Apply();
                top.TerrainData.Geometry.SetRegionDirty(GUtilities.GetRightBottomDirtyRect());
                if (updateMesh)
                {
                    top.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }

            if (topRight != null)
            {
                topRight.TerrainData.Geometry.HeightMap.SetPixel(0, 0, avg);
                topRight.TerrainData.Geometry.HeightMap.Apply();
                topRight.TerrainData.Geometry.SetRegionDirty(GUtilities.GetBottomLeftDirtyRect());
                if (updateMesh)
                {
                    topRight.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }
        }

        private void MatchEdgeRightBottom(bool updateMesh)
        {
            int res = TerrainData.Geometry.HeightMapResolution;
            GStylizedTerrain right = null;
            if (RightNeighbor != null &&
                RightNeighbor.TerrainData != null &&
                RightNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                right = RightNeighbor;
            }

            GStylizedTerrain bottom = null;
            if (BottomNeighbor != null &&
                BottomNeighbor.TerrainData != null &&
                BottomNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                bottom = BottomNeighbor;
            }

            GStylizedTerrain rightBottom = null;
            if (RightNeighbor != null &&
                RightNeighbor.BottomNeighbor != null &&
                RightNeighbor.BottomNeighbor.TerrainData != null &&
                RightNeighbor.BottomNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                rightBottom = RightNeighbor.BottomNeighbor;
            }

            if (rightBottom == null)
            {
                if (BottomNeighbor != null &&
                    BottomNeighbor.RightNeighbor != null &&
                    BottomNeighbor.RightNeighbor.TerrainData != null &&
                    BottomNeighbor.RightNeighbor.TerrainData.Geometry.HeightMapResolution == res)
                {
                    rightBottom = BottomNeighbor.RightNeighbor;
                }
            }

            Color data = TerrainData.Geometry.HeightMap.GetPixel(res - 1, 0);
            int c = 1;

            if (right != null)
            {
                data += right.TerrainData.Geometry.HeightMap.GetPixel(0, 0);
                c += 1;
            }

            if (bottom != null)
            {
                data += bottom.TerrainData.Geometry.HeightMap.GetPixel(res - 1, res - 1);
                c += 1;
            }

            if (rightBottom != null)
            {
                data += rightBottom.TerrainData.Geometry.HeightMap.GetPixel(0, res - 1);
                c += 1;
            }

            Color avg = data / c;
            TerrainData.Geometry.HeightMap.SetPixel(res - 1, 0, avg);
            TerrainData.Geometry.HeightMap.Apply();
            TerrainData.Geometry.SetRegionDirty(GUtilities.GetRightBottomDirtyRect());
            if (updateMesh)
            {
                TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
            }

            if (right != null)
            {
                right.TerrainData.Geometry.HeightMap.SetPixel(0, 0, avg);
                right.TerrainData.Geometry.HeightMap.Apply();
                right.TerrainData.Geometry.SetRegionDirty(GUtilities.GetBottomLeftDirtyRect());
                if (updateMesh)
                {
                    right.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }

            if (bottom != null)
            {
                bottom.TerrainData.Geometry.HeightMap.SetPixel(res - 1, res - 1, avg);
                bottom.TerrainData.Geometry.HeightMap.Apply();
                bottom.TerrainData.Geometry.SetRegionDirty(GUtilities.GetTopRightDirtyRect());
                if (updateMesh)
                {
                    bottom.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }

            if (rightBottom != null)
            {
                rightBottom.TerrainData.Geometry.HeightMap.SetPixel(0, res - 1, avg);
                rightBottom.TerrainData.Geometry.HeightMap.Apply();
                rightBottom.TerrainData.Geometry.SetRegionDirty(GUtilities.GetLeftTopDirtyRect());
                if (updateMesh)
                {
                    rightBottom.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }
        }

        private void MatchEdgeBottomLeft(bool updateMesh)
        {
            int res = TerrainData.Geometry.HeightMapResolution;
            GStylizedTerrain left = null;
            if (LeftNeighbor != null &&
                LeftNeighbor.TerrainData != null &&
                LeftNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                left = LeftNeighbor;
            }

            GStylizedTerrain bottom = null;
            if (BottomNeighbor != null &&
                BottomNeighbor.TerrainData != null &&
                BottomNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                bottom = BottomNeighbor;
            }

            GStylizedTerrain bottomLeft = null;
            if (BottomNeighbor != null &&
                BottomNeighbor.LeftNeighbor != null &&
                BottomNeighbor.LeftNeighbor.TerrainData != null &&
                BottomNeighbor.LeftNeighbor.TerrainData.Geometry.HeightMapResolution == res)
            {
                bottomLeft = BottomNeighbor.LeftNeighbor;
            }

            if (bottomLeft == null)
            {
                if (LeftNeighbor != null &&
                    LeftNeighbor.BottomNeighbor != null &&
                    LeftNeighbor.BottomNeighbor.TerrainData != null &&
                    LeftNeighbor.BottomNeighbor.TerrainData.Geometry.HeightMapResolution == res)
                {
                    bottomLeft = LeftNeighbor.BottomNeighbor;
                }
            }

            Color data = TerrainData.Geometry.HeightMap.GetPixel(0, 0);
            int c = 1;

            if (left != null)
            {
                data += left.TerrainData.Geometry.HeightMap.GetPixel(res - 1, 0);
                c += 1;
            }

            if (bottom != null)
            {
                data += bottom.TerrainData.Geometry.HeightMap.GetPixel(0, res - 1);
                c += 1;
            }

            if (bottomLeft != null)
            {
                data += bottomLeft.TerrainData.Geometry.HeightMap.GetPixel(res - 1, res - 1);
                c += 1;
            }

            Color avg = data / c;
            TerrainData.Geometry.HeightMap.SetPixel(0, 0, avg);
            TerrainData.Geometry.HeightMap.Apply();
            TerrainData.Geometry.SetRegionDirty(GUtilities.GetBottomLeftDirtyRect());
            if (updateMesh)
            {
                TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
            }

            if (left != null)
            {
                left.TerrainData.Geometry.HeightMap.SetPixel(res - 1, 0, avg);
                left.TerrainData.Geometry.HeightMap.Apply();
                left.TerrainData.Geometry.SetRegionDirty(GUtilities.GetRightBottomDirtyRect());
                if (updateMesh)
                {
                    left.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }

            if (bottom != null)
            {
                bottom.TerrainData.Geometry.HeightMap.SetPixel(0, res - 1, avg);
                bottom.TerrainData.Geometry.HeightMap.Apply();
                bottom.TerrainData.Geometry.SetRegionDirty(GUtilities.GetLeftTopDirtyRect());
                if (updateMesh)
                {
                    bottom.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }

            if (bottomLeft != null)
            {
                bottomLeft.TerrainData.Geometry.HeightMap.SetPixel(res - 1, res - 1, avg);
                bottomLeft.TerrainData.Geometry.HeightMap.Apply();
                bottomLeft.TerrainData.Geometry.SetRegionDirty(GUtilities.GetTopRightDirtyRect());
                if (updateMesh)
                {
                    bottomLeft.TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
                }
            }
        }

        /// <summary>
        /// Snap trees position to the terrain mesh or world objects depending on tree settings. Call this function after regenerate surface meshes.
        /// </summary>
        public void UpdateTreesPosition()
        {
            if (TerrainData == null)
                return;

            int oldLayer = gameObject.layer;
            int raycastLayer = LayerMask.NameToLayer(RAYCAST_LAYER);
            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].gameObject.layer = raycastLayer;
            }

            LayerMask mask = LayerMask.GetMask(RAYCAST_LAYER);
            if (TerrainData.Foliage.TreeSnapMode == GSnapMode.World)
            {
                mask |= TerrainData.Foliage.TreeSnapLayerMask;
            }

            NativeArray<Vector2> treePosisitonsNA = TerrainData.Foliage.GetTreesPositionArray(Allocator.TempJob);
            NativeArray<RaycastCommand> commandsNA = new NativeArray<RaycastCommand>(treePosisitonsNA.Length,
                Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<RaycastHit> hitsNA = new NativeArray<RaycastHit>(treePosisitonsNA.Length, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);

            Vector3 terrainSize = TerrainData.Geometry.Size;
            int maxHit = 1;

            GBuildRaycastCommandJob job = new GBuildRaycastCommandJob()
            {
                positions = treePosisitonsNA,
                commands = commandsNA,
                terrainPosition = transform.position,
                terrainSize = terrainSize,
                maxHit = maxHit,
                mask = mask.value
            };
            {
                JobHandle jHandle = job.Schedule(treePosisitonsNA.Length, 100);
                jHandle.Complete();
            }

            treePosisitonsNA.Dispose();

            {
                JobHandle jHandle = RaycastCommand.ScheduleBatch(commandsNA, hitsNA, 100);
                jHandle.Complete();
            }

            List<GTreeInstance> trees = TerrainData.Foliage.TreeInstances;
            int treeCount = trees.Count;

            for (int i = 0; i < treeCount; ++i)
            {
                RaycastHit h = hitsNA[i];
                if (h.collider == null)
                    continue;
                GTreeInstance t = trees[i];
                t.position.y = Mathf.InverseLerp(0, terrainSize.y, transform.InverseTransformPoint(h.point).y);
                trees[i] = t;
            }

            commandsNA.Dispose();
            hitsNA.Dispose();

            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].gameObject.layer = oldLayer;
            }

            TerrainData.Foliage.TreeAllChanged();
        }

        /// <summary>
        /// Snap grass position to the terrain mesh or world objects depending on grass settings. Call this function after regenerate surface meshes.
        /// </summary>
        public void UpdateGrassPatches()
        {
            if (TerrainData == null)
                return;

            int oldLayer = gameObject.layer;
            int raycastLayer = LayerMask.NameToLayer(RAYCAST_LAYER);
            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].gameObject.layer = raycastLayer;
            }

            LayerMask mask = LayerMask.GetMask(RAYCAST_LAYER);
            if (TerrainData.Foliage.GrassSnapMode == GSnapMode.World)
            {
                mask |= TerrainData.Foliage.GrassSnapLayerMask;
            }

            Rect[] dirtyRects = TerrainData.Foliage.GetGrassDirtyRegions();
            List<GGrassPrototype> prototypes = null;
            if (TerrainData.Foliage.Grasses != null)
            {
                prototypes = TerrainData.Foliage.Grasses.Prototypes;
            }

            GGrassPatch[] patches = TerrainData.Foliage.GrassPatches;
            for (int pIndex = 0; pIndex < patches.Length; ++pIndex)
            {
                GGrassPatch p = patches[pIndex];
                if (!GUtilities.TestOverlap(p.GetUvRange(), dirtyRects))
                {
                    continue;
                }

                NativeArray<Vector2> positionsNA = p.GetGrassPositionArray();
                NativeArray<RaycastCommand> commandsNA = new NativeArray<RaycastCommand>(p.InstanceCount,
                    Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                NativeArray<RaycastHit> hitsNA = new NativeArray<RaycastHit>(p.InstanceCount, Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory);

                Vector3 terrainSize = TerrainData.Geometry.Size;
                int maxHit = 1;

                GBuildRaycastCommandJob job = new GBuildRaycastCommandJob()
                {
                    positions = positionsNA,
                    commands = commandsNA,
                    terrainPosition = transform.position,
                    terrainSize = terrainSize,
                    maxHit = maxHit,
                    mask = mask.value
                };
                {
                    JobHandle jHandle = job.Schedule(p.InstanceCount, 100);
                    jHandle.Complete();
                }

                positionsNA.Dispose();

                {
                    JobHandle jHandle = RaycastCommand.ScheduleBatch(commandsNA, hitsNA, 100);
                    jHandle.Complete();
                }

                List<GGrassInstance> grasses = p.Instances;
                int count = grasses.Count;

                for (int i = 0; i < count; ++i)
                {
                    RaycastHit h = hitsNA[i];
                    if (h.collider == null)
                        continue;
                    GGrassInstance g = grasses[i];
                    g.position.y = Mathf.InverseLerp(0, terrainSize.y, transform.InverseTransformPoint(h.point).y);

                    if (prototypes != null && g.prototypeIndex >= 0 && g.prototypeIndex < prototypes.Count)
                    {
                        GGrassPrototype proto = prototypes[g.PrototypeIndex];
                        if (proto.AlignToSurface)
                        {
                            Quaternion currentRotationY = Quaternion.Euler(0, g.rotation.eulerAngles.y, 0);
                            Quaternion rotationXZ = Quaternion.FromToRotation(Vector3.up, h.normal);
                            g.rotation = rotationXZ * currentRotationY;
                        }
                        else
                        {
                            Quaternion currentRotationY = Quaternion.Euler(0, g.rotation.eulerAngles.y, 0);
                            g.rotation = currentRotationY;
                        }
                    }

                    grasses[i] = g;
                }

                commandsNA.Dispose();
                hitsNA.Dispose();

                p.RecalculateBounds();
                p.Changed();
            }

            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].gameObject.layer = oldLayer;
            }
        }

        [ExcludeFromDoc]
        public RenderTexture GetHeightMap(int resolution)
        {
            if (heightMap == null)
            {
                heightMap = new RenderTexture(resolution, resolution, 0, GGeometry.HeightMapRTFormat,
                    RenderTextureReadWrite.Linear);
                RenderHeightMap(heightMap);
            }

            if (heightMap.width != resolution ||
                heightMap.height != resolution ||
                heightMap.format != GGeometry.HeightMapRTFormat)
            {
                heightMap.Release();
                GUtilities.DestroyObject(heightMap);
                heightMap = new RenderTexture(resolution, resolution, 0, GGeometry.HeightMapRTFormat,
                    RenderTextureReadWrite.Linear);
                RenderHeightMap(heightMap);
            }

            if (!heightMap.IsCreated())
            {
                RenderHeightMap(heightMap);
            }

            return heightMap;
        }

        private void RenderHeightMap(RenderTexture rt)
        {
            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                RenderHeightMap(rt, chunks[i]);
            }
        }

        private void RenderHeightMap(RenderTexture rt, GTerrainChunk chunk)
        {
            Mesh m = chunk.MeshFilterComponent.sharedMesh;
            if (m == null)
                return;
            Vector3 terrainSize = TerrainData.Geometry.Size;
            Material mat = GInternalMaterials.GeometricalHeightMapMaterial;
            Vector3[] vertices = m.vertices;

            mat.SetPass(0);
            RenderTexture.active = rt;
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);

            for (int i = 0; i < vertices.Length; ++i)
            {
                Vector3 v = vertices[i];
                Vector2 uv = new Vector2(v.x / terrainSize.x, v.z / terrainSize.z);
                GL.Vertex3(uv.x, uv.y, GetInterpolatedHeightMapSample(uv).x);
            }

            GL.End();
            GL.PopMatrix();
            RenderTexture.active = null;
        }

        [ExcludeFromDoc]
        public RenderTexture GetSharpNormalMap(int resolution)
        {
            if (normalMap == null)
            {
                normalMap = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear);
                RenderSharpNormalMap(normalMap);
            }

            if (normalMap.width != resolution ||
                normalMap.height != resolution)
            {
                normalMap.Release();
                GUtilities.DestroyObject(normalMap);
                normalMap = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear);
                RenderSharpNormalMap(normalMap);
            }

            if (!normalMap.IsCreated())
            {
                RenderSharpNormalMap(normalMap);
            }

            return normalMap;
        }

        private void RenderSharpNormalMap(RenderTexture rt)
        {
            GNormalMapGeneratorParams param = new GNormalMapGeneratorParams();
            param.Terrain = this;
            param.Space = GNormalMapSpace.Local;
            param.Mode = GNormalMapMode.Sharp;
            GNormalMapGenerator gen = new GNormalMapGenerator();
            gen.RenderSharpNormalMap(param, rt);
        }

        [ExcludeFromDoc]
        public RenderTexture GetInterpolatedNormalMap(int resolution)
        {
            if (normalMapInterpolated == null)
            {
                normalMapInterpolated = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear);
                RenderInterpolatedNormalMap(normalMapInterpolated);
            }

            if (normalMapInterpolated.width != resolution ||
                normalMapInterpolated.height != resolution)
            {
                normalMapInterpolated.Release();
                GUtilities.DestroyObject(normalMapInterpolated);
                normalMapInterpolated = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear);
                RenderInterpolatedNormalMap(normalMapInterpolated);
            }

            if (!normalMapInterpolated.IsCreated())
            {
                RenderInterpolatedNormalMap(normalMapInterpolated);
            }

            return normalMapInterpolated;
        }

        private void RenderInterpolatedNormalMap(RenderTexture rt)
        {
            GNormalMapGeneratorParams param = new GNormalMapGeneratorParams();
            param.Terrain = this;
            param.Space = GNormalMapSpace.Local;
            param.Mode = GNormalMapMode.Interpolated;
            GNormalMapGenerator gen = new GNormalMapGenerator();
            gen.RenderInterpolatedNormalMap(param, rt);
        }

        [ExcludeFromDoc]
        public RenderTexture GetPerPixelNormalMap(int resolution)
        {
            if (normalMapPerPixel == null)
            {
                normalMapPerPixel = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear);
                RenderPerPixelNormalMap(normalMapPerPixel);
            }

            if (normalMapPerPixel.width != resolution ||
                normalMapPerPixel.height != resolution)
            {
                normalMapPerPixel.Release();
                GUtilities.DestroyObject(normalMapPerPixel);
                normalMapPerPixel = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear);
                RenderPerPixelNormalMap(normalMapPerPixel);
            }

            if (!normalMapPerPixel.IsCreated())
            {
                RenderPerPixelNormalMap(normalMapPerPixel);
            }

            return normalMapPerPixel;
        }

        private void RenderPerPixelNormalMap(RenderTexture rt)
        {
            GNormalMapGeneratorParams param = new GNormalMapGeneratorParams();
            param.Terrain = this;
            param.Space = GNormalMapSpace.Local;
            param.Mode = GNormalMapMode.PerPixel;
            GNormalMapGenerator gen = new GNormalMapGenerator();
            gen.RenderPerPixelNormalMap(param, rt);
        }

        /// <summary>
        /// Refresh terrain chunks components & meshes assignments.
        /// </summary>
        public void Refresh()
        {
            InitChunks();
            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                chunks[i].Refresh();
            }
        }

#if UNITY_EDITOR
        private void DrawChunkUpdateDebug(Camera cam)
        {
            Material mat = GInternalMaterials.MaskVisualizerMaterial;
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                GTerrainChunk c = chunks[i];
                Mesh m = c.MeshFilterComponent.sharedMesh;
                if (m == null)
                    continue;

                System.DateTime now = System.DateTime.Now;
                System.TimeSpan timeSpan = now - c.LastUpdatedTime;
                float duration = 0.3f;
                float span = (float)timeSpan.TotalSeconds;
                if (span > duration)
                    continue;

                float alpha = 1 - Mathf.InverseLerp(0, duration, span);
                Color color = new Color(1, 1, 1, alpha * 0.5f);
                block.SetColor("_Color", color);
                Graphics.DrawMesh(
                    m,
                    c.transform.localToWorldMatrix,
                    mat,
                    LayerMask.NameToLayer("Default"),
                    cam,
                    0,
                    block);
            }
        }

        private void DrawChunkTopographic(Camera cam)
        {
            if (TerrainData == null)
                return;

            Material mat = GEditorSettings.Instance.topographic.topographicMaterial;
            if (mat == null)
                return;
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            GTerrainChunk[] chunks = GetChunks();
            for (int i = 0; i < chunks.Length; ++i)
            {
                GTerrainChunk c = chunks[i];
                Mesh m = c.MeshFilterComponent.sharedMesh;
                if (m == null)
                    continue;
                Graphics.DrawMesh(
                    m,
                    c.transform.localToWorldMatrix,
                    mat,
                    LayerMask.NameToLayer("Default"),
                    cam,
                    0,
                    block);
            }
        }
#endif

        [ExcludeFromDoc]
        public RenderTexture GetGrassVectorFieldRenderTexture()
        {
            if (TerrainData == null)
                return null;

            int resolution = TerrainData.Foliage.VectorFieldMapResolution;
            if (grassVectorFieldMap == null)
            {
                grassVectorFieldMap = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
                GCommon.FillTexture(grassVectorFieldMap, Color.gray);
            }

            if (grassVectorFieldMap.width != resolution ||
                grassVectorFieldMap.height != resolution)
            {
                grassVectorFieldMap.Release();
                GUtilities.DestroyObject(grassVectorFieldMap);
                grassVectorFieldMap = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
                GCommon.FillTexture(grassVectorFieldMap, Color.gray);
            }

            if (!grassVectorFieldMap.IsCreated())
            {
                GCommon.FillTexture(grassVectorFieldMap, Color.gray);
            }

            if (grassVectorFieldMapTmp == null)
            {
                grassVectorFieldMapTmp = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
            }

            if (grassVectorFieldMapTmp.width != resolution ||
                grassVectorFieldMapTmp.height != resolution)
            {
                grassVectorFieldMapTmp.Release();
                GUtilities.DestroyObject(grassVectorFieldMapTmp);
                grassVectorFieldMapTmp = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
            }

            //TerrainData.Shading.MaterialToRender.SetTexture("_MainTex", grassVectorFieldMap);

            return grassVectorFieldMap;
        }

        private void LateUpdate()
        {
            if (TerrainData != null && TerrainData.Foliage.EnableInteractiveGrass)
            {
                FadeGrassVectorField();
            }

            if (TerrainData != null && !TerrainData.Foliage.EnableInteractiveGrass)
            {
                if (grassVectorFieldMap != null)
                {
                    grassVectorFieldMap.Release();
                    GUtilities.DestroyObject(grassVectorFieldMap);
                }

                if (grassVectorFieldMapTmp != null)
                {
                    grassVectorFieldMapTmp.Release();
                    GUtilities.DestroyObject(grassVectorFieldMapTmp);
                }
            }
        }

        private void FadeGrassVectorField()
        {
            RenderTexture rt = GetGrassVectorFieldRenderTexture();
            RenderTexture bg = grassVectorFieldMapTmp;
            GCommon.CopyToRT(rt, bg);

            Material mat = GInternalMaterials.InteractiveGrassVectorFieldMaterial;
            mat.SetTexture("_Background", bg);
            mat.SetFloat("_Opacity", TerrainData.Foliage.RestoreSensitive);
            int pass = 1;
            GCommon.DrawQuad(rt, GCommon.FullRectUvPoints, mat, pass);
        }

        [ExcludeFromDoc]
        public NativeArray<Rect> GetChunkRectsNA()
        {
            if (TerrainData == null)
                return new NativeArray<Rect>(0, Allocator.TempJob);

            int gridSize = TerrainData.Geometry.ChunkGridSize;
            Vector3 terrainSize = TerrainData.Geometry.Size;
            Vector2 rectSize = new Vector2(terrainSize.x / gridSize, terrainSize.z / gridSize);

            NativeArray<Rect> rects = new NativeArray<Rect>(gridSize * gridSize, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            for (int z = 0; z < gridSize; ++z)
            {
                for (int x = 0; x < gridSize; ++x)
                {
                    Rect r = new Rect();
                    r.x = transform.position.x + x * rectSize.x;
                    r.y = transform.position.z + z * rectSize.y;
                    r.size = rectSize;
                    rects[GUtilities.To1DIndex(x, z, gridSize)] = r;
                }
            }

            return rects;
        }

        /// <summary>
        /// Get the rects (XZ-plane aligned) of all chunks in world space. Used for sweeptesting.
        /// </summary>
        /// <returns>An array of chunk rects in world space.</returns>
        public Rect[] GetChunkRects()
        {
            if (TerrainData == null)
                return new Rect[0];

            int gridSize = TerrainData.Geometry.ChunkGridSize;
            Vector3 terrainSize = TerrainData.Geometry.Size;
            Vector2 rectSize = new Vector2(terrainSize.x / gridSize, terrainSize.z / gridSize);

            Rect[] rects = new Rect[gridSize * gridSize];
            for (int z = 0; z < gridSize; ++z)
            {
                for (int x = 0; x < gridSize; ++x)
                {
                    Rect r = new Rect();
                    r.x = transform.position.x + x * rectSize.x;
                    r.y = transform.position.z + z * rectSize.y;
                    r.size = rectSize;
                    rects[GUtilities.To1DIndex(x, z, gridSize)] = r;
                }
            }

            return rects;
        }

        [ExcludeFromDoc]
        public Matrix4x4 GetUnitRectToWorldMatrix()
        {
            Vector3 worldSize = Vector3.zero;
            if (TerrainData != null)
            {
                worldSize = TerrainData.Geometry.Size;
            }

            Quaternion rotation = Quaternion.identity;
            Vector3 position = transform.position;
            Matrix4x4 rectToWorld = Matrix4x4.TRS(position, rotation, worldSize);
            return rectToWorld;
        }

        [ExcludeFromDoc]
        public void ResetNeighboring()
        {
            leftNeighbor = null;
            leftTerrainData = null;

            topNeighbor = null;
            topTerrainData = null;

            rightNeighbor = null;
            rightTerrainData = null;

            bottomNeighbor = null;
            bottomTerrainData = null;
        }

        [ExcludeFromDoc]
        public void OnDrawGizmosSelected()
        {
            if (Camera.current.cameraType != CameraType.SceneView)
                return;

            foreach (GTreeRenderer3 renderer in perCameraTreeRendererMap.Values)
            {
                renderer.DrawDebug(Camera.current);
            }
            foreach (GGrassRenderer3 renderer in perCameraGrassRendererMap.Values)
            {
                renderer.DrawDebug(Camera.current);
            }
        }
    }
}

#endif