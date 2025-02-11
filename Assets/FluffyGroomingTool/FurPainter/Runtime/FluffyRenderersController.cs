using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if HAS_PACKAGE_UNITY_HDRP_15_0_7
using UnityEngine.Rendering.HighDefinition;
#endif
using RayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode;

namespace FluffyGroomingTool {
    public class FluffyRenderersController {
        internal GameObject hairMeshRendererObject;
        internal GameObject motionVectorRendererObject;
        public StrandRenderType currentStrandRenderType;

        private MeshRenderer hairMeshMeshRenderer;
        private MeshRenderer motionVectorMeshRenderer;
#if HAS_PACKAGE_UNITY_HDRP_15_0_7
        private HDAdditionalMeshRendererSettings additionalMrSettings;
#endif
        public void createRendererObject(
            bool isHdrp,
            bool isUrp,
            Material motionVectorMaterial,
            int verticesCount,
            int[] indices,
            StrandRenderType strandRenderType  
        ) {
            currentStrandRenderType = strandRenderType;
            hairMesh = createFurMesh(verticesCount, indices, strandRenderType);
            if (hairMeshRendererObject == null) {
                hairMeshRendererObject = new GameObject();
            }

            hairMeshRendererObject.hideFlags = HideFlags.HideAndDontSave;
            hairMeshMeshRenderer = getMeshRenderer(hairMeshRendererObject);

#if HAS_PACKAGE_UNITY_HDRP_15_0_7
            additionalMrSettings = hairMeshRendererObject.GetComponent<HDAdditionalMeshRendererSettings>() ??
                                   hairMeshRendererObject.AddComponent<HDAdditionalMeshRendererSettings>();
#endif


            //This should really be MotionVectorGenerationMode.None. But that apparently makes the motionVectorMeshRenderer disappear.
            hairMeshMeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            if (isHdrp && SystemInfo.supportsRayTracing) {
                hairMeshMeshRenderer.rayTracingMode = RayTracingMode.DynamicGeometry;
            }

            setupRenderer(hairMeshRendererObject, "FurRenderer", hairMeshMeshRenderer);

            if (!isUrp) {
                if (motionVectorRendererObject == null) motionVectorRendererObject = new GameObject();
                motionVectorRendererObject.hideFlags = HideFlags.HideAndDontSave;
                motionVectorMeshRenderer = getMeshRenderer(motionVectorRendererObject);
                motionVectorMeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
                motionVectorMeshRenderer.material = motionVectorMaterial;
                setupRenderer(motionVectorRendererObject, "MotionVectorRenderer", motionVectorMeshRenderer);
            }
        }

        public void setHqLineRenderingEnabled(bool enabledQhLineRenderer) {
#if HAS_PACKAGE_UNITY_HDRP_15_0_7
            if (additionalMrSettings != null) {
                additionalMrSettings.enableHighQualityLineRendering = enabledQhLineRenderer;
                motionVectorRendererObject.SetActive(false);
            }
#endif
        }

        internal Mesh hairMesh;
        internal GraphicsBuffer hairMeshBuffer;

        private Mesh createFurMesh(int verticesCount, int[] indices, StrandRenderType strandRenderType = StrandRenderType.TRIANGLE_MESH) {
            if (hairMesh == null) {
                hairMesh = new Mesh();
            }

            var emptyVec2List = new Vector2[verticesCount];
            var emptyVec3List = new Vector3[verticesCount];
            hairMesh.hideFlags = HideFlags.HideAndDontSave;
            hairMesh.indexFormat = IndexFormat.UInt32;
            hairMesh.vertices = emptyVec3List;
            hairMesh.normals = emptyVec3List;
            hairMesh.tangents = new Vector4[verticesCount];
            hairMesh.colors = new Color[verticesCount];
            hairMesh.uv = emptyVec2List;
            hairMesh.uv2 = emptyVec2List;
            //Uv 3-4 is used to store the previous mesh vertex position for use with Motion Vector.
            hairMesh.uv3 = emptyVec2List;
            hairMesh.uv4 = emptyVec2List;
            if (strandRenderType == StrandRenderType.LINE) {
                hairMesh.SetIndices(indices, MeshTopology.Lines, 0);
            } else {
                hairMesh.triangles = indices;
            }

            hairMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            hairMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            hairMeshBuffer = hairMesh.GetVertexBuffer(0);
            return hairMesh;
        }

