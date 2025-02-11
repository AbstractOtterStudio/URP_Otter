using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;

#endif


namespace FluffyGroomingTool {
    public static class FurCreatorExtensions {
        private const string saveGroomText = "Would you also like to save the fur GroomContainer in case you want to make changes later on?" +
                                             " (This will create an asset for the groom, and requires extra disk space)";

        private const string saveHeading = "Save The FurContainer";
        private const string saveHeadingGroom = "Save The GroomContainer";

        public static void finalizeGroomAndSaveFur(this FurCreator f) {
            f.permanentlyDeleteMaskedStrands(() => {
#if UNITY_EDITOR
                f.clearWindSelection();
                var gameObject = f.gameObject;
                gameObject.SetActive(false);
                var furRenderer = f.FurRenderer;
                var path = EditorUtility.SaveFilePanel(saveHeading, "Assets/", gameObject.name + "FurContainer", "asset");
                if (!string.IsNullOrEmpty(path)) {
                    createNewId(furRenderer);
                    path = FileUtil.GetProjectRelativePath(path);

                    furRenderer.furContainer.disposeBuffers();
                    var existingFurContainer = AssetDatabase.LoadAssetAtPath<FurContainer>(path);
                    if (existingFurContainer != null && existingFurContainer != furRenderer.furContainer) {
                        AssetDatabase.DeleteAsset(path);
                        saveFurContainer(furRenderer, path);
                    } else if (existingFurContainer == null) {
                        saveFurContainer(furRenderer, path);
                    }

                    if (EditorUtility.DisplayDialog($"{saveHeadingGroom}?", saveGroomText, "Yes", "No")) {
                        var pathGroom = EditorUtility.SaveFilePanel(saveHeadingGroom,
                            path.Substring(0, path.LastIndexOf('/')), gameObject.name + "GroomContainer", "asset");
                        if (!string.IsNullOrEmpty(pathGroom)) {
                            pathGroom = FileUtil.GetProjectRelativePath(pathGroom);

                            var existingGroom = AssetDatabase.LoadAssetAtPath<GroomContainer>(pathGroom);
                            f.groomContainer.disposeBuffers();
                            if (existingGroom != null && existingGroom != f.groomContainer) {
                                AssetDatabase.DeleteAsset(pathGroom);
                                AssetDatabase.CreateAsset(Object.Instantiate(f.groomContainer), pathGroom);
                            } else if (existingGroom == null) {
                                AssetDatabase.CreateAsset(Object.Instantiate(f.groomContainer), pathGroom);
                            }

                            furRenderer.furContainer.groomContainerGuid = AssetDatabase.AssetPathToGUID(pathGroom);
                        }
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorUtility.FocusProjectWindow();
                    furRenderer.CurrentRenderer = null;
                    gameObject.SetActive(true);
                    furRenderer.furContainer.recreateAll.Invoke();
                    Object.DestroyImmediate(f);
                }

                gameObject.SetActive(true);
#endif
            });
        }

        private static void clearWindSelection(this FurCreator f) {
            if (f.getPainterProperties().type == (int)PaintType.WIND_MAX_DISTANCE) {
                f.getPainterProperties().type = (int)PaintType.HEIGHT;
                f.groomContainer.needsUpdate = true;
                f.groomContainer.update();
                f.FurRenderer.drawWindContribution = false;
            }
        }

        private static void createNewId(FurRenderer furRenderer) {
            furRenderer.furContainer.regenerateID();
            furRenderer.currentFurContainerID = furRenderer.furContainer.id;
        }

        private static void saveFurContainer(FurRenderer furRenderer, string path) {
#if UNITY_EDITOR
            var containerCopy = Object.Instantiate(furRenderer.furContainer);
            AssetDatabase.CreateAsset(containerCopy, path);
            furRenderer.furContainer = containerCopy;
            Selection.activeObject = containerCopy;
#endif
        }
    }

    public class FurMeshCreator {
        private IEnumerator meshCoroutine;
        private Stopwatch sw;
        private const string saveMeshAsset = "Save Mesh Asset";


