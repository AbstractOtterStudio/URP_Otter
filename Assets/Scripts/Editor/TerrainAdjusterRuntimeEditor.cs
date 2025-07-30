using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainAdjusterRuntime))]
public class TerrainAdjusterRuntimeEditor : Editor
{
    TerrainAdjusterRuntimeEditor editor;

    public void OnEnable()
    {
        this.editor = this;

        TerrainAdjusterRuntime targetGameObject = (TerrainAdjusterRuntime)target;

        // Save terrain height on editor load
        targetGameObject.SaveOriginalTerrainHeights();
    }

    void OnDisable()
    {
        TerrainAdjusterRuntime targetGameObject = (TerrainAdjusterRuntime)target;

        // Clean up height data
        targetGameObject.CleanUp();
    }

    void OnPathChanged()
    {
        TerrainAdjusterRuntime targetGameObject = (TerrainAdjusterRuntime)target;
        targetGameObject.ShapeTerrain();
    }

    public override void OnInspectorGUI()
    {
        TerrainAdjusterRuntime targetGameObject = (TerrainAdjusterRuntime)target;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            // Re-shape terrain when values change in inspector
            OnPathChanged();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("Shape Terrain from Crest Spline"))
        {
            OnPathChanged();
        }

        if (GUILayout.Button("Flatten Entire Terrain"))
        {
            SetTerrainHeight(targetGameObject.terrain, 0f);
        }

        EditorGUILayout.EndVertical();
    }

    void SetTerrainHeight(Terrain terrain, float height)
    {
        if (terrain == null) return;

        TerrainData terrainData = terrain.terrainData;
        int w = terrainData.heightmapResolution;
        int h = terrainData.heightmapResolution;

        float[,] allHeights = new float[h, w];

        for (int x = 0; x < h; x++)
        {
            for (int y = 0; y < w; y++)
            {
                allHeights[x, y] = height;
            }
        }

        terrainData.SetHeights(0, 0, allHeights);
    }
}
