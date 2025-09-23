using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FloatingLogHardcodeSteer : MonoBehaviour
{
    public enum AlignAxis { Forward, Right, Left, Back }

    [Header("Path / 平面")]
    public RiverCurrentPath riverPath;
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

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // 允许绕Y自由，锁X/Z 防止倾倒
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (!riverPath) riverPath = FindObjectOfType<RiverCurrentPath>();
    }

    void FixedUpdate()
    {
        Vector3 n = planeNormal.sqrMagnitude > 1e-6f ? planeNormal.normalized : Vector3.up;

        // --- 基础向量：水流、速度（都投影到平面） ---
        Vector3 flow = riverPath ? riverPath.GetFlowDirectionAt(transform.position) : transform.forward;
        flow = Vector3.ProjectOnPlane(flow, n).normalized;
        if (flow.sqrMagnitude < 1e-6f) flow = transform.forward;

        Vector3 v = rb.velocity;
        Vector3 hv = Vector3.ProjectOnPlane(v, n);
        Vector3 velDir = hv.sqrMagnitude > 1e-6f ? hv.normalized : flow;

        // 横向（垂直）向量：水流垂线 & 速度垂线
        Vector3 perpFlow = Vector3.Cross(n, flow).normalized;   // 相当于“右向”
        Vector3 perpVel = Vector3.Cross(n, velDir).normalized;

        if (flipPerp) { perpFlow = -perpFlow; perpVel = -perpVel; }

        // 目标横向方向：在两者之间球面插值
        Vector3 targetPerp = Vector3.Slerp(perpFlow, perpVel, Mathf.Clamp01(slipYawBlend)).normalized;

        // --- 扭矩 PD：让选定轴的“平面投影”对齐到 targetPerp（仅绕 n） ---
        Vector3 curAxis = GetAxisWorld(alignAxis);
        Vector3 curProj = Vector3.ProjectOnPlane(curAxis, n).normalized;

        float angleRad = SignedAngleRad(curProj, targetPerp, n);
        float yawVel = Vector3.Dot(rb.angularVelocity, n);

        float torque = alignKp * angleRad - alignKd * yawVel;
        torque = Mathf.Clamp(torque, -maxAlignTorque, maxAlignTorque);
        rb.AddTorque(n * torque, ForceMode.Acceleration);

        // --- 推进 & 玩家输入的偏移力 ---
        rb.AddForce(flow * flowAccel, ForceMode.Acceleration);

        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        if (left && !right)
            rb.AddForce(transform.TransformDirection(leftForceLocalDir.normalized) * inputAccel, ForceMode.Acceleration);
        else if (right && !left)
            rb.AddForce(transform.TransformDirection(rightForceLocalDir.normalized) * inputAccel, ForceMode.Acceleration);

        // --- 离心增强：根据曲率≈前向采样流向变化，推向外侧 ---
        if (lookaheadDist > 0.01f && centrifugalGain > 0f)
        {
            Vector3 flowAhead = riverPath.GetFlowDirectionAt(transform.position + flow * lookaheadDist);
            flowAhead = Vector3.ProjectOnPlane(flowAhead, n).normalized;

            if (flowAhead.sqrMagnitude > 1e-6f)
            {
                float turnRad = SignedAngleRad(flow, flowAhead, n);     // 左转>0，右转<0
                float curvature = Mathf.Abs(turnRad) / Mathf.Max(lookaheadDist, 1e-3f); // ≈ |dθ|/ds

                // 外侧方向：左转时外侧=+perpFlow；右转时外侧=-perpFlow
                float sign = Mathf.Sign(turnRad);
                Vector3 outward = (sign >= 0f ? perpFlow : -perpFlow);

                float aOut = centrifugalGain * hv.sqrMagnitude * curvature; // v^2 * κ * gain
                aOut = Mathf.Min(aOut, maxCentrifugalAccel);
                if (aOut > 1e-4f)
                    rb.AddForce(outward * aOut, ForceMode.Acceleration);
            }
        }

        // --- 平面内限速 ---
        Vector3 hvClamped = hv;
        float spd = hvClamped.magnitude;
        if (spd > maxHorizontalSpeed)
        {
            hvClamped = hvClamped.normalized * maxHorizontalSpeed;
            rb.velocity = hvClamped + Vector3.Project(v, n);
        }
    }

    // 工具函数
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