        public void createMesh(FurCreator furCreator) {
#if UNITY_EDITOR
            sw = Stopwatch.StartNew();
            var path = EditorUtility.SaveFilePanel(saveMeshAsset, "Assets/", furCreator.name + "FurMesh", "asset");
            if (!string.IsNullOrEmpty(path)) {
                var newMesh = createProceduralMesh(furCreator);
                Undo.RecordObject(furCreator.gameObject, "Undo");

                newMesh.RecalculateBounds();
                newMesh.RecalculateTangents();
                newMesh.boneWeights = createBoneWeights(furCreator, newMesh);
                newMesh.bindposes = furCreator.FurRenderer.meshBaker.sourceMesh.bindposes;
                furCreator.enabled = false;
                furCreator.FurRenderer.enabled = false;
                furCreator.FurRenderer.meshBaker.sourceMesh = null;
                saveMeshAndCreateNewGameObject(furCreator.gameObject, newMesh, path, furCreator.FurRenderer.material);
            }
#endif
        }

        private static BoneWeight[] createBoneWeights(FurCreator furCreator, Mesh newMesh) {
            var sourceMeshVertices = furCreator.FurRenderer.meshBaker.sourceMesh.vertices;
            var sourceMeshBoneWeights = furCreator.FurRenderer.meshBaker.sourceMesh.boneWeights;

            var newMeshVertices = newMesh.vertices;
            var newMeshBoneWeights = new List<BoneWeight>();

            var layerVertexStartIndex = 0;
            foreach (var layer in furCreator.FurRenderer.furContainer.layerStrandsList) {
                var layerVerticesCount = layer.hairStrandsBuffer.count * layer.CardMesh.vertexCount;

                for (var i = 0; i < layerVerticesCount; i++) {
                    var proceduralMeshIndex = layerVertexStartIndex + i;
                    var cardMeshVertexCount = layer.CardMesh.vertexCount;
                    var hairStrandIndex = (int)Math.Floor((float)i / cardMeshVertexCount);

                    var vertexPos = newMeshVertices[proceduralMeshIndex];

                    if (sourceMeshBoneWeights.Length > 0) {
                        newMeshBoneWeights.Add(cetClosestBoneWeight(layer, hairStrandIndex, sourceMeshVertices, sourceMeshBoneWeights, vertexPos));
                    }
                }

                layerVertexStartIndex += layerVerticesCount;
            }

            return newMeshBoneWeights.ToArray();
        }

        private static BoneWeight cetClosestBoneWeight(
            HairStrandLayer layer,
            int hairStrandIndex,
            Vector3[] sourceMeshVertices,
            BoneWeight[] sourceMeshBoneWeights,
            Vector3 vertex
        ) {
            var barycentricTriangleIndex = new[] {
                (int)layer.layerHairStrands[hairStrandIndex].triangles.x,
                (int)layer.layerHairStrands[hairStrandIndex].triangles.y,
                (int)layer.layerHairStrands[hairStrandIndex].triangles.z
            };
            var barycentricVertsPos = new[] {
                sourceMeshVertices[barycentricTriangleIndex[0]],
                sourceMeshVertices[barycentricTriangleIndex[1]],
                sourceMeshVertices[barycentricTriangleIndex[2]]
            };

            var minDistance = float.MaxValue;

            var closestBoneWeight = sourceMeshBoneWeights[barycentricTriangleIndex[0]];
            for (var j = 0; j < 3; j++) {
                var distance = Vector3.Distance(vertex, barycentricVertsPos[j]);
                if (distance < minDistance) {
                    minDistance = distance;
                    closestBoneWeight = sourceMeshBoneWeights[barycentricTriangleIndex[j]];
                }
            }

            return closestBoneWeight;
        }

        struct MeshBufferData {
            public Vector3 vertices;
            public Vector3 normals;
            public Vector4 tangents;
            public Vector4 colors;
            public Vector2 uv1;
            public Vector2 uv2;
            public Vector2 uv3;
            public Vector2 uv4;
        }

