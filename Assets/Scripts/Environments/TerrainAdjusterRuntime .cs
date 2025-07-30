using System.Collections.Generic;
using UnityEngine;
using Crest.Spline;

[ExecuteInEditMode]
[AddComponentMenu("Scripts/Terrain Adjuster Runtime")]
public class TerrainAdjusterRuntime : MonoBehaviour
{
    public List<Terrain> terrains = new List<Terrain>();
    public Spline crestSpline;

    [Range(1f, 50f)] public float roadWidth = 10f;
    [Range(0f, 1f)] public float brushFallOff = 0.3f;
    [Range(0.5f, 10f)] public float brushSpacing = 1f;
    public int[] initialPassRadii = { 15, 7, 2 };

    private Dictionary<Terrain, float[,]> originalTerrainHeights = new();

    public void SaveOriginalTerrainHeights()
    {
        originalTerrainHeights.Clear();
        foreach (var terrain in terrains)
        {
            if (terrain == null) continue;
            var data = terrain.terrainData;
            originalTerrainHeights[terrain] = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);
        }

        Debug.Log("Original terrain heights saved.");
    }

    public void CleanUp()
    {
        originalTerrainHeights.Clear();
        Debug.Log("Original terrain data cleared.");
    }

    public void ShapeTerrain()
    {
        if (crestSpline == null || terrains.Count == 0)
        {
            Debug.LogWarning("Spline or terrain list is missing.");
            return;
        }

        // Load height data
        Dictionary<Terrain, float[,]> heightBuffers = new();
        foreach (var terrain in terrains)
        {
            if (terrain == null) continue;
            var data = terrain.terrainData;
            if (!originalTerrainHeights.TryGetValue(terrain, out var original))
            {
                original = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);
            }
            heightBuffers[terrain] = (float[,])original.Clone();
        }

        // Sample spline points
        List<Vector3> worldPoints = new();
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

        // Sort high to low to preserve slope blending
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

                    Terrain terrain = GetTerrainAtPosition(offsetPoint);
                    if (terrain == null) continue;

                    TerrainData td = terrain.terrainData;
                    Vector3 terrainPos = terrain.transform.position;
                    float terrainHeight = td.size.y;
                    float terrainWidth = td.size.x;
                    float terrainLength = td.size.z;
                    int w = td.heightmapResolution;
                    int h = td.heightmapResolution;

                    float normX = (offsetPoint.x - terrainPos.x) / terrainWidth;
                    float normZ = (offsetPoint.z - terrainPos.z) / terrainLength;

                    if (normX < 0 || normX > 1 || normZ < 0 || normZ > 1)
                        continue;

                    int centerX = Mathf.RoundToInt(normZ * (h - 1));
                    int centerY = Mathf.RoundToInt(normX * (w - 1));
                    float targetHeight = (offsetPoint.y - terrainPos.y) / terrainHeight;

                    AdjustTerrain(heightBuffers[terrain], radius, centerX, centerY, targetHeight);
                }
            }
        }

        // Apply heightmaps
        foreach (var pair in heightBuffers)
        {
            TerrainData data = pair.Key.terrainData;
            data.SetHeights(0, 0, pair.Value);
        }

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

    private Terrain GetTerrainAtPosition(Vector3 worldPos)
    {
        foreach (var terrain in terrains)
        {
            if (terrain == null) continue;
            Bounds bounds = new Bounds(
                terrain.transform.position + terrain.terrainData.size / 2f,
                terrain.terrainData.size);

            if (bounds.Contains(worldPos))
                return terrain;
        }

        return null;
    }

    public void RestoreTerrain()
    {
        foreach (var terrain in terrains)
        {
            if (terrain == null) continue;
            if (!originalTerrainHeights.TryGetValue(terrain, out var original)) continue;

            terrain.terrainData.SetHeights(0, 0, original);
        }

        Debug.Log("Terrain restored to original state.");
    }


}
