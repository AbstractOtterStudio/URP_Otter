#if GRIFFIN
using System.Collections.Generic;
using UnityEngine;
using Type = System.Type;

namespace Pinwheel.Griffin
{
    /// <summary>
    /// An object containing terrain size, mesh density, height map and other settings related to mesh generation.
    /// This should not be used alone without a parent terrain data. In most case you don't instantiate it with ScriptableObject.Create(). The correct way is to accessing it with GTerrainData.Geometry.
    /// </summary>
    public class GGeometry : ScriptableObject
    {
        /// <summary>
        /// Mesh storage behavior
        /// </summary>
        [System.Serializable]      
        public enum GStorageMode
        {
            /// <summary>
            /// Generated surface mesh will be store in a GGeneratedGeometry asset next to your terrain data in Project window.
            /// </summary>
            SaveToAsset, 
            /// <summary>
            /// Generated surface mesh won't be saved. They are generated when the terrain was activated when flushed when the terrain was deactivated.
            /// </summary>
            GenerateOnEnable
        }

        /// <summary>
        /// Name of the height map texture.
        /// </summary>
        public const string HEIGHT_MAP_NAME = "Height Map";

        [SerializeField]
        private GTerrainData terrainData;
        /// <summary>
        /// The parent terrain data object.
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
        internal float width;
        /// <summary>
        /// Size of the terrain along X axis in meters.
        /// </summary>
        public float Width
        {
            get
            {
                return width;
            }
            set
            {
                width = Mathf.Max(1, value);
            }
        }

