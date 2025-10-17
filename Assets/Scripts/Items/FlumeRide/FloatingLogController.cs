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

    // === Spline 参照（给外抛用） ===
    [Header("Spline 参照（给外抛用）")]
    [Tooltip("按水流顺序排列的一组 SplinePoint")]
    public List<SplinePoint> splinePoints = new List<SplinePoint>();
    [Tooltip("是否闭环（最后一个连接回第一个）")]
    public bool closedLoop = true;

    // === 碰撞与摧毁 / VFX SFX ===
    [Header("碰撞/摧毁")]
    [Tooltip("障碍物所在的 Layer（应包含 Obstacle）")]
    public LayerMask obstacleLayer = 0; // 在 Inspector 里勾选 Obstacle
    [Tooltip("两次命中计数之间的最小冷却（秒），避免一次接触多次计数")]
    public float hitCooldown = 0.25f;
    [Tooltip("累计命中次数达到此值时触发摧毁")]
    public int hitLimit = 5;

    [Tooltip("被摧毁时播放的特效预制（可空）")]
    public GameObject destroyedVfxPrefab;

    [Tooltip("是否复用同一个碰撞VFX对象（勾上：每次只移动并重播；不勾：每次实例化临时VFX）")]
    public bool reuseSingleCollisionVfx = true;
    [Tooltip("要复用的VFX对象（场景里的GameObject，留空则首次自动实例化Prefab）")]
    public GameObject collisionVfxInstance;
    [Tooltip("不复用时，临时VFX的自动销毁时间（秒）")]
    public float collisionVfxAutoDestroy = 2.5f;

    [Tooltip("每次命中的音效（可空）")]
    public AudioClip collisionSfx;
    [Tooltip("被摧毁的音效（可空）")]
    public AudioClip destroyedSfx;
    [Tooltip("用于播放音效的 AudioSource（可空；为空则使用 PlayClipAtPoint）")]
    public AudioSource audioSource;

    // --- 依赖 ---
    Rigidbody rb;
    FloatingObject floating;            // Crest 的浮动脚本（提供 CurrentFlowXZ）
    Vector3 prevFlowDir;               // 流向兜底
    Vector3 upN;

    // --- 状态（碰撞） ---
    int hitCount = 0;
    float lastHitTime = -999f;
    bool isDestroyed = false;

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

        // 若未在 Inspector 指定 LayerMask，则默认尝试取 "Obstacle"
        if (obstacleLayer.value == 0)
        {
            int obst = LayerMask.NameToLayer("Obstacle");
            if (obst >= 0) obstacleLayer = (1 << obst);
        }
    }

    void FixedUpdate()
    {
        if (isDestroyed) return;
        if (!GameManager.Instance.GetGameAction()) return;

        Vector3 n = upN;

        // --- 基础向量：水流、速度（都投影到平面） ---
        Vector3 flow = floating ? floating.CurrentFlowXZ : transform.forward;
        flow = Vector3.ProjectOnPlane(flow, n);
        Vector3 flowDir = flow.sqrMagnitude > 1e-6f ? flow.normalized : prevFlowDir;

        Vector3 v = rb.velocity;
        Vector3 hv = Vector3.ProjectOnPlane(v, n);
        Vector3 velDir = hv.sqrMagnitude > 1e-6f ? hv.normalized : flowDir;

        // 横向（垂直）向量：水流垂线 & 速度垂线
        Vector3 perpFlow = Vector3.Cross(n, flowDir).normalized;   // “右向”
        Vector3 perpVel = Vector3.Cross(n, velDir).normalized;

        if (flipPerp) { perpFlow = -perpFlow; perpVel = -perpVel; }

        // 目标横向方向：在两者之间球面插值
        Vector3 targetPerp = Vector3.Slerp(perpFlow, perpVel, Mathf.Clamp01(slipYawBlend)).normalized;

        // --- 扭矩 PD：仅绕 n 对齐 ---
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

        // --- 推进 & 玩家输入 ---
        rb.AddForce(flowDir * flowAccel, ForceMode.Acceleration);

        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        if (left && !right)
            rb.AddForce(transform.TransformDirection(leftForceLocalDir.normalized) * inputAccel, ForceMode.Acceleration);
        else if (right && !left)
            rb.AddForce(transform.TransformDirection(rightForceLocalDir.normalized) * inputAccel, ForceMode.Acceleration);

        // --- 离心增强（用 spline 参照） ---
        if (lookaheadDist > 0.01f && centrifugalGain > 0f && splinePoints != null && splinePoints.Count >= 2)
        {
            Vector3 pathDirNow = GetFlowDirectionAt(transform.position);
            Vector3 pathDirAhead = GetFlowDirectionAt(transform.position + flowDir * lookaheadDist);

            if (pathDirNow.sqrMagnitude > 1e-6f && pathDirAhead.sqrMagnitude > 1e-6f)
            {
                float turnRad = SignedAngleRad(pathDirNow, pathDirAhead, n);     // 左转>0，右转<0
                float curvature = Mathf.Abs(turnRad) / Mathf.Max(lookaheadDist, 1e-3f);

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

    // ====== 碰撞检测（Layer=Obstacle） ======
    void OnCollisionEnter(Collision c)
    {
        if (isDestroyed) return;
        if (!IsObstacleLayer(c.collider.gameObject.layer)) return;

        Vector3 hitPos = c.contacts.Length > 0 ? c.contacts[0].point : c.collider.bounds.ClosestPoint(transform.position);
        RegisterHit(hitPos, c.collider.transform.up);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDestroyed) return;
        if (!IsObstacleLayer(other.gameObject.layer)) return;

        Vector3 hitPos = other.bounds.ClosestPoint(transform.position);
        RegisterHit(hitPos, other.transform.up);
    }

    bool IsObstacleLayer(int layer) => (obstacleLayer.value & (1 << layer)) != 0;

    // —— 修复点：致命一击时不播放碰撞VFX，直接播放“摧毁VFX” —— 
    void RegisterHit(Vector3 pos, Vector3 normalUp)
    {
        if (Time.time - lastHitTime < hitCooldown) return; // 冷却，避免多次计数
        lastHitTime = Time.time;

        int newCount = hitCount + 1;

        // 到达上限：只播摧毁，不播碰撞
        if (newCount >= hitLimit)
        {
            hitCount = newCount;
            StopCollisionVfxIfAny(); // 避免复用的碰撞特效此刻覆盖
            DoDestroySequence(pos, normalUp);
            return;
        }

        // 普通命中：播放碰撞VFX与SFX
        hitCount = newCount;
        MoveOrSpawnCollisionVfx(pos, normalUp);
        PlaySfx(collisionSfx, pos);
    }

    // —— 摧毁流程：在命中点生成摧毁VFX、解绑玩家、下沉 —— 
    void DoDestroySequence(Vector3 pos, Vector3 normalUp)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // 摧毁VFX
        if (destroyedVfxPrefab)
        {
            Vector3 up = (normalUp.sqrMagnitude > 1e-6f ? normalUp : upN).normalized;
            Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            if (forwardOnPlane.sqrMagnitude < 1e-6f) forwardOnPlane = Vector3.Cross(up, transform.right).normalized;
            Quaternion rot = Quaternion.LookRotation(forwardOnPlane, up);
            //Vector3 offset = Vector3.up * 2f; // Y 轴向上偏移一点，避免被地面遮挡
            RestartVfx(Instantiate(destroyedVfxPrefab, pos, rot));
        }
        PlaySfx(destroyedSfx, pos);

        // 解除玩家扒附
        var clingers = FindObjectsOfType<PlayerClingToLog>(true);
        foreach (var cl in clingers)
        {
            if (cl && cl.log == this.transform)
                cl.DetachFromLog(true);
        }

        // 停止浮动，下沉
        if (floating) floating.enabled = false;
        rb.useGravity = true;

        // 停止本控制器继续施力
        enabled = false;
    }

    // —— 碰撞VFX：移动/生成到命中点并重播 —— 
    void MoveOrSpawnCollisionVfx(Vector3 hitPos, Vector3 normalUp)
    {
        Vector3 up = (normalUp.sqrMagnitude > 1e-6f ? normalUp : upN).normalized;
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        if (forwardOnPlane.sqrMagnitude < 1e-6f) forwardOnPlane = Vector3.Cross(up, transform.right).normalized;
        Quaternion rot = Quaternion.LookRotation(forwardOnPlane, up);

        if (reuseSingleCollisionVfx)
        {
            if (collisionVfxInstance)
            {
                collisionVfxInstance.transform.SetPositionAndRotation(hitPos, rot);
                RestartVfx(collisionVfxInstance);
            }
            return;
        }

        if (collisionVfxInstance)
        {
            var go = Instantiate(collisionVfxInstance, hitPos, rot);
            RestartVfx(go);
            if (collisionVfxAutoDestroy > 0f) Destroy(go, collisionVfxAutoDestroy);
        }
    }

    void RestartVfx(GameObject go)
    {
        if (!go) return;

        // 1) ParticleSystem：支持多个
        var pss = go.GetComponentsInChildren<ParticleSystem>(true);
        if (pss != null && pss.Length > 0)
        {
            for (int i = 0; i < pss.Length; i++)
            {
                var ps = pss[i];
                // 建议粒子系统的 Simulation Space 设为 World，避免被“搬家”时拖尾
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);
                ps.Play(true);
            }
        }

        // 2) VFX Graph（如果你的工程里用了 VisualEffect）
#if UNITY_2019_3_OR_NEWER
        var vfx = go.GetComponent<UnityEngine.VFX.VisualEffect>();
        if (vfx)
        {
            // 重新初始化可确保从头播放（如果你用的是比较老的版本，改成 vfx.Stop(); vfx.Play(); 也行）
            vfx.Reinit();
            vfx.Play();
        }
#endif
    }

    void StopCollisionVfxIfAny()
    {
        if (!collisionVfxInstance) return;

        var pss = collisionVfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        if (pss != null && pss.Length > 0)
        {
            for (int i = 0; i < pss.Length; i++)
                pss[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
#if UNITY_2019_3_OR_NEWER
        var vfx = collisionVfxInstance.GetComponent<UnityEngine.VFX.VisualEffect>();
        if (vfx) vfx.Stop();
#endif
    }

    void PlaySfx(AudioClip clip, Vector3 pos)
    {
        if (!clip) return;
        if (audioSource) audioSource.PlayOneShot(clip);
        else AudioSource.PlayClipAtPoint(clip, pos);
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

        Vector3 ab = Vector3.ProjectOnPlane(b - a, upN);
        float len = ab.magnitude;
        if (len < 1e-6f) return;

        Vector3 dir = ab / len;

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
