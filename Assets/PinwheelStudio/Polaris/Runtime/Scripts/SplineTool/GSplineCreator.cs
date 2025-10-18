#if GRIFFIN
using System;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine.Rendering;
#if GRIFFIN_URP
using UnityEngine.Rendering.Universal;
#endif


namespace Pinwheel.Griffin.SplineTool
{
    [System.Serializable]
    [ExecuteInEditMode]
    public partial class GSplineCreator : MonoBehaviour
    {
        public const string SPLINE_LAYER = "POLARIS SPLINE";

        public delegate void SplineChangedHandler(GSplineCreator sender);
        public static event SplineChangedHandler Editor_SplineChanged;

        [SerializeField]
        private int groupId;
        public int GroupId
        {
            get
            {
                return groupId;
            }
            set
            {
                groupId = value;
            }
        }

        [SerializeField]
        private bool enableTerrainMask;
        public bool EnableTerrainMask
        {
            get
            {
                return enableTerrainMask;
            }
            set
            {
                enableTerrainMask = value;
            }
        }

        [SerializeField]
        private Vector3 positionOffset;
        public Vector3 PositionOffset
        {
            get
            {
                return positionOffset;
            }
            set
            {
                positionOffset = value;
            }
        }

        [SerializeField]
        private Quaternion initialRotation;
        public Quaternion InitialRotation
        {
            get
            {
                return initialRotation;
            }
            set
            {
                initialRotation = value;
            }
        }

        [SerializeField]
        private Vector3 initialScale;
        public Vector3 InitialScale
        {
            get
            {
                return initialScale;
            }
            set
            {
                initialScale = value;
            }
        }

        [Obsolete]
        [SerializeField]
        private int smoothness;
        [Obsolete]
        public int Smoothness
        {
            get
            {
                return smoothness;
            }
            set
            {
                smoothness = Mathf.Max(2, value);
            }
        }

        [Obsolete]
        [SerializeField]
        private float width;
        [Obsolete]
        public float Width
        {
            get
            {
                return width;
            }
            set
            {
                width = Mathf.Max(0, value);
            }
        }