        [SerializeField]
        internal float height;
        /// <summary>
        /// Size of the terrain in Y axis in meters.
        /// </summary>
        public float Height
        {
            get
            {
                return height;
            }
            set
            {
                height = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        internal float length;
        /// <summary>
        /// Size of the terrain in Z axis in meters.
        /// </summary>
        public float Length
        {
            get
            {
                return length;
            }
            set
            {
                length = Mathf.Max(1, value);
            }
        }

        /// <summary>
        /// Size of the terrain is XYZ-axes in meters.
        /// </summary>
        public Vector3 Size
        {
            get
            {
                return new Vector3(Width, Height, Length);
            }
        }

        [SerializeField]
        private int heightMapResolution;
        /// <summary>
        /// Size of the height map texture in pixels. When this value changed, the height map will be resized and resampled.
        /// </summary>
        public int HeightMapResolution
        {
            get
            {
                return heightMapResolution;
            }
            set
            {
                int oldValue = heightMapResolution;
                heightMapResolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(value), GCommon.TEXTURE_SIZE_MIN, GCommon.TEXTURE_SIZE_MAX);
                if (oldValue != heightMapResolution)
                {
                    ResampleHeightMap();
                }
            }
        }

        [SerializeField]
        private Texture2D heightMap;
        /// <summary>
        /// Direct reference to the height map texture. It embeds 3 values in 4 color channels as follow:
        /// - RG: Encoded height value.
        /// - B: Additional subdivision. This value will add more polygon upon default subdivision based on height map detailness.
        /// - A: Holes. polygons where this value is >=0.5 will be removed and become holes.
        /// See Polaris.EncodeHeightMapSample() and Polaris.DecodeHeightMapSample().
        /// </summary>
        public Texture2D HeightMap
        {
            get
            {
                if (heightMap == null)
                {
                    heightMap = GCommon.CreateTexture(HeightMapResolution, Color.clear, HeightMapFormat);
                    heightMap.filterMode = FilterMode.Bilinear;
                    heightMap.wrapMode = TextureWrapMode.Clamp;
                    heightMap.name = HEIGHT_MAP_NAME;
                    heightmapVersion = GVersionInfo.Number;
                }
                GCommon.TryAddObjectToAsset(heightMap, TerrainData);
                if (heightMap.format != HeightMapFormat)
                {
                    ReFormatHeightMap();
                }
                return heightMap;
            }
        }

        [SerializeField]
        private float heightmapVersion;
        private const float HEIGHT_MAP_VERSION_ENCODE_RG = 246;

        [ExcludeFromDoc]
        public static TextureFormat HeightMapFormat
        {
            get
            {
                return TextureFormat.RGBA32;
            }
        }

        [ExcludeFromDoc]
        public static RenderTextureFormat HeightMapRTFormat
        {
            get
            {
                return RenderTextureFormat.ARGB32;
            }
        }

        internal Texture2D subDivisionMap;
        [ExcludeFromDoc]
        public Texture2D Internal_SubDivisionMap
        {
            get
            {
                if (subDivisionMap == null)
                {
                    Internal_CreateNewSubDivisionMap();
                }
                return subDivisionMap;
            }
        }

        [SerializeField]
        private int meshBaseResolution;
        /// <summary>
        /// Base polygon density of surface mesh. Each increament value indicate that a triangle at that point will be subdivided for another time.
        /// This is the resolution of region where it has least polygon count.
        /// </summary>
        public int MeshBaseResolution
        {
            get
            {
                return meshBaseResolution;
            }
            set
            {
                meshBaseResolution = Mathf.Min(meshResolution, Mathf.Clamp(value, 0, GCommon.MAX_MESH_BASE_RESOLUTION));
            }
        }

        [SerializeField]
        private int meshResolution;
        /// <summary>
        /// Max polygon density of surface mesh. Each increament value indicate that a triangle at that point will be subdivided for another time.
        /// This is the resolution of region where it has the highest polygon count.
        /// </summary>
        public int MeshResolution
        {
            get
            {
                return meshResolution;
            }
            set
            {
                meshResolution = Mathf.Clamp(value, 0, GCommon.MAX_MESH_RESOLUTION);
            }
        }

        [SerializeField]
        private int chunkGridSize;
        /// <summary>
        /// Size of the chunk grid. The terrain will divide it surface into several chunks (meshes) to get around 65K vertex limit.
        /// Total number of chunks is ChunkGridSize^2
        /// </summary>
        public int ChunkGridSize
        {
            get
            {
                return chunkGridSize;
            }
            set
            {
                chunkGridSize = Mathf.Max(1, value);
            }
        }

        [SerializeField]
        private int lodCount;
        /// <summary>
        /// Number of LOD for each chunk.
        /// </summary>
        public int LODCount
        {
            get
            {
                return lodCount;
            }
            set
            {
                lodCount = Mathf.Clamp(value, 1, GCommon.MAX_LOD_COUNT);
            }
        }

        [SerializeField]
        private int displacementSeed;
        /// <summary>
        /// Random seed for XZ displacement.
        /// </summary>
        public int DisplacementSeed
        {
            get
            {
                return displacementSeed;
            }
            set
            {
                displacementSeed = value;
            }
        }

        [SerializeField]
        private float displacementStrength;
        /// <summary>
        /// Intensity of vertex XZ displacement.
        /// </summary>
        public float DisplacementStrength
        {
            get
            {
                return displacementStrength;
            }
            set
            {
                displacementStrength = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        private GAlbedoToVertexColorMode albedoToVertexColorMode;
        /// <summary>
        /// Determine if it should convert albedo map color to vertex color. Only needed when using Vertex Color shading mode, otherwise set to None.
        /// </summary>
        public GAlbedoToVertexColorMode AlbedoToVertexColorMode
        {
            get
            {
                return albedoToVertexColorMode;
            }
            set
            {
                albedoToVertexColorMode = value;
            }
        }

        [SerializeField]
        private GStorageMode storageMode;
        /// <summary>
        /// Should it store generated mesh in asset? 
        /// </summary>
        public GStorageMode StorageMode
        {
            get
            {
                return storageMode;
            }
            set
            {
                storageMode = value;
                if (storageMode == GStorageMode.GenerateOnEnable)
                {
                    TerrainData.GeometryData = null;
                }
            }
        }

        [SerializeField]
        private bool allowTimeSlicedGeneration;
        /// <summary>
        /// If on, the terrain will split it mesh generation into several frames.
        /// </summary>
        public bool AllowTimeSlicedGeneration
        {
            get
            {
                return allowTimeSlicedGeneration;
            }
            set
            {
                allowTimeSlicedGeneration = value;
            }
        }

        [SerializeField]
        private bool smoothNormal;
        /// <summary>
        /// Generate a smooth normal look for terrain surface. This only modify the normal vector direction for each vertex, vertex count stays the same.
        /// </summary>
        public bool SmoothNormal
        {
            get
            {
                return smoothNormal;
            }
            set
            {
                smoothNormal = value;
            }
        }

        [SerializeField]
        private bool useSmoothNormalMask;
        /// <summary>
        /// If on, it will use the terrain mask G channel to blend between sharp and smooth normal.
        /// Access the terrain mask at GTerrainData.Mask.MaskMap
        /// </summary>
        public bool UseSmoothNormalMask
        {
            get
            {
                return useSmoothNormalMask;
            }
            set
            {
                useSmoothNormalMask = value;
            }
        }

        [SerializeField]
        private bool mergeUv;
        /// <summary>
        /// If on, the terrain will merge the UV coordinate of 3 triangle vertices to the median point.
        /// This will enable sharp look from texture color, similar to Vertex Color material mode without using vertex colors.
        /// </summary>
        public bool MergeUv
        {
            get
            {
                return mergeUv;
            }
            set
            {
                mergeUv = value;
            }
        }

        private List<Rect> dirtyRegion;
        private List<Rect> DirtyRegion
        {
            get
            {
                if (dirtyRegion == null)
                {
                    dirtyRegion = new List<Rect>();
                }
                return dirtyRegion;
            }
            set
            {
                dirtyRegion = value;
            }
        }

        [ExcludeFromDoc]
        public void Reset()
        {
            name = "Geometry";
            Width = GRuntimeSettings.Instance.geometryDefault.width;
            Height = GRuntimeSettings.Instance.geometryDefault.height;
            Length = GRuntimeSettings.Instance.geometryDefault.length;
            HeightMapResolution = GRuntimeSettings.Instance.geometryDefault.heightMapResolution;
            MeshResolution = GRuntimeSettings.Instance.geometryDefault.meshResolution;
            MeshBaseResolution = GRuntimeSettings.Instance.geometryDefault.meshBaseResolution;
            ChunkGridSize = GRuntimeSettings.Instance.geometryDefault.chunkGridSize;
            LODCount = GRuntimeSettings.Instance.geometryDefault.lodCount;
            DisplacementSeed = GRuntimeSettings.Instance.geometryDefault.displacementSeed;
            DisplacementStrength = GRuntimeSettings.Instance.geometryDefault.displacementStrength;
            AlbedoToVertexColorMode = GRuntimeSettings.Instance.geometryDefault.albedoToVertexColorMode;
            StorageMode = GRuntimeSettings.Instance.geometryDefault.storageMode;
            AllowTimeSlicedGeneration = GRuntimeSettings.Instance.geometryDefault.allowTimeSlicedGeneration;
            SmoothNormal = GRuntimeSettings.Instance.geometryDefault.smoothNormal;
            UseSmoothNormalMask = GRuntimeSettings.Instance.geometryDefault.useSmoothNormalMask;
            MergeUv = GRuntimeSettings.Instance.geometryDefault.mergeUv;
        }

        [ExcludeFromDoc]
        public void ResetFull()
        {
            Reset();
            GCommon.FillTexture(HeightMap, Color.clear);
            SetRegionDirty(GCommon.UnitRect);
            TerrainData.SetDirty(GTerrainData.DirtyFlags.GeometryTimeSliced);
        }

        private void ResampleHeightMap()
        {
            if (heightMap == null)
                return;
            Texture2D tmp = GCommon.CreateTexture(HeightMapResolution, Color.clear, HeightMapFormat);
            RenderTexture rt = new RenderTexture(HeightMapResolution, HeightMapResolution, 32, HeightMapRTFormat);

            GCommon.CopyTexture(heightMap, tmp);
            tmp.name = heightMap.name;
            tmp.filterMode = heightMap.filterMode;
            tmp.wrapMode = heightMap.wrapMode;
            Object.DestroyImmediate(heightMap, true);
            heightMap = tmp;
            GCommon.TryAddObjectToAsset(heightMap, TerrainData);

            Internal_CreateNewSubDivisionMap();
            SetRegionDirty(GCommon.UnitRect);
        }

        private void ReFormatHeightMap()
        {
            if (heightMap == null)
                return;
            if (heightmapVersion < HEIGHT_MAP_VERSION_ENCODE_RG)
            {
                Texture2D tmp = GCommon.CreateTexture(HeightMapResolution, Color.clear, HeightMapFormat);
                RenderTexture rt = new RenderTexture(HeightMapResolution, HeightMapResolution, 32, HeightMapRTFormat);
                Material mat = GInternalMaterials.HeightmapConverterEncodeRGMaterial;
                mat.SetTexture("_MainTex", heightMap);
                GCommon.DrawQuad(rt, GCommon.FullRectUvPoints, mat, 0);
                GCommon.CopyFromRT(tmp, rt);
                rt.Release();
                Object.DestroyImmediate(rt);

                tmp.name = heightMap.name;
                tmp.filterMode = heightMap.filterMode;
                tmp.wrapMode = heightMap.wrapMode;
                Object.DestroyImmediate(heightMap, true);
                heightMap = tmp;
                GCommon.TryAddObjectToAsset(heightMap, TerrainData);

                heightmapVersion = HEIGHT_MAP_VERSION_ENCODE_RG;
                Debug.Log("Polaris auto upgrade: Converted Height Map from RGBAFloat to RGBA32.");
            }
        }

        internal void Internal_CreateNewSubDivisionMap()
        {
            if (subDivisionMap != null)
            {
                if (subDivisionMap.width != GCommon.SUB_DIV_MAP_RESOLUTION ||
                    subDivisionMap.height != GCommon.SUB_DIV_MAP_RESOLUTION)
                    Object.DestroyImmediate(subDivisionMap);
            }

            if (subDivisionMap == null)
            {
                subDivisionMap = new Texture2D(GCommon.SUB_DIV_MAP_RESOLUTION, GCommon.SUB_DIV_MAP_RESOLUTION, TextureFormat.RGBA32, false);
            }

            int resolution = GCommon.SUB_DIV_MAP_RESOLUTION;
            RenderTexture rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);
            Material mat = GInternalMaterials.SubDivisionMapMaterial;
            Graphics.Blit(HeightMap, rt, mat);
            GCommon.CopyFromRT(subDivisionMap, rt);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        internal void Internal_CreateNewSubDivisionMap(Texture altHeightMap)
        {
            if (subDivisionMap != null)
            {
                if (subDivisionMap.width != GCommon.SUB_DIV_MAP_RESOLUTION ||
                    subDivisionMap.height != GCommon.SUB_DIV_MAP_RESOLUTION)
                    Object.DestroyImmediate(subDivisionMap);
            }

            if (subDivisionMap == null)
            {
                subDivisionMap = new Texture2D(GCommon.SUB_DIV_MAP_RESOLUTION, GCommon.SUB_DIV_MAP_RESOLUTION, TextureFormat.ARGB32, false);
            }

            int resolution = GCommon.SUB_DIV_MAP_RESOLUTION;
            RenderTexture rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);
            Material mat = GInternalMaterials.SubDivisionMapMaterial;
            Graphics.Blit(altHeightMap, rt, mat);
            GCommon.CopyFromRT(subDivisionMap, rt);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        [ExcludeFromDoc]
        public void CleanUp()
        {
            int count = 0;
            List<Vector3Int> keys = TerrainData.GeometryData.GetKeys();
            for (int i = 0; i < keys.Count; ++i)
            {
                bool delete = false;
                try
                {
                    int indexX = keys[i].x;
                    int indexY = keys[i].y;
                    int lod = keys[i].z;
                    if (indexX >= ChunkGridSize || indexY >= ChunkGridSize)
                    {
                        delete = true;
                    }
                    else if (lod >= LODCount)
                    {
                        delete = true;
                    }
                    else
                    {
                        delete = false;
                    }
                }
                catch
                {
                    delete = false;
                }

                if (delete)
                {
                    count += 1;
                    TerrainData.GeometryData.DeleteMesh(keys[i]);
                }
            }

            if (count > 0)
            {
                Debug.Log(string.Format("Deleted {0} object{1} from generated data!", count, count > 1 ? "s" : ""));
            }
        }

        /// <summary>
        /// Set a region of terrain surface as dirty for mesh regeneration.
        /// </summary>
        /// <param name="uvRect">A rect in [0-1] space.</param>
        public void SetRegionDirty(Rect uvRect)
        {
            DirtyRegion.Add(uvRect);
        }

        [ExcludeFromDoc]
        public void SetRegionDirty(IEnumerable<Rect> uvRects)
        {
            DirtyRegion.AddRange(uvRects);
        }

        /// <summary>
        /// Retrieve all dirty regions of surface where mesh should be regenerated.
        /// </summary>
        /// <returns></returns>
        public Rect[] GetDirtyRegions()
        {
            return DirtyRegion.ToArray();
        }

        /// <summary>
        /// Clear all dirty regions. Call this when you've regenerated the meshes.
        /// </summary>
        public void ClearDirtyRegions()
        {
            DirtyRegion.Clear();
        }

        [ExcludeFromDoc]
        public bool DirtyRegionOverlapTest(Rect r)
        {
            int count = DirtyRegion.Count;
            for (int i = 0; i < count; ++i)
            {
                if (DirtyRegion[i].Overlaps(r))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copy numeric value to other object.
        /// This doesn't copy textures.
        /// </summary>
        /// <param name="des">The other object.</param>
        public void CopyTo(GGeometry des)
        {
            des.Width = Width;
            des.Height = Height;
            des.Length = Length;
            des.HeightMapResolution = HeightMapResolution;
            des.MeshResolution = MeshResolution;
            des.MeshBaseResolution = MeshBaseResolution;
            des.ChunkGridSize = ChunkGridSize;
            des.LODCount = LODCount;
            des.DisplacementSeed = DisplacementSeed;
            des.DisplacementStrength = DisplacementStrength;
            des.AlbedoToVertexColorMode = AlbedoToVertexColorMode;
            des.StorageMode = StorageMode;
            des.AllowTimeSlicedGeneration = AllowTimeSlicedGeneration;
        }

        [ExcludeFromDoc]
        public Vector4 GetDecodedHeightMapSample(Vector2 uv)
        {
            Vector4 c = HeightMap.GetPixelBilinear(uv.x, uv.y);
            Vector2 encodedHeight = new Vector2(c.x, c.y);
            float decodedHeight = GCommon.DecodeTerrainHeight(encodedHeight);
            c.x = decodedHeight;
            c.y = decodedHeight;
            return c;
        }

        [ExcludeFromDoc]
        public float GetHeightMapMemoryStats()
        {
            if (heightMap == null)
                return 0;
            return heightMap.width * heightMap.height * 4;
        }

        [ExcludeFromDoc]
        public void RemoveHeightMap()
        {
            if (heightMap != null)
            {
                GUtilities.DestroyObject(heightMap);
            }
        }

        [ExcludeFromDoc]
        public float[,] GetHeights()
        {
            int res = HeightMapResolution;
            float[,] samples = new float[res, res];
            Vector4 color;

            for (int z = 0; z < res; ++z)
            {
                for (int x = 0; x < res; ++x)
                {
                    color = HeightMap.GetPixel(x, z);
                    float h = GCommon.DecodeTerrainHeight(color);
                    samples[z, x] = h;
                }
            }
            return samples;
        }
    }
}
#endif
