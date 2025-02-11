
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FluffyGroomingTool {
    [ExecuteAlways]
    public class SceneDisk : MonoBehaviour {
        private Mesh disk;
        private Material material; 

        public static SceneDisk createDisk(string diskName) {
            GameObject go = new GameObject();
            go.name = diskName;
            go.SetActive(false);
            return go.AddComponent<SceneDisk>();
        }

        private void Reset() {
            var meshFilter = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshFilter>();
            disk = meshFilter.sharedMesh;
            DestroyImmediate(meshFilter.gameObject);
            material = new Material(Shader.Find("Hidden/BrushCircleShader"));
            renderParams = new RenderParams(material);
            gameObject.hideFlags = HideFlags.HideAndDontSave; 
        }

        public void setup(MeshProperties raycastHit, float size, float falloffBrushDiscSize, float fillOpacity) {
#if UNITY_EDITOR
            var thisTransform = transform;
            thisTransform.position = raycastHit.sourceVertex;
            thisTransform.up = raycastHit.sourceNormal;
            thisTransform.Rotate(new Vector3(1, 0, 0), 90);
            var scale = size * 2;
            float falloffScale = falloffBrushDiscSize * 2 / scale;
            thisTransform.localScale = new Vector3(scale, scale, scale);
            if (material == null) material = new Material(Shader.Find("Hidden/BrushCircleShader"));
            renderParams = new RenderParams(material);
            material.SetFloat("circleObjectScale", scale);
            material.SetFloat("falloffScale", falloffScale / 2);
            material.SetFloat("fillOpacity", fillOpacity);
            
            if (!needsToDrawInUpdateDueToUrpBug()) {
                if (disk == null) { 
                    Reset();
                }
                material.SetPass(0);
                Graphics.DrawMeshNow(disk, transform.localToWorldMatrix);
            }
#endif
        }

        private RenderParams renderParams;
#if UNITY_EDITOR
        private void Update() {
            if (needsToDrawInUpdateDueToUrpBug()) {
                Graphics.RenderMesh(renderParams, disk, 0, transform.localToWorldMatrix);
            }
        }

        private bool needsToDrawInUpdateDueToUrpBug() {
            return SceneView.lastActiveSceneView.drawGizmos;
        }
#endif
    }
}