        private static Mesh createProceduralMesh(FurCreator furCreator) {
            var newMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            furCreator.FurRenderer.IsCreateMeshPass = true;
            furCreator.FurRenderer.furRendererSettings.IsCreateMeshPass = true;
            furCreator.FurRenderer.updateAndRenderFur();
            furCreator.FurRenderer.IsCreateMeshPass = false;
            furCreator.FurRenderer.furRendererSettings.IsCreateMeshPass = false;

            //This is kind of dump, but since the C# side of the mesh knows nothing of our mesh buffer modifications, we need this extra step.
            var furRendererFurMesh = furCreator.FurRenderer.renderersController.hairMesh;
            var furMeshVertexCount = furRendererFurMesh.vertexCount;
            var copiedData = new ComputeBuffer(furMeshVertexCount, Marshal.SizeOf<MeshBufferData>());

            var cs = Resources.Load<ComputeShader>("MeshBufferToNativeCode");
            var vertexBufferStride = furRendererFurMesh.GetVertexBufferStride(0);
            cs.SetInt("vertexBufferStride", vertexBufferStride);
            cs.SetBuffer(0, "sourceMeshData", furCreator.FurRenderer.renderersController.hairMeshBuffer);
            cs.SetBuffer(0, "copiedData", copiedData);

            cs.Dispatch(0, furMeshVertexCount.toCsGroups(), 1, 1);
            var data = new MeshBufferData[furMeshVertexCount];
            copiedData.GetData(data);

            newMesh.vertices = data.Select(it => it.vertices).ToArray();
            newMesh.normals = data.Select(it => it.normals).ToArray();
            newMesh.tangents = data.Select(it => it.tangents).ToArray();
            newMesh.colors = data.Select(it => new Color(it.colors.x, it.colors.y, it.colors.z, it.colors.w)).ToArray();
            newMesh.uv = data.Select(it => it.uv1).ToArray();
            newMesh.uv2 = data.Select(it => it.uv2).ToArray();
            newMesh.uv3 = data.Select(it => it.uv3).ToArray();
            newMesh.uv4 = data.Select(it => it.uv4).ToArray();
            var newMeshTriangles = furRendererFurMesh.triangles;
            newMesh.triangles = newMeshTriangles;
            copiedData.Dispose();
            return newMesh;
        }

        private void saveMeshAndCreateNewGameObject(GameObject gameObject, Mesh newMesh, string path, Material finalMaterial) {
#if UNITY_EDITOR
            path = FileUtil.GetProjectRelativePath(path);
            AssetDatabase.CreateAsset(newMesh, path);
            AssetDatabase.SaveAssets();
            newMesh = (Mesh)AssetDatabase.LoadAssetAtPath(path, typeof(Mesh));
            var furObjectName = getFurObjectName(gameObject);
            var parent = gameObject.transform.parent;
            var existingObject = PainterUtils.findExistingFurObject(furObjectName, parent);
            var furObject = existingObject ? existingObject.gameObject : new GameObject();

            setParentAndTransform(gameObject, parent, furObject);
            furObject.name = furObjectName;
            furObject.SetActive(true);

            var sourceSkinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (sourceSkinnedMeshRenderer != null) {
                var existingSmr = furObject.GetComponent<SkinnedMeshRenderer>();
                var skinnedMeshRenderer = existingSmr ? existingSmr : furObject.AddComponent<SkinnedMeshRenderer>();
                skinnedMeshRenderer.sharedMesh = newMesh;
                skinnedMeshRenderer.rootBone = sourceSkinnedMeshRenderer.rootBone;
                skinnedMeshRenderer.bones = sourceSkinnedMeshRenderer.bones;
                skinnedMeshRenderer.material = finalMaterial;
            } else {
                var existingMf = furObject.GetComponent<MeshFilter>();
                MeshFilter mf = existingMf ? existingMf : furObject.AddComponent<MeshFilter>();
                mf.sharedMesh = newMesh;
                var existingMr = furObject.GetComponent<MeshRenderer>();
                MeshRenderer mr = existingMr ? existingMr : furObject.AddComponent<MeshRenderer>();
                mr.material = finalMaterial;
            }

            sw.Stop();

            Debug.Log("Created mesh in: " + sw.ElapsedMilliseconds / 1000 + " seconds");
#endif
        }

        private static void setParentAndTransform(GameObject gameObject, Transform parent, GameObject furObject) {
            if (parent) {
                furObject.transform.parent = parent;
                furObject.transform.position = gameObject.transform.position;
                furObject.transform.rotation = gameObject.transform.rotation;
                furObject.transform.localScale = gameObject.transform.localScale;
            } else {
                furObject.transform.parent = gameObject.transform;
                furObject.transform.localPosition = Vector3.zero;
                furObject.transform.localRotation = Quaternion.identity;
                furObject.transform.localScale = Vector3.one;
            }
        }

        public static string getFurObjectName(GameObject gameObject) {
            return gameObject.name + "Fur";
        }

