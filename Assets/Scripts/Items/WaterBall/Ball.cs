using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Ball : MonoBehaviour
{
    [Header("Ground Physics")]
    [Min(0)] public float friction = 1.0f;

    [Header("Bounce (AI touch)")]
    public float playerBounceSpeed = 18f;

    [Header("Boundary & Goal Bounce")]
    public LayerMask boundaryMask;
    public LayerMask goalMask;
    public float bounceDetectRadius = 0.70f;
    public float bounceNearDist = 0.55f;
    [Range(0f, 1f)] public float bounceRestitution = 0.65f;
    [Range(0f, 1f)] public float bounceTangentDamping = 0.18f;
    public float minBounceSpeed = 1.0f;
    public float slideInwardBoost = 1.8f;
    public float bounceCooldown = 0.05f;

    [Header("Corner Booster")]
    public float cornerNormalAngleMin = 35f;
    [Range(0f, 1f)] public float cornerRestitution = 0.8f;
    public float cornerInwardBoost = 2.6f;
    public float cornerMinSpeed = 2.2f;

    /* ===== 出脚方向硬纠偏（仍保留以兼容AI） ===== */
    [Header("Kick Direction Clamp")]
    public bool forceSanitizeKick = true;
    public float wallBanLookahead = 2f;
    public float wallBanProbeRadius = 0.35f;
    [Range(0f, 1f)] public float nearWallCenterSlerp = 0.85f;
    public float nearWallDetect = 1.6f;
    public bool beaconHardBan = true;
    public float beaconBanPadding = 0.50f;
    public float beaconBanLookahead = 2f;

    /* ===== 玩家识别与踢球（已禁用碰撞触发） ===== */
    [Header("Human Player Detection")]
    public LayerMask playerMask;
    public bool alsoCheckPlayerTag = true;
    public bool alsoCheckPlayerComponents = true;

    [Header("Human Player Kick (LEGACY)")]
    public float playerKickBase = 10f;
    public float playerKickVelFactor = 0.8f;
    public float playerKickMax = 18f;
    public bool sanitizePlayerKick = true;

    [Header("Overhaul Compat")]
    public bool disableHumanCollisionKick = true; // 取消“碰到就踢”
    public BallPossessionController possession;   // 从 Ball 上同物体挂载

    /* ===== 抢断/远距吸球防护（保留AI用） ===== */
    [Header("Anti Snap-back / AI Claim")]
    public float aiClaimRadius = 1.9f;
    public float playerStealLock = 0.45f;
    public float playerKeepLock = 0.70f;
    public float playerKeepRadius = 3.0f;
    public float aiReacquireMinSpeed = 0.6f;
    public float clearOwnerIfFartherThan = 2.5f;

    private float _lastNonAITouchAt = -999f;
    private Transform _lastNonAIToucher = null;
    private float _ignoreWPUntil = -999f;

    private float _nextBounceAt;

    private bool pickupLockActive;
    private bool pickupLockTeam;
    private float pickupLockUntil;

    public Rigidbody Rb { get; private set; }
    public SphereCollider Col { get; private set; }
    public WaterPlayer Owner;  // 仅 AI 用；玩家持球时置空
    public bool LastTouchTeam; // true 我方 / false 对方

    void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        Col = GetComponent<SphereCollider>();
        Rb.useGravity = false;
        Rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        if (!possession) possession = GetComponent<BallPossessionController>();
        
    }

    void FixedUpdate()
    {
        // ground friction (horizontal)
        Vector3 v = Rb.velocity; v.y = 0f;
        if (v.sqrMagnitude > 0.01f)
            Rb.AddForce(-v.normalized * friction, ForceMode.Acceleration);

        // clear long-distance owner attraction
        if (Owner)
        {
            float d = Vector3.Distance(Owner.Pos, Pos);
            if (d > clearOwnerIfFartherThan) Owner = null;
        }

        BoundaryBounceStep();
    }

    void OnCollisionEnter(Collision col) { HandleHit(col.collider, col.GetContact(0).point); }
    void OnCollisionStay(Collision col) { HandleHit(col.collider, col.GetContact(0).point); }

    /* ===================== KICK API ===================== */

    public void Kick(Vector3 target, float power)
    {
        Vector3 dirRaw = (target - Pos);
        Vector3 dir = forceSanitizeKick ? SanitizeKickDir(dirRaw, null) : SafeNormalize(dirRaw, FallbackCenterDir(null));
        Rb.velocity = dir * power;
        Owner = null;
    }

    public void Kick(Vector3 target, float power, WaterPlayer kicker)
    {
        if (kicker && ShouldIgnoreWPReacquire()) return;
        if (kicker && Vector3.Distance(kicker.Pos, Pos) > aiClaimRadius + 0.2f) return;

        Vector3 dirRaw = (target - Pos);
        Vector3 dir = forceSanitizeKick ? SanitizeKickDir(dirRaw, kicker)
                                        : SafeNormalize(dirRaw, FallbackCenterDir(kicker));
        Rb.velocity = dir * power;
        Owner = null;

        if (kicker) SetPickupLock(kicker.isTeammate, 0.35f);
        LastTouchTeam = kicker ? kicker.isTeammate : LastTouchTeam;
    }

    // —— 新系统统一出脚：释放持球并按方向踢 —— //
    public void KickOverhaul(Vector3 target, float power, WaterPlayer kicker)
    {
        Vector3 dir = (target - Pos); dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;

        bool markTeam = kicker ? kicker.isTeammate : LastTouchTeam;

        if (possession)
        {
            // 这里必须用“方向版”的 API，避免把方向当作坐标
            possession.ReleaseAndKickDir(dir.normalized, power);
        }
        else
        {
            // 没有持球控制器时走旧逻辑
            Kick(target, power, kicker);
        }

        Owner = null;
        LastTouchTeam = markTeam;
    }

    public void SetPickupLock(bool team, float duration)
    {
        pickupLockActive = true;
        pickupLockTeam = team;
        pickupLockUntil = Time.time + duration;
    }

    public Vector3 Pos
    {
        get => new Vector3(transform.position.x, 0, transform.position.z);
        set => transform.position = new Vector3(value.x, transform.position.y, value.z);
    }

    public float FindPower(Vector3 from, Vector3 to, float vEnd)
        => Mathf.Sqrt(vEnd * vEnd - 2 * -friction * Vector3.Distance(from, to));

    public float TimeToCover(Vector3 from, Vector3 to, float u)
        => Vector3.Distance(from, to) / Mathf.Max(u, 0.01f);

    /* ===================== COLLISION HANDLING ===================== */

    void HandleHit(Component other, Vector3 contactPoint)
    {
        if (!other) return;

        // Human player: legacy collision-kick disabled
        if (IsHumanPlayer(other))
        {
            if (disableHumanCollisionKick) return;
            KickFromHuman(other);
            return;
        }

        // AI WaterPlayer：允许“弹走一下”但不强占玩家持球
        var wp = other.GetComponent<WaterPlayer>();
        if (wp)
        {
            if (ShouldIgnoreWPReacquire()) return;
            if (Vector3.Distance(Pos, wp.Pos) > aiClaimRadius) return;
            if (pickupLockActive && Time.time < pickupLockUntil && wp.isTeammate == pickupLockTeam)
                return;

            Owner = wp;
            LastTouchTeam = wp.isTeammate;

            if (playerBounceSpeed > 0f)
            {
                Vector3 n = (transform.position - wp.Pos).normalized; n.y = 0;
                Vector3 dir = forceSanitizeKick ? SanitizeKickDir(n, wp) : SafeNormalize(n, FallbackCenterDir(wp));
                Rb.velocity = dir * playerBounceSpeed;
            }
            return;
        }

        // others: ignore
    }

    bool IsHumanPlayer(Component c)
    {
        if (!c) return false;
        if (playerMask.value != 0) { if (((1 << c.gameObject.layer) & playerMask) != 0) return true; }
        if (alsoCheckPlayerTag && c.CompareTag("Player")) return true;
        if (alsoCheckPlayerComponents)
        {
            if (c.GetComponentInParent<PlayerController>() || c.GetComponentInParent<PlayerMovement>()) return true;
            if (c.GetComponent<PlayerController>() || c.GetComponent<PlayerMovement>()) return true;
        }
        if (c.GetComponentInParent<WaterPlayer>()) return false;
        return false;
    }

    void KickFromHuman(Component player)
    {
        Vector3 forward = Vector3.zero;
        Transform t = player.transform;
        if (t) { forward = t.forward; forward.y = 0f; forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : FallbackCenterDir(null); }
        float add = 0f; var prb = player.GetComponentInParent<Rigidbody>();
        if (prb) { Vector3 hv = new Vector3(prb.velocity.x, 0, prb.velocity.z); add = hv.magnitude * playerKickVelFactor; }
        float power = Mathf.Clamp(playerKickBase + add, 0f, playerKickMax);
        Vector3 dir = sanitizePlayerKick ? SanitizeKickDirForHuman(forward) : SafeNormalize(forward, FallbackCenterDir(null));
        // 一些模型前后颠倒时需要反向；保持你项目里原来的反向处理
        Rb.velocity = -dir * power;
        _lastNonAITouchAt = Time.time;
        _lastNonAIToucher = t;
        _ignoreWPUntil = Time.time + playerStealLock;
        Owner = null;
    }

    Vector3 SanitizeKickDirForHuman(Vector3 raw)
    {
        Vector3 d = SafeNormalize(raw, FallbackCenterDir(null));
        if (DirectionHitsBoundaryShortHorizon(d, 2f))
        {
            Vector3 center = FallbackCenterDir(null);
            d = Vector3.Slerp(d, center, 0.35f).normalized;
        }
        return d;
    }
    bool DirectionHitsBoundaryShortHorizon(Vector3 dir, float lookaheadMeters)
    {
        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();
        Vector3 origin = Pos + Vector3.up * 0.2f;
        int mask = boundaryMask;
        return Physics.SphereCast(origin, wallBanProbeRadius, dir, out _, lookaheadMeters, mask, QueryTriggerInteraction.Ignore);
    }

    bool InPlayerKeepWindow() => (Time.time - _lastNonAITouchAt) <= playerKeepLock;

    bool ShouldIgnoreWPReacquire()
    {
        if (Time.time < _ignoreWPUntil) return true;
        if (!InPlayerKeepWindow()) return false;
        if (_lastNonAIToucher)
        {
            if (Vector3.Distance(Pos, _lastNonAIToucher.position) <= playerKeepRadius)
                return true;
        }
        if (Rb && Rb.velocity.magnitude >= aiReacquireMinSpeed)
            return true;
        return true;
    }

    /* ===================== DIR SANITIZE & HELPERS ===================== */

    Vector3 SanitizeKickDir(Vector3 rawDir, WaterPlayer kicker)
    {
        Vector3 centerDir = FallbackCenterDir(kicker);
        Vector3 d = SafeNormalize(rawDir, centerDir);

        if (kicker != null && DirectionHitsBoundary(d)) d = centerDir;
        if (beaconHardBan && DirectionHitsBeacon(d)) d = centerDir;
        if (IsNearWall(out _)) d = Vector3.Slerp(d, centerDir, nearWallCenterSlerp).normalized;
        return d;
    }

    Vector3 SafeNormalize(Vector3 v, Vector3 fallback)
    {
        v.y = 0f; if (v.sqrMagnitude < 1e-6f) return fallback; return v.normalized;
    }

    Vector3 FallbackCenterDir(WaterPlayer kicker)
    {
        Vector3 center;
        if (kicker && kicker.friendlyGoal && kicker.enemyGoal)
        {
            center = 0.5f * (kicker.friendlyGoal.transform.position + kicker.enemyGoal.transform.position);
        }
        else
        {
            var all = FindObjectsOfType<Goal>();
            if (all != null && all.Length >= 2)
            {
                float best = -1f; Vector3 a = all[0].transform.position, b = a;
                for (int i = 0; i < all.Length; i++)
                    for (int j = i + 1; j < all.Length; j++)
                    {
                        float d = Vector3.SqrMagnitude(all[i].transform.position - all[j].transform.position);
                        if (d > best) { best = d; a = all[i].transform.position; b = all[j].transform.position; }
                    }
                center = 0.5f * (a + b);
            }
            else center = Vector3.zero;
        }
        Vector3 dir = center - Pos; dir.y = 0f;
        return (dir.sqrMagnitude > 1e-6f) ? dir.normalized : Vector3.forward;
    }

    bool DirectionHitsBoundary(Vector3 dir)
    {
        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();
        Vector3 origin = Pos + Vector3.up * 0.2f;
        int mask = boundaryMask;
        if (Physics.SphereCast(origin, wallBanProbeRadius, dir, out var hit, wallBanLookahead, mask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<WaterPlayer>() ||
                hit.collider.GetComponentInParent<PlayerController>() ||
                hit.collider.GetComponentInParent<PlayerMovement>())
                return false;
            return true;
        }
        return Physics.SphereCast(origin, wallBanProbeRadius, dir, out _, wallBanLookahead, mask, QueryTriggerInteraction.Ignore);
    }

    bool DirectionHitsBeacon(Vector3 dir)
    {
        if (!beaconHardBan) return false;
        var list = (AvoidBeacon.All != null) ? AvoidBeacon.All : new List<AvoidBeacon>();
        if (list.Count == 0) return false;
        dir.y = 0f; if (dir.sqrMagnitude < 1e-6f) return false; dir.Normalize();
        Vector3 a = Pos;
        foreach (var b in list)
        {
            if (!b) continue;
            Vector3 c = b.transform.position; c.y = 0f;
            Vector3 ap = c - a;
            float t = Mathf.Clamp(Vector3.Dot(ap, dir), 0f, beaconBanLookahead);
            Vector3 closest = a + dir * t;
            float r = Mathf.Max(0.01f, b.radius + beaconBanPadding);
            if (Vector3.Distance(closest, c) <= r) return true;
        }
        return false;
    }

    bool IsNearWall(out Vector3 normalOut)
    {
        normalOut = Vector3.zero;
        int mask = boundaryMask;
        var hits = Physics.OverlapSphere(Pos + Vector3.up * 0.3f, nearWallDetect, mask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;
        float best = float.MaxValue; Collider nearest = null;
        foreach (var c in hits)
        {
            Vector3 cp = c.ClosestPoint(Pos + Vector3.up * 0.3f);
            Vector3 flat = new Vector3(cp.x, 0, cp.z);
            float d = Vector3.Distance(Pos, flat);
            if (d < best) { best = d; nearest = c; }
        }
        if (!nearest) return false;
        if (Col && Physics.ComputePenetration(
            Col, Col.transform.position, Col.transform.rotation,
            nearest, nearest.transform.position, nearest.transform.rotation,
            out Vector3 dir, out _))
        {
            normalOut = new Vector3(dir.x, 0, dir.z).normalized;
        }
        return true;
    }

    /* ===================== WALL BOUNCE ===================== */

    void BoundaryBounceStep()
    {
        if (Time.time < _nextBounceAt) return;
        if (!TryGetAggregatedNormal(out Vector3 n, out float dist, out bool isCorner)) return;
        if (dist > bounceNearDist) return;
        Vector3 vAll = Rb.velocity;
        Vector3 v = new Vector3(vAll.x, 0f, vAll.z);
        float speed = v.magnitude;
        if (speed < 0.01f) return;
        float R = bounceRestitution;
        float inward = slideInwardBoost;
        float minS = minBounceSpeed;
        if (isCorner)
        {
            R = Mathf.Max(R, cornerRestitution);
            inward = Mathf.Max(inward, cornerInwardBoost);
            minS = Mathf.Max(minS, cornerMinSpeed);
        }
        float vn = Vector3.Dot(v, n);
        Vector3 vt = v - vn * n;
        Vector3 vNew;
        if (speed < minS)
        {
            vNew = v + n * (inward * 0.6f);
        }
        else
        {
            if (vn < 0f)
            {
                float vnAfter = -vn * R;
                Vector3 vtAfter = vt * (1f - bounceTangentDamping);
                vNew = vnAfter * n + vtAfter;
            }
            else
            {
                vNew = v + n * inward;
            }
        }
        Rb.velocity = new Vector3(vNew.x, vAll.y, vNew.z);
        _nextBounceAt = Time.time + bounceCooldown;
    }

    bool TryGetAggregatedNormal(out Vector3 n, out float closest, out bool isCorner)
    {
        n = Vector3.zero; closest = float.MaxValue; isCorner = false;
        Vector3 probe = Pos + Vector3.up * 0.3f;
        int mask = boundaryMask | goalMask;
        var hits = Physics.OverlapSphere(probe, bounceDetectRadius, mask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;
        Vector3 avg = Vector3.zero; int used = 0; Vector3 firstN = Vector3.zero;
        foreach (var c in hits)
        {
            if (c.isTrigger) continue;
            if (c.GetComponentInParent<WaterPlayer>() ||
               c.GetComponentInParent<PlayerController>() ||
               c.GetComponentInParent<PlayerMovement>())
                continue;
            Vector3 cp = c.ClosestPoint(probe);
            Vector3 flat = new Vector3(cp.x, 0, cp.z);
            Vector3 v = (Pos - flat);
            float d = v.magnitude;
            if (d < closest) closest = d;
            Vector3 nn = (d > 1e-4f) ? (v / d) : Vector3.zero;
            if (nn == Vector3.zero) continue;
            if (used == 0) firstN = nn;
            else
            {
                float ang = Vector3.Angle(firstN, nn);
                if (ang >= cornerNormalAngleMin) isCorner = true;
            }
            avg += nn; used++;
        }
        if (used == 0) return false;
        avg.y = 0f;
        if (avg.sqrMagnitude < 1e-6f) return false;
        n = avg.normalized;
        return true;
    }
}
