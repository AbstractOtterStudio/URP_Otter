using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class DribbleZone : MonoBehaviour
{
    [Header("Owner (auto or manual)")]
    [Tooltip("可为空。若存在则直接使用 WaterPlayer 的队伍/位置。")]
    public WaterPlayer ownerWP;                       // 可为空（人类玩家通常没有）
    [Tooltip("当没有 WaterPlayer 时，用这个 Transform 当作“拥有者根对象”（用于位置/朝向）。")]
    public Transform manualOwnerRoot;
    [Tooltip("当没有 WaterPlayer 时使用此队伍标记：true=我方，false=对方。")]
    public bool manualIsTeammate = true;

    [Header("Anchor / Radius")]
    public Transform dribbleAnchor;                   // 球吸附点（放在角色前方）
    [Min(0.2f)] public float radius = 1.8f;          // 圈半径（抢断/吸附几何判断用）

    [Header("Ring UI")]
    public LineRenderer ring;                         // 可留空，运行时自动创建
    [Range(12, 128)] public int ringSegments = 64;
    public float ringWidth = 0.06f;
    [Tooltip("将 LineRenderer 放到这个 Sorting Layer（例如 UI）")]
    public string ringSortingLayer = "UI";
    [Tooltip("Sorting Order 越大越靠前")]
    public int ringSortingOrder = 5000;

    [Header("Colors")]
    public Color colorIdle       = new Color(1f, 1f, 1f, 0.85f);       // 普通
    public Color colorCarrier    = new Color(0.2f, 1f, 0.2f, 0.98f);   // 持球（绿）
    public Color colorStealFlash = new Color(0.2f, 1f, 0.2f, 1f);      // 抢断瞬时闪光
    public Color colorCooldown   = new Color(1f, 0.25f, 0.25f, 0.98f); // 红：CD/无敌
    public Color colorCanSteal   = new Color(0.75f, 0.5f, 1f, 0.98f);  // 紫：此时可抢

    public float stealFlashDuration = 0.18f;

    [Header("Behavior")]
    [Tooltip("把圈节点强制贴在“拥有者中心”（ownerWP 或 manualOwnerRoot）")]
    public bool followOwnerCenter = true;
    [Tooltip("圈的Y高度（略抬一点避免穿插水面）")]
    public float ringHeight = 0.05f;

    [Header("Debug")]
    public bool drawGizmo = false;

    // —— Back-compat：老代码会读这个 —— //
    public bool isTeammate => ownerWP ? ownerWP.isTeammate : manualIsTeammate;

    // 引用
    BallPossessionController possession;
    float _nextResolveAt;
    bool _subscribed;

    // 运行时
    Material _matRuntime;
    Coroutine _flashCR;
    bool _isCarrier;

    void Awake()
    {
        if (!ownerWP) ownerWP = GetComponentInParent<WaterPlayer>();
        if (!possession)
        {
            var ball = FindObjectOfType<Ball>();
            if (ball) possession = ball.GetComponent<BallPossessionController>();
        }

        EnsureRing();
        RedrawRing();
        SetIdleColor();
        TryResolvePossession(force: true);
    }
    
    void OnEnable()
    {
        TryResolvePossession();
        if (possession && !_subscribed)
        {
            possession.OnPossessionChanged += OnPossessionChanged;
            _subscribed = true;
        }
        RegisterSelfWithPC();
    }

    void OnDisable()
    {
        if (possession && _subscribed)
        {
            possession.OnPossessionChanged -= OnPossessionChanged;
            _subscribed = false;
        }
    }

    void RegisterSelfWithPC()
    {
        if (possession && !possession.allZones.Contains(this))
            possession.allZones.Add(this);
    }


    void OnValidate()
    {
        if (!ownerWP) ownerWP = GetComponentInParent<WaterPlayer>();
        if (ringSegments < 12) ringSegments = 12;
        if (Application.isPlaying) { RedrawRing(); ApplyColorImmediate(); }
    }

    [Header("Local Steal Cooldown (visual only)")]
    public float localStealCooldown = 1.0f;
    float _localCdUntil;
    public float LocalCooldownRemaining() => Mathf.Max(0f, _localCdUntil - Time.time);
    public void StartLocalCooldown(float seconds = -1f)
    {
        float dur = (seconds > 0f) ? seconds : localStealCooldown;
        _localCdUntil = Time.time + Mathf.Max(0.01f, dur);
    }

    void OnPossessionChanged(WaterPlayer newOwner, WaterPlayer oldOwner)
    {
        // 如果我就是新的持球圈 → 立刻绿；否则不要保持绿
        var holder = possession ? possession.holderZone : null;
        bool iAmHolder = (holder == this);
        SetCarrier(iAmHolder);  // true=绿，false=回到 idle 颜色，后续 Update 还会根据条件改成紫/红
    }


    void Update()
    {
        if (!possession && Time.time >= _nextResolveAt)
        {
            _nextResolveAt = Time.time + 0.5f;
            TryResolvePossession();
        }
        // 让圈始终以“角色中心”为原点（不再偏移）
        if (followOwnerCenter)
        {
            Transform root = ownerWP ? ownerWP.transform : (manualOwnerRoot ? manualOwnerRoot : transform);
            if (root)
            {
                Vector3 p = root.position;
                transform.position = new Vector3(p.x, 0f, p.z); // 保持在地平面/水面高度（y=0），LineRenderer自身会抬一点
            }
        }

        // —— 颜色优先级 —— //
        var holder = possession ? possession.holderZone : null;
        bool weAreHolder = (holder == this);

        bool overlapHolder = false;
        bool enemyToHolder = false;

        if (possession && holder && holder != this)
        {
            enemyToHolder = (holder.isTeammate != this.isTeammate);
            Vector2 pa = new Vector2(transform.position.x, transform.position.z);
            Vector2 pb = new Vector2(holder.transform.position.x, holder.transform.position.z);
            float d = Vector2.Distance(pa, pb);
            overlapHolder = d < (radius + holder.radius);
        }

        bool invuln = possession && possession.IsInvulnerable;
        bool canStealLocal = possession && enemyToHolder && overlapHolder && (LocalCooldownRemaining() <= 1e-3f) && !invuln;

        if (weAreHolder)
        {
            SetCarrier(true);                         // 绿
        }
        else if (canStealLocal)
        {
            SetColor(colorCanSteal);                  // 紫
        }
        else if (possession && overlapHolder && (LocalCooldownRemaining() > 1e-3f || invuln))
        {
            SetColor(colorCooldown);                  // 红
        }
        else
        {
            SetIdleColor();                           // 白
        }


        //（可选保险：防止别处改了 LineRenderer 的色）
        if (ring && _matRuntime != null)
        {
            ring.startColor = _matRuntime.color;
            ring.endColor = _matRuntime.color;
        }

    }
    
    void TryResolvePossession(bool force = false)
    {
        if (!force && possession && possession.gameObject.activeInHierarchy) return;

        // 0. 先找场上的 Ball
        var ball = FindObjectOfType<Ball>();
        if (ball)
        {
            var pc = ball.GetComponent<BallPossessionController>();
            if (!pc) pc = FindObjectOfType<BallPossessionController>();
            possession = pc;
        }
        else
        {
            possession = FindObjectOfType<BallPossessionController>();
        }

        // 1. 订阅持球权变更（只订阅一次）
        if (possession && !_subscribed)
        {
            possession.OnPossessionChanged += OnPossessionChanged;
            _subscribed = true;
        }
        RegisterSelfWithPC();
    }


    // —— 位置工具 —— //
    public Vector3 AnchorXZ => dribbleAnchor ? dribbleAnchor.position : transform.position;

    public Vector3 OwnerPosXZ
    {
        get
        {
            if (ownerWP) return ownerWP.Pos;
            Transform t = manualOwnerRoot ? manualOwnerRoot : transform;
            return new Vector3(t.position.x, 0, t.position.z);
        }
    }

    // 本地CD是否“应显示为红色”：要求贴身重叠且敌对，否则不红
    bool HasLocalCooldownForUI()
    {
        if (!possession) return false;
        if (possession.StealCooldownRemaining(this) <= 1e-3f) return false;
        return OverlapsHolderIgnoringInvuln();
    }

    // 持球者处于无敌，但“我不是持球者本人”
    bool HolderIsInvulnButNotMe()
    {
        if (!possession) return false;
        if (!possession.IsInvulnerable) return false;
        return possession.holderZone != this;
    }

    // 仅判几何重叠（忽略无敌），用于“贴身”判断
    bool OverlapsHolderIgnoringInvuln()
    {
        if (!possession || !possession.holderZone) return false;

        var a = this;
        var b = possession.holderZone;
        if (a == b) return false;
        if (a.isTeammate == b.isTeammate) return false;

        Vector2 pa = new Vector2(a.transform.position.x, a.transform.position.z);
        Vector2 pb = new Vector2(b.transform.position.x, b.transform.position.z);
        float d   = Vector2.Distance(pa, pb);
        float sum = a.radius + b.radius;

        // 想再“严一点”就乘个系数，比如 0.9：只有更贴身才算红
        return d < sum; // 或者 return d < sum * 0.9f;
    }

    public void EnsureRing()
    {
        if (!ring)
        {
            var go = new GameObject("DribbleRing");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
        }

        ring.alignment = LineAlignment.View;
        ring.textureMode = LineTextureMode.Stretch;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        ring.loop = true;
        ring.useWorldSpace = false;                 // 以本物体为原点
        ring.widthMultiplier = Mathf.Max(0.001f, ringWidth);
        ring.numCornerVertices = 0;
        ring.numCapVertices = 0;
#if UNITY_2021_2_OR_NEWER
        ring.generateLightingData = false;
#endif

        // ✅ 使用 LineRenderer 最稳的 shader：Sprites/Default
        // （URP 下也可用；避免 URP/Unlit 在 LR 上不绘制的问题）
        if (_matRuntime == null)
        {
            var sh = Shader.Find("Sprites/Default");
            if (!sh)
            {
                Debug.LogWarning("[DribbleZone] Shader 'Sprites/Default' not found. Falling back to legacy Unlit/Color.");
                sh = Shader.Find("Unlit/Color");
            }
            _matRuntime = new Material(sh);
        }

        // 强制置顶渲染（避免被水深度盖住）
        // 5000 已经很靠前，若还有问题可以调到 5500
        _matRuntime.renderQueue = 5000;

        // 颜色：务必非全透明（有些材质默认 A=0 导致“不可见”）
        _matRuntime.color = (colorIdle.a < 0.01f)
            ? new Color(colorIdle.r, colorIdle.g, colorIdle.b, 1f)
            : colorIdle;

        // Sprites/Default 某些变体不暴露 _ZTest/_ZWrite；有就设，没有就忽略
        if (_matRuntime.HasProperty("_ZWrite")) _matRuntime.SetInt("_ZWrite", 0);
        if (_matRuntime.HasProperty("_ZTest")) _matRuntime.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

        ring.material = _matRuntime;

        // Sorting Layer/Order（renderQueue 已兜底）
        try { ring.sortingLayerName = ringSortingLayer; } catch { }
        ring.sortingOrder = ringSortingOrder;

        // 同步 LineRenderer 自身的颜色，避免与材质相乘导致过透明
        var c = _matRuntime.color;
        if (c.a < 0.02f) c.a = 1f; // 兜底
        ring.startColor = c;
        ring.endColor = c;
    }


    public void RedrawRing()
    {
        if (!ring) return;
        ring.widthMultiplier = ringWidth;
        ring.positionCount = ringSegments;

        float step = Mathf.PI * 2f / ringSegments;
        for (int i = 0; i < ringSegments; i++)
        {
            float a = step * i;
            float x = Mathf.Cos(a) * radius;
            float z = Mathf.Sin(a) * radius;
            ring.SetPosition(i, new Vector3(x, ringHeight, z)); // 抬离地面一点
        }
    }

    void ApplyColorImmediate()
    {
        if (_matRuntime != null)
            _matRuntime.color = _isCarrier ? colorCarrier : colorIdle;

        // 同步到 LR 自身颜色，防止乘透明
        if (ring)
        {
            var c = _isCarrier ? colorCarrier : colorIdle;
            ring.startColor = c;
            ring.endColor   = c;
        }
    }

    void SetColor(Color c)
    {
        if (_matRuntime != null) _matRuntime.color = c;
        if (ring) { ring.startColor = c; ring.endColor = c; }
    }

    public void SetCarrier(bool v)
    {
        _isCarrier = v;
        SetColor(_isCarrier ? colorCarrier : colorIdle);
    }

    public void SetIdleColor()
    {
        if (_isCarrier) return; // 保持持球绿色
        SetColor(colorIdle);
    }

    public void FlashSteal()
    {
        if (_flashCR != null) StopCoroutine(_flashCR);
        _flashCR = StartCoroutine(CoFlash());
    }

    IEnumerator CoFlash()
    {
        SetColor(colorStealFlash);
        yield return new WaitForSeconds(stealFlashDuration);
        _flashCR = null;
    }

    // —— 状态查询（供变色逻辑） —— //
    bool IsOnLocalCooldownOrInvuln()
    {
        if (!possession) return false;
        if (possession.IsInvulnerable) // 当前持球者处于无敌，所有攻击者都是“红”
        {
            // 若我就是持球者，则仍为绿色；否则显示红
            if (possession.holderZone == this) return false;
            return true;
        }
        // 本圈是否有本地CD
        return possession.StealCooldownRemaining(this) > 1e-3f;
    }

    bool CanStealNow()
    {
        if (!possession) return false;
        // 玩家更关心“能不能抢当前持球者”
        return possession.CanStealNow(this);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;
        Gizmos.color = Color.cyan;
        Vector3 c = transform.position + Vector3.up * ringHeight;
        const int N = 48; float step = Mathf.PI * 2f / N;
        Vector3 prev = c + new Vector3(radius, 0, 0);
        for (int i = 1; i <= N; i++)
        {
            float a = i * step;
            Vector3 p = c + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
