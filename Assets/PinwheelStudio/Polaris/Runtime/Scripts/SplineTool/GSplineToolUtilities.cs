#if GRIFFIN
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityGraphics = UnityEngine.Graphics;

namespace Pinwheel.Griffin.SplineTool
{
    public static class GSplineToolUtilities
    {
        private static string SHADER_NAME = "Hidden/Griffin/SplineExtract";
        private static readonly int VERTICES = Shader.PropertyToID("_Vertices");
        private static readonly int ALPHAS = Shader.PropertyToID("_Alphas");
        private static readonly int WORLD_BOUNDS = Shader.PropertyToID("_WorldBounds");
        private static readonly int TEXTURE_SIZE = Shader.PropertyToID("_TextureSize");
        private static readonly int DEPTH_BUFFER = Shader.PropertyToID("_DepthBuffer");
        private static readonly int MAX_HEIGHT = Shader.PropertyToID("_MaxHeight");

        private static readonly int PASS_DEPTH = 0;
        private static readonly int PASS_MASK_ALPHA = 1;
        private static readonly int PASS_MASK_BOOL = 2;
        //private static readonly int PASS_HEIGHT_MASK = 3;
        private static readonly int PASS_HEIGHT = 4;

        public static void RenderAlphaMask(RenderTexture targetRt, ComputeBuffer worldTrianglesBuffer, ComputeBuffer alphasBuffer, int vertexCount, Vector4 worldBounds)
        {
            ComputeBuffer depthBuffer = new ComputeBuffer(targetRt.width * targetRt.height, sizeof(int));
            
            Material material = new Material(Shader.Find(SHADER_NAME));
            material.SetBuffer(VERTICES, worldTrianglesBuffer);
            material.SetBuffer(ALPHAS, alphasBuffer);
            material.SetVector(WORLD_BOUNDS, worldBounds);
            material.SetVector(TEXTURE_SIZE, new Vector2(targetRt.width, targetRt.height));
            material.SetBuffer(DEPTH_BUFFER, depthBuffer);

            UnityGraphics.SetRandomWriteTarget(1, depthBuffer);
            RenderTexture.active = targetRt;
            GL.Clear(true, true, Color.black);
            GL.PushMatrix();
            GL.LoadOrtho();
            material.SetPass(PASS_DEPTH);
            UnityGraphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
            material.SetPass(PASS_MASK_ALPHA);
            UnityGraphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
            GL.PopMatrix();
            RenderTexture.active = null;
            UnityGraphics.ClearRandomWriteTargets();

            depthBuffer.Release();
            Object.DestroyImmediate(material);
        }

        public static void RenderBoolMask(RenderTexture targetRt, ComputeBuffer worldTrianglesBuffer, int vertexCount, Vector4 worldBounds)
        {
            Material material = new Material(Shader.Find(SHADER_NAME));
            material.SetBuffer(VERTICES, worldTrianglesBuffer);
            material.SetVector(WORLD_BOUNDS, worldBounds);
            material.SetVector(TEXTURE_SIZE, new Vector2(targetRt.width, targetRt.height));

            RenderTexture.active = targetRt;
            GL.PushMatrix();
            GL.LoadOrtho();
            material.SetPass(PASS_MASK_BOOL);
            UnityGraphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
            GL.PopMatrix();
            RenderTexture.active = null;
            Object.DestroyImmediate(material);
        }

        public static void RenderHeightMap(RenderTexture targetRt, ComputeBuffer worldTrianglesBuffer, ComputeBuffer alphasBuffer, int vertexCount, Vector4 worldBounds, float maxHeight)
        {
            ComputeBuffer depthBuffer = new ComputeBuffer(targetRt.width * targetRt.height, sizeof(int));

            Material material = new Material(Shader.Find(SHADER_NAME));
            material.SetBuffer(VERTICES, worldTrianglesBuffer);
            material.SetBuffer(ALPHAS, alphasBuffer);
            material.SetVector(WORLD_BOUNDS, worldBounds);
            material.SetFloat(MAX_HEIGHT, maxHeight);
            material.SetVector(TEXTURE_SIZE, new Vector2(targetRt.width, targetRt.height));
            material.SetBuffer(DEPTH_BUFFER, depthBuffer);

            UnityGraphics.SetRandomWriteTarget(1, depthBuffer);
            RenderTexture.active = targetRt;
            GL.Clear(true, true, Color.black);
            GL.PushMatrix();
            GL.LoadOrtho();
            //material.SetPass(PASS_HEIGHT_MASK);
            //UnityGraphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
            material.SetPass(PASS_HEIGHT);
            UnityGraphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
            GL.PopMatrix();
            RenderTexture.active = null;
            UnityGraphics.ClearRandomWriteTargets();

            depthBuffer.Release();
            Object.DestroyImmediate(material);
        }
    }
}
#endif
