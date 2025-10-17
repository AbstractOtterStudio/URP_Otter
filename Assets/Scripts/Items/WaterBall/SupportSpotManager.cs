using System.Collections.Generic;
using UnityEngine;

public class SupportSpotManager : MonoBehaviour
{
    [Header("Pitch Spots")]
    public List<Transform> spots;

    [Header("General Filters")]
    [Range(0f, 1f)] public float forwardThreshold = 0.05f;
    public float skipRadius = 3.5f;          // 持球者周围屏蔽半径
    public float idealDist = 12f;
    public float maxLeadAttack = 14f;
    public float maxLeadDefend = 16f;

    [Header("Weights (attack / defend)")]
    public float wBallAtk = 0.65f, wGoalAtk = 0.20f;
    public float wBallDef = 0.45f, wGoalDef = 0.30f;

    [Header("Neighbours")]
    public float wMateDist = 0.60f;
    public float minDistFromOpp = 0.6f;
    public float minDistFromMate = 6f;

    public Transform GetBestSpot(
        Vector3 ballPos,
        Vector3 friendlyGoalPos,
        Vector3 enemyGoalPos,
        bool possessionUs,
        Vector3? ballCarrierPos,    // 可为 null
        List<WaterPlayer> opponents,
        List<WaterPlayer> mates)
    {
        float bestScore = -1f;
        Transform bestSpot = null;

        Vector3 fieldFwd = (enemyGoalPos - friendlyGoalPos).normalized;
        Vector3 refDir = possessionUs ? fieldFwd : -fieldFwd;

        float wBall = possessionUs ? wBallAtk : wBallDef;
        float wGoal = possessionUs ? wGoalAtk : wGoalDef;
        float maxLead = possessionUs ? maxLeadAttack : maxLeadDefend;

        foreach (Transform s in spots)
        {
            if (!s) continue;

            Vector3 toSpotDir = (s.position - ballPos).normalized;
            float forward = Vector3.Dot(refDir, toSpotDir);
            if (forward < forwardThreshold) continue;

            float distToBall = Vector3.Distance(ballPos, s.position);
            if (distToBall > maxLead) continue;

            // ① 跳过离持球者过近的点（只有当 ballCarrierPos.HasValue 才生效）
            if (ballCarrierPos.HasValue &&
                Vector3.Distance(ballCarrierPos.Value, s.position) < skipRadius)
                continue;

            // ② 队友占位冲突
            bool occupied = false;
            foreach (var m in mates)
            {
                if (m == null) continue;
                if (Vector3.Distance(m.Pos, s.position) < 1.5f)
                { occupied = true; break; }
            }
            if (occupied) continue;

            // ③ 基础评分：距球的高斯 + 朝向球门
            float gauss = Mathf.Exp(-(distToBall - idealDist) * (distToBall - idealDist) /
                                    (2 * idealDist * idealDist));
            float score = gauss * wBall + forward * wGoal;

            // ④ 对手安全
            bool unsafeOpp = false;
            foreach (var opp in opponents)
            {
                if (!opp) continue;
                if (Vector3.Distance(opp.Pos, s.position) < minDistFromOpp)
                { unsafeOpp = true; break; }
            }
            if (unsafeOpp) continue;

            // ⑤ 队友距离（保持宽度）
            float nearestMate = float.MaxValue;
            foreach (var m in mates)
            {
                if (m == null) continue;
                nearestMate = Mathf.Min(nearestMate, Vector3.Distance(m.Pos, s.position));
            }
            if (nearestMate < minDistFromMate) continue;
            score += (nearestMate / 10f) * wMateDist;

            if (score > bestScore)
            {
                bestScore = score;
                bestSpot = s;
            }
        }

        return bestSpot;
    }

    // 兼容旧调用
    public Transform GetBestSpot(
        Vector3 ballPos,
        Vector3 friendlyGoalPos,
        Vector3 enemyGoalPos,
        bool possessionUs,
        List<WaterPlayer> opponents,
        List<WaterPlayer> mates)
    {
        return GetBestSpot(ballPos, friendlyGoalPos, enemyGoalPos,
                           possessionUs, null, opponents, mates);
    }
}
