using System.Collections.Generic;
using UnityEngine;

/* 3 个场上角色 */
public enum RG_ROLE { Striker, Mid, Defender }

[RequireComponent(typeof(Rigidbody))]
public class WaterPlayer : MonoBehaviour
{
    public static bool isPaused = false;
    public bool IsPaused => isPaused;

    /* ---------- Boundary / Goal ---------- */
    [Header("Boundary / Goal")]
    public LayerMask boundaryMask;    // 边界墙 Layer（实体，非 Trigger）
    public LayerMask goalMask;        // 球门/门柱/球网 Layer（Trigger 也可）

    [Tooltip("沿前进方向的探测半径"), Range(0.1f, 2f)]
    public float boundaryProbeRadius = 0.6f;
    [Tooltip("沿前进方向的探测距离"), Range(0.2f, 5f)]
    public float boundaryProbeAhead = 1.2f;
    [Tooltip("认为“靠墙”的最近距离"), Range(0.2f, 3f)]
    public float boundaryNearDistance = 1.1f;
    public float boundaryEscapeMeters = 8f;
    public float boundaryEscapePower = 13f;

    /* ---------- 远距防墙（软评分 + 否决） ---------- */
    [Header("Far-wall Lookahead Veto")]
    public float wallLookahead = 10f;
    public float hazardProbeRadius = 0.22f;
    [Range(0f, 1f)] public float wallHazardVeto = 0.65f;
    [Range(0f, 1f)] public float wallHazardRotate = 0.55f;
    public float goalToleranceDeg = 18f;

    /* ---------- 近墙识别/解卡 ---------- */
    [Header("Near-wall / Corner")]
    public float wallDetectRadius = 1.6f;
    public float wallNearThreshold = 1.0f;
    public float wallKickCooldown = 0.40f;
    private float _nextWallKick;

    [Header("Wall Zone Centering")]
    public float wallZoneWidth = 1.6f;
    public float centerKickAhead = 8f;
    public float centerKickPower = 13f;
    public float centerKickCooldown = 0.30f;
    private float _nextCenterKick;

    /* ---------- 近墙“硬夹角” ---------- */
    [Header("Near-wall Hard Clamp")]
    [Tooltip("小于该距离时，强制把方向夹回场内")]
    public float wallHardClampDist = 2.0f;
    [Tooltip("出脚方向与墙面法线(指向场内)的最小点积，越大越“背离墙”")]
    [Range(0f, 0.9f)] public float wallOutDotMin = 0.45f;
    [Tooltip("近墙时朝场心的权重（0~1；1=只看场心）")]
    [Range(0f, 1f)] public float centerOverrideWeight = 0.7f;

    /* ---------- “沿墙切向”硬否决 ---------- */
    [Header("Wall Tangential Ban")]
    [Tooltip("若距墙 < 本值且方向与墙切向点积>|tangentialDotMin|，则否决该方向")]
    public float wallTangentialVetoDist = 3.0f;
    [Range(0f, 0.999f)] public float tangentialDotMin = 0.85f;

    /* ---------- 门后 Cutback ---------- */
    [Header("Backline Return")]
    [Tooltip("可选：指定一个 Empty，球过门线后回做到这里；留空则自动回中轴-6m")]
    public Transform enemyBacklineReturn;
    public float backlineDepth = 1.0f;

    /* ---------- 避让：墙边球体（AvoidBeacon） ---------- */
    [Header("Avoid Beacons")]
    [Tooltip("沿踢/带方向前视多远去看禁区球")]
    public float beaconLookahead = 12f;
    [Tooltip("禁区球危险度权重（乘因子）")]
    public float beaconHazardWeight = 1.0f;
    [Tooltip("禁区球半径外再加一点安全余量")]
    public float beaconPadding = 0.3f;

    [Header("Avoid Beacon HARD BAN")]
    [Tooltip("是否对 Beacon 使用硬禁飞（线段穿过就直接否决）")]
    public bool beaconHardBan = true;
    [Tooltip("沿方向检查多远（米）用于判断是否穿过 Beacon 区域）")]
    public float beaconBanLookahead = 50f;
    [Tooltip("在 Beacon 半径之外再加的硬禁余量（米）")]
    public float beaconBanRadiusPadding = 5f;

    /* ---------- “朝墙硬禁飞” & 球场几何兜底 ---------- */
    [Header("Wall HARD BAN")]
    [Tooltip("朝某方向只要能撞到边界墙（非球门），直接否决该方向；若能穿过球门口则放行")]
    public bool wallHardBan = true;
    [Tooltip("硬禁飞的前视距离（米）——建议≥半场长度")]
    public float wallBanLookahead = 80f;
    [Tooltip("硬禁飞的SphereCast半径（≈球半径+余量）")]
    public float wallBanProbeRadius = 0.35f;

    [Header("Pitch Failsafe (walls)")]
    [Tooltip("自动在 Start 里收集 Layer=Boundary 的四面墙 Collider")]
    [SerializeField] Collider[] pitchBoundWalls; // 运行时自动填充
    [Tooltip("收集墙的 Layer 名称；留空则使用 boundaryMask")]
    public string boundaryLayerName = "Boundary";
    public float pitchMargin = 0.20f;  // 离边线的安全余量（米）

    /* ---------- 角色 / 支援点 ---------- */
    [Header("Role / Support")]
    public RG_ROLE role = RG_ROLE.Mid;
    public SupportSpotManager spotManager;   // 可为空
    [Tooltip("角色锚点 vs 支援点的权重（越小越听支援点）"), Range(0f, 1f)]
    public float anchorBias = 0.45f;

    [Header("Search Radius (for role offset)")]
    public float searchRadius_Striker = 14f;
    public float searchRadius_Mid = 11f;
    public float searchRadius_Def = 10f;

    /* ---------- Distances / Abilities ---------- */
    [Header("Distances")]
    public float distPassMin = 6f;
    public float distPassMax = 25f;
    public float closeAutoShootDist = 3.0f;
    public float shotMinDist = 8.0f;
    public float shotMaxDist = 22.0f;

    [Header("Abilities (0~1)")]
    [Range(0.1f, 1f)] public float accuracy = .85f;
    [Range(0.1f, 1f)] public float power = .90f;

    /* ---------- Movement / Animation / Spacing ---------- */
    [Header("Movement / Animation / Spacing")]
    public float baseSpeed = 3.7f;
    public float sprintMultiplier = 1.9f;
    public bool alwaysSprint = true;
    public float turnSpeed = 540f;
    public Animator animator;
    public float maxSwimSpeed = 5f;

    public float separationRadius = 3.5f;
    [Range(0f, 1f)] public float separationWeight = 0.55f;

    /* === 模型朝向修复：flipForward 开关 === */
    [Header("Model Facing")]
    public bool flipForward = true;   // 勾上时，模型正面为 -前进方向

    /* ---------- First Touch / Quick ---------- */
    [Header("First Touch / Quick")]
    public float dribbleTapPower = 7.0f;
    public float dribbleTapMeters = 3.5f;
    public float dribbleTapCooldown = 0.18f;
    public float quickFirstTouchWindow = 0.35f;
    public float quickThreatRadius = 5.5f;

