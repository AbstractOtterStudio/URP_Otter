using System;
using System.Collections.Generic;
using System.Linq;
using FluffyGroomingTool;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;


[PreferBinarySerialization]
public class FurContainer : ScriptableObject {
    [SerializeField, HideInInspector] public int id = -1324198676 + Guid.NewGuid().GetHashCode();
    [SerializeField, HideInInspector] public HairStrandLayer[] layerStrandsList = new HairStrandLayer[0];
    [SerializeField, HideInInspector] public FurLodProperties[] furLods;
    [SerializeField, HideInInspector] public float culledDistance = 1000;
    public bool NeedsUpdate { get; set; }
    [SerializeField, HideInInspector] public UnityEvent recreateAll = new UnityEvent();
    [SerializeField, HideInInspector] public string groomContainerGuid;
    public float worldScale = 1f;

    private void OnValidate() {
        initLods();
    }

    private void initLods() {
        if (furLods == null || furLods.Length == 0) {
            furLods = new FurLodProperties[3];
            furLods[0] = new FurLodProperties() {
                strandsScale = 1f,
                skipStrandsCount = 1,
                startDistance = 0
            };
            furLods[1] = new FurLodProperties() {
                strandsScale = 2f,
                skipStrandsCount = 3,
                startDistance = 10
            };
            furLods[2] = new FurLodProperties {
                strandsScale = 4f,
                skipStrandsCount = 8,
                startDistance = 20
            };
        }
    }

    public int[] TriangleIndexArray { get; set; }

    public void initLodTriangleIndices(HairStrandLayer[] layers) {
        initLods();
        var offset = 0;
        List<int> lodTriangles = new List<int>();
        foreach (var layer in layers) {
            var triangleIndices = createProceduralTrianglesIndices(layer.CardMesh, layer.layerHairStrands.Length, 1, offset);
            offset += layer.layerHairStrands.Length * layer.CardMesh.vertexCount;
            lodTriangles.AddRange(triangleIndices);
        }

        TriangleIndexArray = lodTriangles.ToArray();
    }

    private static List<int> createProceduralTrianglesIndices(Mesh mesh, int hairStrandsCount, int skipStrandCount, int offset) {
        var cardMeshVertCount = mesh.vertexCount;
        var triangles = new List<int>();
        var cardMeshTriangles = mesh.triangles;

        for (var i = 0; i < hairStrandsCount; i++) {
            var offsetTotal = i * cardMeshVertCount + offset;
            triangles.AddRange(cardMeshTriangles.Select(t => t + offsetTotal));
        }

        return triangles;
    }

    public void update() {
        if (NeedsUpdate) {
            NeedsUpdate = false;
            recreateHairStrandsBuffers();
            foreach (var strandLayer in layerStrandsList) {
                foreach (var clumpsModifier in strandLayer.clumpsModifiers) {
                    clumpsModifier.recreateClumpBuffer();
                    clumpsModifier.createClumpAttractionBuffer(strandLayer.cardMeshProperties.getCardMeshVerticesY());
                }
            }

            updateClumpAttrationCurveBuffer();
            initLodTriangleIndices(layerStrandsList);
        }
    }

    public void updateClumpAttrationCurveBuffer() {
        foreach (var strandLayer in layerStrandsList) {
            foreach (var clumpModifier in strandLayer.clumpsModifiers) {
                clumpModifier.updateClumpAttractionBuffer();
            }
        }
    }

    public void recreateHairStrandsBuffers() {
        foreach (var layer in layerStrandsList) {
            layer.recreateHairStrandsBuffers();
        }
    }

    public void copyValuesFromComputeBufferToNativeObject() {
        foreach (var layer in layerStrandsList) {
            layer.copyValuesFromComputeBufferToNativeObject();
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void copyClumpValuesFromComputeBufferToNativeObject() {
        foreach (var strandLayer in layerStrandsList) {
            foreach (var clumpsModifier in strandLayer.clumpsModifiers) {
                clumpsModifier.copyFromComputeBufferToNativeObject();
            }
        }
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public int getLayerStrandsCount(int index) {
        var layerIndex = Math.Min(layerStrandsList.Length - 1, index);
        return layerStrandsList[layerIndex].layerHairStrands.Length;
    }

    public void dispatchClumpsKernel(ComputeShader computeShader, int updateClumpsKernel, FurRenderer renderer) {
        foreach (var strandLayer in layerStrandsList) {
            for (var i = 0; i < strandLayer.clumpsModifiers.Length; i++) {
                var clumpsModifier = strandLayer.clumpsModifiers[i];
                if (i == 0) {
                    clumpsModifier.dispatchParentClumpKernel(computeShader, updateClumpsKernel, renderer, strandLayer.cardMeshProperties);
                }
                else {
                    clumpsModifier.dispatchChildClumpKernel(strandLayer.clumpsModifiers[i - 1], computeShader, updateClumpsKernel, renderer,
                        strandLayer.cardMeshProperties);
                }
            }
        }
    }

    public void removeLayer(int index) {
        var list = layerStrandsList.ToList();
        list[index].dispose();
        list.RemoveAt(index);
        layerStrandsList = list.ToArray();
    }

    public ComputeBuffer getLayerStrandsBuffer(int index) {
        return layerStrandsList[index].hairStrandsBuffer;
    }

    public void disposeBuffers() {
        foreach (var strandLayer in layerStrandsList) {
            strandLayer.dispose();
        }
    }

    public int getCombinedVerticesCount() {
        return Math.Max(layerStrandsList.Sum(layer => layer.layerHairStrands.Length * layer.CardMesh.vertexCount), 1);
    }

    public void recreateCardMeshes() {
        foreach (var layer in layerStrandsList) {
            layer.recreateCardMesh();
        }
    }

    public void removeClumpModifier(int layerIndex, int clumpIndex) {
        var hairStrandLayer = layerStrandsList[layerIndex];
        var clumpModifierLayers = hairStrandLayer.clumpsModifiers.ToList();
        clumpModifierLayers[clumpIndex].dispose();
        clumpModifierLayers.RemoveAt(clumpIndex);
        hairStrandLayer.clumpsModifiers = clumpModifierLayers.ToArray();
    }

    public void duplicateLayer(int index) {
        var hairStrandLayers = layerStrandsList.ToList();
        hairStrandLayers.Add((HairStrandLayer) layerStrandsList[index].Clone());
        layerStrandsList = hairStrandLayers.ToArray();
    }

    public void regenerateID() {
        id = -1324198676 + Guid.NewGuid().GetHashCode();
    }
}