        private static float CARD_MESH_HEIGHT = 0.12f;
        private static float CARD_MESH_WIDTH = 0.19f;

        public static Mesh CreateHairCard(CardMeshProperties cardMeshProperties) {
            var mesh = new Mesh();
            mesh.name = "Fur Card";
            var vertices = new Vector3[(cardMeshProperties.cardSubdivisionsX + 1) * (cardMeshProperties.cardSubdivisionsY + 1)];
            Vector2[] uv = new Vector2[vertices.Length];
            Vector2[] uv2 = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];
            for (int i = 0, y = 0; y <= cardMeshProperties.cardSubdivisionsY; y++) {
                for (int x = 0; x <= cardMeshProperties.cardSubdivisionsX; x++, i++) {
                    var yPercent = (float)y / cardMeshProperties.cardSubdivisionsY;
                    var xPercent = (float)x / cardMeshProperties.cardSubdivisionsX;
                    float widthMultiplier = getWidthMultiplier(yPercent, cardMeshProperties);
                    vertices[i] = new Vector3(0, CARD_MESH_HEIGHT * yPercent,
                        (-CARD_MESH_WIDTH / 2f + CARD_MESH_WIDTH * xPercent) * widthMultiplier);
                    var moveAbility = y == 0 ? 0 : cardMeshProperties.moveCurve.Evaluate(yPercent);

                    colors[i] = new Color(moveAbility, y, 0, 1);

                    uv[i] = new Vector2(xPercent / 2f, yPercent / 2f);
                    uv2[i] = new Vector2(xPercent, yPercent);
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.uv2 = uv2;

            int[] triangles = new int[cardMeshProperties.cardSubdivisionsX * cardMeshProperties.cardSubdivisionsY * 6];
            for (int ti = 0, vi = 0, y = 0; y < cardMeshProperties.cardSubdivisionsY; y++, vi++) {
                for (int x = 0; x < cardMeshProperties.cardSubdivisionsX; x++, ti += 6, vi++) {
                    triangles[ti] = vi;
                    triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                    triangles[ti + 4] = triangles[ti + 1] = vi + cardMeshProperties.cardSubdivisionsX + 1;
                    triangles[ti + 5] = vi + cardMeshProperties.cardSubdivisionsX + 2;
                }
            }

            mesh.colors = colors;
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        public static int[] generateTriangleIndicesForLine(int strandPoints, int strandsCount) {
            var cardSubdivisionsY = (strandPoints - 1) * 2;

            var triangleIndicesCount = cardSubdivisionsY * strandsCount;
            var allTriangles = new int[triangleIndicesCount];
            
            for (var i = 0; i < strandsCount; i++) {
                var triStartIndex = i * cardSubdivisionsY;
                var offset = i;
                for (int j = 0; j < cardSubdivisionsY; j += 2) {
                    allTriangles[triStartIndex + j] = triStartIndex / 2 + j / 2 + offset;
                    allTriangles[triStartIndex + j + 1] = triStartIndex / 2 + j / 2 + 1 + offset;
                }
        
            }

            return allTriangles;
        }

        private static float getWidthMultiplier(float yPercent, CardMeshProperties cardMeshProperties) {
            return cardMeshProperties.shapeCurve.Evaluate(yPercent);
        }

        public static int[] generateTriangleIndicesForStrands(int strandPoints, int strandsCount) {
            var cardSubdivisionsY = strandPoints - 1;

            var triangleIndicesCount = cardSubdivisionsY * 6 * strandsCount;
            var allTriangles = new int[triangleIndicesCount];

            for (var i = 0; i < cardSubdivisionsY * strandsCount; i++) {
                var triStartIndex = i * 6;
                var offset = (int)Mathf.Floor((float)i / cardSubdivisionsY);
                var vStartIndex = i * 2 + offset * 2;
                allTriangles[triStartIndex] = vStartIndex;
                allTriangles[triStartIndex + 1] = vStartIndex + 2;
                allTriangles[triStartIndex + 2] = vStartIndex + 1;

                allTriangles[triStartIndex + 3] = vStartIndex + 2;
                allTriangles[triStartIndex + 4] = vStartIndex + 3;
                allTriangles[triStartIndex + 5] = vStartIndex + 1;
            }

            return allTriangles;
        }
    }
}