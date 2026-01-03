using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation.Editor;
using UnityEngine.AI;

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

        if (GUILayout.Button("将所有NPCAgent放置在水面navmesh上"))
        {
            PlaceNPCsOnSurface();
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

    private void PlaceNPCsOnSurface()
    {
        NPCAgent[] npcAgents = FindObjectsOfType<NPCAgent>();

        if (npcAgents.Length == 0)
        {
            Debug.LogWarning("场景中没有找到NPCAgent组件");
            return;
        }

        int layerMask = WaterNavmesh.WaterSurfaceLayerMask;
        int placedCount = 0;
        float maxDistance = 100f;

        foreach (NPCAgent agent in npcAgents)
        {
            if (agent == null || agent.transform == null)
                continue;

            NavMeshAgent navAgent = agent.GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                Debug.LogWarning($"NPCAgent {agent.name} 没有NavMeshAgent组件");
                continue;
            }

            Vector3 currentPosition = agent.transform.position;

            if (NavMesh.SamplePosition(currentPosition, out NavMeshHit hit, maxDistance, layerMask))
            {
                Undo.RecordObject(agent.transform, "Place NPCAgent on water surface");
                var localPos = agent.transform.InverseTransformPoint(hit.position);
                agent.transform.position = agent.transform.TransformPoint(localPos + Vector3.up * navAgent.baseOffset);
                EditorUtility.SetDirty(agent.transform);
                placedCount++;
            }
            else
            {
                Debug.LogWarning($"无法在navmesh上找到位置: {agent.name} at {currentPosition}");
            }
        }

        Debug.Log($"已将 {placedCount}/{npcAgents.Length} 个NPCAgent放置在水面navmesh上");
        SceneView.RepaintAll();
    }
}