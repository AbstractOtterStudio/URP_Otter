using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DribbleZone))]
public class PlayerDribbleAndKick : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode actionKey = KeyCode.Z; // 抢断 / 踢

    [Header("Orientation")]
    [Tooltip("如果玩家模型朝向与世界 forward 相反，勾选此项。")]
    public bool invertForward = true; // 默认反向

    [Header("Quick Kick")]
    public float quickKickPower = 12f;

    [Header("Charge Kick")]
    public float chargeThreshold = 0.18f;
    public float minChargePower = 10f;
    public float maxChargePower = 22f;
    public float maxChargeTime = 1.2f;

    [Header("Aim Line (dashed)")]
    public LineRenderer aimLine;
    [Range(0.2f, 3f)] public float dashLength = 0.6f;
    [Range(0.1f, 2f)] public float gapLength = 0.35f;
    public int maxDashes = 20;

    [Header("Charge Trajectory Preview")]
    public Color chargeTrajectoryColor = new Color(0.65f, 0.35f, 1f, 0.9f);
    [Range(4, 64)] public int trajectorySegments = 20;
    public float trajectoryRangeMultiplier = 1.0f;
    public float trajectoryArcHeight = 1.4f;

    [Header("Refs")]
    public BallPossessionController ballPC;

    public Animator animator;
    private PlayerStateController stateController;
    DribbleZone zone;
    float pressStart = -999f;
    bool charging;
    float currentChargePower;
    int _uiLayer = -1;

    // ==== NEW: 突击 + 抢断窗口参数 ====
    [Header("Steal Dash")]
    public float dashDistance = 3.0f;       // 向前突击位移
    public float dashTime = 0.20f;          // 突击时长
    public float stealRadius = 2.0f;        // 抢断窗口半径（以玩家圈中心为准）
    public bool lockStateDuringDash = true; // 突击时禁用别的输入/状态
    bool isDashing;                         // 运行时标志
    // ================================

    void Awake()
    {
        zone = GetComponent<DribbleZone>();
        if (!ballPC)
        {
            var ball = FindObjectOfType<Ball>();
            if (ball) ballPC = ball.GetComponent<BallPossessionController>();
            if (!ballPC) ballPC = FindObjectOfType<BallPossessionController>();
        }

        _uiLayer = LayerMask.NameToLayer("UI");

        if (!aimLine)
            aimLine = gameObject.AddComponent<LineRenderer>();

        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.material.renderQueue = 5000;
        aimLine.widthMultiplier = 0.06f;
        aimLine.textureMode = LineTextureMode.Stretch;
        aimLine.alignment = LineAlignment.View;
        aimLine.useWorldSpace = true;
        aimLine.positionCount = 0;
        aimLine.enabled = false;
        if (_uiLayer != -1) aimLine.gameObject.layer = _uiLayer;
        try { aimLine.sortingLayerName = "UI"; } catch { }
        aimLine.sortingOrder = 9999;

        animator = GetComponent<Animator>();
        stateController = FindObjectOfType<PlayerStateController>();
        currentChargePower = minChargePower;

        // 兜底把圈注册到PC
        if (ballPC && zone && !ballPC.allZones.Contains(zone))
            ballPC.allZones.Add(zone);
    }

    void Update()
    {
        if (!ballPC) return;

        if (charging && !IsPossessing())
            StopChargingAndHideAim();

        if (Input.GetKeyDown(actionKey))
        {
            pressStart = Time.time;
            currentChargePower = minChargePower;
            charging = false;
            currentChargePower = minChargePower;
        }

        if (Input.GetKey(actionKey))
        {
            float held = Time.time - pressStart;
            float chargeT = Mathf.Clamp01(held / Mathf.Max(0.01f, maxChargeTime));
            if (!charging && held >= chargeThreshold && IsPossessing())
            {
                charging = true;
                aimLine.enabled = true;
            }
            if (charging)
            {
                currentChargePower = Mathf.Lerp(minChargePower, maxChargePower, chargeT);
                UpdateAimLine(currentChargePower);
            }
        }

        if (Input.GetKeyUp(actionKey))
        {
            float held = Time.time - pressStart;

            if (IsPossessing())
            {
                if (held < chargeThreshold) QuickKick();
                else
                {
                    float t = Mathf.Clamp01(held / Mathf.Max(0.01f, maxChargeTime));
                    float power = Mathf.Lerp(minChargePower, maxChargePower, t);
                    ChargedKick(power);
                }
            }
            else if (ballPC)
            {
                // ==== NEW: 进入“突击抢断” ====
                if (!isDashing)
                    StartCoroutine(CoDashSteal());
            }

            charging = false;
            if (aimLine) { aimLine.enabled = false; aimLine.positionCount = 0; }
        }
    }

    // ==== NEW: 突击 & 抢断窗口 ====
    IEnumerator CoDashSteal()
    {
        isDashing = true;

        // 玩家自己的抢断CD：如果在CD里，直接轻反馈并结束
        if (ballPC.StealCooldownRemaining(zone) > 1e-3f)
        {
            SfxBus.Instance?.PlaySteal(false, zone ? (Vector3?)zone.transform.position : null);
            isDashing = false;
            yield break;
        }

        // 锁状态（可选）
        if (lockStateDuringDash && stateController) stateController.StateOnLock();

        // 触发动画
        if (animator) animator.SetTrigger(ValueShortcut.anim_Grab);
        stateController.ChangeAniState(PlayerInteractAniState.Grab);
        // 记录突击起点与目标点
        Vector3 start = transform.position;
        Vector3 dir = KickDir();     // 与踢球同向（可按需要改成输入方向）
        Vector3 target = start + dir * Mathf.Max(0f, dashDistance);
        float t = 0f;

        bool stolen = false;

        // 冲刺过程
        while (t < dashTime)
        {
            float k = (dashTime <= 0f) ? 1f : Mathf.Clamp01(t / dashTime);
            Vector3 p = Vector3.Lerp(start, target, k);
            transform.position = new Vector3(p.x, transform.position.y, p.z);

            // 每帧检查球是否进入抢断半径：无视AI的CD/无敌与队伍
            if (!stolen && ballPC && ballPC.ball)
            {
                Vector3 ballP = ballPC.ball.Pos;
                Vector3 center = zone ? zone.OwnerPosXZ : transform.position;
                if (Vector3.Distance(ballP, center) <= Mathf.Max(0.01f, stealRadius))
                {
                    // 直接强制夺球（无视AI CD与无敌）
                    ballPC.ForceTake(zone, flash: true, addInvuln: true);

                    // 玩家自己上本地CD（仍然有CD）
                    ballPC.StartLocalCooldown(zone);

                    // 成功反馈
                    SfxBus.Instance?.PlaySteal(true, ballP);
                    SfxBus.Instance?.DoSlowMo(0.6f, 0.18f);

                    stolen = true;
                    // 不立刻break：允许冲刺补完（看手感；如果想立即停下可break）
                }
            }

            t += Time.deltaTime;
            yield return null;
        }

        // 冲刺收尾：确保到终点（可按需要取消）
        transform.position = new Vector3(target.x, transform.position.y, target.z);
        stateController.ChangeAniState(PlayerInteractAniState.Idle);
        // 如果没抢到，给轻反馈
        if (!stolen)
            SfxBus.Instance?.PlaySteal(false, zone ? (Vector3?)zone.transform.position : null);

        isDashing = false;
    }
    // ============================

    void StopChargingAndHideAim()
    {
        charging = false;
        if (aimLine)
        {
            aimLine.enabled = false;
            aimLine.positionCount = 0;
        }
    }

    bool IsPossessing()
    {
        return ballPC && ballPC.holderZone == zone;
    }

    Vector3 KickDir()
    {
        Vector3 d = transform.forward;
        if (invertForward) d = -d;
        d.y = 0f;
        if (d.sqrMagnitude < 1e-6f) d = Vector3.forward;
        return d.normalized;
    }

    void QuickKick()
    {
        ballPC.ReleaseAndKickDir(KickDir(), quickKickPower);
        SfxBus.Instance?.PlayKick(false, Mathf.InverseLerp(6f, 22f, quickKickPower), zone ? (Vector3?)zone.transform.position : null);
    }

    void ChargedKick(float power)
    {
        ballPC.ReleaseAndKickDir(KickDir(), power);
        bool isShot = power >= Mathf.Lerp(minChargePower, maxChargePower, 0.65f);
        SfxBus.Instance?.PlayKick(isShot, Mathf.InverseLerp(minChargePower, maxChargePower, power), zone ? (Vector3?)zone.transform.position : null);
    }

    void UpdateAimLine(float power)
    {
        if (!aimLine) return;
        Vector3 start = zone.dribbleAnchor ? zone.dribbleAnchor.position : transform.position;
        Vector3 forward = KickDir();

        int segments = Mathf.Max(4, trajectorySegments);
        float distance = Mathf.Max(0.1f, power * trajectoryRangeMultiplier);
        Vector3 gravity = Physics.gravity;
        float gravityMagnitude = Mathf.Abs(gravity.y);
        float apex = Mathf.Max(0f, trajectoryArcHeight);
        float verticalVelocity = (apex > 0f && gravityMagnitude > 1e-4f)
            ? Mathf.Sqrt(2f * gravityMagnitude * apex)
            : 0f;
        float totalTime = (verticalVelocity > 0f && gravityMagnitude > 1e-4f)
            ? (2f * verticalVelocity / gravityMagnitude)
            : distance / Mathf.Max(power, 0.01f);
        totalTime = Mathf.Max(totalTime, 0.01f);
        Vector3 horizontalVelocity = forward * (distance / totalTime);
        Vector3 initialVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        aimLine.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = totalTime * (i / (float)segments);
            Vector3 point = start + initialVelocity * t + 0.5f * gravity * t * t;
            aimLine.SetPosition(i, point);
        }

        aimLine.startColor = chargeTrajectoryColor;
        aimLine.endColor = chargeTrajectoryColor;
    }

    bool _subscribed = false;
    void OnEnable()
    {
        if (!ballPC)
        {
            var ball = FindObjectOfType<Ball>();
            if (ball) ballPC = ball.GetComponent<BallPossessionController>();
            if (!ballPC) ballPC = FindObjectOfType<BallPossessionController>();
        }
        if (ballPC && !_subscribed)
        {
            ballPC.OnPossessionChanged += HandlePossessionChanged;
            _subscribed = true;
        }

        // 再兜底注册
        if (ballPC && zone && !ballPC.allZones.Contains(zone))
            ballPC.allZones.Add(zone);
    }

    void OnDisable()
    {
        if (ballPC && _subscribed)
        {
            ballPC.OnPossessionChanged -= HandlePossessionChanged;
            _subscribed = false;
        }
    }

    void HandlePossessionChanged(WaterPlayer newOwner, WaterPlayer oldOwner)
    {
        if (charging && !IsPossessing())
            StopChargingAndHideAim();
    }
}
