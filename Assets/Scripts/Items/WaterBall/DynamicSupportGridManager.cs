using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 规则网格候选点 + 热度打分（前进性/距球高斯/远离对手/分散队友/墙安全/LoS）
/// 额外支持：锚点偏好（anchorBias）与局部搜索半径（maxRadius）
/// ——并内置 Scene 视图 Gizmos 可视化调参。
/// </summary>
public class DynamicSupportGridManager : MonoBehaviour
{
    [Header("Field Bounds")]
    [Tooltip("仅用于生成网格的 AABB；建议勾 IsTrigger 并放在不参与 boundaryMask 的层")]
    public BoxCollider fieldBounds;                 // 若为空则用 manualSize
    public Vector2 manualSize = new Vector2(60f, 36f);

    [Header("Grid")]
    public float cellSize = 3.0f;
    public float yLevel = 0f;

    [Header("Weights")]
    public float wForwardAttack = 0.40f;
    public float wForwardDefend = -0.25f;
    public float wBallGauss = 0.45f;
    public float wOppClear = 0.25f;
    public float wMateSpread = 0.25f;
    public float wWallSafety = 0.20f;
    public float wLoS = 0.20f;

    [Header("Params")]
    public float idealDist = 12f;
    public float minDistFromOpp = 0.8f;
    public float minDistFromMate = 5.5f;
    public float wallMargin = 1.2f;
    public LayerMask boundaryMask;
    public LayerMask obstacleMask; // 可留空

    /* ----------------- 内部缓存 ----------------- */
    private readonly List<Transform> cells = new();
    private Bounds fieldAABB;
    private bool gridReady = false;

    /* =========================================================
     *       核心 API（被 WaterPlayer 调用）
     * ========================================================= */

    public Transform GetBestSpot(
        Vector3 ballPos,
        Vector3 friendlyGoalPos,
        Vector3 enemyGoalPos,
        bool possessionUs,
        List<WaterPlayer> opponents,
        List<WaterPlayer> mates,
        Vector3? carrierPos = null,
        Vector3? anchor = null,
        float anchorBias = 0.0f,
        float maxRadius = Mathf.Infinity)
    {
        EnsureGrid();

        if (cells.Count == 0) return null;

        Vector3 fwd = (enemyGoalPos - friendlyGoalPos).normalized;
        Vector3 center = 0.5f * (friendlyGoalPos + enemyGoalPos);

        float best = float.NegativeInfinity;
        Transform bestT = null;

        for (int i = 0; i < cells.Count; i++)
        {
            Vector3 p = cells[i].position;

            if (anchor.HasValue && maxRadius < Mathf.Infinity)
            {
                float da = Vector3.Distance(p, anchor.Value);
                if (da > maxRadius * 1.25f) continue;
            }

            if (IsNearWall(p)) continue;

            float score = EvalScore(
                p, ballPos, fwd, center, possessionUs,
                opponents, mates, carrierPos,
                anchor, anchorBias, maxRadius,
                out bool rejected
            );
            if (rejected) continue;

            if (score > best) { best = score; bestT = cells[i]; }
        }

        return bestT;
    }

    /* =========================================================
     *                     Gizmos 可视化
     * ========================================================= */

    [Header("Debug / Gizmos")]
    public bool debugDraw = true;
    [Tooltip("仅在 Play 模式绘制（避免编辑器静态场景每帧跑物理）")]
    public bool debugPlayModeOnly = false;

    [Tooltip("调参视角：以“这一队”为 mates，另一队为 opponents")]
    public bool debugTeamIsTeammate = true;
    [Tooltip("强制视为进攻（possessionUs=true）。若为 false 则按球权动态判断")]
    public bool debugForceAttacking = false;

    [Space(6)]
    [Tooltip("可选：只在锚点附近画局部热度")]
    public Transform debugAnchor;
    [Range(0f, 1.2f)] public float debugAnchorBias = 0.45f;
    public float debugMaxRadius = 12f;

    [Space(6)]
    [Tooltip("可视化抽样步长（>=1；越大越稀疏，便于大场景）")]
    public int visualSampleStep = 1;
    public float gizmoPointRadius = 0.25f;
    public float gizmoBestRadius = 0.45f;
    [Tooltip("显示前 N 个高分点的数值标签（0 关闭）")]
    public int debugLabelTopN = 0;

