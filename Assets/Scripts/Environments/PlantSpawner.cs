using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class PlantPrefabSettings
{
    public GameObject prefab;
    [Range(0f, 1f)] public float appearanceRatio = 1f;
    public Vector2 scaleRangeX = Vector2.one;
    public Vector2 scaleRangeY = Vector2.one;
    public Vector2 scaleRangeZ = Vector2.one;
    public Vector2 rotationRangeX = Vector2.zero;
    public Vector2 rotationRangeY = new Vector2(0f, 360f);
    public Vector2 rotationRangeZ = Vector2.zero;
}

public class PlantSpawner : MonoBehaviour
{
    [Header("Terrain & Layer")]
    public Terrain terrain;
    public string targetLayerName = "SeaGrass";
    [Range(0f, 1f)] public float alphaThreshold = 0.2f;

    [Header("Prefab Setup")]
    public List<PlantPrefabSettings> plantPrefabs;
    public Transform parentRoot;

    [Header("Spawn Settings")]
    public int spawnCount = 100;
    public float minDistance = 1.2f;
    public int maxTrialsPerSpawn = 50;

    private List<Vector3> usedPositions = new();
    private Dictionary<GameObject, Transform> prefabParentCache = new();

    public void SpawnPlants()
    {
        if (terrain == null || plantPrefabs.Count == 0)
        {
            Debug.LogWarning("❌ Terrain or prefab list not assigned.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        float[,,] alphaMaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
        int layerIndex = GetLayerIndex(targetLayerName, terrainData);

        if (layerIndex == -1)
        {
            Debug.LogWarning("❌ Layer not found: " + targetLayerName);
            return;
        }

        int maxTrials = spawnCount * maxTrialsPerSpawn;
        int trials = 0;
        int spawned = 0;
        usedPositions.Clear();
        prefabParentCache.Clear();

        while (spawned < spawnCount && trials < maxTrials)
        {
            trials++;
            int x = Random.Range(0, terrainData.alphamapWidth);
            int z = Random.Range(0, terrainData.alphamapHeight);

            float strength = alphaMaps[z, x, layerIndex];
            if (strength < alphaThreshold) continue;

            float normX = x / (float)terrainData.alphamapWidth;
            float normZ = z / (float)terrainData.alphamapHeight;

            float worldX = normX * terrainData.size.x + terrainPos.x;
            float worldZ = normZ * terrainData.size.z + terrainPos.z;
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

            Vector3 worldPos = new Vector3(worldX, worldY, worldZ);
            if (!IsPositionFarEnough(worldPos)) continue;

            GameObject prefab = SelectRandomPrefab();
            if (prefab == null) continue;

            Transform groupParent = GetOrCreatePrefabParent(prefab);
            var setting = GetSetting(prefab);

            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, groupParent);
            instance.transform.localScale = new Vector3(
                Random.Range(setting.scaleRangeX.x, setting.scaleRangeX.y),
                Random.Range(setting.scaleRangeY.x, setting.scaleRangeY.y),
                Random.Range(setting.scaleRangeZ.x, setting.scaleRangeZ.y)
            );
            instance.transform.eulerAngles = new Vector3(
                Random.Range(setting.rotationRangeX.x, setting.rotationRangeX.y),
                Random.Range(setting.rotationRangeY.x, setting.rotationRangeY.y),
                Random.Range(setting.rotationRangeZ.x, setting.rotationRangeZ.y)
            );

            usedPositions.Add(worldPos);
            spawned++;
        }

        if (spawned < spawnCount)
            Debug.LogWarning($"⚠️ Gave up after {trials} trials. Only {spawned}/{spawnCount} plants spawned.");
        else
            Debug.Log($"✅ Successfully spawned {spawned} plants.");
    }

    int GetLayerIndex(string name, TerrainData data)
    {
        for (int i = 0; i < data.terrainLayers.Length; i++)
        {
            if (data.terrainLayers[i].name == name)
                return i;
        }
        return -1;
    }

    GameObject SelectRandomPrefab()
    {
        float total = 0f;
        foreach (var p in plantPrefabs) total += p.appearanceRatio;

        float roll = Random.Range(0, total);
        float sum = 0f;
        foreach (var p in plantPrefabs)
        {
            sum += p.appearanceRatio;
            if (roll <= sum) return p.prefab;
        }
        return null;
    }

    PlantPrefabSettings GetSetting(GameObject prefab)
    {
        return plantPrefabs.Find(p => p.prefab == prefab);
    }

    bool IsPositionFarEnough(Vector3 pos)
    {
        foreach (var p in usedPositions)
        {
            if (Vector3.Distance(p, pos) < minDistance)
                return false;
        }
        return true;
    }

    Transform GetOrCreatePrefabParent(GameObject prefab)
    {
        if (prefabParentCache.TryGetValue(prefab, out var cached))
            return cached;

        string groupName = prefab.name;
        Transform group = null;

        if (parentRoot != null)
        {
            Transform found = parentRoot.Find(groupName);
            if (found != null)
                group = found;
        }

        if (group == null)
        {
            GameObject go = new GameObject(groupName);
            group = go.transform;
            if (parentRoot != null)
                group.SetParent(parentRoot);
            group.localPosition = Vector3.zero;
        }

        prefabParentCache[prefab] = group;
        return group;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlantSpawner))]
public class PlantSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlantSpawner spawner = (PlantSpawner)target;
        if (GUILayout.Button("Spawn Plants"))
        {
            spawner.SpawnPlants();
        }
    }
}
#endif
