using UnityEngine;

namespace FluffyGroomingTool {
    //Don't reorder these since their index is used in switch cases. Go in the skammekrok. 

    public static class PainterUtils {
        public static Mesh getMeshWithoutTransform(GameObject aGO) {
            Mesh curMesh = null;
            if (aGO) {
                MeshFilter meshFilter = aGO.GetComponent<MeshFilter>();
                SkinnedMeshRenderer skinnedMeshRenderer = aGO.GetComponent<SkinnedMeshRenderer>();

                if (meshFilter && !skinnedMeshRenderer) {
                    curMesh = meshFilter.sharedMesh;
                }

                if (!meshFilter && skinnedMeshRenderer) {
                    curMesh = skinnedMeshRenderer.sharedMesh;
                }
            }

            return curMesh;
        }
        public static GameObject findExistingFurObject(string furObjectName, Transform parent) {
            if (parent != null) return parent.Find(furObjectName)?.gameObject;
            return GameObject.Find(furObjectName);
        }
    }
}