    private struct GizCell
    {
        public Vector3 pos;
        public float score;
        public bool eligible;
    }
    private readonly List<GizCell> _gizBuffer = new();

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw) return;
        if (debugPlayModeOnly && !Application.isPlaying) return;

        EnsureGrid();                     // 保证在编辑器也能生成网格
        if (cells.Count == 0) return;

        // 采集场景对象
        Ball ball = FindObjectOfType<Ball>();
        if (!ball) { DrawFieldBoundsGizmo(); return; }

        List<WaterPlayer> allPlayers = GetAllPlayers();
        if (allPlayers.Count == 0) { DrawFieldBoundsGizmo(); return; }

        // 尝试从“我方”队员身上拿到门的位置（更贴近真实比赛方向）
        WaterPlayer proto = null;
        foreach (var p in allPlayers) { if (p && p.isTeammate == debugTeamIsTeammate) { proto = p; break; } }
        if (!proto || !proto.friendlyGoal || !proto.enemyGoal) { DrawFieldBoundsGizmo(); return; }

        Vector3 friendlyGoalPos = proto.friendlyGoal.transform.position;
        Vector3 enemyGoalPos = proto.enemyGoal.transform.position;
        Vector3 fwd = (enemyGoalPos - friendlyGoalPos).normalized;
        Vector3 center = 0.5f * (friendlyGoalPos + enemyGoalPos);

        // 自动分队
        var mates = new List<WaterPlayer>();
        var opps = new List<WaterPlayer>();
        foreach (var p in allPlayers)
            if (p) (p.isTeammate == debugTeamIsTeammate ? mates : opps).Add(p);

        // 判定球权（若未强制为进攻）
        bool possessionUs = true;
        if (!debugForceAttacking)
        {
            if (ball.Owner) possessionUs = (ball.Owner.isTeammate == debugTeamIsTeammate);
            else
            {
                float md = float.MaxValue, od = float.MaxValue;
                foreach (var m in mates) md = Mathf.Min(md, Vector3.Distance(m.Pos, ball.Pos));
                foreach (var o in opps) od = Mathf.Min(od, Vector3.Distance(o.Pos, ball.Pos));
                possessionUs = (md <= od + 0.05f);
            }
        }

        // 锚点与搜索半径（可选）
        Vector3? anchor = debugAnchor ? (Vector3?)debugAnchor.position : null;
        float anchorBias = debugAnchorBias;
        float maxRadius = debugAnchor ? debugMaxRadius : Mathf.Infinity;
        Vector3? carrier = ball.Owner ? ball.Owner.Pos : (Vector3?)null;

        // 计算所有点的分数（用于配色归一化 & TopN）
        _gizBuffer.Clear();
        float sMin = float.PositiveInfinity, sMax = float.NegativeInfinity;
        Transform bestT = null; float bestS = float.NegativeInfinity;

        for (int i = 0; i < cells.Count; i += Mathf.Max(1, visualSampleStep))
        {
            Vector3 p = cells[i].position;

            if (anchor.HasValue && maxRadius < Mathf.Infinity)
            {
                float da = Vector3.Distance(p, anchor.Value);
                if (da > maxRadius * 1.25f) { _gizBuffer.Add(new GizCell { pos = p, score = float.NegativeInfinity, eligible = false }); continue; }
            }

            if (IsNearWall(p)) { _gizBuffer.Add(new GizCell { pos = p, score = float.NegativeInfinity, eligible = false }); continue; }

            float s = EvalScore(p, ball.Pos, fwd, center, possessionUs, opps, mates, carrier, anchor, anchorBias, maxRadius, out bool rejected);

            if (rejected)
            {
                _gizBuffer.Add(new GizCell { pos = p, score = float.NegativeInfinity, eligible = false });
                continue;
            }

            sMin = Mathf.Min(sMin, s);
            sMax = Mathf.Max(sMax, s);
            if (s > bestS) { bestS = s; bestT = cells[i]; }
            _gizBuffer.Add(new GizCell { pos = p, score = s, eligible = true });
        }

        DrawFieldBoundsGizmo();

        // 画点（颜色：低→高 = 红→黄→绿→青）
        foreach (var c in _gizBuffer)
        {
            if (!c.eligible)
            {
                Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.35f);
                Gizmos.DrawSphere(c.pos, gizmoPointRadius * 0.65f);
                continue;
            }

            float t = (sMax > sMin) ? Mathf.InverseLerp(sMin, sMax, c.score) : 0.5f;
            Gizmos.color = ScoreToColor(t);
            Gizmos.DrawSphere(c.pos, gizmoPointRadius);
        }

        // 高亮最佳点
        if (bestT)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(bestT.position, gizmoBestRadius);
        }

