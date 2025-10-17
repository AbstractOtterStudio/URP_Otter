using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class RiverCurrentPath : MonoBehaviour
{
    [Tooltip("顺序摆放的球体(Transform)。最后一个会与第一个首尾相连形成闭环。")]
    public List<Transform> markers = new List<Transform>();

    [Tooltip("用于可视化的箭头长度。")]
    public float gizmoArrowLen = 1.5f;

    /// <summary>
    /// 给定空间中的任意点，返回该点处的水流方向（单位向量）。
    /// 做法：找到离该点最近的“线段”（marker[i] -> marker[i+1]，闭环），返回该线段的归一化方向。
    /// </summary>
    public Vector3 GetFlowDirectionAt(Vector3 worldPos)
    {
        if (markers == null || markers.Count < 2)
            return Vector3.forward;

        float bestDistSqr = float.MaxValue;
        int bestIdx = -1;

        for (int i = 0; i < markers.Count; i++)
        {
            Transform a = markers[i];
            Transform b = markers[(i + 1) % markers.Count];
            // 投影到线段，比较最近点的平方距离
            Vector3 seg = b.position - a.position;
            float segLenSqr = Mathf.Max(0.0001f, seg.sqrMagnitude);
            float t = Vector3.Dot(worldPos - a.position, seg) / segLenSqr;
            t = Mathf.Clamp01(t);
            Vector3 closest = a.position + seg * t;
            float d2 = (worldPos - closest).sqrMagnitude;
            if (d2 < bestDistSqr)
            {
                bestDistSqr = d2;
                bestIdx = i;
            }
        }

        if (bestIdx < 0)
            return Vector3.forward;

        Vector3 dir = (markers[(bestIdx + 1) % markers.Count].position - markers[bestIdx].position);
        dir.y = 0f; // 只在水平面漂流；若需三维水道可移除此行
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
    }

    /// <summary>返回最近线段上的最近点（便于调试或做吸附）。</summary>
    public Vector3 GetClosestPointOnPath(Vector3 worldPos)
    {
        if (markers == null || markers.Count < 2)
            return worldPos;

        float bestDistSqr = float.MaxValue;
        Vector3 bestPoint = worldPos;

        for (int i = 0; i < markers.Count; i++)
        {
            Transform a = markers[i];
            Transform b = markers[(i + 1) % markers.Count];
            Vector3 seg = b.position - a.position;
            float segLenSqr = Mathf.Max(0.0001f, seg.sqrMagnitude);
            float t = Vector3.Dot(worldPos - a.position, seg) / segLenSqr;
            t = Mathf.Clamp01(t);
            Vector3 closest = a.position + seg * t;
            float d2 = (worldPos - closest).sqrMagnitude;
            if (d2 < bestDistSqr)
            {
                bestDistSqr = d2;
                bestPoint = closest;
            }
        }
        return bestPoint;
    }

    private void OnDrawGizmos()
    {
        if (markers == null || markers.Count < 2) return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < markers.Count; i++)
        {
            Transform a = markers[i];
            Transform b = markers[(i + 1) % markers.Count];
            if (!a || !b) continue;

            // 画河段
            Gizmos.DrawLine(a.position, b.position);

            // 画方向箭头
            Vector3 dir = (b.position - a.position).normalized;
            Vector3 mid = Vector3.Lerp(a.position, b.position, 0.5f);
            Vector3 tip = mid + dir * gizmoArrowLen;
            Gizmos.DrawLine(mid, tip);
            // 简单箭头翅
            Vector3 wingL = Quaternion.AngleAxis(25f, Vector3.up) * (-dir);
            Vector3 wingR = Quaternion.AngleAxis(-25f, Vector3.up) * (-dir);
            Gizmos.DrawLine(tip, tip + wingL * 0.4f * gizmoArrowLen);
            Gizmos.DrawLine(tip, tip + wingR * 0.4f * gizmoArrowLen);
        }
    }
}
