using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluffyGroomingTool {
    [ExecuteAlways, Serializable]
    public class SDFCollider : MonoBehaviour {
        private SDFColliderCommon sdfColliderCommon;
        public VerletSimulationSettings verletSimulationSettings;
        private MeshBaker meshBaker;
        public bool isStatic;
        private bool isFirst;
        private Renderer currentRenderer;


        private void OnEnable() {
            currentRenderer = GetComponent<Renderer>();

#if UNITY_EDITOR
            
            if (currentRenderer == null) {
                if (UnityEditor.EditorUtility.DisplayDialog("Missing Renderer",
                    "SDF Colliders can only be added GameObjects with a MeshRenderer or SkinnedMeshRenderer attached.", "OK")) { }

                gameObject.AddComponent<DestroyObject>().destroyTarget = this;
            }
#endif

            if (Application.isPlaying) {
                initialize();
                RenderPipelineManager.beginFrameRendering += beginFrameRendering;
                Camera.onPreRender += cameraPreRender;
                sdfColliderCommon.debug = true;
            }
        }

        private void initialize() {
            isFirst = true;
            meshBaker = new MeshBaker(gameObject, Instantiate(Resources.Load<ComputeShader>(ShaderID.MESH_BAKER_CS_NAME)),true);
            verletSimulationSettings ??= new VerletSimulationSettings();
            sdfColliderCommon = new SDFColliderCommon(GetComponent<Renderer>(), meshBaker, verletSimulationSettings);
        }

        private void beginFrameRendering(ScriptableRenderContext rc, Camera[] cameras) {
            updateSDF();
        }

        private void updateSDF() {
            if ((!isStatic || isFirst) && !verletSimulationSettings.isUnsupportedSDFPlatform && isActiveAndEnabled) {
                meshBaker.bakeSkinnedMesh(false);
                sdfColliderCommon.createSDF(transform, null, 0);
                isFirst = false;
            }
        }

        private void OnDisable() {
            if (Application.isPlaying) {
                clearResources();
                RenderPipelineManager.beginFrameRendering -= beginFrameRendering;
                Camera.onPreRender -= cameraPreRender;
            }
        }

        private void cameraPreRender(Camera cam) {
            updateSDF();
        }

        private void clearResources() {
            meshBaker?.dispose();
            sdfColliderCommon?.dispose();
            meshBaker = null;
            sdfColliderCommon = null;
        }

        private Bounds collideBounds;

        public void collideWith(ComputeBuffer nodesBuffer, Bounds furOrHairRendererBounds, GraphicsBuffer furMeshBuffer, int vertexBufferStride) {
            collideBounds.center = furOrHairRendererBounds.center;
            collideBounds.extents = furOrHairRendererBounds.extents + Vector3.one;
            if (isActiveAndEnabled && Application.isPlaying && currentRenderer.bounds.Intersects(collideBounds)) {
                sdfColliderCommon?.collideWith(nodesBuffer, furMeshBuffer, vertexBufferStride);
            }
        }

        public void recreateSDF() {
            if (Application.isPlaying) {
                clearResources();
                initialize();
            }
        }
    }

    [ExecuteAlways]
    public class DestroyObject : MonoBehaviour {
        public SDFCollider destroyTarget;

        private void Update() {
            DestroyImmediate(destroyTarget);
            DestroyImmediate(this);
        }
    }
}