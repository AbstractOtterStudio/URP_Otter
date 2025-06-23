#if GRIFFIN
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.ComponentModel;

namespace Pinwheel.Griffin.SplineTool
{
    [GDisplayName("Ramp Maker")]
    [ExecuteInEditMode]
    public class GRampMaker : GSplineModifier
    {       
        [SerializeField]
        private AnimationCurve falloff;
        public AnimationCurve Falloff
        {
            get
            {
                if (falloff == null)
                {
                    falloff = AnimationCurve.EaseInOut(0, 0, 1, 1);
                }
                return falloff;
            }
            set
            {
                falloff = value;
            }
        }

        [SerializeField]
        private bool lowerHeight;
        public bool LowerHeight
        {
            get
            {
                return lowerHeight;
            }
            set
            {
                lowerHeight = value;
            }
        }

        [SerializeField]
        private bool raiseHeight;
        public bool RaiseHeight
        {
            get
            {
                return raiseHeight;
            }
            set
            {
                raiseHeight = value;
            }
        }

        [SerializeField]
        private int additionalMeshResolution;
        public int AdditionalMeshResolution
        {
            get
            {
                return additionalMeshResolution;
            }
            set
            {
                additionalMeshResolution = Mathf.Clamp(value, 0, 10);
            }
        }

        [SerializeField]
        private float heightOffset;
        public float HeightOffset
        {
            get
            {
                return heightOffset;
            }
            set
            {
                heightOffset = value;
            }
        }

        [SerializeField]
        private int stepCount;
        public int StepCount
        {
            get
            {
                return stepCount;
            }
            set
            {
                stepCount = Mathf.Max(1, value);
            }
        }

        [SerializeField]
        private Texture2D falloffNoise;
        public Texture2D FalloffNoise
        {
            get
            {
                return falloffNoise;
            }
            set
            {
                falloffNoise = value;
            }
        }

        [SerializeField]
        private Vector2 falloffNoiseSize;
        public Vector2 FalloffNoiseSize
        {
            get
            {
                return falloffNoiseSize;
            }
            set
            {
                falloffNoiseSize = value;
            }
        }

        private Texture2D falloffTexture;

        private Material rampMaterial;
        private Material RampMaterial
        {
            get
            {
                if (rampMaterial == null)
                {
                    rampMaterial = new Material(GRuntimeSettings.Instance.internalShaders.rampMakerShader);
                }
                return rampMaterial;
            }
        }

        private Vector3[] splineVertices;
        private float[] splineAlphas;

        public override void Apply()
        {
            if (SplineCreator == null)
                return;
            if (falloffTexture != null)
                Object.DestroyImmediate(falloffTexture);
            Internal_UpdateFalloffTexture();
            List<GStylizedTerrain> terrains = GUtilities.ExtractTerrainsFromOverlapTest(SplineCreator.SweepTest(curviness, width, falloffWidth));
            for (int i = 0; i < terrains.Count; ++i)
            {
                DrawOnTexture(terrains[i]);
            }

            for (int i = 0; i < terrains.Count; ++i)
            {
                UpdateTerrain(terrains[i]);
            }

            for (int i = 0; i < terrains.Count; ++i)
            {
                terrains[i].MatchEdges();
            }
        }

        private void DrawOnTexture(GStylizedTerrain t)
        {
            if (t.TerrainData == null)
                return;
            int heightMapResolution = t.TerrainData.Geometry.HeightMapResolution;
            RenderTexture rt = new RenderTexture(heightMapResolution, heightMapResolution, 0, GGeometry.HeightMapRTFormat, RenderTextureReadWrite.Linear);
            Internal_DrawOnTexture(t, rt);

            Color[] oldHeightMapColors = t.TerrainData.Geometry.HeightMap.GetPixels();
            RenderTexture.active = rt;
            t.TerrainData.Geometry.HeightMap.ReadPixels(new Rect(0, 0, heightMapResolution, heightMapResolution), 0, 0);
            t.TerrainData.Geometry.HeightMap.Apply();
            RenderTexture.active = null;
            Color[] newHeightMapColors = t.TerrainData.Geometry.HeightMap.GetPixels();

            rt.Release();
            Object.DestroyImmediate(rt);

            List<Rect> dirtyRects = new List<Rect>(GCommon.CompareTerrainTexture(t.TerrainData.Geometry.ChunkGridSize, oldHeightMapColors, newHeightMapColors));
            for (int i = 0; i < dirtyRects.Count; ++i)
            {
                t.TerrainData.Geometry.SetRegionDirty(dirtyRects[i]);
                t.TerrainData.Foliage.SetTreeRegionDirty(dirtyRects[i]);
                t.TerrainData.Foliage.SetGrassRegionDirty(dirtyRects[i]);
            }
        }

        private void UpdateTerrain(GStylizedTerrain t)
        {
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Geometry);

