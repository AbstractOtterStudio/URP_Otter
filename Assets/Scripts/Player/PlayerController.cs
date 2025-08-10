using UnityEngine;
using System.Collections.Generic;

public class ItemComparer : IComparer<ItemProperties>
{
    private Transform player;

    public ItemComparer(Transform player)
    {
        this.player = player;
    }

    public int Compare(ItemProperties itemA, ItemProperties itemB)
    {
        float distanceA = Vector3.Distance(itemA.transform.position, player.position);
        float distanceB = Vector3.Distance(itemB.transform.position, player.position);
        return distanceA.CompareTo(distanceB);
    }
}

[RequireComponent(typeof(PlayerStateController))]
[RequireComponent(typeof(PlayerHand))]
[RequireComponent(typeof(PlayerProperty))]
[RequireComponent(typeof(AnimatorManager))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    [Tooltip("按下互动键超过多少秒则判断为投掷")]
    [SerializeField] private float throwHoldThreshold = 0.4f;

    [Tooltip("投掷力量增加速度(每秒)")]
    [SerializeField] private float throwStrengthIncrement = 1.0f;

    [Tooltip("最大投掷力量")]
    [SerializeField] private float maxThrowStrength = 10.0f;

    [Tooltip("这些layer[不]可以作为投掷目的地")]
    [SerializeField] private LayerMask nonThrowableLayers;

    [SerializeField, Tooltip("落点半径检测，用于判定撞到禁投层")]
    private float endPointCheckRadius = 0.5f;

    [SerializeField, Tooltip("从玩家位置向上抬起的投掷起点偏移")]
    private float throwOffset = 1.2f;

    private List<ItemProperties> availableItems = new List<ItemProperties>();
    private List<ItemProperties> knockableItems = new List<ItemProperties>();

    [SerializeField] private Material playerMaterial;
    [SerializeField] private TrajectoryLine trajectoryLine;

    private PlayerStateController stateController;
    private PlayerProperty playerProperty;
    private PlayerHand hand;
    private AnimatorManager animatorManager;
    private PlayerMovement playerMovement;
    private PlayerInputHandler inputHandler;

    private float materialBlendValue;
    private bool isGrowing;

    [Header("Throw Ring UI（玩家身边的可抛圈）")]
    [SerializeField] private float throwAllowRadius = 6f;
    [SerializeField] private int ringSegments = 64;
    [SerializeField] private float ringLineWidth = 0.03f;

    [Header("Aim Ring UI（鼠标落点圈）")]
    [SerializeField, Tooltip("落点提示圈半径")]
    private float aimRingRadius = 0.35f;
    [SerializeField, Tooltip("为了避免与地面ZFight，抬起的高度偏移")]
    private float ringYOffset = 0.02f;
    [SerializeField, Tooltip("落点圈线宽")]
    private float aimRingLineWidth = 0.03f;

    [Header("Ballistic Flight Time")]
    [SerializeField, Tooltip("最短飞行时间（秒）")]
    private float minFlightTime = 0.40f;
    [SerializeField, Tooltip("最长飞行时间（秒）")]
    private float maxFlightTime = 0.90f;
    [SerializeField, Tooltip("若为 true，则无论长按多少都用固定飞行时间 fixedFlightTime")]
    private bool useFixedFlightTime = false;
    [SerializeField, Tooltip("固定飞行时间（秒），当 useFixedFlightTime = true 时生效")]
    private float fixedFlightTime = 0.60f;

    [Header("Throw Collision (No Rigidbody)")]
    [SerializeField, Tooltip("抛掷飞行过程中用于命中的物理层（通常排除 Player、UI 等）")]
    private LayerMask throwHitMask = ~0;
    [SerializeField, Tooltip("抛掷飞行时是否忽略触发器")]
    private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField, Tooltip("每帧最大步进时间（秒），步进越小，穿透越难，但开销越大")]
    private float maxStepDeltaTime = 0.02f;

    [Header("UI Layer")]
    [SerializeField] private int worldUILayer = 5;
    private int cachedWorldUILayer = -2;


    [Header("Ballistic Line")]
    [SerializeField] private int ballisticSegments = 32;
    [SerializeField] private float ballisticLineWidth = 0.03f;

    private LineRenderer throwRing;   // 玩家身边圈（局部空间）
    private LineRenderer aimRing;     // 落点圈（世界空间）
    private LineRenderer ballisticLine; // 实时抛物线

    private ItemComparer itemComparer;

    // 缓存鼠标落点（以玩家当前高度平面）
    private Vector3 currentAimEndPos;

    // 投掷状态变量
    private float throwHoldTimer = 0.0f;
    private bool isThrowing = false;
    private bool isThrowAiming = false;
    private float throwStrength = 0;
    private bool cancelThrowSuggested = false;

    private static readonly Color COLOR_WHITE = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color COLOR_RED = new Color(1f, 0.25f, 0.25f, 0.95f);
    private static readonly Color COLOR_RING = new Color(0.8f, 0.8f, 0.8f, 0.9f);

    private void Start()
    {
        hand = GetComponent<PlayerHand>();
        stateController = GetComponent<PlayerStateController>();
        playerProperty = GetComponent<PlayerProperty>();
        animatorManager = GetComponent<AnimatorManager>();
        playerMovement = GetComponent<PlayerMovement>();
        inputHandler = GetComponent<PlayerInputHandler>();

        itemComparer = new ItemComparer(this.transform);

        CreateThrowRing();
        CreateAimRing();
        CreateBallisticLine();

        if (trajectoryLine != null)
            trajectoryLine.SetColor(Color.white);
    }

    private void Update()
    {
        if (GameManager.Instance.GetGameAction())
        {
            HandleInteractions();
            HandleEatOrKnock();
            UpdatePlayerGrowth();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponent<ItemProperties>();
        if (item != null)
        {
            if (item.CanCatch)
            {
                AddToListIfNotExists(availableItems, item);
                // 抓取键是 C
                EventCenter.Broadcast(GameEvents.ShowButtonHint, ButtonHintType.Button_Z);
            }

            if (item.CanKnock && stateController.PlayerPlaceState == PlayerPlaceState.Float)
            {
                AddToListIfNotExists(knockableItems, item);
                if (hand.grabItemInHand != null && !hand.grabItemInHand.IsBroken &&
                    stateController.PlayerPlaceState == PlayerPlaceState.Float)
                {
                    // 敲打仍使用 X
                    EventCenter.Broadcast(GameEvents.ShowButtonHint, ButtonHintType.Button_X);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var item = other.GetComponent<ItemProperties>();
        if (item != null)
        {
            RemoveFromList(availableItems, item);
            RemoveFromList(knockableItems, item);
        }
    }

    #region 交互处理

    private void HandleInteractions()
    {
        if (hand.grabItemInHand != null)
        {
            HandleItemInHandInteractions();
        }
        else
        {
            HandleNoItemInHandInteractions();
        }
    }

    private void HandleItemInHandInteractions()
    {
        // 瞄准中：累积力度 + 刷新落点、圈/轨迹颜色、抛物线
        if (isThrowAiming)
        {
            AccumulateThrowStrength();
            UpdateTrajectoryAndRings();
        }

        // 按住 C 计时
        if (Input.GetKey(GlobalSetting.InterectKey))
        {
            throwHoldTimer += Time.deltaTime;

            // 进入投掷瞄准
            if (throwHoldTimer > throwHoldThreshold && !isThrowing)
            {
                BeginThrowingItem();
                throwHoldTimer = 0.0f;
            }
        }
        // 松开 C
        else if (Input.GetKeyUp(GlobalSetting.InterectKey))
        {
            if (isThrowing)
            {
                EndThrowingItem();
            }
            else
            {
                // 未在瞄准：单击=放下。若已建议取消，也清除标志
                if (cancelThrowSuggested) cancelThrowSuggested = false;
                ReleaseItem();
            }
        }
    }

    private void HandleNoItemInHandInteractions()
    {
        // 统一使用 C 键松开作为单击事件
        if (Input.GetKeyUp(GlobalSetting.InterectKey))
        {
            if (stateController.CanClean
                && stateController.PlayerPlaceState == PlayerPlaceState.Float
                && availableItems.Count <= 0)
            {
                if (GameManager.Instance.GetDayState() == DayState.Night)
                {
                    Sleep();
                    return;
                }
                Clean();
            }
            else if (!isThrowing)
            {
                GrabItem();
            }
        }
    }

    private void HandleEatOrKnock()
    {
        if (stateController.PlayerAniState == PlayerInteractAniState.Throw)
            return;

        if (inputHandler.IsEatingOrKnocking && hand.grabItemInHand != null)
        {
            if (stateController.PlayerPlaceState == PlayerPlaceState.Float)
            {
                var item = hand.grabItemInHand.GetComponent<ItemProperties>();
                if (item.CanEat && item.IsBroken)
                {
                    PlayEatAnimation();
                }
                else
                {
                    if (knockableItems.Count > 0)
                    {
                        Knock();
                    }
                }
            }
        }
    }

    #endregion

    #region 交互方法（投掷）

    private void AccumulateThrowStrength()
    {
        throwStrength = Mathf.Min(
            throwStrength + throwStrengthIncrement * Time.deltaTime,
            maxThrowStrength
        );
    }

    /// <summary>
    /// 更新鼠标落点、判定可抛与否，并同步三处颜色：
    /// 1) 轨迹线（若有） 2) 玩家身边 ThrowRing 3) 落点 AimRing 4) 实时抛物线
    /// </summary>
    private void UpdateTrajectoryAndRings()
    {
        currentAimEndPos = GetMouseEndPosOnPlayerPlane();

        bool inRing = IsInsideAllowRing(currentAimEndPos);
        bool layerOK = IsEndPosLayerOK(currentAimEndPos);
        bool can = inRing && layerOK;

        cancelThrowSuggested = !can;

        if (trajectoryLine != null)
            trajectoryLine.SetColor(can ? COLOR_WHITE : COLOR_RED);

        UpdateThrowRingColor(can);
        UpdateAimRing(currentAimEndPos, can);

        // —— 画抛物线 —— //
        float T = PickFlightTime();
        Vector3 start = GetThrowStartPosition();
        Vector3 v0 = ComputeBallisticVelocity(start, currentAimEndPos, T);
        DrawBallisticLine(start, v0, T, can);
    }

    private Vector3 GetMouseEndPosOnPlayerPlane()
    {
        Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        Ray ray = inputHandler.cam.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        // 兜底：若未击中，放在玩家前方 2 米
        return transform.position + transform.forward * 2f;
    }

    private bool IsInsideAllowRing(Vector3 endPos)
    {
        Vector3 a = new Vector3(endPos.x, transform.position.y, endPos.z);
        Vector3 b = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        float dist = Vector3.Distance(a, b);
        return dist <= throwAllowRadius;
    }

    private bool IsEndPosLayerOK(Vector3 endPos)
    {
        var colliders = Physics.OverlapSphere(endPos, endPointCheckRadius);
        foreach (var c in colliders)
        {
            if (hand.grabItemInHand != null && ReferenceEquals(c.gameObject, hand.grabItemInHand.gameObject))
                continue;

            // 命中禁投层
            if (((1 << c.gameObject.layer) & nonThrowableLayers.value) != 0)
                return false;
        }
        return true;
    }

    private void BeginThrowingItem()
    {
        // 需要限制只能在水面时投掷：
        if (stateController.PlayerPlaceState != PlayerPlaceState.Float)
            return;

        SetIsThrowing(true);
    }

    private void EndThrowingItem()
    {
        bool can = CanThrow();

        // 退出瞄准/UI
        SetIsThrowing(false);
        if (trajectoryLine != null) trajectoryLine.FuckOff();

        if (!can)
        {
            cancelThrowSuggested = true;
            EventCenter.Broadcast(GameEvents.ShowButtonHint, ButtonHintType.CancelThrowOrDrop);
            return;
        }

        if (hand.grabItemInHand == null) return;

        Vector3 startPos = GetThrowStartPosition();
        Vector3 targetPos = currentAimEndPos;   // 精确命中鼠标落点
        float T = PickFlightTime();             // 固定或由力度映射
        Vector3 v0 = ComputeBallisticVelocity(startPos, targetPos, T);

        ItemProperties item = hand.grabItemInHand;
        Transform itemTf = item.transform;
        Collider itemCol = item.GetComponent<Collider>();

        // 脱手
        itemTf.parent = null;
        item.Release(); // 让物体离开“被抓取”状态

        hand.ReleaseGrabItem();
        cancelThrowSuggested = false;

        // 设置初始位置
        itemTf.position = startPos;

        var catchable = item.GetComponent<Catchable>();
        if (catchable)
        {
            catchable.OnRelease();
            catchable.MarkThrownByPlayer();  // 标记“被玩家投出”
        }

        // 启动位移协程
        StartCoroutine(ThrowItemKinematic(itemTf, itemCol, startPos, v0, T));
    }

    private System.Collections.IEnumerator ThrowItemKinematic(
        Transform obj,
        Collider objCollider,
        Vector3 start,
        Vector3 v0,
        float flightTime)
    {
        float elapsed = 0f;
        Vector3 prev = start;

        // 飞行
        while (elapsed < flightTime && obj != null)
        {
            float step = Mathf.Min(Time.deltaTime, maxStepDeltaTime);
            float nextTime = Mathf.Min(elapsed + step, flightTime);

            Vector3 nextPos = start
                            + v0 * nextTime
                            + 0.5f * Physics.gravity * nextTime * nextTime;

            // 射线检测 prev -> nextPos，找最近命中
            Vector3 dir = nextPos - prev;
            float dist = dir.magnitude;
            if (dist > 1e-5f)
            {
                var hits = Physics.RaycastAll(prev, dir.normalized, dist, throwHitMask, triggerInteraction);
                float minDist = float.MaxValue;
                bool hasHit = false;
                RaycastHit bestHit = default;

                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];

                    // 忽略自身
                    if (objCollider != null && h.collider == objCollider)
                        continue;
                    // 忽略玩家
                    if (h.collider.transform.IsChildOf(this.transform))
                        continue;

                    if (h.distance < minDist)
                    {
                        minDist = h.distance;
                        bestHit = h;
                        hasHit = true;
                    }
                }

                if (hasHit)
                {
                    obj.position = bestHit.point;
                    yield break; // 命中即结束
                }
            }

            obj.position = nextPos;
            prev = nextPos;
            elapsed = nextTime;

            yield return null;
        }

        // 正常结束，落到目标点
        if (obj != null)
        {
            obj.position = start
                         + v0 * flightTime
                         + 0.5f * Physics.gravity * flightTime * flightTime;
        }
    }

    private float PickFlightTime()
    {
        if (useFixedFlightTime) return Mathf.Max(0.05f, fixedFlightTime);

        float t = Mathf.Lerp(minFlightTime, maxFlightTime,
                             Mathf.Clamp01(throwStrength / maxThrowStrength));
        return Mathf.Max(0.05f, t);
    }

    private Vector3 ComputeBallisticVelocity(Vector3 start, Vector3 target, float flightTime)
    {
        Vector3 g = Physics.gravity;
        Vector3 to = target - start;
        // v0 = (Δp - 0.5*g*T^2) / T
        return (to - 0.5f * g * flightTime * flightTime) / flightTime;
    }

    private bool CanThrow()
    {
        return IsInsideAllowRing(currentAimEndPos) && IsEndPosLayerOK(currentAimEndPos);
    }

    private void SetIsThrowing(bool throwing)
    {
        if (!isThrowing && throwing)
        {
            animatorManager.OffLockState();
            animatorManager.playerAnimator.SetTrigger(ValueShortcut.anim_ThrowAim);
            playerMovement.OnPlayerSpeedChange?.Invoke(PlayerSpeedState.Slow);

            inputHandler.ExternalBlockMovement = true;   // 需要在 PlayerInputHandler 中实现
            ShowThrowRing(true);
            ShowAimRing(true);
            ShowBallisticLine(true);
            SetIsThrowAiming(true);
        }
        else if (isThrowing && !throwing)
        {
            animatorManager.OffLockState();
            animatorManager.playerAnimator.SetTrigger(ValueShortcut.anim_Throw);
            playerMovement.OnPlayerSpeedChange?.Invoke(PlayerSpeedState.Normal);

            inputHandler.ExternalBlockMovement = false;
            ShowThrowRing(false);
            ShowAimRing(false);
            ShowBallisticLine(false);
            SetIsThrowAiming(false);
        }

        isThrowing = throwing;
    }

    private void SetIsThrowAiming(bool throwAiming)
    {
        isThrowAiming = throwAiming;
        throwStrength = 0.0f;
    }

    private Vector3 GetThrowStartPosition() => transform.position + transform.up * throwOffset;

    #endregion

    #region 抓取/放下/敲打/进食

    private void ReleaseItem()
    {
        stateController.ChangeAniState(PlayerInteractAniState.Release);
        if (hand.grabItemInHand == null) return;
        availableItems.Add(hand.grabItemInHand);
        hand.grabItemInHand.Release();
        hand.ReleaseGrabItem();
    }

    private void GrabItem()
    {
        if (stateController.IsStateLocked) return;
        stateController.ChangeAniState(PlayerInteractAniState.Grab);
    }

    // 由动画事件调用
    private void GrabItemLogic()
    {
        if (availableItems.Count <= 0) return;
        ItemProperties item = availableItems[0];
        hand.GrabItem(item);
        availableItems.Remove(item);
        if (knockableItems.Contains(item))
        {
            knockableItems.Remove(item);
        }
        item.Catch(hand.playerHandModel);

        var catchable = item.GetComponent<Catchable>();
        if (catchable) catchable.OnCatch(hand.playerHandModel);
    }

    private void Knock()
    {
        if (stateController.IsStateLocked) return;
        stateController.ChangeAniState(PlayerInteractAniState.Knock);
        Vector3 direction = -(knockableItems[0].transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);

        hand.grabItemInHand.KnockWith(knockableItems[0]);
        if (knockableItems[0].IsBroken)
        {
            knockableItems.RemoveAt(0);
        }
    }

    private void PlayEatAnimation()
    {
        if (stateController.IsStateLocked) return;
        stateController.ChangeAniState(PlayerInteractAniState.Eat);
    }

    // 由动画事件调用
    private void EatFood()
    {
        (float oxygen, float health) foodAdd = hand.grabItemInHand.Eat();

        if (hand.grabItemInHand.GetComponent<Item_Urchin>())
        {
            AnimatorManager.Instance.PlayerCelebrate();
        }
        hand.grabItemInHand.transform.parent = null;
        hand.grabItemInHand = null;
        playerProperty.ModifyHealth(foodAdd.health);
        playerProperty.ModifyMaxOxygen(foodAdd.oxygen);
        EventCenter.Broadcast(GameEvents.BecomeGrowth);
    }

    #endregion

    #region UI 圈与通用辅助

    private int GetWorldUILayer()
    {
        if (cachedWorldUILayer != -2) return cachedWorldUILayer;

        int id = worldUILayer;
        if (id < 0 || id > 31)
        {
            Debug.LogWarning(
                $"[PlayerController] Layer '{worldUILayer}' 不存在或非法。请到 Project Settings > Tags and Layers 新建该层。" +
                "已回退到 Default(0)。");
            id = 0; // 回退到 Default
        }
        cachedWorldUILayer = id;
        return id;
    }


    private void CreateThrowRing()
    {
        GameObject go = new GameObject("ThrowRing");
        go.layer = GetWorldUILayer();                 // 放到 WorldUI 层
        go.transform.SetParent(transform, false);     // 跟随玩家
        go.transform.localPosition = Vector3.zero;

        throwRing = go.AddComponent<LineRenderer>();
        throwRing.loop = true;
        throwRing.useWorldSpace = false;              // 围绕玩家
        throwRing.widthMultiplier = ringLineWidth;
        throwRing.positionCount = ringSegments;
        throwRing.numCornerVertices = 4;
        throwRing.numCapVertices = 2;

        throwRing.material = new Material(Shader.Find("Sprites/Default"));
        throwRing.sortingOrder = 32767;

        Vector3[] pts = new Vector3[ringSegments];
        for (int i = 0; i < ringSegments; i++)
        {
            float a = (float)i / ringSegments * Mathf.PI * 2f;
            pts[i] = new Vector3(Mathf.Cos(a) * throwAllowRadius, 0f, Mathf.Sin(a) * throwAllowRadius);
        }
        throwRing.SetPositions(pts);
        throwRing.gameObject.SetActive(false);
    }

    private void DrawBallisticLine(Vector3 start, Vector3 v0, float T, bool canThrow)
    {
        if (ballisticLine == null || !ballisticLine.gameObject.activeSelf) return;

        Color c = canThrow ? COLOR_WHITE : COLOR_RED;
        ballisticLine.startColor = c;
        ballisticLine.endColor = c;

        int segs = Mathf.Max(2, ballisticSegments);
        ballisticLine.positionCount = segs;

        float dt = T / (segs - 1);
        Vector3[] pts = new Vector3[segs];
        for (int i = 0; i < segs; i++)
        {
            float t = dt * i;
            pts[i] = start + v0 * t + 0.5f * Physics.gravity * t * t;
            pts[i].y += ringYOffset; // 抬一点避免与地面ZFight
        }
        ballisticLine.SetPositions(pts);
    }


    private void CreateAimRing()
    {
        GameObject go = new GameObject("AimRing");
        go.layer = GetWorldUILayer();                 // 放到 WorldUI 层

        aimRing = go.AddComponent<LineRenderer>();
        aimRing.loop = true;
        aimRing.useWorldSpace = true;                 // 世界空间，直接放落点
        aimRing.widthMultiplier = aimRingLineWidth;
        aimRing.positionCount = ringSegments;
        aimRing.numCornerVertices = 4;
        aimRing.numCapVertices = 2;

        aimRing.material = new Material(Shader.Find("Sprites/Default"));
        aimRing.sortingOrder = 32767;

        Vector3[] pts = new Vector3[ringSegments];
        for (int i = 0; i < ringSegments; i++)
        {
            float a = (float)i / ringSegments * Mathf.PI * 2f;
            pts[i] = new Vector3(Mathf.Cos(a) * aimRingRadius, 0f, Mathf.Sin(a) * aimRingRadius);
        }
        aimRing.SetPositions(pts);
        aimRing.gameObject.SetActive(false);
    }

    private void CreateBallisticLine()
    {
        GameObject go = new GameObject("BallisticLine");
        go.layer = GetWorldUILayer();                 // 放到 WorldUI 层

        ballisticLine = go.AddComponent<LineRenderer>();
        ballisticLine.loop = false;
        ballisticLine.useWorldSpace = true;
        ballisticLine.widthMultiplier = ballisticLineWidth;
        ballisticLine.positionCount = Mathf.Max(2, ballisticSegments);
        ballisticLine.numCornerVertices = 2;
        ballisticLine.numCapVertices = 0;

        ballisticLine.material = new Material(Shader.Find("Sprites/Default"));
        ballisticLine.sortingOrder = 32767;

        ballisticLine.gameObject.SetActive(false);
    }

    private void UpdateThrowRingColor(bool canThrow)
    {
        if (throwRing == null) return;
        Color c = canThrow ? COLOR_WHITE : COLOR_RED;
        throwRing.startColor = c;
        throwRing.endColor = c;
    }

    private void UpdateAimRing(Vector3 worldPos, bool canThrow)
    {
        if (aimRing == null) return;

        Color c = canThrow ? COLOR_WHITE : COLOR_RED;
        aimRing.startColor = c;
        aimRing.endColor = c;

        Vector3[] pts = new Vector3[ringSegments];
        float y = worldPos.y + ringYOffset;
        for (int i = 0; i < ringSegments; i++)
        {
            float a = (float)i / ringSegments * Mathf.PI * 2f;
            pts[i] = new Vector3(
                worldPos.x + Mathf.Cos(a) * aimRingRadius,
                y,
                worldPos.z + Mathf.Sin(a) * aimRingRadius
            );
        }
        aimRing.SetPositions(pts);
    }

    private void ShowThrowRing(bool show)
    {
        if (throwRing != null) throwRing.gameObject.SetActive(show);
    }

    private void ShowAimRing(bool show)
    {
        if (aimRing != null) aimRing.gameObject.SetActive(show);
    }

    private void ShowBallisticLine(bool show)
    {
        if (ballisticLine != null) ballisticLine.gameObject.SetActive(show);
    }

    private void AddToListIfNotExists(List<ItemProperties> list, ItemProperties item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);
            list.Sort(itemComparer);
        }
    }

    private void RemoveFromList<T>(List<T> list, T item)
    {
        if (list.Contains(item))
        {
            list.Remove(item);
        }
    }

    #endregion

    #region 成长/清洁

    private void UpdatePlayerGrowth()
    {
        if (isGrowing)
        {
            materialBlendValue += Time.deltaTime / 5;
            if (playerProperty.Status.Level == 2 && playerMaterial.GetFloat("Step1To2") < 0.99f)
            {
                playerMaterial.SetFloat("Step1To2", materialBlendValue);
            }
            else if (playerProperty.Status.Level == 3 && playerMaterial.GetFloat("Step2To3") < 0.99f)
            {
                playerMaterial.SetFloat("Step2To3", materialBlendValue);
            }
            else
            {
                isGrowing = false;
            }
        }
    }

    private void Clean()
    {
        if (stateController.IsStateLocked) return;
        playerProperty.ModifyCleanliness(playerProperty.Status.Cleanliness);
        stateController.ChangeAniState(PlayerInteractAniState.Clean);
        EventCenter.Broadcast(GameEvents.BecomeGrowth);
    }

    private void Sleep()
    {
        if (stateController.IsStateLocked) return;
        stateController.ChangeAniState(PlayerInteractAniState.Sleep);
        int previousLevel = playerProperty.Status.Level;

        if (playerProperty.Status.Level > previousLevel)
        {
            isGrowing = true;
            materialBlendValue = 0;
        }
    }

    #endregion
}
