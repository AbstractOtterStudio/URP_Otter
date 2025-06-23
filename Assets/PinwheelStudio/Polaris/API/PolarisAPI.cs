#if GRIFFIN_3
using Pinwheel.Griffin.Physic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pinwheel.Griffin
{
    public static partial class Polaris
    {
        [ExcludeFromDoc]
        public static void GetVersion(ref int major, ref int minor, ref int patch)
        {
            major = GVersionInfo.Major;
            minor = GVersionInfo.Minor;
            patch = GVersionInfo.Patch;
        }

        [ExcludeFromDoc]
        public static float GetVersionNumber()
        {
            return GVersionInfo.Number;
        }

        /// <summary>
        /// Instantiate a new GTerrainData object and init to default value. Values that are not used by the target texturing model will be set to the lowest possible (ex: texturingModel = ColorMap --> SplatControlResolution = 32).
        /// </summary>
        /// <param name="texturingModel">Texturing model to use.</param>
        /// <returns>The terrain data object. This object is not an asset file, you have to call editor functions to save to asset if needed.</returns>
        public static GTerrainData CreateAndInitTerrainData(GTexturingModel texturingModel)
        {
            GTerrainData data = ScriptableObject.CreateInstance<GTerrainData>();

            if (Application.isPlaying) //Reset() only called in edit mode
            {
                data.Reset();
                data.Geometry.Reset();
                data.Shading.Reset();
                data.Rendering.Reset();
                data.Foliage.Reset();
                data.Mask.Reset();
            }

            if (texturingModel == GTexturingModel.VertexColor)
            {
                data.Geometry.AlbedoToVertexColorMode = GAlbedoToVertexColorMode.Sharp;
            }

            if (texturingModel == GTexturingModel.GradientLookup)
            {
                data.Shading.UpdateLookupTextures();
                data.Shading.SplatControlResolution = 32;
            }
            if (texturingModel == GTexturingModel.ColorMap ||
                texturingModel == GTexturingModel.VertexColor)
            {
                data.Shading.SplatControlResolution = 32;
            }
            if (texturingModel == GTexturingModel.Splat)
            {
                data.Shading.AlbedoMapResolution = 32;
                data.Shading.MetallicMapResolution = 32;
            }
            return data;
        }

        /// <summary>
        /// Find a terrain shader corresponding to provided lighting/texturing/splat model and assign to the material.
        /// </summary>
        /// <param name="mat">The material to init.</param>
        /// <param name="lightingModel">The lighting model.</param>
        /// <param name="texturingModel">The texturing model.</param>
        /// <param name="splatModel">the splat model.</param>
        /// <returns>True if a shader was found and material was successfully initialized.</returns>
        public static bool InitTerrainMaterial(Material mat, GLightingModel lightingModel, GTexturingModel texturingModel, GSplatsModel splatModel = GSplatsModel.Splats4)
        {
            Shader shader = GRuntimeSettings.Instance.terrainRendering.GetTerrainShader(GCommon.CurrentRenderPipeline, lightingModel, texturingModel, splatModel);
            if (shader != null)
            {
                mat.shader = shader;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Assign a material to the terrain then update material properties (textures, vectors, floats, etc)
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <param name="mat">The material.</param>
        public static void SetTerrainMaterial(GTerrainData data, Material mat)
        {
            data.Shading.CustomMaterial = mat;
            data.Shading.UpdateMaterials();
        }

        /// <summary>
        /// Instantiate a new terrain game object using provided terrain data.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <returns>The low poly terrain component associated with terrain game object.</returns>
        public static GStylizedTerrain CreateTerrain(GTerrainData data)
        {
            GameObject g = new GameObject();
            GStylizedTerrain terrain = g.AddComponent<GStylizedTerrain>();
            terrain.TerrainData = data;

            return terrain;
        }

        /// <summary>
        /// Add collider to a terrain's trees.
        /// </summary>
        /// <param name="t">The target terrain object.</param>
        /// <returns>The component handling tree collision.</returns>
        public static GTreeCollider AttachTreeCollider(GStylizedTerrain t)
        {
            GameObject colliderGO = new GameObject("Tree Collider");
            colliderGO.transform.parent = t.transform;
            colliderGO.transform.localPosition = Vector3.zero;
            colliderGO.transform.localRotation = Quaternion.identity;
            colliderGO.transform.localScale = Vector3.one;

            GTreeCollider collider = colliderGO.AddComponent<GTreeCollider>();
            collider.Terrain = t;

            return collider;
        }

        /// <summary>
        /// Clone a terrain data object. This will also create new texture object for terrain textures.
        /// Use this function instead of Instantiate when cloning terrain data object.
        /// </summary>
        /// <param name="src">Source terrain data object.</param>
        /// <returns>Cloned terrain data object.</returns>
        public static GTerrainData DeepCloneTerrainData(GTerrainData src)
        {
            GTerrainData des = GTerrainData.CreateInstance<GTerrainData>();
            src.CopyTo(des);
            src.GeometryData = null;

            des.Geometry.HeightMap.SetPixelData(src.Geometry.HeightMap.GetRawTextureData(), 0, 0);
            if (src.Shading.HasAlbedoMap)
            {
                des.Shading.AlbedoMap.SetPixelData(src.Shading.AlbedoMap.GetRawTextureData(), 0, 0);
            }
            if (src.Shading.HasMetallicMap)
            {
                des.Shading.MetallicMap.SetPixelData(src.Shading.MetallicMap.GetRawTextureData(), 0, 0);
            }
            if (src.Shading.SplatControlMapCount > 0)
            {
                for (int i = 0; i < src.Shading.SplatControlMapCount; ++i)
                {
                    Texture2D srcControl = src.Shading.GetSplatControl(i);
                    Texture2D desControl = des.Shading.GetSplatControl(i);
                    desControl.SetPixelData(srcControl.GetRawTextureData(), 0, 0);
                }
            }
            des.Foliage.TreeInstances.AddRange(src.Foliage.TreeInstances);
            des.Foliage.AddGrassInstances(src.Foliage.GetGrassInstances());
            if (src.Mask.HasMaskMap)
            {
                des.Mask.MaskMap.SetPixelData(src.Mask.MaskMap.GetRawTextureData(), 0, 0);
            }

            return des;
        }

        /// <summary>
        /// Get the index of splat texture with highest intensity at a location, for making things such as footstep system or bullet impact particles.
        /// </summary>
        /// <param name="t">The terrain object.</param>
        /// <param name="worldPos">The position in world space to sample. Y component will be ignored.</param>
        /// <returns>Index of the splat texture with highest intensity.</returns>
        public static int GetDominantTextureIndex(GStylizedTerrain t, Vector3 worldPos)
        {
            Vector2 uv = t.WorldPointToUV(worldPos);
            int splatControlCount = t.TerrainData.Shading.SplatControlMapCount;
            float max = -1;
            int index = 0;
            for (int ti = 0; ti < splatControlCount; ++ti)
            {
                Texture2D controlMap = t.TerrainData.Shading.GetSplatControl(ti);
                Color c = controlMap.GetPixelBilinear(uv.x, uv.y);
                for (int ci = 0; ci < 4; ++ci)
                {
                    if (c[ci] > max)
                    {
                        max = c[ci];
                        index = ti * 4 + ci;
                    }
                }
            }
            return index;
        }

        /// <summary>
        /// Convert a point in world space to terrain's UV [0-1] space, for texture sampling.
        /// </summary>
        /// <param name="t">The terrain object.</param>
        /// <param name="worldPos">Position in world space. Y component will be ignored.</param>
        /// <returns>Position in UV [0-1] space.</returns>
        public static Vector2 WorldPositionToUV(GStylizedTerrain t, Vector3 worldPos)
        {
            return t.WorldPointToUV(worldPos);
        }

        /// <summary>
        /// Retrieve a direct reference to terrain height map texture.
        /// Color format (RG: encoded height value, B: additional subdivision, A: hole)
        /// </summary>
        /// <param name="data">Terrain data object.</param>
        /// <returns>The height map texture.</returns>
        public static Texture2D GetHeightMap(GTerrainData data)
        {
            return data.Geometry.HeightMap;
        }

        /// <summary>
        /// Encode height map data before writing to the height map.
        /// </summary>
        /// <param name="height01">Normalized height value, should be in [0, 1) (exclusive).</param>
        /// <param name="additionalSubdiv01">Addiitonal subdivision value, in [0,1] (inclusive)</param>
        /// <param name="visibility01">Visibility value, in [0,1] (inclusive), where >=0.5 is holes.</param>
        /// <returns>The pixel color containing height map values.</returns>
        public static Color EncodeHeightMapSample(float height01, float additionalSubdiv01, float visibility01)
        {
            Vector4 enc = GCommon.EncodeTerrainHeight(height01);
            enc.z = additionalSubdiv01;
            enc.w = visibility01;
            return enc;
        }

        /// <summary>
        /// Decode a height map pixel to usable values.
        /// </summary>
        /// <param name="sample">The height map sample, retrieved from Texture2D.GetPixel() or GetPixelBilinear().</param>
        /// <param name="height01">Normalized height value, in [0-1) (exclusive)</param>
        /// <param name="subdiv01">Additional subdivision value, in [0-1] (inclusive)</param>
        /// <param name="visibility01">Visibility value, in [0-1] (inclusive), where >=0.5 is holes.</param>
        public static void DecodeHeightMapSample(Color sample, ref float height01, ref float subdiv01, ref float visibility01)
        {
            height01 = GCommon.DecodeTerrainHeight(new Vector2(sample.r, sample.g));
            subdiv01 = sample.b;
            visibility01 = sample.a;
        }

        /// <summary>
        /// Regenerate surface mesh after modifying its height map.
        /// </summary>
        /// <param name="terrain">The terrain object.</param>
        /// <param name="regions01">A collection of regions (in normalized space) that has height map pixels modified. Duplicated and overlapped regions are allowed but should be kept at minimal.</param>
        public static void UpdateTerrainMesh(GStylizedTerrain terrain, IEnumerable<Rect> regions01 = null)
        {
            terrain.TerrainData.Geometry.SetRegionDirty(regions01);
            terrain.TerrainData.SetDirty(GTerrainData.DirtyFlags.Geometry);
            terrain.TerrainData.Geometry.ClearDirtyRegions();
        }

        /// <summary>
        /// Retrieve a direct reference to the terrain's albedo map.
        /// This will allocate a new albedo map if it's null.
        /// Call this function if you're going to WRITE to it, otherwise call the GetAlbedoMapForReadingBilinear() to avoid unnecessary allocation.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <returns>The albedo map.</returns>
        public static Texture2D GetAlbedoMap(GTerrainData data)
        {
            return data.Shading.AlbedoMap;
        }

        /// <summary>
        /// Retrieve the albedo map for reading with bilinear sampling. If the albedo map is null, it won't allocate a new texture but return a small default texture instead.
        /// Call this function if you only READ from the map, otherwise use the GetAlbedoMap() function instead.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <returns>The albedo map or default texture for reading with bilinear sampling.</returns>
        public static Texture2D GetAlbedoMapForReadingBilinear(GTerrainData data)
        {
            return data.Shading.AlbedoMapOrDefault;
        }

        /// <summary>
        /// Retrieve a direct reference to the terrain's metallic map.
        /// This will allocate a new metallic map if it's null.
        /// Call this function if you're going to WRITE to it, otherwise call the GetMetallicMapForReadingBilinear() to avoid unnecessary allocation.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <returns>The metallic map.</returns>
        public static Texture2D GetMetallicMap(GTerrainData data)
        {
            return data.Shading.MetallicMap;
        }

        /// <summary>
        /// Retrieve the metallic map for reading with bilinear sampling. If the metallic map is null, it won't allocate a new texture but return a small default texture instead.
        /// Call this function if you only READ from the map, otherwise use the GetMetallicMap() function instead.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <returns>The metallic map or default texture for reading with bilinear sampling.</returns>
        public static Texture2D GetMetallicMapForReadingBilinear(GTerrainData data)
        {
            return data.Shading.MetallicMapOrDefault;
        }

        /// <summary>
        /// Get the number of splat control maps.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <returns>Number of splat control maps.</returns>
        public static int GetControlMapCount(GTerrainData data)
        {
            return data.Shading.SplatControlMapCount;
        }

        /// <summary>
        /// Retrieve a direct reference to the terrain's control map at index.
        /// This will allocate a new control map if it's null.
        /// Call this function if you're going to WRITE to it, otherwise call the GetControlMapForReadingBilinear() to avoid unnecessary allocation.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <returns>The control map.</returns>
        public static Texture2D GetControlMap(GTerrainData data, int index)
        {
            return data.Shading.GetSplatControl(index);
        }

        /// <summary>
        /// Retrieve the control map at index for reading with bilinear sampling. If the control map is null, it won't allocate a new texture but return a small default texture instead.
        /// Call this function if you only READ from the map, otherwise use the GetControlMap() function instead.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <returns>The control map or default texture for reading with bilinear sampling.</returns>
        public static Texture2D GetControlMapForReadingBilinear(GTerrainData data, int index)
        {
            return data.Shading.GetSplatControlOrDefault(index);
        }

        /// <summary>
        /// Get the number of tree instances.
        /// </summary>
        /// <param name="data">Terrain data object.</param>
        /// <returns>Number of tree instances.</returns>
        public static int GetTreeInstanceCount(GTerrainData data)
        {
            return data.Foliage.TreeInstances.Count;
        }

        /// <summary>
        /// Add some trees to the terrain.
        /// </summary>
        /// <param name="data">The terrain data object</param>
        /// <param name="instances">Collection of tree instances.</param>
        public static void AddTreeInstance(GTerrainData data, IEnumerable<GTreeInstance> instances)
        {
            data.Foliage.AddTreeInstances(instances);
        }

        /// <summary>
        /// Remove tree instances from the terrain that match a condition.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <param name="condition">The condition that will be tested against every tree instances. If return true the tree will be removed.</param>
        public static void RemoveTreeInstance(GTerrainData data, System.Predicate<GTreeInstance> condition)
        {
            data.Foliage.RemoveTreeInstances(condition);
        }

        /// <summary>
        /// Erase all trees from the terrain.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        public static void ClearTreeInstances(GTerrainData data)
        {
            data.Foliage.ClearTreeInstances();
        }

        /// <summary>
        /// Add some grass instances to the terrain.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        /// <param name="instances">A list of grass instances.</param>
        public static void AddGrassInstances(GTerrainData data, List<GGrassInstance> instances)
        {
            data.Foliage.AddGrassInstances(instances);
        }

        /// <summary>
        /// Remove grass instances from the terrain that match a condition.
        /// </summary>
        /// <param name="data">The terrai data objects.</param>
        /// <param name="condition">The condition that will be tested against every grass instances. If return true the instance will be removed.</param>
        public static void RemoveGrassInstances(GTerrainData data, System.Predicate<GGrassInstance> condition)
        {
            GGrassPatch[] patches = data.Foliage.GrassPatches;
            foreach(GGrassPatch p in patches)
            {
                p.RemoveInstances(condition);
            }
        }

        /// <summary>
        /// Erase all grass instances from the terrain.
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        public static void ClearGrassInstances(GTerrainData data)
        {
            data.Foliage.ClearGrassInstances();
        }

        /// <summary>
        /// Refresh terrain material properties (textures, vectors, floats, etc.).
        /// </summary>
        /// <param name="data">The terrain data object.</param>
        public static void UpdateMaterial(GTerrainData data)
        {
            data.Shading.UpdateMaterials();
        }
    }
}
#endif