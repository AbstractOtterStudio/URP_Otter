using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 球是否持续被双方 AI 夹在半径内（即来回踢却脱不开）
/// </summary>
public static class BallStuckDetector
{
    /* ───── 可调参数 ───── */
    public const float Radius    = 2.5f; // 球与各队最近 AI 的距离阈值 (米)
    public const float NeedTime  = 0.5f; // 满足条件累积多少秒判定卡球

    /* ───── 内部状态 ───── */
    private static float timer = 0f;

    /// <summary>本帧是否满足“被双方 AI 同时夹住”条件</summary>
    private static bool ConditionMet(Ball ball, IReadOnlyList<WaterPlayer> players)
    {
        float nearestBlue = float.MaxValue;
        float nearestRed  = float.MaxValue;

        foreach (var p in players)
        {
            float d = Vector3.Distance(p.Pos, ball.Pos);
            if (p.isTeammate)
                nearestBlue = Mathf.Min(nearestBlue, d);
            else
                nearestRed  = Mathf.Min(nearestRed , d);
        }

        if(nearestBlue <= Radius && nearestRed <= Radius)
        {
            Debug.Log(nearestBlue + " , " + nearestRed + ", " + timer);
        }

        return nearestBlue <= Radius && nearestRed <= Radius;
    }

    /// <summary> 每帧调用一次，用于更新计时 </summary>
    public static void UpdateStuckState(Ball ball, IReadOnlyList<WaterPlayer> players)
    {
        if (ConditionMet(ball, players))
            timer += Time.deltaTime;
        else
            timer  = 0f;
    }

    /// <summary> 是否已经卡球 </summary>
    public static bool BallCurrentlyStuck => timer >= NeedTime;
}
