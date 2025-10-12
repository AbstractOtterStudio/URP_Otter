using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class AvoidBeacon : MonoBehaviour
{
    [Tooltip("禁区球的影响半径（米）")]
    public float radius = 5f;

    [Tooltip("该点的权重（>1更强，<1更弱）")]
    public float weight = 10.0f;

    // 全局注册表（AI 直接访问）
    public static readonly List<AvoidBeacon> All = new();

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, .5f, 0f, .15f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(1f, .5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
