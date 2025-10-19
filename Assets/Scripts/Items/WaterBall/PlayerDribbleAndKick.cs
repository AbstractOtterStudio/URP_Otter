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
    public float chargeThreshold = 0.18f; // 达到此按压时间后进入蓄力
    public float minChargePower = 10f;
    public float maxChargePower = 22f;
    public float maxChargeTime = 1.2f;    // 到达最大力度需要的时长

    [Header("Aim Line (dashed)")]
    public LineRenderer aimLine;
    [Range(0.2f, 3f)] public float dashLength = 0.6f;
    [Range(0.1f, 2f)] public float gapLength = 0.35f;
    public int maxDashes = 20;

    [Header("Refs")]
    public BallPossessionController ballPC; // 从场上 Ball 自动查找

    DribbleZone zone;
    float pressStart = -999f;
    bool charging;

    void Awake()
    {
        zone = GetComponent<DribbleZone>();
        if (!ballPC)
        {
            var ball = FindObjectOfType<Ball>();
            if (ball) ballPC = ball.GetComponent<BallPossessionController>();
            if (!ballPC) ballPC = FindObjectOfType<BallPossessionController>();
        }

        if (!aimLine)
        {
            aimLine = gameObject.AddComponent<LineRenderer>();
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
            aimLine.widthMultiplier = 0.06f;
            aimLine.textureMode = LineTextureMode.Stretch;
            aimLine.alignment = LineAlignment.View;
            aimLine.useWorldSpace = true;
            aimLine.positionCount = 0;
            aimLine.enabled = false;
        }
    }

    void Update()
    {
        if (!ballPC) return;

        if (Input.GetKeyDown(actionKey))
        {
            pressStart = Time.time;
            charging = false;
        }

        if (Input.GetKey(actionKey))
        {
            float held = Time.time - pressStart;
            if (!charging && held >= chargeThreshold && IsPossessing())
            {
                charging = true;
                aimLine.enabled = true;
            }
            if (charging) UpdateAimLine();
        }

        if (Input.GetKeyUp(actionKey))
        {
            float held = Time.time - pressStart;

            if (IsPossessing())
            {
                if (held < chargeThreshold)
                {
                    QuickKick();
                }
                else
                {
                    float t = Mathf.Clamp01(held / Mathf.Max(0.01f, maxChargeTime));
                    float power = Mathf.Lerp(minChargePower, maxChargePower, t);
                    ChargedKick(power);
                }
            }
            else
            {
                // 主动抢断（仅自己尝试）
                ballPC.TrySteal(zone, out _);
            }

            charging = false;
            aimLine.enabled = false;
            aimLine.positionCount = 0;
        }
    }

    bool IsPossessing()
    {
        return ballPC && ballPC.holderZone == zone; // 用持球圈来判定
    }

    Vector3 KickDir()
    {
        Vector3 d = transform.forward;
        if (invertForward) d = -d; // 反向
        d.y = 0f;
        if (d.sqrMagnitude < 1e-6f) d = Vector3.forward;
        return d.normalized;
    }

    void QuickKick()
    {
        ballPC.ReleaseAndKickDir(KickDir(), quickKickPower);
    }

    void ChargedKick(float power)
    {
        ballPC.ReleaseAndKickDir(KickDir(), power);
    }

    void UpdateAimLine()
    {
        if (!aimLine) return;
        Vector3 start = zone.dribbleAnchor ? zone.dribbleAnchor.position : transform.position + KickDir() * 0.6f;
        Vector3 dir = KickDir();

        var pts = new List<Vector3>(maxDashes * 2);
        float total = 0f;
        float maxLen = (maxChargePower + quickKickPower) * 1.0f;
        while (pts.Count / 2 < maxDashes && total < maxLen)
        {
            Vector3 a = start + dir * total;
            total += dashLength;
            Vector3 b = start + dir * total;
            pts.Add(a); pts.Add(b);
            total += gapLength;
        }
        aimLine.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++) aimLine.SetPosition(i, new Vector3(pts[i].x, start.y + 0.05f, pts[i].z));
        aimLine.startColor = aimLine.endColor = Color.white;
    }
}