        [Obsolete]
        [SerializeField]
        private float falloffWidth;
        [Obsolete]
        public float FalloffWidth
        {
            get
            {
                return falloffWidth;
            }
            set
            {
                falloffWidth = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        private GSpline spline;
        public GSpline Spline
        {
            get
            {
                if (spline == null)
                {
                    spline = new GSpline();
                }
                return spline;
            }
            set
            {
                spline = value;
            }
        }

#if UNITY_EDITOR
        private List<Vector4> vertices;
        public List<Vector4> Editor_Vertices
        {
            get
            {
                if (vertices == null)
                    vertices = new List<Vector4>();
                return vertices;
            }
            set
            {
                vertices = value;
            }
        }
#endif

        public void Reset()
        {
            PositionOffset = Vector3.up * 5;
            InitialRotation = Quaternion.identity;
            InitialScale = Vector3.one;
        }

        private void OnEnable()
        {
            //UpdateMeshes();
        }

        private void OnDisable()
        {
            CleanUp();
        }

        [Obsolete]
        public void UpdateMeshes()
        {
            List<GSplineSegment> segments = Spline.Segments;
            for (int i = 0; i < segments.Count; ++i)
            {
                UpdateMesh(i);
            }
        }

        [Obsolete]
        public void UpdateMesh(int sIndex)
        {
            List<GSplineSegment> segments = Spline.Segments;
            if (sIndex < 0 || sIndex >= segments.Count)
                throw new System.IndexOutOfRangeException("segmentIndex is out of range");

            GSplineSegment s = segments[sIndex];
            List<Vector3> vertices = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> triangles = new List<int>();

            float tStep = 1f / (Smoothness - 1);
            for (int tIndex = 0; tIndex < Smoothness - 1; ++tIndex)
            {
                float t0 = tIndex * tStep;
                Vector3 translation0 = Spline.EvaluatePosition(sIndex, t0);
                Quaternion rotation0 = Spline.EvaluateRotation(sIndex, t0);
                Vector3 scale0 = Spline.EvaluateScale(sIndex, t0);

                float t1 = (tIndex + 1) * tStep;
                Vector3 translation1 = Spline.EvaluatePosition(sIndex, t1);
                Quaternion rotation1 = Spline.EvaluateRotation(sIndex, t1);
                Vector3 scale1 = Spline.EvaluateScale(sIndex, t1);

                Matrix4x4 matrix0 = Matrix4x4.TRS(translation0, rotation0, scale0);
                Matrix4x4 matrix1 = Matrix4x4.TRS(translation1, rotation1, scale1);

                Vector3 bl, tl, tr, br;
                float halfWidth = Width * 0.5f;

                if (FalloffWidth > 0)
                {
                    //Left falloff
                    bl = matrix0.MultiplyPoint(new Vector3(-halfWidth - FalloffWidth, 0, 0));
                    tl = matrix1.MultiplyPoint(new Vector3(-halfWidth - FalloffWidth, 0, 0));
                    tr = matrix1.MultiplyPoint(new Vector3(-halfWidth, 0, 0));
                    br = matrix0.MultiplyPoint(new Vector3(-halfWidth, 0, 0));

                    vertices.Add(bl);
                    vertices.Add(tl);
                    vertices.Add(tr);
                    colors.Add(Color.clear);
                    colors.Add(Color.clear);
                    colors.Add(Color.white);

                    vertices.Add(bl);
                    vertices.Add(tr);
                    vertices.Add(br);
                    colors.Add(Color.clear);
                    colors.Add(Color.white);
                    colors.Add(Color.white);
                }

                if (Width > 0)
                {
                    //Center
                    bl = matrix0.MultiplyPoint(new Vector3(-halfWidth, 0, 0));
                    tl = matrix1.MultiplyPoint(new Vector3(-halfWidth, 0, 0));
                    tr = matrix1.MultiplyPoint(new Vector3(halfWidth, 0, 0));
                    br = matrix0.MultiplyPoint(new Vector3(halfWidth, 0, 0));

                    vertices.Add(bl);
                    vertices.Add(tl);
                    vertices.Add(tr);
                    colors.Add(Color.white);
                    colors.Add(Color.white);
                    colors.Add(Color.white);

                    vertices.Add(bl);
                    vertices.Add(tr);
                    vertices.Add(br);
                    colors.Add(Color.white);
                    colors.Add(Color.white);
                    colors.Add(Color.white);
                }

                if (FalloffWidth > 0)
                {
                    //Right falloff
                    bl = matrix0.MultiplyPoint(new Vector3(halfWidth, 0, 0));
                    tl = matrix1.MultiplyPoint(new Vector3(halfWidth, 0, 0));
                    tr = matrix1.MultiplyPoint(new Vector3(halfWidth + FalloffWidth, 0, 0));
                    br = matrix0.MultiplyPoint(new Vector3(halfWidth + FalloffWidth, 0, 0));

                    vertices.Add(bl);
                    vertices.Add(tl);
                    vertices.Add(tr);
                    colors.Add(Color.white);
                    colors.Add(Color.white);
                    colors.Add(Color.clear);

                    vertices.Add(bl);
                    vertices.Add(tr);
                    vertices.Add(br);
                    colors.Add(Color.white);
                    colors.Add(Color.clear);
                    colors.Add(Color.clear);
                }
            }

            Mesh m = s.Mesh;
            m.Clear();
            m.SetVertices(vertices);
            m.SetColors(colors);
            m.SetTriangles(GUtilities.GetIndicesArray(vertices.Count), 0);
            m.uv = null;
            m.normals = null;
            m.tangents = null;
        }

        [Obsolete]
        public void UpdateMesh(IEnumerable<int> indices)
        {
            IEnumerator<int> i = indices.GetEnumerator();
            while (i.MoveNext())
            {
                UpdateMesh(i.Current);
            }
        }

        public void CleanUp()
        {
            Spline.Dispose();
        }

        [Obsolete]
        public List<Vector4> GenerateVerticesWithFalloff()
        {
            List<Vector4> vertices = new List<Vector4>();

            return vertices;
        }

        [Obsolete]
        public IEnumerable<Rect> SweepDirtyRect(GStylizedTerrain terrain)
        {
            if (terrain.TerrainData == null)
                return new List<Rect>();
            int gridSize = terrain.TerrainData.Geometry.ChunkGridSize;
            List<Rect> uvRects = new List<Rect>();
            for (int x = 0; x < gridSize; ++x)
            {
                for (int z = 0; z < gridSize; ++z)
                {
                    uvRects.Add(GCommon.GetUvRange(gridSize, x, z));
                }
            }

            HashSet<Rect> dirtyRects = new HashSet<Rect>();
            Vector3 terrainSize = new Vector3(
                terrain.TerrainData.Geometry.Width,
                terrain.TerrainData.Geometry.Height,
                terrain.TerrainData.Geometry.Length);
            float splineSize = Mathf.Max(1, Width + FalloffWidth * 2);
            Vector2 sweepRectSize = new Vector2(
                Mathf.InverseLerp(0, terrainSize.x, splineSize),
                Mathf.InverseLerp(0, terrainSize.z, splineSize));
            Rect sweepRect = new Rect();
            sweepRect.size = sweepRectSize;

            int segmentCount = Spline.Segments.Count;
            for (int sIndex = 0; sIndex < segmentCount; ++sIndex)
            {
                float tStep = 1f / (Smoothness - 1);
                for (int tIndex = 0; tIndex < Smoothness - 1; ++tIndex)
                {
                    float t = tIndex * tStep;
                    Vector3 worldPos = transform.TransformPoint(Spline.EvaluatePosition(sIndex, t));
                    Vector3 scale = transform.TransformVector(Spline.EvaluateScale(sIndex, t));
                    float maxScaleComponent = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    Vector3 normalizedPos = terrain.WorldPointToNormalized(worldPos);
                    sweepRect.center = new Vector2(normalizedPos.x, normalizedPos.z);
                    sweepRect.size = sweepRectSize * maxScaleComponent;
                    for (int rIndex = 0; rIndex < uvRects.Count; ++rIndex)
                    {
                        if (uvRects[rIndex].Overlaps(sweepRect))
                        {
                            dirtyRects.Add(uvRects[rIndex]);
                        }
                    }
                }
            }

            return dirtyRects;
        }

        [Obsolete]
        public bool OverlapTest(GStylizedTerrain terrain)
        {
            Rect terrainRect = terrain.Rect;
            float splineSize = Mathf.Max(1, Width + FalloffWidth * 2);
            Vector2 sweepRectSize = Vector2.one * splineSize;
            Rect sweepRect = new Rect();
            sweepRect.size = sweepRectSize;

            int segmentCount = Spline.Segments.Count;
            for (int sIndex = 0; sIndex < segmentCount; ++sIndex)
            {
                float tStep = 1f / (Smoothness - 1);
                for (int tIndex = 0; tIndex < Smoothness - 1; ++tIndex)
                {
                    float t = tIndex * tStep;
                    Vector3 worldPos = Spline.EvaluatePosition(sIndex, t);
                    Vector3 scale = Spline.EvaluateScale(sIndex, t);
                    float maxScaleComponent = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    sweepRect.center = new Vector2(worldPos.x, worldPos.z);
                    sweepRect.size = sweepRectSize * maxScaleComponent;
                    if (sweepRect.Overlaps(terrainRect))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void MarkSplineChanged(GSplineCreator sender)
        {
            if (Editor_SplineChanged != null)
            {
                Editor_SplineChanged.Invoke(sender);
            }
        }

        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void DrawOnTexture(RenderTexture rt, Bounds worldBounds, Material mat)
        {
            Vector3 cameraWorldPos = new Vector3(worldBounds.center.x, worldBounds.max.y + worldBounds.size.y, worldBounds.center.z);

            GameObject cameraObject = new GameObject("Camera") { hideFlags = HideFlags.DontSave };
            Camera cam = cameraObject.AddComponent<Camera>();
            cam.transform.position = cameraWorldPos;
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.transform.localScale = Vector3.one;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = worldBounds.size.y * 2;
            cam.clearFlags = CameraClearFlags.Nothing;
            cam.cameraType = CameraType.Preview;
            cam.aspect = worldBounds.size.x / worldBounds.size.z;
            cam.orthographic = true;
            cam.orthographicSize = worldBounds.size.z * 0.5f;
            cam.useOcclusionCulling = false;
            cam.cullingMask = LayerMask.GetMask(SPLINE_LAYER);
            cam.enabled = false;
            cam.targetTexture = rt;

            Matrix4x4 worldToNormalized = Matrix4x4.TRS(
                worldBounds.min,
                Quaternion.identity,
                worldBounds.size).inverse;
            mat.SetMatrix("_WorldToNormalized", worldToNormalized);

            List<GSplineSegment> segments = Spline.Segments;
            for (int i = 0; i < segments.Count; ++i)
            {
                Mesh m = segments[i].Mesh;
                Graphics.DrawMesh(
                    m,
                    transform.localToWorldMatrix,
                    mat,
                    LayerMask.NameToLayer(SPLINE_LAYER),
                    cam,
                    0,
                    null,
                    ShadowCastingMode.Off,
                    false,
                    null,
                    LightProbeUsage.Off,
                    null);
            }

            cam.Render();
            cam.targetTexture = null;
            GUtilities.DestroyGameobject(cameraObject);
        }

        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void DrawOnTexture(RenderTexture rt, Bounds worldBounds, Material mat, ScriptableRenderContext context)
        {
#if GRIFFIN_URP
            Vector3 cameraWorldPos = new Vector3(worldBounds.center.x, worldBounds.max.y + worldBounds.size.y, worldBounds.center.z);

            GameObject cameraObject = new GameObject("Camera") { hideFlags = HideFlags.DontSave };
            Camera cam = cameraObject.AddComponent<Camera>();
            cam.transform.position = cameraWorldPos;
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.transform.localScale = Vector3.one;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = worldBounds.size.y * 2;
            cam.clearFlags = CameraClearFlags.Nothing;
            cam.cameraType = CameraType.Preview;
            cam.aspect = worldBounds.size.x / worldBounds.size.z;
            cam.orthographic = true;
            cam.orthographicSize = worldBounds.size.z * 0.5f;
            cam.useOcclusionCulling = false;
            cam.cullingMask = LayerMask.GetMask(SPLINE_LAYER);
            cam.enabled = false;
            cam.targetTexture = rt;

            Matrix4x4 worldToNormalized = Matrix4x4.TRS(
                worldBounds.min,
                Quaternion.identity,
                worldBounds.size).inverse;
            mat.SetMatrix("_WorldToNormalized", worldToNormalized);
            mat.SetPass(0);

            if (GCommon.CurrentRenderPipeline == GRenderPipelineType.Universal)
            {
                UniversalAdditionalCameraData urpCameraData = cam.GetUniversalAdditionalCameraData();
                urpCameraData.renderType = CameraRenderType.Overlay;
            }

            List<GSplineSegment> segments = Spline.Segments;
            for (int i = 0; i < segments.Count; ++i)
            {
                Mesh m = segments[i].Mesh;
                Graphics.DrawMesh(
                    m,
                    transform.localToWorldMatrix,
                    mat,
                    LayerMask.NameToLayer(SPLINE_LAYER),
                    cam,
                    0,
                    null,
                    ShadowCastingMode.Off,
                    false,
                    null,
                    LightProbeUsage.Off,
                    null);
            }

#if UNITY_2022_2_OR_NEWER
            RenderPipeline.SubmitRenderRequest(cam, new RenderPipeline.StandardRequest());
#else
            UniversalRenderPipeline.RenderSingleCamera(context, cam); 
#endif

            cam.targetTexture = null;
            GUtilities.DestroyGameobject(cameraObject);
#endif
        }

        public void GenerateWorldVerticesAndAlphas(Vector3[] vertices, float[] alphas, int curviness, float width, float falloffWidth)
        {
            int vIndex = 0;
            int aIndex = 0;
            List<GSplineSegment> segments = Spline.Segments;
            float tStep = 1f / (curviness - 1);
            float eps = tStep * 0.5f;

            //generate vertices in local space first
            for (int sIndex = 0; sIndex < segments.Count; ++sIndex)
            {                
                for (int tIndex = 0; tIndex < curviness - 1; ++tIndex)
                {
                    float t0 = tIndex * tStep;
                    float t1 = (tIndex + 1) * tStep;

                    Vector3 c0 = spline.EvaluatePosition(sIndex, t0);
                    Vector3 c1 = spline.EvaluatePosition(sIndex, t1);

                    Vector3 normal0, normal1;
                    if (t0 == 0)
                    {
                        Matrix4x4 matrix0 = Spline.TRS(sIndex, t0);
                        normal0 = transform.TransformVector(matrix0.MultiplyVector(Vector3.left));
                    }
                    else
                    {
                        float dt00 = t0 - eps;
                        float dt01 = t0 + eps;
                        dt00 += sIndex;
                        dt01 += sIndex;
                        Vector3 tangent0 = (spline.EvaluatePosition(dt01) - spline.EvaluatePosition(dt00)).normalized;
                        Vector3 up0 = spline.EvaluateUpVector(sIndex, t0);
                        normal0 = Vector3.Cross(tangent0, up0);
                    }


                    if (t1 == 1)
                    {
                        Matrix4x4 matrix1 = Spline.TRS(sIndex, t1);
                        normal1 = transform.TransformVector(matrix1.MultiplyVector(Vector3.left));
                    }
                    else
                    {
                        float dt10 = t1 - eps;
                        float dt11 = t1 + eps;
                        dt10 += sIndex;
                        dt11 += sIndex;
                        Vector3 tangent1 = (spline.EvaluatePosition(dt11) - spline.EvaluatePosition(dt10)).normalized;
                        Vector3 up1 = spline.EvaluateUpVector(sIndex, t1);
                        normal1 = Vector3.Cross(tangent1, up1);
                    }

                    Vector3 bl, tl, tr, br;
                    float halfWidth = width * 0.5f;

                    if (falloffWidth > 0)
                    {
                        //Left falloff
                        bl = c0 - normal0 * (halfWidth + falloffWidth);
                        tl = c1 - normal1 * (halfWidth + falloffWidth);
                        tr = c1 - normal1 * halfWidth;
                        br = c0 - normal0 * halfWidth;

                        vertices[vIndex++] = bl;
                        vertices[vIndex++] = tl;
                        vertices[vIndex++] = tr;
                        alphas[aIndex++] = 0;
                        alphas[aIndex++] = 0;
                        alphas[aIndex++] = 1;

                        vertices[vIndex++] = bl;
                        vertices[vIndex++] = tr;
                        vertices[vIndex++] = br;
                        alphas[aIndex++] = 0;
                        alphas[aIndex++] = 1;
                        alphas[aIndex++] = 1;
                    }

                    if (width > 0)
                    {
                        //Center
                        bl = c0 - normal0 * halfWidth;
                        tl = c1 - normal1 * halfWidth;
                        tr = c1 + normal1 * halfWidth;
                        br = c0 + normal0 * halfWidth;

                        vertices[vIndex++] = bl;
                        vertices[vIndex++] = tl;
                        vertices[vIndex++] = tr;
                        alphas[aIndex++] = 1;
                        alphas[aIndex++] = 1;
                        alphas[aIndex++] = 1;

                        vertices[vIndex++] = bl;
                        vertices[vIndex++] = tr;
                        vertices[vIndex++] = br;
                        alphas[aIndex++] = 1;
                        alphas[aIndex++] = 1;
                        alphas[aIndex++] = 1;
                    }

                    if (falloffWidth > 0)
                    {
                        //Right falloff
                        bl = c0 + normal0 * halfWidth;
                        tl = c1 + normal1 * halfWidth;
                        tr = c1 + normal1 * (halfWidth + falloffWidth);
                        br = c0 + normal0 * (halfWidth + falloffWidth);

                        vertices[vIndex++] = bl;
                        vertices[vIndex++] = tl;
                        vertices[vIndex++] = tr;
                        alphas[aIndex++] = 1;
                        alphas[aIndex++] = 1;
                        alphas[aIndex++] = 0;

                        vertices[vIndex++] = bl;
                        vertices[vIndex++] = tr;
                        vertices[vIndex++] = br;
                        alphas[aIndex++] = 1;
                        alphas[aIndex++] = 0;
                        alphas[aIndex++] = 0;
                    }
                }
            }

            //transform to world space
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = transform.TransformPoint(vertices[i]);
            }
        }

        public int GetVerticesCount(int smoothness, float width, float falloffWidth)
        {
            int fragmentCountPerSegment = smoothness - 1;
            int segmentCount = Spline.Segments.Count;

            int vertexCountPerFragment = 0;
            if (width > 0)
            {
                vertexCountPerFragment += 6;
            }
            if (falloffWidth > 0)
            {
                vertexCountPerFragment += 12;
            }

            return segmentCount * fragmentCountPerSegment * vertexCountPerFragment;
        }
    }
}
#endif