        public static MeshRenderer getMeshRenderer(GameObject gameObject) {
            var mr = gameObject.GetComponent<MeshRenderer>();
            return mr ? mr : gameObject.AddComponent<MeshRenderer>();
        }


        private void setupRenderer(GameObject target, string extensionName, Renderer renderer) {
            var mf = target.GetComponent<MeshFilter>();
            var meshFilter = mf ? mf : target.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = hairMesh;
            meshFilter.hideFlags = HideFlags.DontSave;
            target.name = "FluffyRenderer" + extensionName;
#if UNITY_EDITOR
            EditorUtility.SetSelectedRenderState(renderer, EditorSelectedRenderState.Hidden);
#endif
        }

        public void setupRenderers(
            bool isCreateMeshPass,
            Material material,
            Vector3 position,
            Renderer currentRenderer,
            bool isUrp,
            bool isMotionVectorsEnabled
        ) {
            if (!isCreateMeshPass) {
                if (material != null && hairMeshMeshRenderer != null) {
                    hairMeshMeshRenderer.sharedMaterial = material;
                    hairMeshMeshRenderer.bounds = calculateBounds(position, currentRenderer);
                    hairMeshRendererObject.transform.position = Vector3.zero;
                    hairMeshRendererObject.transform.rotation = Quaternion.identity;
                    if (currentRenderer != null && currentRenderer.gameObject.layer != hairMeshRendererObject.layer) {
                        hairMeshRendererObject.layer = currentRenderer.gameObject.layer;
                    }

                    if (currentRenderer != null && hairMeshMeshRenderer.renderingLayerMask != currentRenderer.renderingLayerMask) {
                        hairMeshMeshRenderer.renderingLayerMask = currentRenderer.renderingLayerMask;
                    }

                    if (!isUrp) {
                        motionVectorMeshRenderer.bounds = hairMeshMeshRenderer.bounds;
                        motionVectorRendererObject.transform.position = Vector3.zero;
                        motionVectorRendererObject.transform.rotation = Quaternion.identity;
                        setMotionVectorEnabledState(false, isMotionVectorsEnabled);
                    }
                }
            }
        }

        /**
         * We currently just guesstimate the bounds. We should calculate that in the compute shader but it's so darn expensive to do.
         *  Maybe we should expose the params?
         */
        private static Bounds calculateBounds(Vector3 position, Renderer currentRenderer) {
            if (currentRenderer == null) return new Bounds(position, Vector3.one * 3);
            var currentRendererBounds = currentRenderer.bounds;
            return new Bounds(currentRendererBounds.center, currentRendererBounds.size * 3);
        }

        private void setMotionVectorEnabledState(bool isUrp, bool isMotionVectorsEnabled) {
            if (isUrp && motionVectorRendererObject.activeSelf) {
                motionVectorRendererObject.SetActive(false);
                hairMeshMeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                return;
            }

            if (isMotionVectorsEnabled && !motionVectorRendererObject.activeSelf) {
                motionVectorRendererObject.SetActive(true);
                //This should really be MotionVectorGenerationMode.ForceNoMotion. But that apparently makes the motionVectorMeshRenderer disappear.
                hairMeshMeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            } else if (!isMotionVectorsEnabled && motionVectorRendererObject.activeSelf) {
                motionVectorRendererObject.SetActive(false);
                hairMeshMeshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }
        }

        public void clearObjects() {
            hairMeshRendererObject = null;
            motionVectorRendererObject = null;
        }

        public void destroy() {
            Object.DestroyImmediate(hairMeshRendererObject);
            if (motionVectorRendererObject != null) Object.DestroyImmediate(motionVectorRendererObject);
            hairMeshBuffer?.Dispose();
            Object.DestroyImmediate(hairMesh);
            hairMeshBuffer = null;
        }

        public void disableRenderer() {
            hairMeshMeshRenderer.enabled = false;
        }

        public void enableRenderer() {
            hairMeshMeshRenderer.enabled = true;
        }

        public bool isReady() {
            return hairMeshRendererObject != null;
        }

        public int getVertexBufferStride() {
            return hairMesh.GetVertexBufferStride(0);
        }

        public Bounds getRendererBounds() {
            return hairMeshMeshRenderer.bounds;
        }
    }
}