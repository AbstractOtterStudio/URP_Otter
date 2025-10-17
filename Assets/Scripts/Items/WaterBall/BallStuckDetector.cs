using System.Collections.Generic;
using UnityEngine;

public static class BallStuckDetector
{
    const float pairMaxSep = 3.5f;
    const float bothCloseToBall = 1.6f;
    const float ballNearMidRadius = 1.2f;
    const float minCheckTime = 0.80f;
    const float ballNetAdvanceMin = 1.0f;
    const float speedUpperLimit = 10f;

    static Vector3 accumStartPos;
    static float satisfiedTimer;
    static bool wasInitialized;

    public static bool IsBallStuck => BallCurrentlyStuck;
    public static bool BallCurrentlyStuck { get; private set; }

    public static void UpdateStuckState(Ball ball, IReadOnlyList<WaterPlayer> players)
    {
        if (ball == null || players == null || players.Count < 2)
        {
            BallCurrentlyStuck = false;
            wasInitialized = false;
            satisfiedTimer = 0f;
            return;
        }

        if (!wasInitialized)
        {
            accumStartPos = ball.Pos;
            satisfiedTimer = 0f;
            BallCurrentlyStuck = false;
            wasInitialized = true;
        }

        WaterPlayer a = null, b = null;
        float da = float.MaxValue, db = float.MaxValue;

        foreach (var p in players)
        {
            if (p == null) continue;
            float d = Vector3.Distance(p.Pos, ball.Pos);

            if (a == null || d < da) { b = a; db = da; a = p; da = d; }
            else if (b == null || d < db) { b = p; db = d; }
        }

        bool validPair = a != null && b != null && (a.isTeammate != b.isTeammate);
        bool cond = false;

        if (validPair)
        {
            float pairDist = Vector3.Distance(a.Pos, b.Pos);
            Vector3 mid = (a.Pos + b.Pos) * 0.5f;
            float ball2Mid = Vector3.Distance(ball.Pos, mid);

            cond =
                pairDist <= pairMaxSep &&
                da <= bothCloseToBall &&
                db <= bothCloseToBall &&
                ball2Mid <= ballNearMidRadius &&
                ball.Rb.velocity.magnitude <= speedUpperLimit;
        }

        if (cond) satisfiedTimer += Time.deltaTime;
        else { satisfiedTimer = 0f; accumStartPos = ball.Pos; }

        float netAdvance = Vector3.Distance(accumStartPos, ball.Pos);
        BallCurrentlyStuck = satisfiedTimer >= minCheckTime && netAdvance <= ballNetAdvanceMin;

        if (!BallCurrentlyStuck && satisfiedTimer == 0f)
            accumStartPos = ball.Pos;
    }
}
