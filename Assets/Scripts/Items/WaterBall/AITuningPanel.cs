using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 运行时调参面板（IMGUI）
/// - 自动发现：Ball / DynamicSupportGridManager / 所有 WaterPlayer
/// - 实时同步：把滑杆值推送到“所有 AI / 全场唯一球 / 全局网格”
/// - 预设：三组保存/读取（PlayerPrefs）
/// - 快捷键：F1 显示/隐藏；Auto Apply 实时应用
/// - 网格重建：按钮触发反射调用 DynamicSupportGridManager 的私有 EnsureGrid（并清理旧 cells）
///
/// 注意：
/// 1) 仅使用运行时安全 API，不依赖 Editor；
/// 2) 某些参数（如 cellSize）改变后需要 Rebuild Grid 才生效；
/// 3) 如果你的类名与这里一致（Ball / WaterPlayer / DynamicSupportGridManager），即插即用。
/// </summary>
public class AITuningPanel : MonoBehaviour
{
    /* ---------- runtime discover ---------- */
    Ball ball;
    DynamicSupportGridManager grid;
    readonly List<WaterPlayer> players = new();

    /* ---------- ui state ---------- */
    bool show = true;
    bool autoApply = true;
    Vector2 scroll;
    Rect win = new Rect(24, 24, 520, 720);

    bool fBall = true, fWP_Role = true, fWP_Move = false, fWP_PassShoot = false, fWP_Wall = true, fGrid = true, fMisc = false;

    /* ---------- tunables (UI model) ---------- */
    // Ball
    float ball_friction = 1.0f;
    float ball_playerBounceSpeed = 18f;
    float bounceRestitution = 0.65f, bounceTangentDamping = 0.15f, slideInwardBoost = 1.8f;
    float minBounceSpeed = 1.0f, bounceNearDist = 0.55f, bounceDetectRadius = 0.65f, bounceCooldown = 0.05f;

    // WaterPlayer (role/grid)
    float anchorBias = 0.45f;
    float searchR_St = 14f, searchR_Mid = 11f, searchR_Def = 10f;

    // WaterPlayer (move/anim/spacing)
    float baseSpeed = 3.7f, sprintMultiplier = 1.9f, turnSpeed = 540f, maxSwimSpeed = 5f;
    float separationRadius = 3.5f, separationWeight = 0.55f;

    // WaterPlayer (pass/shoot distances + ability)
    float distPassMin = 6f, distPassMax = 25f;
    float closeAutoShootDist = 3f, shotMinDist = 8f, shotMaxDist = 22f;
    float abilityAccuracy = .85f, abilityPower = .90f;

    // WaterPlayer (dribble/quick)
    float dribbleTapPower = 7f, dribbleTapMeters = 3.5f, dribbleTapCooldown = 0.18f;
    float quickFirstTouchWindow = 0.35f, quickThreatRadius = 5.5f;

    // WaterPlayer (wall/escape)
    float boundaryProbeRadius = 0.6f, boundaryProbeAhead = 1.2f, boundaryNearDistance = 1.1f;
    float boundaryEscapeMeters = 8f, boundaryEscapePower = 13f;

    float wallDetectRadius = 1.6f, wallNearThreshold = 1.0f, wallKickCooldown = 0.40f;
    float wallZoneWidth = 1.6f, centerKickAhead = 8f, centerKickPower = 13f, centerKickCooldown = 0.30f;

    // WaterPlayer (far-wall lookahead veto)
    float wallLookahead = 10f, hazardProbeRadius = 0.22f, wallHazardVeto = 0.65f, wallHazardRotate = 0.55f, goalToleranceDeg = 18f;

    // Grid (weights/params)
    float wForwardAttack = 0.45f, wForwardDefend = -0.25f, wBallGauss = 0.50f, wOppClear = 0.25f, wMateSpread = 0.28f, wWallSafety = 0.22f, wLoS = 0.20f;
    float idealDist = 12f, minDistFromOpp = 1.0f, minDistFromMate = 6.0f, wallMargin = 1.3f;
    float cellSize = 3.0f; // 需 Rebuild 才生效

    void Awake()
    {
        RefreshSceneRefs();
        PullFromScene(); // 将场景当前值拉到 UI
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) show = !show;

        // 防止运行中物体被销毁/重建
        if (!ball || !grid || players.Count == 0 || AnyPlayerNull()) RefreshSceneRefs();

