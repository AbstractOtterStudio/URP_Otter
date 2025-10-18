#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(Terrain))]
public class TerrainLayerDebugger : MonoBehaviour
{
    [Header("Visualization Settings")]
    public bool visualize = true;
    public float cubeSize = 0.8f;
    public int step = 4;

    [Header("Layer Filtering")]
    public List<string> visualizedLayers = new List<string>();  // Only visualize these layers

    private static readonly Color[] LayerColors = new Color[]
    {
        Color.red, Color.green, Color.blue, Color.yellow,
        Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f),
        Color.white, Color.gray
    };

    void OnDrawGizmos()
    {
        if (!visualize) return;

#if UNITY_EDITOR
        SceneView.RepaintAll(); // Force scene view refresh
#endif

        Terrain terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;
        if (terrainData == null) return;

        Vector3 terrainPos = terrain.transform.position;
        int res = terrainData.alphamapResolution;
        float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, res, res);
        int numLayers = alphamaps.GetLength(2);

        for (int y = 0; y < res; y += step)
        {
            for (int x = 0; x < res; x += step)
            {
                float normX = x / (float)(res - 1);
                float normY = y / (float)(res - 1);
                float height = terrainData.GetInterpolatedHeight(normX, normY);

                Vector3 worldPos = terrainPos + new Vector3(normX * terrainData.size.x, height, normY * terrainData.size.z);

                for (int layer = 0; layer < numLayers; layer++)
                {
                    TerrainLayer terrainLayer = terrainData.terrainLayers[layer];
                    if (terrainLayer == null) continue;

                    string layerName = terrainLayer.name;

                    // Skip if not in the selected layer list
                    if (visualizedLayers.Count > 0 && !visualizedLayers.Contains(layerName))
                        continue;

                    float weight = alphamaps[y, x, layer];
                    if (weight > 0.05f)
                    {
                        Gizmos.color = GetColorForLayer(layer, weight);
                        Gizmos.DrawCube(worldPos + Vector3.up * 0.2f, Vector3.one * cubeSize);
                    }
                }
            }
        }
    }

    private Color GetColorForLayer(int index, float alpha)
    {
        Color baseColor = LayerColors[index % LayerColors.Length];
        baseColor.a = alpha;
        return baseColor;
    }
}
