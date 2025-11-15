using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation.Editor;

[CustomEditor(typeof(WaterNavmesh))]
public class WaterNavmeshEditor : Editor
{
    static bool wireframeEnabled = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("更新navmesh"))
        {
            UpdateNavmesh();
        }

        if (GUILayout.Button("切换wireframe模式"))
        {
            ToggleWireframe();
        }
    }

    private void ToggleWireframe()
    {
        wireframeEnabled = !wireframeEnabled;

        foreach (SceneView sceneView in SceneView.sceneViews)
        {
            SetWireframe(sceneView, wireframeEnabled);
        }

        SceneView.RepaintAll();
    }

    private void SetWireframe(SceneView view, bool enable)
    {
        if (enable)
            view.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.Wireframe);
        else
            view.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.Textured);
    }

    private void UpdateNavmesh()
    {
        var waterNavmesh = (WaterNavmesh)target;
        Object[] surfaces = new Object[] { waterNavmesh.SurfaceNavMesh, waterNavmesh.UnderwaterNavMesh };

        NavMeshAssetManager.instance.ClearSurfaces(surfaces);
        waterNavmesh.UpdateNavmeshNoRebuild();
        NavMeshAssetManager.instance.StartBakingSurfaces(surfaces);

        EditorUtility.SetDirty(waterNavmesh.SurfaceNavMesh);
        EditorUtility.SetDirty(waterNavmesh.UnderwaterNavMesh);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SceneView.RepaintAll();
    }
}