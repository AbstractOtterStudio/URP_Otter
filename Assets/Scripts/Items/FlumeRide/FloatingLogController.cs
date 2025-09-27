using Crest.Spline;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FloatingLogHardcodeSteer : MonoBehaviour
{
    public enum AlignAxis { Forward, Right, Left, Back }

    [Header("Path / 平面")]
    [Tooltip("在这个平面内运动/对齐（一般世界Up）")]
    public Vector3 planeNormal = Vector3.up;
    [Tooltip("用哪个本地轴去对齐横向（你的木头长边沿 X 选 Right；沿 Z 选 Forward）")]
    public AlignAxis alignAxis = AlignAxis.Right;
    [Tooltip("横向反了就勾上")]
    public bool flipPerp = false;

    [Header("对齐（扭矩 PD）")]
    [Tooltip("0=只看水流垂线；1=只看速度垂线；建议 0.4~0.8")]
    [Range(0f, 1f)] public float slipYawBlend = 0.65f;
    [Tooltip("比例增益（越大越“跟”目标）")]
    public float alignKp = 10f;
    [Tooltip("阻尼增益（抑制过冲/抖动）")]
    public float alignKd = 3.5f;
    [Tooltip("扭矩上限（ForceMode.Acceleration）")]
    public float maxAlignTorque = 80f;

    [Header("推进/输入")]
    [Tooltip("顺流推进加速度")]
    public float flowAccel = 12f;
    [Tooltip("玩家输入的偏移加速度")]
    public float inputAccel = 25f;
    [Tooltip("按左时的局部力方向（默认向左 -X）")]
    public Vector3 leftForceLocalDir = new Vector3(-1, 0, 0);
    [Tooltip("按右时的局部力方向（默认向右 +X）")]
    public Vector3 rightForceLocalDir = new Vector3(1, 0, 0);

    [Header("离心增强（拐弯才生效）")]
    [Tooltip("向前采样距离（≈路标间距的一半~一倍）")]
    public float lookaheadDist = 3f;
    [Tooltip("离心加速度系数，最终 a_out ≈ gain * |curvature| * speed^2")]
    public float centrifugalGain = 0.7f;
    [Tooltip("离心加速度上限（防止过猛）")]
    public float maxCentrifugalAccel = 30f;

    [Header("速度限制")]
    [Tooltip("平面内最大速度")]
    public float maxHorizontalSpeed = 12f;

    // === 新增：Spline 参照（仅用于 GetFlowDirectionAt） ===
    [Header("Spline 参照（给外抛用）")]
    [Tooltip("按水流顺序排列的一组 SplinePoint")]
    public List<SplinePoint> splinePoints = new List<SplinePoint>();
    [Tooltip("是否闭环（最后一个连接回第一个）")]
    public bool closedLoop = true;

    // --- 依赖 ---
    Rigidbody rb;
    FloatingObject floating;            // Crest 的浮动脚本（提供 CurrentFlowXZ）
    Vector3 prevFlowDir;               // 流向兜底
    Vector3 upN;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // 允许绕Y自由，锁X/Z 防止倾倒
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        floating = GetComponent<FloatingObject>();

        upN = planeNormal.sqrMagnitude > 1e-6f ? planeNormal.normalized : Vector3.up;

        // 初始化上一帧流向
        Vector3 initFlow = floating ? floating.CurrentFlowXZ : transform.forward;
        initFlow = Vector3.ProjectOnPlane(initFlow, upN);
        prevFlowDir = initFlow.sqrMagnitude > 1e-6f ? initFlow.normalized
                                                    : Vector3.ProjectOnPlane(transform.forward, upN).normalized;
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.GetGameAction()) return;
            Vector3 n = upN;

        // --- 基础向量：水流、速度（都投影到平面） ---
        // 【改动1】flow 改为从 FloatingObject 取
        Vector3 flow = floating ? floating.CurrentFlowXZ : transform.forward;
        flow = Vector3.ProjectOnPlane(flow, n);
        Vector3 flowDir = flow.sqrMagnitude > 1e-6f ? flow.normalized : prevFlowDir; // 小于阈值用上一帧，防止原地自转

        Vector3 v = rb.velocity;
        Vector3 hv = Vector3.ProjectOnPlane(v, n);
        Vector3 velDir = hv.sqrMagnitude > 1e-6f ? hv.normalized : flowDir;

        // 横向（垂直）向量：水流垂线 & 速度垂线
        Vector3 perpFlow = Vector3.Cross(n, flowDir).normalized;   // “右向”
        Vector3 perpVel = Vector3.Cross(n, velDir).normalized;

        if (flipPerp) { perpFlow = -perpFlow; perpVel = -perpVel; }

        // 目标横向方向：在两者之间球面插值
        Vector3 targetPerp = Vector3.Slerp(perpFlow, perpVel, Mathf.Clamp01(slipYawBlend)).normalized;

        // --- 扭矩 PD：让选定轴的“平面投影”对齐到 targetPerp（仅绕 n） ---
        Vector3 curAxis = GetAxisWorld(alignAxis);
        Vector3 curProj = Vector3.ProjectOnPlane(curAxis, n).normalized;

        if (curProj.sqrMagnitude > 1e-6f && targetPerp.sqrMagnitude > 1e-6f)
        {
            float angleRad = SignedAngleRad(curProj, targetPerp, n);
            float yawVel = Vector3.Dot(rb.angularVelocity, n);

            float torque = alignKp * angleRad - alignKd * yawVel;
            torque = Mathf.Clamp(torque, -maxAlignTorque, maxAlignTorque);
            rb.AddTorque(n * torque, ForceMode.Acceleration);
        }

        // --- 推进 & 玩家输入的偏移力（保持原逻辑不变） ---
        rb.AddForce(flowDir * flowAccel, ForceMode.Acceleration);

        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        if (left && !right)
            rb.AddForce(transform.TransformDirection(leftForceLocalDir.normalized) * inputAccel, ForceMode.Acceleration);
        else if (right && !left)
            rb.AddForce(transform.TransformDirection(rightForceLocalDir.normalized) * inputAccel, ForceMode.Acceleration);

        // --- 离心增强：把 riverPath.GetFlowDirectionAt(...) 改为本地的 GetFlowDirectionAt(...) ---
        if (lookaheadDist > 0.01f && centrifugalGain > 0f && splinePoints != null && splinePoints.Count >= 2)
        {
            Vector3 pathDirNow = GetFlowDirectionAt(transform.position);
            Vector3 pathDirAhead = GetFlowDirectionAt(transform.position + flowDir * lookaheadDist); // 维持你原先的“pos + flow*lookaheadDist”采样语义

            if (pathDirNow.sqrMagnitude > 1e-6f && pathDirAhead.sqrMagnitude > 1e-6f)
            {
                float turnRad = SignedAngleRad(pathDirNow, pathDirAhead, n);     // 左转>0，右转<0
                float curvature = Mathf.Abs(turnRad) / Mathf.Max(lookaheadDist, 1e-3f); // ≈ |dθ|/ds

                // 外侧方向：左转时外侧=+perp(pathDirNow)；右转时外侧=-perp
                Vector3 perpPath = Vector3.Cross(n, pathDirNow).normalized;
                Vector3 outward = (turnRad >= 0f ? perpPath : -perpPath);

                float aOut = centrifugalGain * hv.sqrMagnitude * curvature; // v^2 * κ * gain
                aOut = Mathf.Min(aOut, maxCentrifugalAccel);
                if (aOut > 1e-4f)
                    rb.AddForce(outward * aOut, ForceMode.Acceleration);
            }
        }

        // --- 平面内限速 ---
        if (hv.magnitude > maxHorizontalSpeed)
        {
            Vector3 hvClamped = hv.normalized * maxHorizontalSpeed;
            rb.velocity = hvClamped + Vector3.Project(v, n);
        }

        // 更新兜底方向
        prevFlowDir = flowDir;
    }

    // ===== GetFlowDirectionAt：用 splinePoints 最近线段方向 =====
    public Vector3 GetFlowDirectionAt(Vector3 worldPos)
    {
        if (splinePoints == null || splinePoints.Count < 2)
            return Vector3.ProjectOnPlane(transform.forward, upN).normalized;

        Vector3 bestDir = Vector3.zero;
        float bestDist2 = float.MaxValue;

        int count = splinePoints.Count;
        int last = count - 1;

        for (int i = 0; i < count - 1; i++)
            EvalSegment(i, ref bestDir, ref bestDist2, worldPos);

        if (closedLoop)
            EvalSegment(last, ref bestDir, ref bestDist2, worldPos, loopToStart: true);

        if (bestDir.sqrMagnitude < 1e-6f)
            bestDir = Vector3.ProjectOnPlane(transform.forward, upN).normalized;

        return bestDir;
    }

    void EvalSegment(int i, ref Vector3 bestDir, ref float bestDist2, Vector3 p, bool loopToStart = false)
    {
        Transform ta = splinePoints[i] ? splinePoints[i].transform : null;
        Transform tb = null;

        if (loopToStart && i == splinePoints.Count - 1)
            tb = splinePoints[0] ? splinePoints[0].transform : null;
        else
            tb = splinePoints[i + 1] ? splinePoints[i + 1].transform : null;

        if (!ta || !tb) return;

        Vector3 a = ta.position;
        Vector3 b = tb.position;

        // 在对齐平面内计算
        Vector3 ab = Vector3.ProjectOnPlane(b - a, upN);
        float len = ab.magnitude;
        if (len < 1e-6f) return;

        Vector3 dir = ab / len;

        // 最近点参数 t∈[0,1]
        float t = Mathf.Clamp01(Vector3.Dot(p - a, dir) / len);
        Vector3 proj = a + dir * (t * len);

        float d2 = (p - proj).sqrMagnitude;
        if (d2 < bestDist2)
        {
            bestDist2 = d2;
            bestDir = dir;
        }
    }

    // ===== 工具函数（不变） =====
    Vector3 GetAxisWorld(AlignAxis a)
    {
        switch (a)
        {
            case AlignAxis.Forward: return transform.forward;
            case AlignAxis.Back: return -transform.forward;
            case AlignAxis.Right: return transform.right;
            case AlignAxis.Left: return -transform.right;
        }
        return transform.right;
    }

    float SignedAngleRad(Vector3 from, Vector3 to, Vector3 axisNormal)
    {
        from.Normalize(); to.Normalize(); axisNormal.Normalize();
        float sin = Vector3.Dot(axisNormal, Vector3.Cross(from, to));
        float cos = Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f);
        return Mathf.Atan2(sin, cos); // -pi..pi
    }
}
