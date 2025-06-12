using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupportSpotManager:MonoBehaviour
{
    /* ───────── Inspector ───────── */
    [Header("Pitch Spots")]
    public List<Transform> spots;

    [Header("General Filters")]
    public float forwardThreshold = 0.05f;  // 在球前方
    public float skipRadius       = 3.5f;   // << 新增：屏蔽持球者周围点 (需求 1)

    [Header("Distance Prefs")]
    public float idealDist     = 12f;       // 偏好距球 12 m
    public float maxLeadAttack = 14f;
    public float maxLeadDefend = 16f;

    [Header("Weights (attack / defend)")]
    public float wBallAtk  = 0.65f, wGoalAtk  = 0.20f;
    public float wBallDef  = 0.45f, wGoalDef  = 0.30f;
    public float wMateDist = 0.60f;

    [Header("Spread Control")]
    public bool preferWide = true;
    public float fieldCenter = 0f;
    public bool alongXAxis = false; // 球场沿 X 或 Z 放置
    public float widthBias = 0.15f;

    public float minDistFromOpp = 0.6f;
    public float minDistFromMate = 6f;

    /* ───────── 主要接口 ───────── */
   public Transform GetBestSpot(
    Vector3           ballPos,
    Vector3           friendlyGoalPos,
    Vector3           enemyGoalPos,
    bool              possessionUs,
    Vector3?          ballCarrierPos,
    List<WaterPlayer> opponents,
    List<WaterPlayer> mates)
    {
        float     bestScore = -1f;
        Transform bestSpot  = null;

        // 1. 攻守方向根据“球权归属”
        bool   attacking = possessionUs;
        Vector3 fieldFwd = (enemyGoalPos - friendlyGoalPos).normalized;
        Vector3 refDir   = attacking ? fieldFwd : -fieldFwd;
        float   maxLead  = attacking ? maxLeadAttack : maxLeadDefend;
        float   wBall    = attacking ? wBallAtk     : wBallDef;
        float   wGoal    = attacking ? wGoalAtk     : wGoalDef;

        // 2. 遍历支援点
        foreach (Transform s in spots)
        {
            Vector3 toSpotDir = (s.position - ballPos).normalized;
            float   forward   = Vector3.Dot(refDir, toSpotDir);
            if (forward < forwardThreshold) continue;

            float distToBall = Vector3.Distance(ballPos, s.position);
            if (distToBall > maxLead) continue;

            // 2a. 屏蔽持球者周围点
            if (ballCarrierPos.HasValue &&
                Vector3.Distance(ballCarrierPos.Value, s.position) < skipRadius)
                continue;

            // 2b. 禁止多人占用同一点（新增）
            bool occupied = false;
            foreach (var m in mates)
            {
                if (m == null) continue;
                if (Vector3.Distance(m.Pos, s.position) < 1.5f)
                {
                    occupied = true;
                    break;
                }
            }
            if (occupied) continue;

            // 3. 评分：距球 + 对门方向
            float gauss = Mathf.Exp(-Mathf.Pow(distToBall - idealDist, 2) /
                                    (2 * idealDist * idealDist));
            float score = gauss * wBall + forward * wGoal;

            // 4. 排除危险点
            bool unsafeOpp = false;
            foreach (var opp in opponents)
            {
                if (Vector3.Distance(opp.Pos, s.position) < minDistFromOpp)
                { unsafeOpp = true; break; }
            }
            if (unsafeOpp) continue;

    
            // 5. 队友间距
            float nearestMate = float.MaxValue;
            foreach (var m in mates)
            {
                if (m == null) continue;
                nearestMate = Mathf.Min(nearestMate,
                                        Vector3.Distance(m.Pos, s.position));
            }
            if (nearestMate < minDistFromMate) continue;

            // // 5b. 越靠边越好（拉开宽度）
            // if (preferWide)
            // {
            //     float axis = alongXAxis ? s.position.z : s.position.x;
            //     float distFromCenter = Mathf.Abs(axis - fieldCenter);
            //     score += distFromCenter * widthBias;
            // }

            score += (nearestMate / 10f) * wMateDist;

            // 6. 择优
            if (score > bestScore)
            {
                bestScore = score;
                bestSpot  = s;
            }
        }

        return bestSpot;
    }

}
