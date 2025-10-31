using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BallPossessionController : MonoBehaviour
{
    [Header("Links")]
    public Ball ball;
    public List<DribbleZone> allZones = new();

    [Header("Follow When Owned")]
    public float followLerp = 24f;
    public float followMaxDistBreak = 3.2f;

    [Header("Pickup / Steal")]
    public float freeBallSnapRange = 1.6f;

    [Header("Locks / Cooldowns")]
    public float postKickNoPickupSeconds = 1f;
    public float possessionInvulnSeconds = 1f;
    public float stealLocalCooldown = 1f;

    [Header("Debug")]
    public bool showInfo;

    // —— 状态 —— //
    public bool IsPossessed => _ownerZone != null;
    public DribbleZone holder => _ownerZone;
    public DribbleZone holderZone => _ownerZone;

    public event Action<WaterPlayer, WaterPlayer> OnPossessionChanged;

    WaterPlayer _owner;              // 仅 AI 时非空；玩家持球时为 null
    DribbleZone _ownerZone;
    float _noPickupUntil;
    float _invulnUntil;

    [Header("Steal Leniency")]
    public float stealLeniencyMeters = 0.25f;     // 超出重叠半径也算成功的补偿
    public float stealLeniencyEarly = 0.12f;      // 无敌即将结束前的提前容错（秒）

    // 各圈本地CD
    readonly Dictionary<DribbleZone, float> _localStealCD = new();

    public bool IsInvulnerable => Time.time < _invulnUntil;

    void Reset()
    {
        if (!ball) ball = GetComponent<Ball>();
        if (!ball) ball = FindObjectOfType<Ball>();
        if (allZones.Count == 0) allZones.AddRange(FindObjectsOfType<DribbleZone>());
    }

    void Awake()
    {
        if (!ball) ball = GetComponent<Ball>();
        if (!ball) ball = FindObjectOfType<Ball>();
        if (allZones.Count == 0) allZones.AddRange(FindObjectsOfType<DribbleZone>());
    }

    void Update() => TickLocalCooldowns();

    void FixedUpdate()
    {
        if (_ownerZone == null) TryAutoPickup();
        else FollowAnchorPhysics();
    }

    // ==== 强制夺球（给玩家突击使用） ====
    public void ForceTake(DribbleZone z, bool flash = true, bool addInvuln = true)
    {
        if (!z) return;
        SetOwner(z.ownerWP, z, flash);
        if (addInvuln)
            _invulnUntil = Time.time + possessionInvulnSeconds; // 给新持球者一个短无敌，手感更稳
    }

    // ==== NEW: 手动给某圈添加本地CD（玩家仍受CD约束） ====
    public void StartLocalCooldown(DribbleZone z, float? seconds = null)
    {
        if (!z) return;
        _localStealCD[z] = seconds.HasValue ? Mathf.Max(0f, seconds.Value) : stealLocalCooldown;
    }

    void TickLocalCooldowns()
    {
        if (_localStealCD.Count == 0) return;
        var keys = _localStealCD.Keys.ToArray();
        for (int i = 0; i < keys.Length; i++)
        {
            _localStealCD[keys[i]] -= Time.deltaTime;
            if (_localStealCD[keys[i]] <= 0f) _localStealCD.Remove(keys[i]);
        }
    }

    void TryAutoPickup()
    {
        if (!ball) return;
        if (Time.time < _noPickupUntil) return;

        DribbleZone best = null; float bestD2 = float.MaxValue;
        foreach (var z in allZones)
        {
            if (!z || !z.dribbleAnchor) continue;
            if (_localStealCD.TryGetValue(z, out float cd) && cd > 0f) continue;

            float d2 = (ball.Pos - z.AnchorXZ).sqrMagnitude;
            float r = Mathf.Max(z.radius, freeBallSnapRange);
            if (d2 <= r * r && d2 < bestD2) { bestD2 = d2; best = z; }
        }

        if (best != null)
        {
            SetOwner(best.ownerWP, best, flash: true);
            if (showInfo) Debug.Log($"[Pickup] {best.name} picked the ball.");
        }
    }

    void FollowAnchorPhysics()
    {
        if (_ownerZone == null || !_ownerZone.dribbleAnchor || !ball || !ball.Rb)
        { ClearOwner(); return; }

        Vector3 target = _ownerZone.AnchorXZ;
        Vector3 pos = ball.Pos;
        float a = 1f - Mathf.Exp(-followLerp * Time.fixedDeltaTime);
        Vector3 newPos = Vector3.Lerp(pos, target, a);

        Vector3 v = ball.Rb.velocity;
        ball.Rb.velocity = new Vector3(0f, v.y, 0f);
        Vector3 moved = new Vector3(newPos.x, ball.transform.position.y, newPos.z);
        ball.Rb.MovePosition(moved);

        Vector3 ownerPos = _owner ? _owner.Pos : _ownerZone.OwnerPosXZ;
        if (Vector3.Distance(ownerPos, moved) > followMaxDistBreak) { ClearOwner(); }

        ball.Owner = _owner; // 仅 AI
    }

    // ========= 出脚 =========
    public void ReleaseAndKick(Vector3 dir, float power)
    {
        ClearOwnerInternalKeepNoPickup();
        if (ball) ball.Kick(ball.Pos + dir.normalized * 10f, power);
    }
    public void ReleaseAndKick(Vector3 dir, float power, bool markTeam)
    {
        ClearOwnerInternalKeepNoPickup();
        if (ball) ball.Kick(ball.Pos + dir.normalized * 10f, power);
        if (ball) ball.LastTouchTeam = markTeam;
    }
    public void ReleaseAndKick(Vector3 dir, float power, WaterPlayer kicker)
    {
        ClearOwnerInternalKeepNoPickup();
        if (ball) ball.Kick(ball.Pos + dir.normalized * 10f, power, kicker);
    }
    public void ReleaseAndKickDir(Vector3 dir, float power) => ReleaseAndKick(dir, power);

    // ========= 抢断（含兼容重载）=========
    public bool TrySteal() { return TrySteal(FindNearestZoneToBall()); }
    public bool TrySteal(DribbleZone attacker)
    {
        bool ok; return TrySteal(attacker, out ok) && ok;
    }
    public bool TrySteal(DribbleZone attacker, out bool success)
    {
        success = false;
        if (_ownerZone == null) return false;
        if (!attacker || attacker == _ownerZone) return false;

        // 敌我 + 距离判断
        if (attacker.isTeammate == _ownerZone.isTeammate) return false;

        // 基础几何是否重叠
        bool overlap = CirclesOverlap(attacker, _ownerZone);

        // —— 容错 1：稍微超出一点也算（玩家手感）
        if (!overlap)
        {
            Vector2 pa = new Vector2(attacker.transform.position.x, attacker.transform.position.z);
            Vector2 pb = new Vector2(_ownerZone.transform.position.x, _ownerZone.transform.position.z);
            float d = Vector2.Distance(pa, pb);
            float need = attacker.radius + _ownerZone.radius + Mathf.Max(0f, stealLeniencyMeters);
            overlap = d < need;
        }

        // 无重叠/不在容错范围 → 不给CD，不成功
        if (!overlap) return false;

        // —— 容错 2：无敌末尾的提前抢断（early window）
        bool invuln = IsInvulnerable;
        if (invuln)
        {
            float remain = Mathf.Max(0f, _invulnUntil - Time.time);
            if (remain > stealLeniencyEarly)
            {
                // 无敌还很久：失败（给一下失败音效/无慢动作）
                SfxBus.Instance?.PlaySteal(false, ball ? (Vector3?)ball.Pos : null);
                return false;
            }
            // 无敌将结束：允许“提前抢”
        }

        // 本地CD：攻击者如果在CD就直接失败（不覆盖CD）
        if (_localStealCD.TryGetValue(attacker, out float cd) && cd > 0f)
        {
            SfxBus.Instance?.PlaySteal(false, ball ? (Vector3?)ball.Pos : null);
            return false;
        }

        // ✅ 成功抢断
        var oldOwner = _ownerZone;
        SetOwner(attacker.ownerWP, attacker, flash: true);

        // 给双方短 CD
        _localStealCD[attacker] = stealLocalCooldown;
        _localStealCD[oldOwner] = stealLocalCooldown;

        _invulnUntil = Time.time + possessionInvulnSeconds;

        // 反馈：音效 + 慢动作
        SfxBus.Instance?.PlaySteal(true, ball ? (Vector3?)ball.Pos : null);
        SfxBus.Instance?.DoSlowMo(0.6f, 0.18f);

        if (showInfo) Debug.Log($"[Steal] {attacker.name} -> {oldOwner.name}");
        success = true;
        return true;
    }



    DribbleZone FindNearestZoneToBall()
    {
        DribbleZone best = null; float bestD2 = float.MaxValue;
        Vector3 p = ball ? ball.Pos : Vector3.zero;
        foreach (var z in allZones)
        {
            if (!z) continue;
            float d2 = (z.OwnerPosXZ - p).sqrMagnitude;
            if (d2 < bestD2) { bestD2 = d2; best = z; }
        }
        return best;
    }

    bool CirclesOverlap(DribbleZone a, DribbleZone b)
    {
        Vector2 pa = new Vector2(a.transform.position.x, a.transform.position.z);
        Vector2 pb = new Vector2(b.transform.position.x, b.transform.position.z);
        float d = Vector2.Distance(pa, pb);
        return d < (a.radius + b.radius);
    }

    // ========= 所有权 =========
    public void SetOwner(WaterPlayer wp, DribbleZone zone, bool flash)
    {
        if (_ownerZone) _ownerZone.SetIdleColor();

        var old = _owner;

        _owner     = wp;
        _ownerZone = zone;

        if (_ownerZone && _ownerZone.dribbleAnchor && ball)
            ball.Pos = _ownerZone.AnchorXZ;

        if (ball) ball.Owner = _owner;

        if (flash) _ownerZone.FlashSteal();
        _ownerZone.SetCarrier(true);

        _invulnUntil = Time.time + possessionInvulnSeconds;

        OnPossessionChanged?.Invoke(_owner, old);
        int poss = 0;
        if (_ownerZone != null)
        {
            // 需要知道“本地玩家是哪队”，这里找第一个 ownerWP==null 的 DribbleZone（人类玩家）
            bool localTeam = true;
            foreach (var z in allZones) { if (z && z.ownerWP == null) { localTeam = z.isTeammate; break; } }
            poss = (_ownerZone.isTeammate == localTeam) ? -1 : 1; // -1我方，1对方
        }
        GameUIController.I?.SetBorderByPossession(poss);
    }

    void ClearOwnerInternalKeepNoPickup()
    {
        if (_ownerZone) _ownerZone.SetIdleColor();
        var old = _owner;

        _owner = null;
        _ownerZone = null;

        if (ball) ball.Owner = null;

        _noPickupUntil = Time.time + postKickNoPickupSeconds;

        OnPossessionChanged?.Invoke(null, old);
        int poss = 0;
        if (_ownerZone != null)
        {
            // 需要知道“本地玩家是哪队”，这里找第一个 ownerWP==null 的 DribbleZone（人类玩家）
            bool localTeam = true;
            foreach (var z in allZones) { if (z && z.ownerWP == null) { localTeam = z.isTeammate; break; } }
            poss = (_ownerZone.isTeammate == localTeam) ? -1 : 1; // -1我方，1对方
        }
        GameUIController.I?.SetBorderByPossession(poss);
    }

    public void ClearOwner()
    {
        if (_ownerZone) _ownerZone.SetIdleColor();
        var old = _owner;

        _owner = null;
        _ownerZone = null;

        if (ball) ball.Owner = null;

        OnPossessionChanged?.Invoke(null, old);
        int poss = 0;
        if (_ownerZone != null)
        {
            // 需要知道“本地玩家是哪队”，这里找第一个 ownerWP==null 的 DribbleZone（人类玩家）
            bool localTeam = true;
            foreach (var z in allZones) { if (z && z.ownerWP == null) { localTeam = z.isTeammate; break; } }
            poss = (_ownerZone.isTeammate == localTeam) ? -1 : 1; // -1我方，1对方
        }
        GameUIController.I?.SetBorderByPossession(poss);
    }

    // ========= 提供给 UI / 圈 的查询接口 =========
    public float StealCooldownRemaining(DribbleZone z)
    {
        if (z == null) return 0f;
        return _localStealCD.TryGetValue(z, out float t) ? Mathf.Max(0f, t) : 0f;
    }

    public bool CanStealNow(DribbleZone attacker)
    {
        if (_ownerZone == null) return false;                       // 自由球不可抢
        if (attacker == null || attacker == _ownerZone) return false;
        if (IsInvulnerable) return false;                           // 持球无敌
        if (_localStealCD.TryGetValue(attacker, out float cd) && cd > 0f) return false;
        if (attacker.isTeammate == _ownerZone.isTeammate) return false;
        return CirclesOverlap(attacker, _ownerZone);
    }
}