            t.UpdateTreesPosition();
            t.UpdateGrassPatches();
            t.TerrainData.Foliage.ClearTreeDirtyRegions();
            t.TerrainData.Foliage.ClearGrassDirtyRegions();
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Foliage);
        }

        private static readonly int HEIGHT_MAP = Shader.PropertyToID("_HeightMap");
        private static readonly int SPLINE_HEIGHT_MAP = Shader.PropertyToID("_SplineHeightMap");
        private static readonly int SPLINE_ALPHA_MASK = Shader.PropertyToID("_SplineAlphaMask");
        private static readonly int FALLOFF_CURVE_MAP = Shader.PropertyToID("_FalloffCurve");
        private static readonly int HEIGHT_OFFSET_01 = Shader.PropertyToID("_HeightOffset01");
        private static readonly int LOWER_HEIGHT = Shader.PropertyToID("_LowerHeight");
        private static readonly int RAISE_HEIGHT = Shader.PropertyToID("_RaiseHeight");
        private static readonly int ADDITIONAL_MESH_RESOLUTION = Shader.PropertyToID("_AdditionalMeshResolution");
        private static readonly int FALLOFF_NOISE = Shader.PropertyToID("_FalloffNoise");
        private static readonly int TERRAIN_MASK = Shader.PropertyToID("_TerrainMask");
        private static readonly int STEP_COUNT = Shader.PropertyToID("_StepCount");
        private static readonly int WORLD_BOUNDS = Shader.PropertyToID("_WorldBounds");

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Internal_DrawOnTexture(GStylizedTerrain t, RenderTexture rt)
        {
            int vCount = SplineCreator.GetVerticesCount(Curviness, Width, FalloffWidth);
            if (splineVertices == null || splineVertices.Length != vCount)
            {
                splineVertices = new Vector3[vCount];
                splineAlphas = new float[vCount];
            }

            SplineCreator.GenerateWorldVerticesAndAlphas(splineVertices, splineAlphas, Curviness, Width, FalloffWidth);

            ComputeBuffer worldTrianglesBuffer = new ComputeBuffer(splineVertices.Length, sizeof(float) * 3);
            worldTrianglesBuffer.SetData(splineVertices);

            ComputeBuffer alphasBuffer = new ComputeBuffer(splineAlphas.Length, sizeof(float));
            alphasBuffer.SetData(splineAlphas);

            Bounds b = t.Bounds;
            Vector4 worldBounds = new Vector4(b.min.x, b.min.z, b.size.x, b.size.z);

            RenderTexture splineAlphaMask = RenderTexture.GetTemporary(rt.width, rt.height, 0, RenderTextureFormat.RFloat);
            GSplineToolUtilities.RenderAlphaMask(splineAlphaMask, worldTrianglesBuffer, alphasBuffer, splineVertices.Length, worldBounds);

            RenderTexture splineHeightMap = RenderTexture.GetTemporary(rt.width, rt.height, 0, RenderTextureFormat.RFloat);
            GSplineToolUtilities.RenderHeightMap(splineHeightMap, worldTrianglesBuffer, alphasBuffer, splineVertices.Length, worldBounds, t.TerrainData.Geometry.Height);

            Material mat = RampMaterial;
            mat.SetTexture(HEIGHT_MAP, t.TerrainData.Geometry.HeightMap);
            mat.SetTexture(SPLINE_ALPHA_MASK, splineAlphaMask);
            mat.SetTexture(SPLINE_HEIGHT_MAP, splineHeightMap);
            mat.SetTexture(FALLOFF_CURVE_MAP, falloffTexture);
            mat.SetFloat(HEIGHT_OFFSET_01, HeightOffset / t.TerrainData.Geometry.Height);
            mat.SetInt(LOWER_HEIGHT, LowerHeight ? 1 : 0);
            mat.SetInt(RAISE_HEIGHT, RaiseHeight ? 1 : 0);
            mat.SetFloat(ADDITIONAL_MESH_RESOLUTION, GCommon.SUB_DIV_STEP * AdditionalMeshResolution);
            mat.SetTexture(FALLOFF_NOISE, FalloffNoise != null ? FalloffNoise : Texture2D.blackTexture);
            mat.SetTextureScale(FALLOFF_NOISE, new Vector2(
                FalloffNoiseSize.x != 0 ? 1f / FalloffNoiseSize.x : 0,
                FalloffNoiseSize.y != 0 ? 1f / FalloffNoiseSize.y : 0));
            mat.SetTextureOffset(FALLOFF_NOISE, Vector2.zero);
            mat.SetVector(WORLD_BOUNDS, worldBounds);
            if (SplineCreator.EnableTerrainMask)
            {
                mat.SetTexture(TERRAIN_MASK, t.TerrainData.Mask.MaskMapOrDefault);
            }
            else
            {
                mat.SetTexture(TERRAIN_MASK, Texture2D.blackTexture);
            }
            mat.SetInt(STEP_COUNT, StepCount);
            GCommon.DrawQuad(rt, GCommon.unitQuad, mat, 0);

            RenderTexture.ReleaseTemporary(splineHeightMap);
            RenderTexture.ReleaseTemporary(splineAlphaMask);
            alphasBuffer.Release();
            worldTrianglesBuffer.Release();
        }
                
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Internal_UpdateFalloffTexture()
        {
            falloffTexture = GCommon.CreateTextureFromCurve(Falloff, 256, 1);
        }

        public void Reset()
        {
            SplineCreator = GetComponent<GSplineCreator>();
            Falloff = AnimationCurve.EaseInOut(0, 0, 1, 1);
            LowerHeight = true;
            RaiseHeight = true;
            HeightOffset = 0;
            StepCount = 1000;
        }
    }
}
#endif
