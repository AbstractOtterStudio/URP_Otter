#if GRIFFIN
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pinwheel.Griffin.SplineTool
{
    [GDisplayName("Path Painter")]
    public class GPathPainter : GSplineModifier
    {
        public enum PaintChannel
        {
            AlbedoAndMetallic, Splat
        }

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
        private PaintChannel channel;
        public PaintChannel Channel
        {
            get
            {
                return channel;
            }
            set
            {
                channel = value;
            }
        }

        [SerializeField]
        private Color color;
        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
            }
        }

        [SerializeField]
        private float metallic;
        public float Metallic
        {
            get
            {
                return metallic;
            }
            set
            {
                metallic = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private float smoothness;
        public float Smoothness
        {
            get
            {
                return smoothness;
            }
            set
            {
                smoothness = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        private int splatIndex;
        public int SplatIndex
        {
            get
            {
                return splatIndex;
            }
            set
            {
                splatIndex = Mathf.Max(0, value);
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

        private Material applyAlbedoMaterial;
        private Material ApplyAlbedoMaterial
        {
            get
            {
                if (applyAlbedoMaterial == null)
                {
                    applyAlbedoMaterial = new Material(GRuntimeSettings.Instance.internalShaders.pathPainterAlbedoShader);
                }
                return applyAlbedoMaterial;
            }
        }

        private Material applyMetallicSmoothnessMaterial;
        private Material ApplyMetallicSmoothnessMaterial
        {
            get
            {
                if (applyMetallicSmoothnessMaterial == null)
                {
                    applyMetallicSmoothnessMaterial = new Material(GRuntimeSettings.Instance.internalShaders.pathPainterMetallicSmoothnessShader);
                }
                return applyMetallicSmoothnessMaterial;
            }
        }

        private Material applySplatMaterial;
        private Material ApplySplatMaterial
        {
            get
            {
                if (applySplatMaterial == null)
                {
                    applySplatMaterial = new Material(GRuntimeSettings.Instance.internalShaders.pathPainterSplatShader);
                }
                return applySplatMaterial;
            }
        }

        private Vector3[] splineVertices;
        private float[] splineAlphas;

        private void Reset()
        {
            SplineCreator = GetComponent<GSplineCreator>();
            Falloff = AnimationCurve.EaseInOut(0, 0, 1, 1);
            Channel = PaintChannel.AlbedoAndMetallic;
            Color = Color.white;
            Metallic = 0;
            Smoothness = 0;
        }

        public override void Apply()
        {
            if (SplineCreator == null)
                return;
            if (falloffTexture != null)
                Object.DestroyImmediate(falloffTexture);
            Internal_UpdateFalloffTexture();
            List<GOverlapTestResult> sweepTests = SplineCreator.SweepTest(Curviness, Width, FalloffWidth);
            foreach (GOverlapTestResult st in sweepTests)
            {
                if (st.IsOverlapped)
                {
                    Apply(st.Terrain);
                }
            }
        }

        private void Apply(GStylizedTerrain t)
        {
            if (t.TerrainData == null)
                return;

            if (Channel == PaintChannel.AlbedoAndMetallic)
            {
                ApplyAlbedoAndMetallic(t);
            }
            else if (Channel == PaintChannel.Splat)
            {
                ApplySplat(t);
            }
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }

        private void ApplyAlbedoAndMetallic(GStylizedTerrain t)
        {
            int albedoResolution = t.TerrainData.Shading.AlbedoMapResolution;
            RenderTexture rtAlbedo = new RenderTexture(albedoResolution, albedoResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Internal_ApplyAlbedo(t, rtAlbedo);

            RenderTexture.active = rtAlbedo;
            t.TerrainData.Shading.AlbedoMap.ReadPixels(new Rect(0, 0, albedoResolution, albedoResolution), 0, 0);
            t.TerrainData.Shading.AlbedoMap.Apply();
            RenderTexture.active = null;
            rtAlbedo.Release();
            Object.DestroyImmediate(rtAlbedo);

            int metallicResolution = t.TerrainData.Shading.MetallicMapResolution;
            RenderTexture rtMetallic = new RenderTexture(metallicResolution, metallicResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Internal_ApplyMetallic(t, rtMetallic);

            RenderTexture.active = rtMetallic;
            t.TerrainData.Shading.MetallicMap.ReadPixels(new Rect(0, 0, metallicResolution, metallicResolution), 0, 0);
            t.TerrainData.Shading.MetallicMap.Apply();
            RenderTexture.active = null;
            rtMetallic.Release();
            Object.DestroyImmediate(rtMetallic);
        }

        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
        private static readonly int SPLINE_ALPHA_MAP = Shader.PropertyToID("_SplineAlphaMap");
        private static readonly int FALLOFF_CURVE = Shader.PropertyToID("_FalloffCurve");
        private static readonly int FALLOFF_NOISE = Shader.PropertyToID("_FalloffNoise");
        private static readonly int TERRAIN_MASK = Shader.PropertyToID("_TerrainMask");
        private static readonly int COLOR = Shader.PropertyToID("_Color");
        private static readonly int METALLIC = Shader.PropertyToID("_Metallic");
        private static readonly int SMOOTHNESS = Shader.PropertyToID("_Smoothness");
        private static readonly int CHANNEL_INDEX = Shader.PropertyToID("_ChannelIndex");
        private static readonly int WORLD_BOUNDS = Shader.PropertyToID("_WorldBounds");

        public void Internal_ApplyAlbedo(GStylizedTerrain t, RenderTexture rtAlbedo)
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

            int alphaMaskRes = t.TerrainData.Shading.AlbedoMapResolution;
            RenderTexture splineAlphaMask = RenderTexture.GetTemporary(alphaMaskRes, alphaMaskRes, 0, RenderTextureFormat.RFloat);
            GSplineToolUtilities.RenderAlphaMask(splineAlphaMask, worldTrianglesBuffer, alphasBuffer, splineVertices.Length, worldBounds);

            GCommon.CopyToRT(t.TerrainData.Shading.AlbedoMapOrDefault, rtAlbedo);
            Material mat = ApplyAlbedoMaterial;
            mat.SetTexture(MAIN_TEX, t.TerrainData.Shading.AlbedoMapOrDefault);
            mat.SetTexture(SPLINE_ALPHA_MAP, splineAlphaMask);
            mat.SetTexture(FALLOFF_CURVE, falloffTexture);
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

            mat.SetColor(COLOR, Color);
            GCommon.DrawQuad(rtAlbedo, GCommon.unitQuad, mat, 0);

            RenderTexture.ReleaseTemporary(splineAlphaMask);
            worldTrianglesBuffer.Dispose();
            alphasBuffer.Dispose();
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }

        public void Internal_ApplyMetallic(GStylizedTerrain t, RenderTexture rtMetallic)
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

            int alphaMaskRes = t.TerrainData.Shading.MetallicMapResolution;
            RenderTexture splineAlphaMask = RenderTexture.GetTemporary(alphaMaskRes, alphaMaskRes, 0, RenderTextureFormat.RFloat);
            GSplineToolUtilities.RenderAlphaMask(splineAlphaMask, worldTrianglesBuffer, alphasBuffer, splineVertices.Length, worldBounds);

            GCommon.CopyToRT(t.TerrainData.Shading.MetallicMapOrDefault, rtMetallic);
            Material mat = ApplyMetallicSmoothnessMaterial;
            mat.SetTexture(MAIN_TEX, t.TerrainData.Shading.MetallicMapOrDefault);
            mat.SetTexture(SPLINE_ALPHA_MAP, splineAlphaMask);
            mat.SetTexture(FALLOFF_CURVE, falloffTexture);
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

            mat.SetFloat(METALLIC, Metallic);
            mat.SetFloat(SMOOTHNESS, Smoothness);
            GCommon.DrawQuad(rtMetallic, GCommon.unitQuad, mat, 0);

            RenderTexture.ReleaseTemporary(splineAlphaMask);
            worldTrianglesBuffer.Dispose();
            alphasBuffer.Dispose();
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }

        public void Internal_UpdateFalloffTexture()
        {
            falloffTexture = GCommon.CreateTextureFromCurve(Falloff, 256, 1);
        }

        private void ApplySplat(GStylizedTerrain t)
        {
            int splatControlResolution = t.TerrainData.Shading.SplatControlResolution;
            int controlMapCount = t.TerrainData.Shading.SplatControlMapCount;
            RenderTexture[] rtControls = new RenderTexture[controlMapCount];
            for (int i = 0; i < controlMapCount; ++i)
            {
                rtControls[i] = new RenderTexture(splatControlResolution, splatControlResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            }
            Internal_ApplySplat(t, rtControls);
            for (int i = 0; i < controlMapCount; ++i)
            {
                Texture2D splatControl = t.TerrainData.Shading.GetSplatControl(i);
                RenderTexture.active = rtControls[i];
                splatControl.ReadPixels(new Rect(0, 0, splatControlResolution, splatControlResolution), 0, 0);
                splatControl.Apply();
                RenderTexture.active = null;

                rtControls[i].Release();
                Object.DestroyImmediate(rtControls[i]);
            }
        }

        public void Internal_ApplySplat(GStylizedTerrain t, RenderTexture[] rtControls)
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

            int alphaMaskRes = t.TerrainData.Shading.SplatControlResolution;
            RenderTexture splineAlphaMask = RenderTexture.GetTemporary(alphaMaskRes, alphaMaskRes, 0, RenderTextureFormat.RFloat);
            GSplineToolUtilities.RenderAlphaMask(splineAlphaMask, worldTrianglesBuffer, alphasBuffer, splineVertices.Length, worldBounds);

            Material mat = ApplySplatMaterial;
            mat.SetTexture(SPLINE_ALPHA_MAP, splineAlphaMask);
            mat.SetTexture(FALLOFF_CURVE, falloffTexture);
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

            for (int i = 0; i < rtControls.Length; ++i)
            {
                Texture2D splatControl = t.TerrainData.Shading.GetSplatControlOrDefault(i);
                GCommon.CopyToRT(splatControl, rtControls[i]);
                mat.SetTexture(MAIN_TEX, splatControl);
                if (SplatIndex / 4 == i)
                {
                    mat.SetInt(CHANNEL_INDEX, SplatIndex % 4);
                }
                else
                {
                    mat.SetInt(CHANNEL_INDEX, -1);
                }
                GCommon.DrawQuad(rtControls[i], GCommon.unitQuad, mat, 0);
            }

            RenderTexture.ReleaseTemporary(splineAlphaMask);
            worldTrianglesBuffer.Dispose();
            alphasBuffer.Dispose();
            t.TerrainData.SetDirty(GTerrainData.DirtyFlags.Shading);
        }
    }
}
#endif
