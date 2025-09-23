using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家扒在浮木上：
/// - 始终跟随浮木位置与朝向；
/// - 只有“横向(沿木头长边)”采用平滑滞后，从而产生左右滑动的视觉；
/// - 与浮木的相对位置由 baseLocalOffset 决定（局部坐标）；
/// - 可自动从 Log 控制脚本推断“横向轴”，或手动指定。
/// </summary>
[DisallowMultipleComponent]
public class PlayerClingToLog : MonoBehaviour
{
    [Header("必填引用")]
    public Transform log;               // 浮木根(带Rigidbody的那个)
    public Transform logAnchor;         // 浮木上的锚点(木头表面中线)
    public Transform playerHandPoint;   // 玩家手中心点(建议为玩家根的直接子物体)

    [Header("前进方向与平面")]
    public RiverCurrentPath riverPath;  // 可为空：为空则用log水平速度/forward
    public Vector3 planeNormal = Vector3.up; // 对齐所在平面(通常世界Up)
    [Tooltip("勾选后，角色朝向取反（例如面向下游改为面向上游）。")]
    public bool flipFacing = false;

    [Header("插值参数（指数平滑）")]
    [Tooltip("旋转插值速度(1/秒)。越大越快贴回。")]
    public float rotLerpSpeed = 10f;
    [Tooltip("位置插值速度(1/秒)。越大越快贴回。")]
    public float posLerpSpeed = 12f;

    [Header("启动/大跳变")]
    [Tooltip("启动帧是否立刻把手点Snap到锚点")]
    public bool snapOnStart = true;
    [Tooltip("当目标点相对上帧跳变超过该距离(米)时，直接重置插值起点，避免闪烁/爆速。")]
    public float warpSnapDistance = 1.0f;

    [Header("刚体驱动(可选)")]
    [Tooltip("玩家带Rigidbody时建议勾上：用MovePosition/MoveRotation")]
    public bool driveRigidbody = true;
    [Tooltip("是否写回刚体线速度(默认关，避免与其它系统打架)")]
    public bool setVelocityFromMotion = false;

    // ===== 内部状态 =====
    Rigidbody rb; bool hasRb;
    Vector3 handLocalFromRoot;   // 手点相对玩家根的局部位移(假设根缩放=1,1,1最佳)
    Vector3 prevTargetRoot;      // 上一帧我们设置的“目标根位置”，用于估速与抗闪
    bool inited;

    void Awake()
    {
        if (!log || !logAnchor || !playerHandPoint)
        {
            Debug.LogError("[PlayerCling_TargetCoincide_Lerped] 请把 log / logAnchor / playerHandPoint 都拖上。");
            enabled = false; return;
        }
        if (playerHandPoint.parent != transform)
            Debug.LogWarning("[PlayerCling_TargetCoincide_Lerped] 建议 playerHandPoint 是玩家根的直接子物体，以避免骨骼动画影响。");
        if (transform.lossyScale != Vector3.one)
            Debug.LogWarning("[PlayerCling_TargetCoincide_Lerped] 玩家根缩放不是(1,1,1)，可能导致对齐误差。");

        // 记录手点相对玩家根的局部位移
        handLocalFromRoot = transform.InverseTransformPoint(playerHandPoint.position);

        rb = GetComponent<Rigidbody>();
        hasRb = rb != null;
        if (hasRb && driveRigidbody) rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        Vector3 n = SafeUp(planeNormal);

        // 计算“理想朝向”
        Vector3 fwd = ComputeForwardDir(n);
        if (flipFacing) fwd = -fwd;
        Quaternion idealRot = Quaternion.LookRotation(fwd, n);

        // 用该旋转解出“理想根位置”，使手点重合
        Vector3 idealRoot = logAnchor.position - (idealRot * handLocalFromRoot);

        if (snapOnStart)
        {
            ApplyPose(idealRoot, idealRot, zeroVelocity: true);
            prevTargetRoot = idealRoot;
        }
        else
        {
            prevTargetRoot = transform.position;
        }

        inited = true;
    }

    void FixedUpdate()
    {
        if (!inited) return;
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;

        Vector3 n = SafeUp(planeNormal);

        // 1) 计算当帧“理想旋转”
        Vector3 fwd = ComputeForwardDir(n);
        if (flipFacing) fwd = -fwd;
        Quaternion desiredRot = Quaternion.LookRotation(fwd, n);

        // 2) 先做旋转插值 -> newRot
        Quaternion curRot = hasRb ? rb.rotation : transform.rotation;
        float aRot = 1f - Mathf.Exp(-rotLerpSpeed * dt);
        Quaternion newRot = Quaternion.Slerp(curRot, desiredRot, aRot);

        // 3) 用 newRot 反解“理想根位置”，让手点重合
        Vector3 desiredRoot = logAnchor.position - (newRot * handLocalFromRoot);

        // 抗闪：目标大跳变则重置“上一目标点”
        if ((desiredRoot - prevTargetRoot).sqrMagnitude > warpSnapDistance * warpSnapDistance)
            prevTargetRoot = desiredRoot;

        // 4) 对根位置做插值 -> newPos
        float aPos = 1f - Mathf.Exp(-posLerpSpeed * dt);
        Vector3 newPos = Vector3.Lerp(prevTargetRoot, desiredRoot, aPos);

        // 5) 驱动
        if (hasRb && driveRigidbody)
        {
            rb.MoveRotation(newRot);
            rb.MovePosition(newPos);

            if (setVelocityFromMotion)
            {
                Vector3 v = (newPos - prevTargetRoot) / dt;
                rb.velocity = v; // 不写角速度，避免打架
            }
        }
        else
        {
            ApplyPose(newPos, newRot, zeroVelocity: false);
        }

        // 6) 记录
        prevTargetRoot = newPos;
    }

    // ====== 工具 ======
    Vector3 SafeUp(Vector3 up) => (up.sqrMagnitude > 1e-6f ? up.normalized : Vector3.up);

    // “前进方向”：优先水流，其次 log 的水平速度，再次 log.forward 的水平投影
    Vector3 ComputeForwardDir(Vector3 upN)
    {
        Vector3 f = Vector3.zero;
        if (riverPath)
        {
            f = riverPath.GetFlowDirectionAt(log.position);
            f = Vector3.ProjectOnPlane(f, upN).normalized;
        }
        if (f.sqrMagnitude < 1e-6f && log.TryGetComponent<Rigidbody>(out var lrb))
        {
            Vector3 v = Vector3.ProjectOnPlane(lrb.velocity, upN);
            if (v.sqrMagnitude > 1e-6f) f = v.normalized;
        }
        if (f.sqrMagnitude < 1e-6f)
            f = Vector3.ProjectOnPlane(log.forward, upN).normalized;

        return f.sqrMagnitude > 1e-6f ? f : Vector3.forward;
    }

    void ApplyPose(Vector3 p, Quaternion r, bool zeroVelocity)
    {
        transform.SetPositionAndRotation(p, r);
        if (zeroVelocity && hasRb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!logAnchor || !playerHandPoint) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(logAnchor.position, 0.06f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerHandPoint.position, 0.06f);
    }
#endif
}
