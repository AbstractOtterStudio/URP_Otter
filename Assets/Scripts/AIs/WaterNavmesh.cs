using UnityEngine;
using Unity.AI.Navigation; // for NavMeshSurface
using Crest;               // assumes Crest is imported
using UnityEngine.AI;

/**
 * 水面和水底NavMesh的管理
*/
[DisallowMultipleComponent]
public class WaterNavmesh : MonoBehaviour
{
    [Header("基础水面navmesh设置")]
    [SerializeField] RegisterHeightInput referenceWaterBody = null;
    [SerializeField] float surfaceOffset = 0.0f;
    [SerializeField] float underwaterOffset = -10.0f;

    [Header("Navmesh维度")]
    [SerializeField] Vector2 navmeshSize = new Vector2(100, 100);
    [SerializeField] float navmeshHeight = 4;

    [Header("Navmesh组件")]
    [SerializeField] NavMeshSurface surfaceNavMesh;
    [SerializeField] NavMeshSurface underwaterNavMesh;

    public NavMeshSurface SurfaceNavMesh => surfaceNavMesh;
    public NavMeshSurface UnderwaterNavMesh => underwaterNavMesh;
    public float WaterHeight => OceanRenderer.Instance != null
        ? OceanRenderer.Instance.SeaLevel
        : transform.position.y;

    public static int UnderwaterLayerMask { get => 1 << NavMesh.GetAreaFromName("Underwater"); }
    public static int WaterSurfaceLayerMask { get => 1 << NavMesh.GetAreaFromName("Walkable"); }

    void OnValidate()
    {
        if (navmeshSize.magnitude < Mathf.Epsilon)
        {
            Debug.LogError($"Invalid navmesh size: {navmeshSize}");
            navmeshSize = new Vector2(100, 100);
        }

        if (Mathf.Abs(navmeshHeight) < Mathf.Epsilon)
        {
            Debug.LogError($"Invalid navmesh height: {navmeshHeight}");
            navmeshHeight = 4;
        }

        void UpdateSurfaceSize(NavMeshSurface surface)
        {
            surface.collectObjects = CollectObjects.Volume;
            surface.size = new Vector3(navmeshSize.x, navmeshHeight, navmeshSize.y);
        }

        UpdateSurfaceSize(SurfaceNavMesh);
        UpdateSurfaceSize(UnderwaterNavMesh);
    }

    public void UpdateNavmeshNoRebuild()
    {
        var baseHeight = WaterHeight;
        if (referenceWaterBody != null)
            baseHeight = referenceWaterBody.transform.position.y;

        if (surfaceNavMesh != null)
            surfaceNavMesh.transform.position = surfaceNavMesh.transform.position.Y(baseHeight + surfaceOffset);

        if (underwaterNavMesh != null)
            underwaterNavMesh.transform.position = underwaterNavMesh.transform.position.Y(baseHeight + underwaterOffset);
    }
}
