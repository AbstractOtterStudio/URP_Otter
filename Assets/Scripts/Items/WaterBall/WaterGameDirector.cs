using UnityEngine;

public class WaterGameDirector : MonoBehaviour
{
    public Ball ball;
    public float commandCooldown = 1.2f;

    [Header("Unjam Guard")]
    [Tooltip("AI 必须在球多少米内才允许触发解卡踢球")]
    public float assistKickMaxRange = 2.0f;

    private float nextCommandAt = 0f;
    public static bool IsGloballyStuck { get; private set; }

    private void Reset()
    {
        if (!ball) ball = FindObjectOfType<Ball>();
    }

    private void Update()
    {
        if (!ball) return;

        BallStuckDetector.UpdateStuckState(ball, WaterPlayerManager.All);
        IsGloballyStuck = BallStuckDetector.IsBallStuck;

        if (!IsGloballyStuck || Time.time < nextCommandAt) return;

        // 选最近的 AI，但必须真的在球边
        WaterPlayer best = null;
        float bestD = float.MaxValue;

        foreach (var p in WaterPlayerManager.All)
        {
            if (!p) continue;
            float d = Vector3.Distance(p.Pos, ball.Pos);
            if (d < bestD) { bestD = d; best = p; }
        }

        if (!best) return;

        // 球已被某人持有 → 不下发解卡
        if (ball.Owner != null) return;

        // 距离约束：不允许“隔空踢球”
        if (bestD > assistKickMaxRange) return;

        best.ForceUnjamKick();
        nextCommandAt = Time.time + commandCooldown;
    }
}