    /* ---------- 传球倾向 ---------- */
    [Header("Passing Tuning")]
    [Range(0f, 1f)] public float passAggression = 0.75f;  // 越大越爱传
    [Range(0f, 1f)] public float passMinScore = 0.26f;    // 最低通过评分
    public float passLaneHalfWidth = 0.9f;               // 传球线走廊半宽
    public float passOppClearRadius = 2.0f;              // 对手离走廊/落点安全距离
    public float passCooldown = 0.18f;
    private float _nextPassAt;

    /* ---------- 稳定性（防抽搐） ---------- */
    [Header("Stability / Hysteresis")]
    [Tooltip("最短保持 Idle 状态的时间")]
    public float stateMinHold_Idle = 0.35f;
    [Tooltip("最短保持 Chase 状态的时间")]
    public float stateMinHold_Chase = 0.35f;
    [Tooltip("最短保持 Decision 状态的时间")]
    public float stateMinHold_Decision = 0.25f;

    [Tooltip("重新计算带球方向的节流（秒）")]
    public float driveRecalcInterval = 0.18f;
    [Tooltip("带球方向的平滑（0~1，越大越跟随新方向）")]
    [Range(0f, 1f)] public float driveDirSmoothing = 0.35f;

    [Tooltip("传/射再次评估间隔（秒）的最小/最大值")]
    public Vector2 decisionTickRange = new Vector2(0.18f, 0.35f);

    [Header("Ownership Safety")]
    public float dropOwnerIfFar = 2.3f;          // 距离超过这个值就放掉 Owner
    public float dropOwnerIfAngleDeg = 70f;      // 球速与“自己->球”夹角太背离也放掉

    /* ---------- Team & Pitch ---------- */
    [Header("Team & Pitch")]
    public bool isTeammate;
    public Goal friendlyGoal;
    public Goal enemyGoal;
    public List<WaterPlayer> team = new();
    public List<WaterPlayer> opponents = new();

    [Header("Human Receiver")]
    public Transform human;                 // 指向玩家角色（PlayerMovement/PlayerController的transform）
    [Range(0f, 1f)] public float humanPassBias = 0.25f;   // 只是保留参数位，当前方案用“优先通道”更简单直接
    public float humanPassMaxDist = 28f;
    public float humanOpenThresh = 0.55f;   // 通道安全阈值（0~1）

    /* ---------- Runtime ---------- */
    public Ball ball { get; private set; }
    Rigidbody rb;

    public bool HasBall => ball && ball.Owner == this;
    public Vector3 Pos => new Vector3(transform.position.x, 0, transform.position.z);
    public Vector3 FieldForward => (enemyGoal.transform.position - friendlyGoal.transform.position).normalized;
    public Vector3 FieldRight => Vector3.Cross(Vector3.up, FieldForward);

    /* FSM（状态类在 WaterAIState.cs） */
    WAState state;
    public string stateStr;
    float holdStart;
    float stateLockUntil;
    public bool CanChangeState => Time.time >= stateLockUntil;

    /* 追球者（每队 1 名） */
    public static WaterPlayer[] BallChaser = new WaterPlayer[2];
    private static float[] chaserExpiry = { 0f, 0f };
    public int TeamIdx => isTeammate ? 0 : 1;

    /* ---------- Unity ---------- */
    public void Pause() => rb.constraints = RigidbodyConstraints.FreezeAll;
    public void Resume() => rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ball = FindObjectOfType<Ball>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!spotManager) spotManager = FindObjectOfType<SupportSpotManager>(); // 可为空
    }

    void Start()
    {
        Change(new WIdle(this), 0f);
        CollectPitchWalls(); // ★ 自动收集边界墙
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) CollectPitchWalls();
    }
