using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class ObjectClusterGenerator : MonoBehaviour
{
    [System.Serializable]
    public class PrefabCategory
    {
        public string categoryName;
        public List<GameObject> prefabs;
        public float expectedRadius = 1f;
        public float weight = 1f;
    }

    [Header("Main Object Configuration")]
    public List<PrefabCategory> mainObjects;

    [Header("Detail Object Configuration")]
    public List<PrefabCategory> detailObjects;

    [Header("Terrains To Use")]
    public List<Terrain> targetTerrains = new List<Terrain>();

    [Header("Terrain & Cluster Settings")]
    public string targetTerrainLayer; // Must match a TerrainLayer name
    public float clusterRadius = 10f;
    public int clusterCount = 5;
    public int maxIterations = 10;
    public int maxDetailPerCluster = 20;

    [Header("Main Item Limit")]
    public int maxMainPerCluster = 5;

    [Header("Parent Transform")]
    public Transform clusterParents;

    public void GenerateClustersEditor()
    {
        if (!Application.isPlaying)
        {
            GenerateClusters();
        }
    }

    public void GenerateClusters()
    {
        if (targetTerrains == null || targetTerrains.Count == 0)
        {
            Debug.LogError("No terrains assigned in targetTerrains list.");
            return;
        }

        List<Bounds> globalPlacedBounds = new List<Bounds>();

        foreach (var terrain in targetTerrains)
        {
            int paintLayerIndex = GetTerrainLayerIndex(terrain, targetTerrainLayer);
            if (paintLayerIndex < 0)
            {
                Debug.LogWarning($"Terrain layer '{targetTerrainLayer}' not found on terrain '{terrain.name}'. Skipping this terrain.");
                continue;
            }
            var clusterCenters = FindClusterCenters(terrain, clusterCount, maxIterations, paintLayerIndex);
            foreach (var center in clusterCenters)
            {
                CreateCluster(center, terrain, globalPlacedBounds);
            }
        }
    }

    List<Vector3> FindClusterCenters(Terrain terrain, int numClusters, int iterations, int paintLayerIndex)
    {
        List<Vector3> samples = new List<Vector3>();
        var terrainSize = terrain.terrainData.size;

        for (int i = 0; i < numClusters * 10; i++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(0, terrainSize.x),
                0,
                Random.Range(0, terrainSize.z)
            );

            float height = terrain.SampleHeight(randomPoint);
            randomPoint.y = height;

            Vector3 worldPoint = terrain.transform.position + randomPoint;
            if (IsOnTerrainLayer(terrain, worldPoint, paintLayerIndex))
                samples.Add(worldPoint);
        }

        var centers = samples.OrderBy(x => Random.value).Take(numClusters).ToList();

        for (int i = 0; i < iterations; i++)
        {
            var clusters = centers.Select(c => new List<Vector3>()).ToList();

            foreach (var sample in samples)
            {
                int nearestCenterIndex = centers
                    .Select((center, index) => new { index, dist = Vector3.Distance(sample, center) })
                    .OrderBy(x => x.dist)
                    .First().index;

                clusters[nearestCenterIndex].Add(sample);
            }

            for (int c = 0; c < centers.Count; c++)
            {
                if (clusters[c].Count == 0) continue;
                centers[c] = clusters[c].Aggregate(Vector3.zero, (acc, val) => acc + val) / clusters[c].Count;
            }
        }

        return centers;
    }

    void CreateCluster(Vector3 center, Terrain terrain, List<Bounds> globalPlacedBounds)
    {
        GameObject clusterRoot = new GameObject("ClusterRoot");
        clusterRoot.transform.SetParent(clusterParents);
        clusterRoot.transform.position = center;

        GameObject mainRoot = new GameObject("MainRoot");
        mainRoot.transform.SetParent(clusterRoot.transform);
        mainRoot.transform.position = center;

        List<Bounds> placedBounds = new List<Bounds>(); // For this cluster

        // ==== Main Item Placement: limit total main objects per cluster ====
        var mainCandidates = new List<(GameObject prefab, float expectedRadius, string categoryName)>();
        foreach (var category in mainObjects)
        {
            foreach (var prefab in category.prefabs)
            {
                mainCandidates.Add((prefab, category.expectedRadius, category.categoryName));
            }
        }
        mainCandidates = mainCandidates.OrderBy(_ => Random.value).ToList();

        int placedMainCount = 0;
        Dictionary<string, Transform> mainCategoryParents = new Dictionary<string, Transform>();

        foreach (var candidate in mainCandidates)
        {
            if (placedMainCount >= maxMainPerCluster)
                break;
            if (!mainCategoryParents.TryGetValue(candidate.categoryName, out var parent))
            {
                var catObj = new GameObject(candidate.categoryName);
                catObj.transform.SetParent(mainRoot.transform);
                catObj.transform.position = center;
                parent = catObj.transform;
                mainCategoryParents[candidate.categoryName] = parent;
            }
            bool placed = TryPlaceMainItem(center, terrain, candidate.prefab, candidate.expectedRadius, parent, placedBounds, globalPlacedBounds);
            if (placed)
                placedMainCount++;
        }

        // ==== Detail Item Placement: strict weight-based allocation ====
        GameObject detailRoot = new GameObject("DetailRoot");
        detailRoot.transform.SetParent(clusterRoot.transform);
        detailRoot.transform.position = center;

        int totalDetailCount = maxDetailPerCluster;
        float totalWeight = detailObjects.Sum(c => c.weight);

        Dictionary<PrefabCategory, int> categoryDetailTargets = new();
        foreach (var cat in detailObjects)
        {
            int count = Mathf.RoundToInt(totalDetailCount * (cat.weight / totalWeight));
            categoryDetailTargets[cat] = count;
        }

        foreach (var pair in categoryDetailTargets.OrderBy(_ => Random.value))
        {
            for (int i = 0; i < pair.Value; i++)
            {
                var selectedPrefab = pair.Key.prefabs[Random.Range(0, pair.Key.prefabs.Count)];
                bool placed = TryPlaceSingleDetail(center, terrain, selectedPrefab, pair.Key.expectedRadius, detailRoot.transform, placedBounds, globalPlacedBounds);
                // 可加 failedAttempts 判断防止死循环
            }
        }
    }

    bool TryPlaceMainItem(Vector3 center, Terrain terrain, GameObject prefab, float expectedRadius, Transform parent, List<Bounds> clusterBounds, List<Bounds> globalPlacedBounds)
    {
        for (int attempts = 0; attempts < 20; attempts++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * clusterRadius;
            Vector3 position = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            position.y = terrain.SampleHeight(position);

            MeshRenderer renderer = prefab.GetComponentInChildren<MeshRenderer>();
            if (renderer == null) continue;
            Bounds prefabBounds = renderer.bounds;
            prefabBounds.center = position;

            float safeRadius = GetSafeRadius(prefab, expectedRadius);

            bool positionValid =
                !clusterBounds.Any(b => b.Intersects(prefabBounds)) &&
                !globalPlacedBounds.Any(b => b.Intersects(prefabBounds)) &&
                clusterBounds.All(b => Vector3.Distance(b.center, position) >= safeRadius) &&
                globalPlacedBounds.All(b => Vector3.Distance(b.center, position) >= safeRadius);

            if (positionValid)
            {
                GameObject obj = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0, 360), 0), parent);
                MeshRenderer newRenderer = obj.GetComponentInChildren<MeshRenderer>();
                if (newRenderer != null)
                {
                    clusterBounds.Add(newRenderer.bounds);
                    globalPlacedBounds.Add(newRenderer.bounds);
                }
                return true;
            }
        }
        return false;
    }

    bool TryPlaceSingleDetail(Vector3 center, Terrain terrain, GameObject prefab, float expectedRadius, Transform parent, List<Bounds> clusterBounds, List<Bounds> globalPlacedBounds)
    {
        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * clusterRadius;
            Vector3 position = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            position.y = terrain.SampleHeight(position);

            MeshRenderer renderer = prefab.GetComponentInChildren<MeshRenderer>();
            if (renderer == null) continue;
            Bounds prefabBounds = renderer.bounds;
            prefabBounds.center = position;

            float safeRadius = GetSafeRadius(prefab, expectedRadius);

            bool positionValid =
                !clusterBounds.Any(b => b.Intersects(prefabBounds)) &&
                !globalPlacedBounds.Any(b => b.Intersects(prefabBounds)) &&
                clusterBounds.All(b => Vector3.Distance(b.center, position) >= safeRadius) &&
                globalPlacedBounds.All(b => Vector3.Distance(b.center, position) >= safeRadius);

            if (positionValid)
            {
                GameObject obj = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0, 360), 0), parent);
                MeshRenderer newRenderer = obj.GetComponentInChildren<MeshRenderer>();
                if (newRenderer != null)
                {
                    clusterBounds.Add(newRenderer.bounds);
                    globalPlacedBounds.Add(newRenderer.bounds);
                }
                return true;
            }
        }
        return false;
    }

    float GetSafeRadius(GameObject prefab, float userRadius)
    {
        MeshRenderer renderer = prefab.GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return userRadius;

        Bounds bounds = renderer.bounds;
        float calculatedRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        return Mathf.Max(userRadius, calculatedRadius);
    }

    bool IsOnTerrainLayer(Terrain terrain, Vector3 position, int paintLayerIndex)
    {
        Vector3 terrainLocalPos = position - terrain.transform.position;
        var terrainData = terrain.terrainData;

        Vector3 normalizedPos = new Vector3(
            terrainLocalPos.x / terrainData.size.x,
            terrainLocalPos.y / terrainData.size.y,
            terrainLocalPos.z / terrainData.size.z);

        int x = Mathf.Clamp((int)(normalizedPos.x * terrainData.alphamapWidth), 0, terrainData.alphamapWidth - 1);
        int z = Mathf.Clamp((int)(normalizedPos.z * terrainData.alphamapHeight), 0, terrainData.alphamapHeight - 1);

        if (paintLayerIndex < 0 || paintLayerIndex >= terrainData.alphamapLayers)
        {
            Debug.LogWarning("Invalid paint layer index: " + paintLayerIndex);
            return false;
        }

        var alphamaps = terrainData.GetAlphamaps(x, z, 1, 1);
        float layerWeight = alphamaps[0, 0, paintLayerIndex];

        return layerWeight > 0.5f;
    }

    int GetTerrainLayerIndex(Terrain terrain, string terrainLayerName)
    {
#if UNITY_2018_3_OR_NEWER
        var terrainLayers = terrain.terrainData.terrainLayers;
        for (int i = 0; i < terrainLayers.Length; i++)
        {
            if (terrainLayers[i] != null && terrainLayers[i].name == terrainLayerName)
                return i;
        }
#else
        var splatPrototypes = terrain.terrainData.splatPrototypes;
        for (int i = 0; i < splatPrototypes.Length; i++)
        {
            if (splatPrototypes[i] != null && splatPrototypes[i].texture != null && splatPrototypes[i].texture.name == terrainLayerName)
                return i;
        }
#endif
        return -1;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ObjectClusterGenerator))]
public class ObjectClusterGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ObjectClusterGenerator generator = (ObjectClusterGenerator)target;
        if (GUILayout.Button("Generate Clusters"))
        {
            generator.GenerateClustersEditor();
        }
    }
}
#endif
