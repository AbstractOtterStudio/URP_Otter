using System.Collections.Generic;
using UnityEngine;
using Crest.Spline;

[ExecuteInEditMode]
public class TerrainAdjusterRuntime : MonoBehaviour
{
    public Terrain terrain;
    public Spline crestSpline;

    [Range(1f, 50f)]
    public float roadWidth = 10f;

    [Range(0f, 1f)]
    public float brushFallOff = 0.3f;

    [Range(0.5f, 10f)]
    public float brushSpacing = 1f;

    public int[] initialPassRadii = { 15, 7, 2 };

    private float[,] originalTerrainHeights;

    public void SaveOriginalTerrainHeights()
    {
        if (terrain == null) return;
        var data = terrain.terrainData;
        originalTerrainHeights = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);
        Debug.Log("Original terrain heights saved.");
    }

    public void CleanUp()
    {
        originalTerrainHeights = null;
        Debug.Log("Original terrain data cleared.");
    }

    public void ShapeTerrain()
    {
        if (terrain == null || crestSpline == null) return;

        if (originalTerrainHeights == null)
            SaveOriginalTerrainHeights();

        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        float terrainHeight = data.size.y;
        float terrainWidth = data.size.x;
        float terrainLength = data.size.z;

        int width = data.heightmapResolution;
        int height = data.heightmapResolution;

        float[,] heightMap = (float[,])originalTerrainHeights.Clone();

        List<Vector3> worldPoints = new List<Vector3>();
        var splinePoints = crestSpline.GetComponentsInChildren<SplinePoint>();
        for (int i = 0; i < splinePoints.Length - 1; i++)
        {
            Vector3 p0 = splinePoints[i].transform.position;
            Vector3 p1 = splinePoints[i + 1].transform.position;

            float segLength = Vector3.Distance(p0, p1);
            int steps = Mathf.Max(1, Mathf.CeilToInt(segLength / brushSpacing));

            for (int j = 0; j <= steps; j++)
            {
                float t = j / (float)steps;
                Vector3 point = Vector3.Lerp(p0, p1, t);
                worldPoints.Add(point);
            }
        }

        // Optional: sort from high to low to avoid inconsistencies
        worldPoints.Sort((a, b) => -a.y.CompareTo(b.y));

        foreach (int radius in initialPassRadii)
        {
            for (int i = 0; i < worldPoints.Count - 1; i++)
            {
                Vector3 point = worldPoints[i];
                Vector3 next = worldPoints[i + 1];
                Vector3 tangent = (next - point).normalized;
                Vector3 normalXZ = Vector3.Cross(tangent, Vector3.up).normalized;

                int widthSteps = Mathf.CeilToInt(roadWidth / brushSpacing);

                for (int j = -widthSteps / 2; j <= widthSteps / 2; j++)
                {
                    Vector3 offsetPoint = point + normalXZ * j * brushSpacing;

                    float targetHeight = (offsetPoint.y - terrainPos.y) / terrainHeight;

                    float normalizedX = (offsetPoint.x - terrainPos.x) / terrainWidth;
                    float normalizedZ = (offsetPoint.z - terrainPos.z) / terrainLength;

                    int centerX = Mathf.RoundToInt(normalizedZ * (height - 1));
                    int centerY = Mathf.RoundToInt(normalizedX * (width - 1));

                    AdjustTerrain(heightMap, radius, centerX, centerY, targetHeight);
                }
            }
        }

        data.SetHeights(0, 0, heightMap);
        Debug.Log("Terrain shaping complete.");
    }

    private void AdjustTerrain(float[,] heightMap, int radius, int centerX, int centerY, float targetHeight)
    {
        int width = heightMap.GetLength(1);
        int height = heightMap.GetLength(0);
        int sqrRadius = radius * radius;
        float deltaHeight = targetHeight - heightMap[centerX, centerY];

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int dx = centerX + x;
                int dy = centerY + y;

                if (dx < 0 || dx >= height || dy < 0 || dy >= width)
                    continue;

                int distSq = x * x + y * y;
                if (distSq > sqrRadius) continue;

                float dist = Mathf.Sqrt(distSq);
                float t = dist / radius;
                float weight = Mathf.Exp(-t * t / brushFallOff);

                heightMap[dx, dy] += deltaHeight * weight;
                heightMap[dx, dy] = Mathf.Min(heightMap[dx, dy], targetHeight);
            }
        }
    }
}
