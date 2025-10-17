using UnityEngine;
using System;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BallPossessionController : MonoBehaviour
{
    [Header("Links")]
    public Ball ball;
    public List<DribbleZone> allZones = new();

    [Header("Follow When Owned")]
    public float followLerp = 20f;
    public float followMaxDistBreak = 3.0f;
    public bool disableBallCollisionsWhileOwned = true;

    [Header("Pickup / Steal")]
    public float freeBallSnapRange = 2.0f;
    public float stealCooldown = 0.35f;
    public float postStealImmunity = 1.00f;   // 抢到球后持球者免疫时长
    [Range(0.2f, 2f)] public float stealAggression = 1.0f;

    [Header("Release (anti re-pick)")]
    [Tooltip("踢/传/射后，这段时间内禁用自动吸附，避免立刻被原持球人再次吸回。")]
    public float afterKickNoPickup = 0.35f;

    [Header("Debug")]
    public bool showInfo;

    /* ------- 运行时 ------- */
    Dictionary<DribbleZone, float> _cooldowns = new();   // 攻击者冷却
    Dictionary<DribbleZone, float> _immuneUntil = new(); // 持球者免疫截止时间
    float _autoPickupDisabledUntil = -999f;              // 全局禁吸附窗口

    WaterPlayer _owner;              // AI持球时为其引用；玩家持球时可为 null
    DribbleZone _ownerZone;

    public bool IsPossessed => _ownerZone != null;
    public WaterPlayer holder => _owner;
    public DribbleZone holderZone => _ownerZone;

    public event Action<WaterPlayer, WaterPlayer> OnPossessionChanged;

    void Reset()
    {
        if (!ball) ball = FindObjectOfType<Ball>();
        if (allZones.Count == 0) allZones.AddRange(FindObjectsOfType<DribbleZone>());
    }

    void Awake()
    {
        if (!ball) ball = FindObjectOfType<Ball>();
        if (allZones.Count == 0) allZones.AddRange(FindObjectsOfType<DribbleZone>());
    }

    void Update()
    {
        // 推进冷却
        if (_cooldowns.Count > 0)
        {
            var keys = new List<DribbleZone>(_cooldowns.Keys);
            foreach (var k in keys)
            {
                _cooldowns[k] -= Time.deltaTime;
                if (_cooldowns[k] <= 0f) _cooldowns.Remove(k);
            }
        }

        // 无人持球 → 自动吸附（若未处于禁吸附窗口）
        if (_ownerZone == null)
        {
            if (Time.time >= _autoPickupDisabledUntil)
                TryAutoPickup();
            return;
        }

        // 有人持球 → 跟随
        FollowAnchor();

        // 持球免疫期内禁止抢断
        if (IsImmune(_ownerZone)) return;

        InternalTrySteal(attackerOnly: null);
    }

    /* ===================== 核心 API ===================== */

    // 释放并按方向踢（玩家/AI都可用）
    public void ReleaseAndKickDir(Vector3 dir, float power)
    {
        // 记录当前持球方，用于 LastTouchTeam
        bool teamFlag = _ownerZone ? _ownerZone.isTeammate : ball.LastTouchTeam;

        ClearOwner(); // 会恢复碰撞
        _autoPickupDisabledUntil = Time.time + Mathf.Max(0f, afterKickNoPickup);

        if (ball)
        {
            ball.LastTouchTeam = teamFlag;
            ball.Rb.velocity = dir.normalized * power;
        }
    }

    // 释放并按方向踢（显式传入队伍标记）
    public void ReleaseAndKick(Vector3 dir, float power, bool teammateFlag)
    {
        ClearOwner();
        _autoPickupDisabledUntil = Time.time + Mathf.Max(0f, afterKickNoPickup);

        if (ball)
        {
            ball.LastTouchTeam = teammateFlag;
            ball.Rb.velocity = dir.normalized * power;
        }
    }

    // “只让我抢”的 API（玩家按键）
    public bool TrySteal(DribbleZone attacker)
    {
        return InternalTrySteal(attackerOnly: attacker);
    }
    public bool TrySteal(DribbleZone attacker, out bool success)
    {
        success = TrySteal(attacker);
        return success;
    }

    public void ClearOwner()
    {
        if (_ownerZone) _ownerZone.SetIdleColor();
        ApplyOwnedPhysics(false);
        var old = _owner;
        _owner = null;
        _ownerZone = null;
        if (ball) ball.Owner = null;
        OnPossessionChanged?.Invoke(null, old);
    }

    public void SetOwner(WaterPlayer wp, DribbleZone zone, bool flash)
    {
        if (_ownerZone) _ownerZone.SetIdleColor();

        var old = _owner;
        _owner = wp;             // 玩家持球时 wp 可能是 null
        _ownerZone = zone;

        // 吸附到锚点
        if (_ownerZone && _ownerZone.dribbleAnchor)
            ball.Pos = _ownerZone.AnchorXZ;

        // AI 才写 Owner（玩家持球保持 null）
        ball.Owner = _owner;
        ball.LastTouchTeam = _ownerZone.isTeammate;

        // 视觉 & 免疫
        if (flash) _ownerZone.FlashSteal();
        _ownerZone.SetCarrier(true);
        _immuneUntil[_ownerZone] = Time.time + postStealImmunity;

        ApplyOwnedPhysics(true);

        OnPossessionChanged?.Invoke(_owner, old);
    }

    /* ===================== 内部逻辑 ===================== */

    void TryAutoPickup()
    {
        DribbleZone best = null; float bestD2 = float.MaxValue;
        foreach (var z in allZones)
        {
            if (!z || !z.dribbleAnchor) continue;
            float d2 = (ball.Pos - z.AnchorXZ).sqrMagnitude;
            float r2 = z.radius * z.radius;
            if (d2 <= r2 && d2 < bestD2) { bestD2 = d2; best = z; }
            else if (d2 <= freeBallSnapRange * freeBallSnapRange && d2 < bestD2) { bestD2 = d2; best = z; }
        }
        if (best != null) SetOwner(best.ownerWP, best, flash: true);
    }

    void FollowAnchor()
    {
        if (_ownerZone == null || !_ownerZone.dribbleAnchor) { ClearOwner(); return; }

        Vector3 target = _ownerZone.AnchorXZ;
        Vector3 pos = ball.Pos;
        Vector3 newPos = Vector3.Lerp(pos, target, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
        ball.Pos = newPos;

        // 强制停止物理，避免抖动
        if (ball.Rb) ball.Rb.velocity = Vector3.zero;

        // 超远脱落
        Vector3 ownerPos = _owner ? _owner.Pos : _ownerZone.OwnerPosXZ;
        float far = Vector3.Distance(ownerPos, ball.Pos);
        if (far > followMaxDistBreak) ClearOwner();

        // AI 才写 Owner
        ball.Owner = _owner;
    }

    bool InternalTrySteal(DribbleZone attackerOnly)
    {
        if (_ownerZone == null) return false;
        if (IsImmune(_ownerZone)) return false; // 持球者免疫中

        foreach (var z in allZones)
        {
            if (!z || z == _ownerZone) continue;
            if (attackerOnly && z != attackerOnly) continue;

            // 同队不抢
            if (z.isTeammate == _ownerZone.isTeammate) continue;

            // 攻击者冷却
            if (_cooldowns.TryGetValue(z, out float t) && t > 0f) continue;

            float p = StealProbability(_ownerZone, z);
            if (p <= 0f) continue;

            if (UnityEngine.Random.value < p)
            {
                var oldZone = _ownerZone;

                SetOwner(z.ownerWP, z, flash: true);   // 设置新持球者（内部已加免疫）

                // 被抢的一方也进冷却，避免立刻连抢
                _cooldowns[oldZone] = stealCooldown;
                if (showInfo) Debug.Log($"[STEAL] {z.name} stole from {oldZone.name}, p={p:0.00}");
                return true;
            }
            else
            {
                _cooldowns[z] = stealCooldown;
            }
        }
        return false;
    }

    float StealProbability(DribbleZone aOwner, DribbleZone bAttacker)
    {
        Vector2 a = new Vector2(aOwner.transform.position.x, aOwner.transform.position.z);
        Vector2 b = new Vector2(bAttacker.transform.position.x, bAttacker.transform.position.z);
        float r1 = aOwner.radius, r2 = bAttacker.radius;
        float d = Vector2.Distance(a, b);

        if (d >= r1 + r2) return 0f;

        float overlapArea = CircleOverlapArea(r1, r2, d);
        float baseArea = Mathf.PI * Mathf.Min(r1, r2) * Mathf.Min(r1, r2);
        float norm = Mathf.Clamp01(overlapArea / Mathf.Max(0.001f, baseArea));

        float distToBall = Vector3.Distance(bAttacker.AnchorXZ, ball.Pos);
        float bonus = Mathf.Clamp01(1f - distToBall / r2) * 0.2f;

        float p = Mathf.Clamp01((norm + bonus) * stealAggression);
        return p;
    }

    static float CircleOverlapArea(float r1, float r2, float d)
    {
        if (d <= Mathf.Abs(r1 - r2))
        {
            float r = Mathf.Min(r1, r2);
            return Mathf.PI * r * r;
        }
        if (d >= r1 + r2) return 0f;

        float alpha = 2f * Mathf.Acos((r1 * r1 + d * d - r2 * r2) / (2f * r1 * d));
        float beta = 2f * Mathf.Acos((r2 * r2 + d * d - r1 * r1) / (2f * r2 * d));

        float area1 = 0.5f * r1 * r1 * (alpha - Mathf.Sin(alpha));
        float area2 = 0.5f * r2 * r2 * (beta - Mathf.Sin(beta));
        return area1 + area2;
    }

    bool IsImmune(DribbleZone zone)
    {
        if (zone == null) return false;
        if (_immuneUntil.TryGetValue(zone, out float until)) return Time.time < until;
        return false;
    }

    void ApplyOwnedPhysics(bool owned)
    {
        if (!ball || !ball.Rb) return;
        ball.Rb.velocity = owned ? Vector3.zero : ball.Rb.velocity;
        if (disableBallCollisionsWhileOwned)
            ball.Rb.detectCollisions = !owned; // 持球时禁用碰撞，彻底消抖
    }
}