        if (autoApply) ApplyToScene();
    }

    bool AnyPlayerNull()
    {
        for (int i = 0; i < players.Count; i++) if (!players[i]) return true;
        return false;
    }

    void RefreshSceneRefs()
    {
        ball = FindObjectOfType<Ball>();
        grid = FindObjectOfType<DynamicSupportGridManager>();

        players.Clear();
        // 优先用 WaterPlayerManager；如果没跑起来，就全局找
        var regType = typeof(WaterPlayerManager);
        var propAll = regType.GetProperty("All", BindingFlags.Public | BindingFlags.Static);
        var list = propAll?.GetValue(null) as System.Collections.IEnumerable;
        if (list != null)
        {
            foreach (var p in list) if (p is WaterPlayer wp && wp) players.Add(wp);
        }
        if (players.Count == 0)
            players.AddRange(FindObjectsOfType<WaterPlayer>());
    }

    void OnGUI()
    {
        if (!show) return;

        win = GUI.Window(GetInstanceID(), win, DrawWindow, "<b>AI Runtime Tuner</b>");
        // 允许拖拽
        if (Event.current.type == EventType.MouseDrag && win.Contains(Event.current.mousePosition))
            GUI.DragWindow();
    }

    void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        autoApply = GUILayout.Toggle(autoApply, "Auto Apply", GUILayout.Width(100));
        if (GUILayout.Button("Apply Now", GUILayout.Width(90))) ApplyToScene();
        if (GUILayout.Button("Pull From Scene", GUILayout.Width(130))) PullFromScene();
        if (GUILayout.Button("Save Preset 1", GUILayout.Width(110))) SavePreset(1);
        if (GUILayout.Button("Load Preset 1", GUILayout.Width(110))) { LoadPreset(1); ApplyToScene(); }
        GUILayout.EndHorizontal();

        scroll = GUILayout.BeginScrollView(scroll);

        /* ---------------- Ball ---------------- */
        fBall = FoldoutHeader(fBall, "Ball (bounce/ground)");
        if (fBall && ball)
        {
            ball_friction = Slider("friction", ball_friction, 0f, 6f);
            ball_playerBounceSpeed = Slider("playerBounceSpeed", ball_playerBounceSpeed, 0f, 30f);
            GUILayout.Label("[Boundary / Goal bounce]");
            bounceRestitution = Slider("bounceRestitution", bounceRestitution, 0f, 1f);
            bounceTangentDamping = Slider("bounceTangentDamping", bounceTangentDamping, 0f, 1f);
            slideInwardBoost = Slider("slideInwardBoost", slideInwardBoost, 0f, 4f);
            minBounceSpeed = Slider("minBounceSpeed", minBounceSpeed, 0f, 6f);
            bounceNearDist = Slider("bounceNearDist", bounceNearDist, 0.2f, 2.5f);
            bounceDetectRadius = Slider("bounceDetectRadius", bounceDetectRadius, 0.2f, 3f);
            bounceCooldown = Slider("bounceCooldown", bounceCooldown, 0f, 0.5f);
            SpaceLine();
        }

        /* ---------------- WaterPlayer: Role/Grid ---------------- */
        fWP_Role = FoldoutHeader(fWP_Role, "WaterPlayer • Role & Grid");
        if (fWP_Role)
        {
            anchorBias = Slider("anchorBias", anchorBias, 0f, 1f);
            searchR_St = Slider("searchRadius_Striker", searchR_St, 4f, 25f);
            searchR_Mid = Slider("searchRadius_Mid", searchR_Mid, 4f, 25f);
            searchR_Def = Slider("searchRadius_Def", searchR_Def, 4f, 25f);
            SpaceLine();
        }

        /* ---------------- WaterPlayer: Move/Spacing ---------------- */
        fWP_Move = FoldoutHeader(fWP_Move, "WaterPlayer • Movement / Spacing");
        if (fWP_Move)
        {
            baseSpeed = Slider("baseSpeed", baseSpeed, 1f, 8f);
            sprintMultiplier = Slider("sprintMultiplier", sprintMultiplier, 1f, 3f);
            turnSpeed = Slider("turnSpeed", turnSpeed, 120f, 1080f);
            maxSwimSpeed = Slider("maxSwimSpeed(anim)", maxSwimSpeed, 1f, 12f);
            separationRadius = Slider("separationRadius", separationRadius, 0.5f, 8f);
            separationWeight = Slider("separationWeight", separationWeight, 0f, 1f);
            SpaceLine();
        }

        /* ---------------- WaterPlayer: Pass / Shoot ---------------- */
        fWP_PassShoot = FoldoutHeader(fWP_PassShoot, "WaterPlayer • Pass / Shoot / Ability");
        if (fWP_PassShoot)
        {
            distPassMin = Slider("distPassMin", distPassMin, 0.5f, 20f);
            distPassMax = Slider("distPassMax", distPassMax, 1f, 40f);
            closeAutoShootDist = Slider("closeAutoShootDist", closeAutoShootDist, 0.5f, 8f);
            shotMinDist = Slider("shotMinDist", shotMinDist, 2f, 20f);
            shotMaxDist = Slider("shotMaxDist", shotMaxDist, 6f, 40f);
            abilityAccuracy = Slider("accuracy", abilityAccuracy, 0.1f, 1f);
            abilityPower = Slider("power", abilityPower, 0.1f, 1f);

            GUILayout.Label("[Dribble / Quick]");
            dribbleTapPower = Slider("dribbleTapPower", dribbleTapPower, 0.5f, 16f);
            dribbleTapMeters = Slider("dribbleTapMeters", dribbleTapMeters, 0.5f, 8f);
            dribbleTapCooldown = Slider("dribbleTapCooldown", dribbleTapCooldown, 0f, 1f);
            quickFirstTouchWindow = Slider("quickFirstTouchWindow", quickFirstTouchWindow, 0f, 1f);
            quickThreatRadius = Slider("quickThreatRadius", quickThreatRadius, 0.5f, 12f);
            SpaceLine();
        }

        /* ---------------- WaterPlayer: Wall / Escape ---------------- */
        fWP_Wall = FoldoutHeader(fWP_Wall, "WaterPlayer • Wall / Hazard / Escape");
        if (fWP_Wall)
        {
            GUILayout.Label("[Near-wall sense + escape]");
            boundaryProbeRadius = Slider("boundaryProbeRadius", boundaryProbeRadius, 0.1f, 2.0f);
            boundaryProbeAhead = Slider("boundaryProbeAhead", boundaryProbeAhead, 0.2f, 4.0f);
            boundaryNearDistance = Slider("boundaryNearDistance", boundaryNearDistance, 0.2f, 3.0f);
            boundaryEscapeMeters = Slider("boundaryEscapeMeters", boundaryEscapeMeters, 2f, 20f);
            boundaryEscapePower = Slider("boundaryEscapePower", boundaryEscapePower, 4f, 30f);

            GUILayout.Label("[Wall Zone Centering]");
            wallZoneWidth = Slider("wallZoneWidth", wallZoneWidth, 0.6f, 3.0f);
            centerKickAhead = Slider("centerKickAhead", centerKickAhead, 2f, 18f);
            centerKickPower = Slider("centerKickPower", centerKickPower, 6f, 25f);
            centerKickCooldown = Slider("centerKickCooldown", centerKickCooldown, 0f, 1.0f);

            GUILayout.Label("[Robust wall (near)]");
            wallDetectRadius = Slider("wallDetectRadius", wallDetectRadius, 0.2f, 4f);
            wallNearThreshold = Slider("wallNearThreshold", wallNearThreshold, 0.2f, 3f);
            wallKickCooldown = Slider("wallKickCooldown", wallKickCooldown, 0f, 2f);

            GUILayout.Label("[Far-wall lookahead veto]");
            wallLookahead = Slider("wallLookahead", wallLookahead, 2f, 25f);
            hazardProbeRadius = Slider("hazardProbeRadius", hazardProbeRadius, 0.05f, 0.8f);
            wallHazardVeto = Slider("wallHazardVeto", wallHazardVeto, 0f, 1f);
            wallHazardRotate = Slider("wallHazardRotate", wallHazardRotate, 0f, 1f);
            goalToleranceDeg = Slider("goalToleranceDeg", goalToleranceDeg, 0f, 45f);
            SpaceLine();
        }

        /* ---------------- Grid ---------------- */
        fGrid = FoldoutHeader(fGrid, "DynamicSupportGrid (weights / params)");
        if (fGrid && grid)
        {
            GUILayout.Label("[Weights]");
            wForwardAttack = Slider("wForwardAttack", wForwardAttack, -1f, 1f);
            wForwardDefend = Slider("wForwardDefend", wForwardDefend, -1f, 1f);
            wBallGauss = Slider("wBallGauss", wBallGauss, 0f, 1.5f);
            wOppClear = Slider("wOppClear", wOppClear, 0f, 1.5f);
            wMateSpread = Slider("wMateSpread", wMateSpread, 0f, 1.5f);
            wWallSafety = Slider("wWallSafety", wWallSafety, 0f, 1.5f);
            wLoS = Slider("wLoS", wLoS, 0f, 1.5f);

            GUILayout.Label("[Params]");
            idealDist = Slider("idealDist", idealDist, 4f, 24f);
            minDistFromOpp = Slider("minDistFromOpp", minDistFromOpp, 0.3f, 3f);
            minDistFromMate = Slider("minDistFromMate", minDistFromMate, 2f, 12f);
            wallMargin = Slider("wallMargin", wallMargin, 0.5f, 3f);

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            cellSize = Slider("cellSize", cellSize, 1f, 6f);
            if (GUILayout.Button("Rebuild Grid", GUILayout.Width(120))) ForceGridRebuild();
            GUILayout.EndHorizontal();
            SpaceLine();
        }

        /* ---------------- Misc ---------------- */
        fMisc = FoldoutHeader(fMisc, "Misc");
        if (fMisc)
        {
            GUILayout.Label("Hotkey: F1 显示/隐藏面板");
            GUILayout.Label("Tip: Auto Apply 开启后滑杆即刻推送到场景");
            GUILayout.Label("Grid Rebuild 需要 DynamicSupportGridManager 已挂载");
            SpaceLine();
        }

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Preset 2")) SavePreset(2);
        if (GUILayout.Button("Load Preset 2")) { LoadPreset(2); ApplyToScene(); }
        if (GUILayout.Button("Save Preset 3")) SavePreset(3);
        if (GUILayout.Button("Load Preset 3")) { LoadPreset(3); ApplyToScene(); }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close (F1)", GUILayout.Width(100))) show = false;
        GUILayout.EndHorizontal();

        GUI.DragWindow();
    }

    /* ========================= Apply & Pull ========================= */

    void ApplyToScene()
    {
        // Ball
        if (ball)
        {
            ball.friction = ball_friction;
            ball.playerBounceSpeed = ball_playerBounceSpeed;
            ball.bounceRestitution = bounceRestitution;
            ball.bounceTangentDamping = bounceTangentDamping;
            ball.slideInwardBoost = slideInwardBoost;
            ball.minBounceSpeed = minBounceSpeed;
            ball.bounceNearDist = bounceNearDist;
            ball.bounceDetectRadius = bounceDetectRadius;
            ball.bounceCooldown = bounceCooldown;
        }

        // WaterPlayers
        foreach (var p in players)
        {
            if (!p) continue;

            // role/grid
            p.anchorBias = anchorBias;
            p.searchRadius_Striker = searchR_St;
            p.searchRadius_Mid = searchR_Mid;
            p.searchRadius_Def = searchR_Def;

            // move/spacing
            p.baseSpeed = baseSpeed;
            p.sprintMultiplier = sprintMultiplier;
            p.turnSpeed = turnSpeed;
            p.maxSwimSpeed = maxSwimSpeed;
            p.separationRadius = separationRadius;
            p.separationWeight = separationWeight;

            // pass/shoot/ability
            p.distPassMin = distPassMin;
            p.distPassMax = distPassMax;
            p.closeAutoShootDist = closeAutoShootDist;
            p.shotMinDist = shotMinDist;
            p.shotMaxDist = shotMaxDist;
            p.accuracy = abilityAccuracy;
            p.power = abilityPower;

            // dribble/quick
            p.dribbleTapPower = dribbleTapPower;
            p.dribbleTapMeters = dribbleTapMeters;
            p.dribbleTapCooldown = dribbleTapCooldown;
            p.quickFirstTouchWindow = quickFirstTouchWindow;
            p.quickThreatRadius = quickThreatRadius;

            // wall / escape
            p.boundaryProbeRadius = boundaryProbeRadius;
            p.boundaryProbeAhead = boundaryProbeAhead;
            p.boundaryNearDistance = boundaryNearDistance;
            p.boundaryEscapeMeters = boundaryEscapeMeters;
            p.boundaryEscapePower = boundaryEscapePower;

            p.wallDetectRadius = wallDetectRadius;
            p.wallNearThreshold = wallNearThreshold;
            p.wallKickCooldown = wallKickCooldown;

            p.wallZoneWidth = wallZoneWidth;
            p.centerKickAhead = centerKickAhead;
            p.centerKickPower = centerKickPower;
            p.centerKickCooldown = centerKickCooldown;

            // far-wall veto
            p.wallLookahead = wallLookahead;
            p.hazardProbeRadius = hazardProbeRadius;
            p.wallHazardVeto = wallHazardVeto;
            p.wallHazardRotate = wallHazardRotate;
            p.goalToleranceDeg = goalToleranceDeg;
        }

        // Grid
        if (grid)
        {
            grid.wForwardAttack = wForwardAttack;
            grid.wForwardDefend = wForwardDefend;
            grid.wBallGauss = wBallGauss;
            grid.wOppClear = wOppClear;
            grid.wMateSpread = wMateSpread;
            grid.wWallSafety = wWallSafety;
            grid.wLoS = wLoS;

            grid.idealDist = idealDist;
            grid.minDistFromOpp = minDistFromOpp;
            grid.minDistFromMate = minDistFromMate;
            grid.wallMargin = wallMargin;

            // cellSize 变更仅记录，需手动 Rebuild
            grid.cellSize = cellSize;
        }
    }

    void PullFromScene()
    {
        if (ball)
        {
            ball_friction = ball.friction;
            ball_playerBounceSpeed = ball.playerBounceSpeed;
            bounceRestitution = Get(ball, "bounceRestitution", bounceRestitution);
            bounceTangentDamping = Get(ball, "bounceTangentDamping", bounceTangentDamping);
            slideInwardBoost = Get(ball, "slideInwardBoost", slideInwardBoost);
            minBounceSpeed = Get(ball, "minBounceSpeed", minBounceSpeed);
            bounceNearDist = Get(ball, "bounceNearDist", bounceNearDist);
            bounceDetectRadius = Get(ball, "bounceDetectRadius", bounceDetectRadius);
            bounceCooldown = Get(ball, "bounceCooldown", bounceCooldown);
        }

        if (players.Count > 0)
        {
            var p = players[0];
            anchorBias = p.anchorBias;
            searchR_St = p.searchRadius_Striker;
            searchR_Mid = p.searchRadius_Mid;
            searchR_Def = p.searchRadius_Def;

            baseSpeed = p.baseSpeed;
            sprintMultiplier = p.sprintMultiplier;
            turnSpeed = p.turnSpeed;
            maxSwimSpeed = p.maxSwimSpeed;
            separationRadius = p.separationRadius;
            separationWeight = p.separationWeight;

            distPassMin = p.distPassMin;
            distPassMax = p.distPassMax;
            closeAutoShootDist = p.closeAutoShootDist;
            shotMinDist = p.shotMinDist;
            shotMaxDist = p.shotMaxDist;
            abilityAccuracy = p.accuracy;
            abilityPower = p.power;

            dribbleTapPower = p.dribbleTapPower;
            dribbleTapMeters = p.dribbleTapMeters;
            dribbleTapCooldown = p.dribbleTapCooldown;
            quickFirstTouchWindow = p.quickFirstTouchWindow;
            quickThreatRadius = p.quickThreatRadius;

            boundaryProbeRadius = p.boundaryProbeRadius;
            boundaryProbeAhead = p.boundaryProbeAhead;
            boundaryNearDistance = p.boundaryNearDistance;
            boundaryEscapeMeters = p.boundaryEscapeMeters;
            boundaryEscapePower = p.boundaryEscapePower;

            wallDetectRadius = p.wallDetectRadius;
            wallNearThreshold = p.wallNearThreshold;
            wallKickCooldown = p.wallKickCooldown;

            wallZoneWidth = p.wallZoneWidth;
            centerKickAhead = p.centerKickAhead;
            centerKickPower = p.centerKickPower;
            centerKickCooldown = p.centerKickCooldown;

            wallLookahead = p.wallLookahead;
            hazardProbeRadius = p.hazardProbeRadius;
            wallHazardVeto = p.wallHazardVeto;
            wallHazardRotate = p.wallHazardRotate;
            goalToleranceDeg = p.goalToleranceDeg;
        }

        if (grid)
        {
            wForwardAttack = grid.wForwardAttack;
            wForwardDefend = grid.wForwardDefend;
            wBallGauss = grid.wBallGauss;
            wOppClear = grid.wOppClear;
            wMateSpread = grid.wMateSpread;
            wWallSafety = grid.wWallSafety;
            wLoS = grid.wLoS;

            idealDist = grid.idealDist;
            minDistFromOpp = grid.minDistFromOpp;
            minDistFromMate = grid.minDistFromMate;
            wallMargin = grid.wallMargin;

            cellSize = grid.cellSize;
        }
    }

    /* ========================= Helpers ========================= */

    float Slider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(190));
        value = GUILayout.HorizontalSlider(value, min, max);
        value = Mathf.Clamp(value, min, max);
        GUILayout.Label(value.ToString("F2"), GUILayout.Width(60));
        GUILayout.EndHorizontal();
        return value;
    }

    void SpaceLine() { GUILayout.Space(6); DrawLine(); GUILayout.Space(6); }
    bool FoldoutHeader(bool v, string title)
    {
        GUILayout.Space(2);
        GUILayout.BeginHorizontal("Box");
        v = GUILayout.Toggle(v, v ? "▼ " + title : "► " + title, "Label");
        GUILayout.EndHorizontal();
        return v;
    }
    void DrawLine()
    {
        var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1));
        EditorGUI_DrawRect(rect, new Color(1, 1, 1, 0.18f));
    }
    void EditorGUI_DrawRect(Rect r, Color c)
    {
        Color old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
    }

    T Get<T>(object o, string fieldName, T fallback)
    {
        var f = o.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return fallback;
        var v = f.GetValue(o);
        if (v is T t) return t;
        return fallback;
    }

    void ForceGridRebuild()
    {
        if (!grid) return;

        var t = grid.GetType();

        // 1) 尝试清理 cells 列表并销毁旧对象
        var fCells = t.GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fCells != null)
        {
            var lst = fCells.GetValue(grid) as System.Collections.IList;
            if (lst != null)
            {
                for (int i = 0; i < lst.Count; i++)
                {
                    var tr = lst[i] as Transform;
                    if (tr) Destroy(tr.gameObject);
                }
                lst.Clear();
            }
        }

        // 2) 标记 gridReady = false
        var fReady = t.GetField("gridReady", BindingFlags.Instance | BindingFlags.NonPublic);
        if (fReady != null) fReady.SetValue(grid, false);

        // 3) 直接调用 EnsureGrid()
        var mEnsure = t.GetMethod("EnsureGrid", BindingFlags.Instance | BindingFlags.NonPublic);
        if (mEnsure != null) mEnsure.Invoke(grid, null);
    }

    /* ========================= Presets ========================= */

    void SavePreset(int idx)
    {
        string p = "AI_TUNER_P" + idx + "_";

        // Ball
        PlayerPrefs.SetFloat(p + "bf", ball_friction);
        PlayerPrefs.SetFloat(p + "pbs", ball_playerBounceSpeed);
        PlayerPrefs.SetFloat(p + "br", bounceRestitution);
        PlayerPrefs.SetFloat(p + "btd", bounceTangentDamping);
        PlayerPrefs.SetFloat(p + "sib", slideInwardBoost);
        PlayerPrefs.SetFloat(p + "mbs", minBounceSpeed);
        PlayerPrefs.SetFloat(p + "bnd", bounceNearDist);
        PlayerPrefs.SetFloat(p + "bdr", bounceDetectRadius);
        PlayerPrefs.SetFloat(p + "bc", bounceCooldown);

        // WP role/grid
        PlayerPrefs.SetFloat(p + "ab", anchorBias);
        PlayerPrefs.SetFloat(p + "srS", searchR_St);
        PlayerPrefs.SetFloat(p + "srM", searchR_Mid);
        PlayerPrefs.SetFloat(p + "srD", searchR_Def);

        // WP move
        PlayerPrefs.SetFloat(p + "bs", baseSpeed);
        PlayerPrefs.SetFloat(p + "sm", sprintMultiplier);
        PlayerPrefs.SetFloat(p + "ts", turnSpeed);
        PlayerPrefs.SetFloat(p + "ms", maxSwimSpeed);
        PlayerPrefs.SetFloat(p + "spr", separationRadius);
        PlayerPrefs.SetFloat(p + "spw", separationWeight);

        // pass/shoot/ability
        PlayerPrefs.SetFloat(p + "dmin", distPassMin);
        PlayerPrefs.SetFloat(p + "dmax", distPassMax);
        PlayerPrefs.SetFloat(p + "cas", closeAutoShootDist);
        PlayerPrefs.SetFloat(p + "smin", shotMinDist);
        PlayerPrefs.SetFloat(p + "smax", shotMaxDist);
        PlayerPrefs.SetFloat(p + "acc", abilityAccuracy);
        PlayerPrefs.SetFloat(p + "pow", abilityPower);

        // dribble/quick
        PlayerPrefs.SetFloat(p + "dtp", dribbleTapPower);
        PlayerPrefs.SetFloat(p + "dtm", dribbleTapMeters);
        PlayerPrefs.SetFloat(p + "dtc", dribbleTapCooldown);
        PlayerPrefs.SetFloat(p + "qfw", quickFirstTouchWindow);
        PlayerPrefs.SetFloat(p + "qtr", quickThreatRadius);

        // wall/escape
        PlayerPrefs.SetFloat(p + "bpr", boundaryProbeRadius);
        PlayerPrefs.SetFloat(p + "bpa", boundaryProbeAhead);
        PlayerPrefs.SetFloat(p + "bnd2", boundaryNearDistance);
        PlayerPrefs.SetFloat(p + "bem", boundaryEscapeMeters);
        PlayerPrefs.SetFloat(p + "bep", boundaryEscapePower);

        PlayerPrefs.SetFloat(p + "wdr", wallDetectRadius);
        PlayerPrefs.SetFloat(p + "wnt", wallNearThreshold);
        PlayerPrefs.SetFloat(p + "wkc", wallKickCooldown);

        PlayerPrefs.SetFloat(p + "wzw", wallZoneWidth);
        PlayerPrefs.SetFloat(p + "cka", centerKickAhead);
        PlayerPrefs.SetFloat(p + "ckp", centerKickPower);
        PlayerPrefs.SetFloat(p + "ckc", centerKickCooldown);

        PlayerPrefs.SetFloat(p + "wl", wallLookahead);
        PlayerPrefs.SetFloat(p + "hpr", hazardProbeRadius);
        PlayerPrefs.SetFloat(p + "whv", wallHazardVeto);
        PlayerPrefs.SetFloat(p + "whr", wallHazardRotate);
        PlayerPrefs.SetFloat(p + "gtd", goalToleranceDeg);

        // grid
        PlayerPrefs.SetFloat(p + "wfa", wForwardAttack);
        PlayerPrefs.SetFloat(p + "wfd", wForwardDefend);
        PlayerPrefs.SetFloat(p + "wbg", wBallGauss);
        PlayerPrefs.SetFloat(p + "woc", wOppClear);
        PlayerPrefs.SetFloat(p + "wms", wMateSpread);
        PlayerPrefs.SetFloat(p + "wws", wWallSafety);
        PlayerPrefs.SetFloat(p + "wls", wLoS);

        PlayerPrefs.SetFloat(p + "idl", idealDist);
        PlayerPrefs.SetFloat(p + "mdo", minDistFromOpp);
        PlayerPrefs.SetFloat(p + "mdm", minDistFromMate);
        PlayerPrefs.SetFloat(p + "wm", wallMargin);
        PlayerPrefs.SetFloat(p + "cs", cellSize);

        PlayerPrefs.Save();
    }

    void LoadPreset(int idx)
    {
        string p = "AI_TUNER_P" + idx + "_";

        // Ball
        ball_friction = PlayerPrefs.GetFloat(p + "bf", ball_friction);
        ball_playerBounceSpeed = PlayerPrefs.GetFloat(p + "pbs", ball_playerBounceSpeed);
        bounceRestitution = PlayerPrefs.GetFloat(p + "br", bounceRestitution);
        bounceTangentDamping = PlayerPrefs.GetFloat(p + "btd", bounceTangentDamping);
        slideInwardBoost = PlayerPrefs.GetFloat(p + "sib", slideInwardBoost);
        minBounceSpeed = PlayerPrefs.GetFloat(p + "mbs", minBounceSpeed);
        bounceNearDist = PlayerPrefs.GetFloat(p + "bnd", bounceNearDist);
        bounceDetectRadius = PlayerPrefs.GetFloat(p + "bdr", bounceDetectRadius);
        bounceCooldown = PlayerPrefs.GetFloat(p + "bc", bounceCooldown);

        // WP role/grid
        anchorBias = PlayerPrefs.GetFloat(p + "ab", anchorBias);
        searchR_St = PlayerPrefs.GetFloat(p + "srS", searchR_St);
        searchR_Mid = PlayerPrefs.GetFloat(p + "srM", searchR_Mid);
        searchR_Def = PlayerPrefs.GetFloat(p + "srD", searchR_Def);

        // move
        baseSpeed = PlayerPrefs.GetFloat(p + "bs", baseSpeed);
        sprintMultiplier = PlayerPrefs.GetFloat(p + "sm", sprintMultiplier);
        turnSpeed = PlayerPrefs.GetFloat(p + "ts", turnSpeed);
        maxSwimSpeed = PlayerPrefs.GetFloat(p + "ms", maxSwimSpeed);
        separationRadius = PlayerPrefs.GetFloat(p + "spr", separationRadius);
        separationWeight = PlayerPrefs.GetFloat(p + "spw", separationWeight);

        // pass/shoot
        distPassMin = PlayerPrefs.GetFloat(p + "dmin", distPassMin);
        distPassMax = PlayerPrefs.GetFloat(p + "dmax", distPassMax);
        closeAutoShootDist = PlayerPrefs.GetFloat(p + "cas", closeAutoShootDist);
        shotMinDist = PlayerPrefs.GetFloat(p + "smin", shotMinDist);
        shotMaxDist = PlayerPrefs.GetFloat(p + "smax", shotMaxDist);
        abilityAccuracy = PlayerPrefs.GetFloat(p + "acc", abilityAccuracy);
        abilityPower = PlayerPrefs.GetFloat(p + "pow", abilityPower);

        // dribble/quick
        dribbleTapPower = PlayerPrefs.GetFloat(p + "dtp", dribbleTapPower);
        dribbleTapMeters = PlayerPrefs.GetFloat(p + "dtm", dribbleTapMeters);
        dribbleTapCooldown = PlayerPrefs.GetFloat(p + "dtc", dribbleTapCooldown);
        quickFirstTouchWindow = PlayerPrefs.GetFloat(p + "qfw", quickFirstTouchWindow);
        quickThreatRadius = PlayerPrefs.GetFloat(p + "qtr", quickThreatRadius);

        // wall/escape
        boundaryProbeRadius = PlayerPrefs.GetFloat(p + "bpr", boundaryProbeRadius);
        boundaryProbeAhead = PlayerPrefs.GetFloat(p + "bpa", boundaryProbeAhead);
        boundaryNearDistance = PlayerPrefs.GetFloat(p + "bnd2", boundaryNearDistance);
        boundaryEscapeMeters = PlayerPrefs.GetFloat(p + "bem", boundaryEscapeMeters);
        boundaryEscapePower = PlayerPrefs.GetFloat(p + "bep", boundaryEscapePower);

        wallDetectRadius = PlayerPrefs.GetFloat(p + "wdr", wallDetectRadius);
        wallNearThreshold = PlayerPrefs.GetFloat(p + "wnt", wallNearThreshold);
        wallKickCooldown = PlayerPrefs.GetFloat(p + "wkc", wallKickCooldown);

        wallZoneWidth = PlayerPrefs.GetFloat(p + "wzw", wallZoneWidth);
        centerKickAhead = PlayerPrefs.GetFloat(p + "cka", centerKickAhead);
        centerKickPower = PlayerPrefs.GetFloat(p + "ckp", centerKickPower);
        centerKickCooldown = PlayerPrefs.GetFloat(p + "ckc", centerKickCooldown);

        wallLookahead = PlayerPrefs.GetFloat(p + "wl", wallLookahead);
        hazardProbeRadius = PlayerPrefs.GetFloat(p + "hpr", hazardProbeRadius);
        wallHazardVeto = PlayerPrefs.GetFloat(p + "whv", wallHazardVeto);
        wallHazardRotate = PlayerPrefs.GetFloat(p + "whr", wallHazardRotate);
        goalToleranceDeg = PlayerPrefs.GetFloat(p + "gtd", goalToleranceDeg);

        // grid
        wForwardAttack = PlayerPrefs.GetFloat(p + "wfa", wForwardAttack);
        wForwardDefend = PlayerPrefs.GetFloat(p + "wfd", wForwardDefend);
        wBallGauss = PlayerPrefs.GetFloat(p + "wbg", wBallGauss);
        wOppClear = PlayerPrefs.GetFloat(p + "woc", wOppClear);
        wMateSpread = PlayerPrefs.GetFloat(p + "wms", wMateSpread);
        wWallSafety = PlayerPrefs.GetFloat(p + "wws", wWallSafety);
        wLoS = PlayerPrefs.GetFloat(p + "wls", wLoS);

        idealDist = PlayerPrefs.GetFloat(p + "idl", idealDist);
        minDistFromOpp = PlayerPrefs.GetFloat(p + "mdo", minDistFromOpp);
        minDistFromMate = PlayerPrefs.GetFloat(p + "mdm", minDistFromMate);
        wallMargin = PlayerPrefs.GetFloat(p + "wm", wallMargin);
        cellSize = PlayerPrefs.GetFloat(p + "cs", cellSize);
    }
}