#endif

    void Update() { if (!IsPaused) { state?.Update(); stateStr = state.name; } }
    void FixedUpdate() { if (IsPaused) return; }

    void LateUpdate()
    {
        if (!ball) return;

        // 如果自己“拥有球”，但已经明显“离球太远/方向背离”，立刻放掉，避免任何吸附效果
        if (ball.Owner == this)
        {
            float d = Vector3.Distance(Pos, ball.Pos);
            if (d > dropOwnerIfFar) { ball.Owner = null; return; }

            Vector3 toBall = ball.Pos - Pos; toBall.y = 0f;
            Vector3 v = ball.Rb ? new Vector3(ball.Rb.velocity.x, 0, ball.Rb.velocity.z) : Vector3.zero;

            if (v.sqrMagnitude > 0.01f && toBall.sqrMagnitude > 0.01f)
            {
                float ang = Vector3.Angle(toBall, v);
                if (ang > dropOwnerIfAngleDeg) { ball.Owner = null; return; }
            }
        }
    }

    /* ---------- FSM hooks ---------- */
    public void Change(WAState s, float lockFor = 0.22f) { state = s; state.Enter(); stateLockUntil = Time.time + Mathf.Max(0f, lockFor); }
    public void StartHold() => holdStart = Time.time;
    public bool InFirstTouch => Time.time - holdStart <= quickFirstTouchWindow;

    /* ---------- 团队/球相关的基础方法 ---------- */
    public bool TeamHasBall => ball && ball.Owner && (ball.Owner.isTeammate == isTeammate);

    public bool IsClosestToBallInTeam()
    {
        if (!ball) return false;
        float my = Vector3.Distance(Pos, ball.Pos);
        foreach (var m in team) { if (!m || m == this) continue; if (Vector3.Distance(m.Pos, ball.Pos) < my - 0.05f) return false; }
        return true;
    }

    public bool ShouldChaseBall()
    {
        if (!ball) return false;
        if (TeamHasBall && !HasBall) return false;

        if (BallChaser[TeamIdx] == this && Time.time < chaserExpiry[TeamIdx]) return true;

        if (BallChaser[TeamIdx] == null || Time.time >= chaserExpiry[TeamIdx])
        {
            if (IsClosestToBallInTeam())
            {
                BallChaser[TeamIdx] = this;
                chaserExpiry[TeamIdx] = Time.time + 1.2f;
                return true;
            }
        }
        return false;
    }

    /* ---------- Move / Face / Spacing ---------- */
    public void MoveTo(Vector3 target)
    {
        Vector3 dir = target - Pos; dir.y = 0;
        if (dir.sqrMagnitude < 0.05f) { if (animator) animator.SetFloat(_blend, 0f); return; }
        dir.Normalize();

        // 与队友分离
        Vector3 repel = Vector3.zero;
        foreach (var m in team) { if (!m || m == this) continue; float d = Vector3.Distance(Pos, m.Pos); if (d < separationRadius) repel += (Pos - m.Pos) * (1f / Mathf.Max(d, 0.4f)); }
        dir = (dir + repel * separationWeight).normalized;

        float speed = baseSpeed * (alwaysSprint ? sprintMultiplier : 1f);
        Vector3 h = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(dir * speed - h, ForceMode.Acceleration);

        // 使用 flipForward 开关控制朝向
        Vector3 face = flipForward ? -dir : dir;
        Quaternion q = Quaternion.LookRotation(face, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, q, turnSpeed * Time.deltaTime);

        if (animator) { float hv = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude; animator.SetFloat(_blend, Mathf.Clamp01(hv / maxSwimSpeed), 0.12f, Time.deltaTime); }
    }

    public void MoveToBallPred(float leadFactor = 1.1f, float maxLead = 1.4f)
    {
        if (IsPaused || ball == null) return;
        Vector3 v = ball.Rb ? ball.Rb.velocity : Vector3.zero; v.y = 0f;
        float d = Vector3.Distance(Pos, ball.Pos);
        float t = (baseSpeed > 0f) ? d / (baseSpeed * Mathf.Max(leadFactor, 0.01f)) : 0f;
        t = Mathf.Clamp(t, 0f, Mathf.Max(maxLead, 0f));
        Vector3 target = (v.sqrMagnitude > 0.01f) ? (ball.Pos + v * t) : ball.Pos;
        MoveTo(target);
    }

    /* ---------- 角色锚点 & 支援点 ---------- */
    public Vector3 GetRoleAnchor(bool possessionUs)
    {
        Vector3 f = FieldForward, r = FieldRight, b = ball ? ball.Pos : Pos;
        Vector3 gF = friendlyGoal.transform.position, gE = enemyGoal.transform.position;
        float L = Mathf.Max(0.01f, Vector3.Dot(gE - gF, f));
        float tBall = ball ? Mathf.Clamp01(Vector3.Dot(b - gF, f) / L) : 0.5f;

        if (possessionUs)
        {
            switch (role)
            {
                case RG_ROLE.Striker: return ClampY(b + f * 12f);
                case RG_ROLE.Mid: return ClampY(b + f * 8.5f + r * (isTeammate ? +3.5f : -3.5f));
                case RG_ROLE.Defender:
                    float basePush = Mathf.Lerp(6f, L * 0.55f, Mathf.SmoothStep(0f, 1f, tBall));
                    float minPush = Mathf.Lerp(L * 0.40f, L * 0.52f, Mathf.InverseLerp(0.5f, 1f, tBall));
                    float push = Mathf.Max(basePush, minPush);
                    return ClampY(gF + f * push + r * (isTeammate ? -6f : +6f));
            }
        }
        else
        {
            Vector3 dir = (b - gF); dir.y = 0; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : f;
            switch (role)
            {
                case RG_ROLE.Striker: return ClampY(gF + dir * 11f);
                case RG_ROLE.Mid: return ClampY(gF + dir * 8f + r * (isTeammate ? +4.5f : -4.5f));
                case RG_ROLE.Defender: return ClampY(gF + dir * 5.5f);
            }
        }
        return ClampY(b + f * 6f);
    }

    Vector3 ClampY(Vector3 p) => new Vector3(p.x, 0, p.z);

    public Transform GetSupportSpot()
    {
        if (!spotManager) return null;
        bool possessionUs = TeamHasBall;
        return spotManager.GetBestSpot(
            ball ? ball.Pos : Pos,
            friendlyGoal.transform.position,
            enemyGoal.transform.position,
            possessionUs,
            opponents,
            team
        );
    }

    /* ---------- First Touch：拿球瞬间先做合理动作 ---------- */
    public bool FirstTouchPlay(out string reason)
    {
        reason = null;
        if (!HasBall) return false;

        if (IsPastEnemyGoalLine(out Vector3 backT)) { PassOrKick(backT, false); reason = "Cutback"; return true; }

        // 0) 玩家优先
        if (TryHumanPass(out var hT)) { Pass(hT); reason = "PassToHuman"; return true; }

        // 1) 可射先射
        if (CanShootSmart(out Vector3 g)) { Shoot(g); reason = "ShootFirst"; return true; }

        // 2) 再考虑AI队友传球
        bool preferFwd = (role == RG_ROLE.Defender) || IsInAttackingHalf();
        if (FindBestPassOption(out Vector3 tgt, out _, preferFwd)) { Pass(tgt); reason = "PassFirst"; return true; }

        // 3) 贴墙处理
        if (TryBoundaryEscapeOrQuickRelease(out string why)) { reason = why; return true; }

        return false;
    }

    /* ---------- 方向工具 ---------- */
    Vector3 ForceAwayFromWall(Vector3 dir, Vector3 wallN, float minDot)
    {
        dir.y = 0; wallN.y = 0;
        if (dir.sqrMagnitude < 1e-6f) return wallN.normalized;
        dir.Normalize(); wallN.Normalize();

        float d = Vector3.Dot(dir, wallN);
        if (d >= minDot) return dir;

        Vector3 tangential = Vector3.ProjectOnPlane(dir, wallN).normalized;
        float a = Mathf.Clamp01(minDot);
        Vector3 adj = (tangential * Mathf.Sqrt(1f - a * a) + wallN * a).normalized;
        return adj;
    }
    Vector3 ComputeSafeCenterDirNearWall(Vector3 wallN)
    {
        Vector3 toCenter = ToCenterDir();
        Vector3 desired = Vector3.Slerp(FieldForward, toCenter, centerOverrideWeight).normalized;
        return ForceAwayFromWall(desired, wallN, wallOutDotMin);
    }
    Vector3 SteerAwayFromPoint(Vector3 dir, Vector3 from, Vector3 point, float strength = 0.6f)
    {
        Vector3 away = from - point; away.y = 0f;
        if (away.sqrMagnitude < 1e-6f) return dir;
        away.Normalize();
        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) return away;
        return Vector3.Slerp(dir.normalized, away, Mathf.Clamp01(strength)).normalized;
    }

    /* ---------- Drive 方向选择（强化：墙切向否决 + 壁距→场心权重 + 门口豁免） ---------- */
    public Vector3 ComputeDriveDir()
    {
        // 近墙硬夹角仍然优先生效
        if (TryGetWallNormalRobust(out Vector3 wallN, out float wallDist) && wallDist <= wallHardClampDist)
            return ComputeSafeCenterDirNearWall(wallN);

        Vector3 toGoal = (enemyGoal.transform.position - (ball ? ball.Pos : Pos)); toGoal.y = 0;
        toGoal = toGoal.sqrMagnitude > 0.01f ? toGoal.normalized : FieldForward;
        Vector3 toCenter = ToCenterDir();

        // 原有 5 个候选（保留）
        Vector3[] seeds = {
        (toGoal + toCenter).normalized,
        toGoal,
        (toGoal + FieldRight*0.55f).normalized,
        (toGoal - FieldRight*0.55f).normalized,
        toCenter
    };

        float best = float.NegativeInfinity;
        Vector3 bestDir = toCenter;

        // 先用原候选跑一遍（保留旧行为）
        foreach (var d0 in seeds)
        {
            if (d0.sqrMagnitude < 1e-6f) continue;
            float sc0 = ScoreDirection(d0.normalized, toGoal, toCenter, out bool veto0);
            if (!veto0 && sc0 > best) { best = sc0; bestDir = d0.normalized; }
        }

        // 再做一圈 360° 低开销采样（固定 N=24）
        const int N = 24;
        Vector3 baseDir = Vector3.Slerp(toCenter, FieldForward, 0.25f).normalized;
        for (int i = 0; i < N; i++)
        {
            Vector3 d = Quaternion.AngleAxis(360f * i / N, Vector3.up) * baseDir; d.y = 0f;
            if (d.sqrMagnitude < 1e-6f) continue;
            d.Normalize();

            float sc = ScoreDirection(d, toGoal, toCenter, out bool veto);
            if (!veto && sc > best) { best = sc; bestDir = d; }
        }

        // 兜底
        if (best == float.NegativeInfinity) return toCenter;
        return bestDir;
    }


    float ScoreDirection(Vector3 dir, Vector3 toGoal, Vector3 toCenter, out bool vetoed)
    {
        vetoed = false;
        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) { vetoed = true; return float.NegativeInfinity; }
        dir.Normalize();

        // ① 硬禁：Beacon
        if (beaconHardBan && DirectionBannedByBeacon(dir, beaconBanLookahead, beaconBanRadiusPadding, out _))
        { vetoed = true; return float.NegativeInfinity; }

        // ② 硬禁：墙（允许“穿过球门口”的特例）
        if (DirectionBlockedByBoundaryFirstHit(dir, wallBanLookahead) && !AllowIfThroughGoalMouth(dir, wallBanLookahead))
        { vetoed = true; return float.NegativeInfinity; }

        // ③ 近墙：切向硬否决（不允许沿边线平行推进）
        if (TryGetWallNormalRobust(out Vector3 wallN, out float wallDist))
        {
            if (wallDist < wallTangentialVetoDist)
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, wallN).normalized;
                float ad = Mathf.Abs(Vector3.Dot(dir, tangent));
                if (ad >= tangentialDotMin)
                { vetoed = true; return float.NegativeInfinity; }
            }
        }

        // ④ 软风险评分
        float hzWall = ComputeWallHazard(dir, wallLookahead, out _, out bool hitGoal);
        float hzBeacon = ComputeBeaconHazard(dir, beaconLookahead, out _, out _);
        float hazard = Mathf.Max(hzWall, beaconHazardWeight * hzBeacon);
        if (!hitGoal && hazard >= wallHazardVeto)
        { vetoed = true; return float.NegativeInfinity; }

        // ⑤ 基础得分：向前 + 向中
        float advance = Vector3.Dot(dir, FieldForward);
        float centric = Vector3.Dot(dir, toCenter);
        float score = 0.70f * advance + 0.30f * centric - 1.0f * hazard;

        // ⑥ 壁距→场心的**额外加分**（连续函数，越靠边越强推向中）
        if (TryGetWallNormalRobust(out _, out float dWall))
        {
            float w = Mathf.InverseLerp(6f, 1f, Mathf.Clamp(dWall, 0f, 6f)); // d<=1m→1，d>=6m→0
            score += w * Mathf.Clamp01(Vector3.Dot(dir, toCenter)) * 0.6f;
        }

        // ⑦ 命中球门方向的微增益
        bool inGoalCone = Vector3.Angle(dir, toGoal) <= goalToleranceDeg;
        if (hitGoal && inGoalCone) score += 0.15f;

        return score;
    }

    float ComputeWallHazard(Vector3 dir, float lookahead, out Vector3 hitN, out bool hitGoal)
    {
        hitN = Vector3.zero; hitGoal = false;
        int mask = boundaryMask | goalMask;
        Vector3 origin = (ball ? ball.Pos : Pos) + Vector3.up * 0.2f;

        if (Physics.SphereCast(origin, hazardProbeRadius, dir, out var hit, lookahead, mask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<AvoidBeacon>() != null) return 0f;
            hitN = hit.normal;
            hitGoal = ((1 << hit.collider.gameObject.layer) & goalMask) != 0;
            return Mathf.Clamp01(1f - hit.distance / Mathf.Max(lookahead, 0.01f));
        }
        return 0f;
    }

    // —— 避让球体（评分用途） —— //
    float ComputeBeaconHazard(Vector3 dir, float lookahead, out AvoidBeacon worst, out float worstDist)
    {
        worst = null; worstDist = float.MaxValue;
        if (AvoidBeacon.All.Count == 0) return 0f;
        Vector3 a = (ball ? ball.Pos : Pos); a.y = 0f;
        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) return 0f; dir.Normalize();

        float worstH = 0f;
        foreach (var b in AvoidBeacon.All)
        {
            if (!b) continue;
            Vector3 c = b.transform.position; c.y = 0f;

            Vector3 ap = c - a;
            float t = Mathf.Clamp(Vector3.Dot(ap, dir), 0f, lookahead);
            Vector3 closest = a + dir * t;
            float d = Vector3.Distance(closest, c);

            float r = Mathf.Max(0.01f, b.radius + beaconPadding);
            float h = Mathf.Clamp01(1f - d / r) * Mathf.Max(0f, 1f - t / Mathf.Max(lookahead, 0.01f)) * b.weight;
            if (h > worstH) { worstH = h; worst = b; worstDist = d; }
        }
        return worstH;
    }

    public Vector3 ToCenterDir()
    {
        Vector3 center = 0.5f * (friendlyGoal.transform.position + enemyGoal.transform.position);
        Vector3 d = (center - (ball ? ball.Pos : Pos)); d.y = 0; return d.sqrMagnitude > 0.01f ? d.normalized : -FieldRight;
    }

    /* ---------- 统一“出脚方向修正” ---------- */
    Vector3 ResolveKickDirection(Vector3 desired, float lookahead)
    {
        Vector3 d = desired; d.y = 0f;
        // ★ 关键：0 向量回退到“朝场心”，不要用 FieldForward
        if (d.sqrMagnitude < 1e-6f) d = ToCenterDir();
        d.Normalize();

        // 1) Beacon 硬禁→强行扭出
        int tries = 3;
        while (tries-- > 0 && beaconHardBan && DirectionBannedByBeacon(d, beaconBanLookahead, beaconBanRadiusPadding, out AvoidBeacon banB))
            d = PushDirOutOfBeacon(d, ball ? ball.Pos : Pos, banB, 0.9f);

        // 2) 近墙切向硬否决→强压向场心
        if (TryGetWallNormalRobust(out Vector3 wallN, out float wallD) && wallD < wallTangentialVetoDist)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, wallN).normalized;
            if (Mathf.Abs(Vector3.Dot(d, tangent)) >= tangentialDotMin)
                d = ComputeSafeCenterDirNearWall(wallN);
        }

        // 3) 朝墙硬禁，但允许“先撞到球门（goalMask/Goal组件）”
        if (DirectionBlockedByBoundaryFirstHit(d, lookahead))
            d = ToCenterDir();

        return d.normalized;
    }


    /* ---------- 带球 ---------- */
    float _nextDribbleTap;
    public void DribbleAlong(Vector3 dir /* 保留签名，不再使用 */)
    {
        if (!ball) return;
        if (!HasBall)
        {                  // 不持球时只跟到球，严禁出脚
            MoveTo(ball ? ball.Pos : Pos);
            return;
        }

        if (Time.time < _nextDribbleTap) { MoveTo(Pos + ToCenterDir() * 2f); return; }
        if (!ball || Vector3.Distance(Pos, ball.Pos) > 1.8f) { MoveTo(ball ? ball.Pos : Pos); return; }

        // 关键：不再用外部传进来的 dir，统一用内部规划器
        Vector3 desired = ComputeDriveDir();
        if (desired.sqrMagnitude < 1e-6f)
            desired = ToCenterDir(); // 兜底

        // 统一做方向“硬修正”
        Vector3 d = ResolveKickDirection(desired, wallBanLookahead);

        // 如果仍被判定会先撞边界，再兜底到中路
        if (DirectionBlockedByBoundaryFirstHit(d, wallBanLookahead))
            d = ToCenterDir();

        Vector3 tapTarget = (ball ? ball.Pos : Pos) + d * dribbleTapMeters;
        ball.Kick(tapTarget, dribbleTapPower, this);
        _nextDribbleTap = Time.time + dribbleTapCooldown;
    }

    public void SafeDropBallOwnership()
    {
        if (ball && ball.Owner == this) ball.Owner = null;
    }

    // 尝试把方向从某个 Beacon 的禁区里扭出去（大角度偏离）
    Vector3 PushDirOutOfBeacon(Vector3 dir, Vector3 from, AvoidBeacon b, float strength = 0.85f)
    {
        // 朝“远离 beacon 中心”的方向强拉
        return SteerAwayFromPoint(dir, from, b.transform.position, Mathf.Clamp01(strength));
    }

    /* ---------- 快传/回做/墙区处理 ---------- */
    public bool TryBoundaryEscapeOrQuickRelease(out string reason)
    {
        reason = null;
        if (!HasBall) return false;

        // A) 门后：强制回做
        if (IsPastEnemyGoalLine(out Vector3 backT))
        {
            Vector3 d = ResolveKickDirection((backT - (ball ? ball.Pos : Pos)).normalized, wallBanLookahead);
            ball.Kick((ball ? ball.Pos : Pos) + d * Mathf.Max(centerKickAhead, 6f), Mathf.Max(centerKickPower, 12f), this);
            if (animator) animator.SetTrigger(_shoot);
            reason = "BacklineReturn";
            return true;
        }

        bool threatened = IsThreatened(quickThreatRadius);
        bool nearWall = TryGetWallNormalRobust(out Vector3 wallN, out float wallDist);
        bool inWall = nearWall && (wallDist <= wallZoneWidth);

        if ((inWall || threatened) && TryHumanPass(out var hT)) { Pass(hT); reason = "QuickPass(Human)"; return true; }

        // B) 贴墙：先传/可射就射
        if (inWall)
        {
            if (FindBestPassOption(out Vector3 pT, out _, preferForward: true)) { Pass(pT); reason = "QuickPass(Wall)"; return true; }
            if (CanShootSmart(out Vector3 g)) { Shoot(g); reason = "QuickShoot(Wall)"; return true; }
        }
        else if (threatened)
        {
            bool preferFwd = role != RG_ROLE.Striker;
            if (FindBestPassOption(out Vector3 pT2, out _, preferFwd)) { Pass(pT2); reason = "QuickPass"; return true; }
            if (CanShootSmart(out Vector3 g2)) { Shoot(g2); reason = "QuickShoot"; return true; }
        }

        // C) 墙区回中轴（强制出到中路）
        if (inWall && Time.time >= _nextCenterKick)
        {
            Vector3 center = 0.5f * (friendlyGoal.transform.position + enemyGoal.transform.position);
            Vector3 lateral = Vector3.Project(ball.Pos - center, FieldRight);
            Vector3 central = ball.Pos - lateral;
            Vector3 target = central + FieldForward * Mathf.Max(4f, centerKickAhead);

            Vector3 d = ResolveKickDirection((target - ball.Pos).normalized, wallBanLookahead);
            ball.Kick(ball.Pos + d * centerKickAhead, centerKickPower, this);
            if (animator) animator.SetTrigger(_shoot);
            _nextCenterKick = Time.time + centerKickCooldown;
            reason = "CenterKick";
            return true;
        }

        // D) 常规斜线清
        if (nearWall)
        {
            Vector3 diag = PickBestDiagonal(wallN);
            Vector3 d = ResolveKickDirection(diag, wallBanLookahead);
            Vector3 target = ball.Pos + d * boundaryEscapeMeters;
            ball.Kick(target, boundaryEscapePower, this);
            if (animator) animator.SetTrigger(_shoot);
            reason = "DiagonalClear";
            return true;
        }
        return false;
    }

    public void ForceUnjamKick()
    {
        if (Time.time < _nextWallKick) return;
        if (!ball) return;

        // ★ 距离自检：不在球边就别“遥控出脚”
        if (Vector3.Distance(Pos, ball.Pos) > 2.0f) return;

        _nextWallKick = Time.time + wallKickCooldown;

        Vector3 dir;
        if (TryGetWallNormalRobust(out Vector3 wallN, out _)) dir = PickBestDiagonal(wallN);
        else
        {
            Vector3 toC = ToCenterDir();
            dir = (FieldForward * 0.8f + toC * 0.6f).normalized;
        }
        dir = ResolveKickDirection(dir, wallBanLookahead);

        Vector3 target = ball.Pos + dir * Mathf.Max(boundaryEscapeMeters, 6f);
        float p = ball.FindPower(ball.Pos, target, 2.5f) * 1.05f;
        ball.Kick(target, p, this);
        if (animator) animator.SetTrigger(_shoot);
    }


    /* ---------- 传球/射门 ---------- */
    public void Pass(Vector3 tgt)
    {
        if (!HasBall || !ball) return;
        Vector3 d = ResolveKickDirection((tgt - ball.Pos).normalized, wallBanLookahead);
        Vector3 adj = ball.Pos + d * Vector3.Distance(ball.Pos, tgt);
        float pow = ball.FindPower(ball.Pos, adj, 1.2f) * 1.15f;
        ball.Kick(adj, pow, this);
        if (animator) animator.SetTrigger(_shoot);
        _nextPassAt = Time.time + passCooldown;
    }

    public void Shoot(Vector3 tgt)
    {
        if (!HasBall || !ball) return;
        if (ball.Owner != this) return;
        Vector3 d = ResolveKickDirection((tgt - ball.Pos).normalized, wallBanLookahead);
        Vector3 adj = ball.Pos + d * Vector3.Distance(ball.Pos, tgt);
        float pow = ball.FindPower(ball.Pos, adj, 4f) * 1.1f;
        ball.Kick(adj, pow, this);
        if (animator) animator.SetTrigger(_shoot);
    }

    public bool CanShootSmart(out Vector3 goal)
    {
        goal = enemyGoal.transform.position;
        Vector3 a = ball ? ball.Pos : Pos;
        float dist = Vector3.Distance(a, goal);

        if (dist <= closeAutoShootDist) return true;
        if (dist > shotMaxDist) return false;

        Vector3 dir = goal - a; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();

        // 路径可达（先撞墙就不行；穿过球门口豁免）
        if (DirectionBlockedByBoundaryFirstHit(dir, wallBanLookahead)) return false;

        // 额外软风险：靠边 → 向门射的可行度降低
        float wallHz = ComputeWallHazard(dir, wallLookahead, out _, out bool hitGoal);
        float tForward = Mathf.Clamp01(Vector3.Dot(dir, FieldForward) * 0.5f + 0.5f);  // 面向前场
        float tAngle = Mathf.InverseLerp(goalToleranceDeg * 2f, 0f, Vector3.Angle(dir, (goal - a).normalized)); // 越准越高
        float tDist = Mathf.InverseLerp(shotMaxDist, shotMinDist, dist); // 越近越高
        float tSide = TryGetWallNormalRobust(out _, out float dWall) ? Mathf.InverseLerp(6f, 1f, Mathf.Clamp(dWall, 0f, 6f)) : 0f; // 越靠边越降权

        // 评分（去掉随机）
        float score = 0.45f * tDist + 0.30f * tAngle + 0.15f * tForward - 0.25f * wallHz - 0.20f * tSide;

        // 在对方半场，可稍微降低阈值
        float need = IsInAttackingHalf() ? 0.48f : 0.58f;

        return hitGoal || score >= need;
    }


    // —— 传球：避开禁区球的线路 —— //
    public bool FindBestPassOption(out Vector3 target, out WaterPlayer recv, bool preferForward)
    {
        target = Vector3.zero; recv = null;
        if (Time.time < _nextPassAt || team == null || team.Count == 0 || ball == null) return false;

        float best = float.NegativeInfinity;
        Vector3 toC = ToCenterDir();

        foreach (var m in team)
        {
            if (!m || m == this) continue;

            Vector3 seg = m.Pos - ball.Pos; seg.y = 0f;
            float dist = seg.magnitude;
            if (dist < distPassMin || dist > distPassMax) continue;

            Vector3 dir = seg / Mathf.Max(0.01f, dist);

            // 硬禁：墙线（允许门口豁免）或 Beacon
            if ((DirectionBlockedByBoundaryFirstHit(dir, dist) && !AllowIfThroughGoalMouth(dir, dist)) ||
                (beaconHardBan && DirectionBannedByBeacon(dir, dist, beaconBanRadiusPadding, out _)))
                continue;

            float progress = Mathf.Clamp01(Vector3.Dot(dir, FieldForward) * 0.5f + 0.5f); // 向前=1，向后=0
            float centric = Mathf.Clamp01(Vector3.Dot(dir, toC) * 0.5f + 0.5f);

            float diag = Mathf.Max(
                Vector3.Dot(dir, (FieldForward + FieldRight).normalized),
                Vector3.Dot(dir, (FieldForward - FieldRight).normalized)
            );
            diag = Mathf.Clamp01(diag);

            // 走廊安全：对手
            float laneSafe = MinOpponentDistanceToSegment(ball.Pos, m.Pos, out float minToLine);
            float openScore = Mathf.InverseLerp(passOppClearRadius * 0.5f, passOppClearRadius * 2.2f, Mathf.Min(laneSafe, minToLine));

            // —— 禁区球走廊安全（硬过滤） —— //
            float beaconClear = MinBeaconClearanceOnSegment(ball.Pos, m.Pos, out _);
            if (beaconClear < passLaneHalfWidth) continue;

            float ideal = Mathf.Lerp(distPassMin, distPassMax, 0.45f);
            float distScore = Mathf.Exp(-Mathf.Pow((dist - ideal) / (0.55f * ideal), 2f));

            float wFwd = preferForward ? 0.45f : 0.25f;
            float score = wFwd * progress + 0.20f * centric + 0.20f * openScore + 0.15f * distScore + (preferForward ? 0.12f : 0.05f) * diag;

            if (progress < 0.45f) score -= 0.08f * (0.45f - progress);

            if (score > best && score >= passMinScore * (0.8f + 0.4f * passAggression))
            { best = score; target = m.Pos; recv = m; }
        }
        return recv != null;
    }

    float MinOpponentDistanceToSegment(Vector3 a, Vector3 b, out float minToLine)
    {
        minToLine = float.MaxValue;
        float best = float.MaxValue;
        foreach (var o in opponents)
        {
            if (!o) continue;
            float d = DistancePointToSegment(o.Pos, a, b, out float dLine);
            if (d < best) best = d;
            if (dLine < minToLine) minToLine = dLine;
        }
        return best;
    }
    float MinBeaconClearanceOnSegment(Vector3 a, Vector3 b, out AvoidBeacon blocker)
    {
        blocker = null;
        if (AvoidBeacon.All.Count == 0) return float.PositiveInfinity;
        a.y = b.y = 0f;
        Vector3 ab = b - a; float ab2 = Mathf.Max(1e-6f, ab.sqrMagnitude);

        float best = float.PositiveInfinity;
        foreach (var bk in AvoidBeacon.All)
        {
            if (!bk) continue;
            Vector3 c = bk.transform.position; c.y = 0f;
            float t = Mathf.Clamp01(Vector3.Dot(c - a, ab) / ab2);
            Vector3 p = a + t * ab;
            float d = Vector3.Distance(c, p);
            float clearance = d - (bk.radius + beaconPadding);
            if (clearance < best) { best = clearance; blocker = bk; }
        }
        return best;
    }
    float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b, out float dToLine)
    {
        Vector3 ap = p - a, ab = b - a; ap.y = 0; ab.y = 0;
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
        Vector3 proj = a + t * ab;
        dToLine = Vector3.Distance(p, proj);
        return Vector3.Distance(p, proj);
    }

    /* ---------- 环境/几何 ---------- */
    public bool IsThreatened(float radius)
    {
        foreach (var opp in opponents) if (opp && Vector3.Distance(opp.Pos, Pos) <= radius) return true;
        return false;
    }
    public bool IsInAttackingHalf()
    {
        Vector3 gF = friendlyGoal.transform.position;
        float t = Vector3.Dot((ball ? ball.Pos : Pos) - gF, FieldForward);
        float len = Vector3.Dot(enemyGoal.transform.position - gF, FieldForward);
        return t >= len * 0.5f;
    }
    public bool IsPastEnemyGoalLine(out Vector3 returnTarget)
    {
        returnTarget = Vector3.zero;
        if (!ball) return false;
        Vector3 gF = friendlyGoal.transform.position, gE = enemyGoal.transform.position, f = FieldForward;
        float L = Mathf.Max(0.01f, Vector3.Dot(gE - gF, f));
        float tBall = Vector3.Dot(ball.Pos - gF, f) / L;
        bool past = tBall > (1f + backlineDepth / L);
        if (!past) return false;

        if (enemyBacklineReturn) returnTarget = enemyBacklineReturn.position;
        else
        {
            Vector3 center = 0.5f * (gF + gE);
            Vector3 lateral = Vector3.Project(ball.Pos - center, FieldRight);
            Vector3 central = ball.Pos - lateral;
            returnTarget = central - f * 6f;
        }
        return true;
    }

    /* ---------- “墙 & 球门口”几何 ---------- */

    // 收集四面墙
    void CollectPitchWalls()
    {
        int layer = -1;
        if (!string.IsNullOrEmpty(boundaryLayerName))
            layer = LayerMask.NameToLayer(boundaryLayerName);

        int mask = (layer >= 0) ? (1 << layer) : boundaryMask.value;

        Collider[] all;
#if UNITY_2020_1_OR_NEWER
        all = UnityEngine.Object.FindObjectsOfType<Collider>(true);
#else
        all = UnityEngine.Object.FindObjectsOfType<Collider>();
#endif

        var list = new List<Collider>(32);
        foreach (var c in all)
        {
            if (!c) continue;
            if (c.GetComponentInParent<WaterPlayer>() ||
            c.GetComponentInParent<PlayerController>() ||
            c.GetComponentInParent<PlayerMovement>()) 
                continue;
            if (((1 << c.gameObject.layer) & mask) == 0) continue;
            if (c.isTrigger) continue; // 只收实体墙
            if (c.GetComponentInParent<AvoidBeacon>() != null) continue;
            list.Add(c);
        }
        pitchBoundWalls = list.ToArray();
    }

    bool TryGetPitchEdgesFromWalls(out float minX, out float maxX, out float minZ, out float maxZ)
    {
        minX = maxX = minZ = maxZ = 0f;
        if (pitchBoundWalls == null || pitchBoundWalls.Length == 0) return false;

        Vector3 avg = Vector3.zero; int cnt = 0;
        foreach (var c in pitchBoundWalls) { if (!c) continue; avg += c.bounds.center; cnt++; }
        if (cnt == 0) return false;
        avg /= cnt;

        float leftInner = float.NegativeInfinity;
        float rightInner = float.PositiveInfinity;
        float downInner = float.NegativeInfinity;
        float upInner = float.PositiveInfinity;

        foreach (var c in pitchBoundWalls)
        {
            if (!c) continue;
            var b = c.bounds;
            bool vertical = b.size.x < b.size.z; // 细长方向判断

            if (vertical)
            {
                if (b.center.x <= avg.x) leftInner = Mathf.Max(leftInner, b.max.x);
                else rightInner = Mathf.Min(rightInner, b.min.x);
            }
            else
            {
                if (b.center.z <= avg.z) downInner = Mathf.Max(downInner, b.max.z);
                else upInner = Mathf.Min(upInner, b.min.z);
            }
        }

        if (!float.IsFinite(leftInner) || !float.IsFinite(rightInner) ||
            !float.IsFinite(downInner) || !float.IsFinite(upInner)) return false;

        minX = leftInner; maxX = rightInner;
        minZ = downInner; maxZ = upInner;
        return (maxX > minX) && (maxZ > minZ);
    }

    bool DirectionLeavesPitchByWalls(Vector3 dir, float lookahead, float margin)
    {
        if (!TryGetPitchEdgesFromWalls(out float minX, out float maxX, out float minZ, out float maxZ))
            return false;

        minX += margin; maxX -= margin;
        minZ += margin; maxZ -= margin;

        Vector3 a = (ball ? ball.Pos : Pos);
        if (a.x < minX || a.x > maxX || a.z < minZ || a.z > maxZ) return false;

        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();

        float tHit = float.PositiveInfinity;
        if (Mathf.Abs(dir.x) > 1e-5f)
        {
            float tx1 = (minX - a.x) / dir.x;
            float tx2 = (maxX - a.x) / dir.x;
            if (tx1 > 0) tHit = Mathf.Min(tHit, tx1);
            if (tx2 > 0) tHit = Mathf.Min(tHit, tx2);
        }
        if (Mathf.Abs(dir.z) > 1e-5f)
        {
            float tz1 = (minZ - a.z) / dir.z;
            float tz2 = (maxZ - a.z) / dir.z;
            if (tz1 > 0) tHit = Mathf.Min(tHit, tz1);
            if (tz2 > 0) tHit = Mathf.Min(tHit, tz2);
        }
        return tHit < float.PositiveInfinity && tHit <= lookahead;
    }

    // 敌方球门口端点（若没有 bottomLeft/bottomRight，则回退到 5m 口宽）
    bool GetEnemyGoalMouth(out Vector3 L, out Vector3 R, out Vector3 C, out float halfWidth)
    {
        C = enemyGoal ? enemyGoal.transform.position : Pos + FieldForward * 20f;
        Vector3 right = FieldRight;

        Transform bl = null, br = null;
        var goalComp = enemyGoal ? enemyGoal.GetComponent<Goal>() : null;
        if (goalComp)
        {
            bl = goalComp.bottomLeft;
            br = goalComp.bottomRight;
        }

        if (bl && br)
        {
            L = bl.position; R = br.position;
            C = 0.5f * (L + R);
            halfWidth = 0.5f * Vector3.Distance(L, R);
            return true;
        }
        else
        {
            halfWidth = 2.5f;
            L = C - right * halfWidth;
            R = C + right * halfWidth;
            return false;
        }
    }

    // 射线（a->a+dir*t）是否在 lookahead 里穿过“球门口线段”
    bool RayHitsSegment(Vector3 a, Vector3 dir, Vector3 s0, Vector3 s1, float lookahead, out float tRay)
    {
        tRay = float.PositiveInfinity;
        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();
        a.y = s0.y = s1.y = 0f;

        Vector3 v1 = a - s0;
        Vector3 v2 = s1 - s0;
        Vector3 v3 = new Vector3(-dir.z, 0, dir.x); // 2D 右法向

        float dot = Vector3.Dot(v2, v3);
        if (Mathf.Abs(dot) < 1e-6f) return false; // 平行

        float t1 = Vector3.Cross(v2, v1).y / dot; // ray t
        float t2 = Vector3.Dot(v1, v3) / dot;     // seg t

        if (t1 >= 0f && t1 <= lookahead && t2 >= 0f && t2 <= 1f)
        { tRay = t1; return true; }

        return false;
    }

    // 如果方向会“穿过球门口”，则允许（不视为朝墙）
    bool AllowIfThroughGoalMouth(Vector3 dir, float lookahead)
    {
        if (!enemyGoal) return false;
        GetEnemyGoalMouth(out Vector3 L, out Vector3 R, out Vector3 C, out _);
        return RayHitsSegment(ball ? ball.Pos : Pos, dir, L, R, lookahead, out _);
    }

    bool DirectionBlockedByBoundaryFirstHit(Vector3 dir, float lookahead)
    {
        if (!wallHardBan) return false;

        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();
        Vector3 origin = (ball ? ball.Pos : Pos) + Vector3.up * 0.2f;

        // 用“边界|球门”的联合掩码，并开启 Collide（让网/门触发器也能命中）
        int mask = boundaryMask | goalMask;
        if (Physics.SphereCast(origin, wallBanProbeRadius, dir, out var hit, lookahead, mask, QueryTriggerInteraction.Collide))
        {
            // 避免把 Beacon 当墙
            if (hit.collider && hit.collider.GetComponentInParent<AvoidBeacon>() != null)
                return false;

            bool isGoal = (((1 << hit.collider.gameObject.layer) & goalMask) != 0) || hit.collider.GetComponentInParent<Goal>() != null;
            bool isBoundary = (((1 << hit.collider.gameObject.layer) & boundaryMask) != 0);

            // ★ 如果“最先”撞到的是球门 → 允许（不算朝墙）
            if (isGoal) return false;

            // 如果“最先”撞到的是边界 → 还有一个容错：若在命中之前已经穿过“球门口线段”，也放行
            if (isBoundary && AllowIfThroughGoalMouthBefore(hit.distance, dir))
                return false;

            // 否则就是朝墙
            if (isBoundary) return true;
        }

        // 几何兜底：用收集的四面墙外框判断是否会在 lookahead 内越界
        return DirectionLeavesPitchByWalls(dir, lookahead, pitchMargin);
    }

    // 命中球门口线段且在“tHit前”即发生 → 视为可以（不算朝墙）
    bool AllowIfThroughGoalMouthBefore(float tHit, Vector3 dir)
    {
        if (!enemyGoal) return false;
        if (!GetEnemyGoalMouth(out Vector3 L, out Vector3 R, out _, out _)) { /*没有端点也没关系，继续算*/ }

        return RayHitsSegment(ball ? ball.Pos : Pos, dir, L, R, tHit + 1e-3f, out _);
    }

    bool TryHumanPass(out Vector3 target)
    {
        target = Vector3.zero;
        if (!human || !ball) return false;

        Vector3 hp = new Vector3(human.position.x, 0, human.position.z);
        float dist = Vector3.Distance(ball.Pos, hp);
        if (dist < distPassMin || dist > humanPassMaxDist) return false;

        Vector3 dir = (hp - ball.Pos); dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();

        // 硬否决：墙/Beacon（允许穿过球门口的特例）
        if ((DirectionBlockedByBoundaryFirstHit(dir, dist) && !AllowIfThroughGoalMouth(dir, dist)) ||
            (beaconHardBan && DirectionBannedByBeacon(dir, dist, beaconBanRadiusPadding, out _)))
            return false;

        // 走廊安全：对手 + Beacon
        float laneSafe = MinOpponentDistanceToSegment(ball.Pos, hp, out float minToLine);
        float openScore = Mathf.InverseLerp(passOppClearRadius * 0.5f, passOppClearRadius * 2.0f, Mathf.Min(laneSafe, minToLine));
        float beaconClear = MinBeaconClearanceOnSegment(ball.Pos, hp, out _);
        if (beaconClear < passLaneHalfWidth) return false;

        if (openScore < humanOpenThresh) return false;

        target = hp;
        return true;
    }
    public bool TryGetWallNormalRobust(out Vector3 n, out float closest)
    {
        n = Vector3.zero; closest = float.MaxValue;
        if (!ball) return false;
        Vector3 probe = ball.Pos + Vector3.up * 0.3f;
        int mask = boundaryMask | goalMask;

        var hits = Physics.OverlapSphere(probe, Mathf.Max(wallDetectRadius, boundaryNearDistance * 1.2f),
                                         mask, QueryTriggerInteraction.Ignore);
        Collider nearest = null;

        foreach (var c in hits)
        {
            if (c.GetComponentInParent<AvoidBeacon>() != null) continue;
            Vector3 cp = c.ClosestPoint(probe);
            Vector3 flat = new Vector3(cp.x, 0, cp.z);
            Vector3 v = (ball.Pos - flat);
            float d = v.magnitude;
            if (d < closest) { closest = d; nearest = c; n = (d > 1e-3f) ? (v / d) : Vector3.zero; }
        }

        if (nearest != null && (n == Vector3.zero || closest <= 0.05f))
        {
            if (ball.Col && Physics.ComputePenetration(
                ball.Col, ball.Col.transform.position, ball.Col.transform.rotation,
                nearest, nearest.transform.position, nearest.transform.rotation,
                out Vector3 dir, out float dist))
            {
                Vector3 fd = new Vector3(dir.x, 0, dir.z);
                if (fd.sqrMagnitude > 1e-6f) n = fd.normalized;
                if (dist < closest) closest = dist;
            }
        }
        bool near = (nearest != null) && (closest <= Mathf.Max(wallNearThreshold, boundaryNearDistance));
        return near && n != Vector3.zero;
    }

    // Beacon 硬禁
    bool DirectionBannedByBeacon(Vector3 dir, float lookahead, float extraPad, out AvoidBeacon hitBeacon)
    {
        hitBeacon = null;
        if (!beaconHardBan || AvoidBeacon.All.Count == 0) return false;

        Vector3 a = (ball ? ball.Pos : Pos); a.y = 0f;
        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();

        foreach (var b in AvoidBeacon.All)
        {
            if (!b) continue;
            Vector3 c = b.transform.position; c.y = 0f;
            Vector3 ap = c - a;
            float t = Mathf.Clamp(Vector3.Dot(ap, dir), 0f, lookahead);
            Vector3 closest = a + dir * t;

            float r = Mathf.Max(0.01f, b.radius + beaconPadding + extraPad);
            float d = Vector3.Distance(closest, c);
            if (d <= r) { hitBeacon = b; return true; }
        }
        return false;
    }

    public Vector3 PickBestDiagonal(Vector3 wallN)
    {
        Vector3 f = FieldForward;
        Vector3 d1 = (wallN + f).normalized;
        Vector3 d2 = (wallN - f).normalized;
        return (Vector3.Dot(d1, ToCenterDir()) >= Vector3.Dot(d2, ToCenterDir())) ? d1 : d2;
    }

    /* ---------- 小工具 ---------- */
    public void PassOrKick(Vector3 target, bool preferPass)
    {
        if (preferPass && FindTeammateNear(target, 3.5f, out var mate)) Pass(mate.Pos);
        else
        {
            Vector3 d = ResolveKickDirection((target - ball.Pos).normalized, wallBanLookahead);
            Vector3 adj = ball.Pos + d * Vector3.Distance(ball.Pos, target);
            float pow = ball.FindPower(ball.Pos, adj, 2.5f) * 1.05f;
            ball.Kick(adj, pow, this);
            if (animator) animator.SetTrigger(_shoot);
        }
    }

    public bool FindTeammateNear(Vector3 pt, float r, out WaterPlayer mate)
    {
        mate = null; float best = r * r;
        foreach (var m in team) { if (!m || m == this) continue; float d = (m.Pos - pt).sqrMagnitude; if (d < best) { best = d; mate = m; } }
        return mate != null;
    }

    /* Animator hashes */
    static readonly int _blend = Animator.StringToHash("Blend");
    static readonly int _shoot = Animator.StringToHash("Shoot");

    /* ---------- 调试可视化 ---------- */
    void OnDrawGizmosSelected()
    {
        if (pitchBoundWalls == null || pitchBoundWalls.Length == 0) return;
        if (!TryGetPitchEdgesFromWalls(out float minX, out float maxX, out float minZ, out float maxZ)) return;

        Gizmos.color = Color.cyan;
        Vector3 a = new Vector3(minX, 0, minZ);
        Vector3 b = new Vector3(maxX, 0, minZ);
        Vector3 c = new Vector3(maxX, 0, maxZ);
        Vector3 d = new Vector3(minX, 0, maxZ);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);

        if (GetEnemyGoalMouth(out Vector3 L, out Vector3 R, out Vector3 C, out _))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(L, R);
            Gizmos.DrawSphere(C, 0.2f);
        }
    }
}