#if UNITY_EDITOR
        // TopN 标签
        if (debugLabelTopN > 0 && _gizBuffer.Count > 0)
        {
            var top = new List<GizCell>();
            foreach (var c in _gizBuffer) if (c.eligible) top.Add(c);
            top.Sort((a, b) => b.score.CompareTo(a.score));
            int n = Mathf.Min(debugLabelTopN, top.Count);
            for (int i = 0; i < n; i++)
            {
                var c = top[i];
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(c.pos + Vector3.up * 0.35f, $"{i + 1}:{c.score:F2}");
            }
        }
#endif
    }

    /* =========================================================
     *                      评分函数（公私通用）
     * ========================================================= */

    private float EvalScore(
        Vector3 p,
        Vector3 ballPos,
        Vector3 fwd,
        Vector3 center,
        bool possessionUs,
        List<WaterPlayer> opponents,
        List<WaterPlayer> mates,
        Vector3? carrierPos,
        Vector3? anchor,
        float anchorBias,
        float maxRadius,
        out bool rejected)
    {
        rejected = false;

        // 前进性
        float forward = Vector3.Dot((p - ballPos).normalized, possessionUs ? fwd : -fwd);
        float wForward = possessionUs ? wForwardAttack : wForwardDefend;

        // 距球高斯
        float dBall = Vector3.Distance(ballPos, p);
        float gauss = Mathf.Exp(-(dBall - idealDist) * (dBall - idealDist) / (2 * idealDist * idealDist));

        // 敌人清空
        float oppScore = 0f;
        foreach (var o in opponents)
        {
            if (!o) continue;
            float d = Vector3.Distance(o.Pos, p);
            oppScore += Mathf.Clamp01((d - minDistFromOpp) / (minDistFromOpp * 4f));
        }
        if (opponents.Count > 0) oppScore /= opponents.Count;

        // 队友分散
        float mateScore = 0f;
        foreach (var m in mates)
        {
            if (!m) continue;
            float d = Vector3.Distance(m.Pos, p);
            mateScore += Mathf.Clamp01((d - minDistFromMate) / (minDistFromMate * 4f));
        }
        if (mates.Count > 0) mateScore /= mates.Count;

        // 屏蔽持球者脚边
        if (carrierPos.HasValue && Vector3.Distance(p, carrierPos.Value) < 3.2f)
        { rejected = true; return float.NegativeInfinity; }

        // LoS（可选）
        float los = 1f;
        if (obstacleMask.value != 0)
        {
            Vector3 dir = (p - ballPos); dir.y = 0;
            float dist = dir.magnitude;
            dir = dir.normalized;
            if (Physics.SphereCast(ballPos + Vector3.up * 0.2f, 0.2f, dir, out var hit, dist, obstacleMask))
                los = 0.2f;
        }

        // 墙安全
        float wallScore = WallSafety(p);

        // 锚点偏好
        float anchorBonus = 0f;
        if (anchor.HasValue && anchorBias > 0f)
        {
            float da = Vector3.Distance(p, anchor.Value);
            float norm = (maxRadius < Mathf.Infinity && maxRadius > 0.1f)
                       ? Mathf.Clamp01(da / maxRadius)
                       : Mathf.Clamp01(da / (cellSize * 8f));
            anchorBonus = anchorBias * (1f - norm);
        }

        float score =
            wForward * forward +
            wBallGauss * gauss +
            wOppClear * oppScore +
            wMateSpread * mateScore +
            wWallSafety * wallScore +
            wLoS * los +
            anchorBonus;

        // 微弱地偏向场地中心，避免贴边
        Vector3 toC = (center - p); toC.y = 0; toC.Normalize();
        score += 0.05f * Vector3.Dot(toC, fwd);

        return score;
    }

    /* =========================================================
     *                   网格生成 & 工具
     * ========================================================= */

    private void EnsureGrid()
    {
        if (gridReady && cells.Count > 0 && fieldAABB.size != Vector3.zero) return;

        cells.Clear();

        if (fieldBounds) fieldAABB = fieldBounds.bounds;
        else fieldAABB = new Bounds(transform.position, new Vector3(manualSize.x, 1f, manualSize.y));

        if (fieldAABB.size == Vector3.zero) return;

        int nx = Mathf.Max(1, Mathf.FloorToInt(fieldAABB.size.x / cellSize));
        int nz = Mathf.Max(1, Mathf.FloorToInt(fieldAABB.size.z / cellSize));

        for (int ix = 0; ix < nx; ix++)
            for (int iz = 0; iz < nz; iz++)
            {
                Vector3 pos = new Vector3(
                    fieldAABB.min.x + (ix + 0.5f) * cellSize,
                    yLevel,
                    fieldAABB.min.z + (iz + 0.5f) * cellSize
                );

                // 为了不污染层级视图，Gizmos 模式下不实例化 GameObject；
                // 运行时（Play）才生成 Transform（与原实现一致）
                if (Application.isPlaying)
                {
                    var go = new GameObject($"Cell_{ix}_{iz}");
                    go.transform.SetParent(transform, false);
                    go.transform.position = pos;
                    cells.Add(go.transform);
                }
                else
                {
                    // 编辑器预览：用一个临时 Transform-less 的点位记下来
                    // 我们用 fieldAABB + 索引计算位置即可，不需要真对象
                    // 这里用一个占位的空对象容器
                    var go = new GameObject(); // 注意：编辑器下也会出现在层级，这里换成“虚拟容器”
                    go.hideFlags = HideFlags.HideAndDontSave;
                    go.transform.position = pos;
                    cells.Add(go.transform);
                }
            }

        gridReady = true;
    }

    private bool IsNearWall(Vector3 p)
    {
        Vector3[] dirs = { Vector3.forward, -Vector3.forward, Vector3.right, -Vector3.right };
        foreach (var d in dirs)
            if (Physics.Raycast(p + Vector3.up * 0.3f, d, wallMargin, boundaryMask))
                return true;
        return false;
    }

    private float WallSafety(Vector3 p)
    {
        int hitCount = 0;
        Vector3[] dirs = { Vector3.forward, -Vector3.forward, Vector3.right, -Vector3.right };
        foreach (var d in dirs)
            if (Physics.Raycast(p + Vector3.up * 0.3f, d, out var _, wallMargin * 4f, boundaryMask))
                hitCount++;
        return 1f - (hitCount / 4f);
    }

    private List<WaterPlayer> GetAllPlayers()
    {
        // Play 时优先用注册表（更快更准），否则场景扫描
        var list = new List<WaterPlayer>();
        if (Application.isPlaying)
        {
            foreach (var p in WaterPlayerManager.All) if (p) list.Add(p);
        }
        else
        {
            var arr = FindObjectsOfType<WaterPlayer>();
            list.AddRange(arr);
        }
        return list;
    }

    private void DrawFieldBoundsGizmo()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 0.9f, 0.25f);
        Gizmos.matrix = Matrix4x4.identity;

        Bounds b = fieldBounds ? fieldBounds.bounds
                               : new Bounds(transform.position, new Vector3(manualSize.x, 0.1f, manualSize.y));

        Gizmos.DrawWireCube(b.center, b.size);
    }

    private Color ScoreToColor(float t)
    {
        // t: 0..1  红 -> 黄 -> 绿 -> 青
        t = Mathf.Clamp01(t);
        if (t < 0.5f)
        {
            // 红(1,0,0) 到 黄(1,1,0)
            float u = t / 0.5f;
            return new Color(1f, u, 0f, 0.9f);
        }
        else
        {
            // 黄(1,1,0) 到 青(0,1,1)
            float u = (t - 0.5f) / 0.5f;
            return new Color(1f - u, 1f, u, 0.9f);
        }
    }

    private void OnDestroy()
    {
        // 清理编辑器下的 HideAndDontSave 对象，避免泄露
        if (!Application.isPlaying && cells.Count > 0)
        {
            foreach (var t in cells) if (t) DestroyImmediate(t.gameObject);
            cells.Clear();
        }
    }
}
