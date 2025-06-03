using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WaterPlayer : MonoBehaviour
{
    /* ───────── Inspector ───────── */
    [Header("Team & Pitch refs")]
    public bool  isTeammate;
    public Goal  friendlyGoal;
    public Goal  enemyGoal;
    public List<WaterPlayer> team;
    public List<WaterPlayer> opponents;
    public SupportSpotManager spotManager;

    [Header("Distances")]
    public float distPassMin  = 6f;
    public float distPassMax  = 25f;
    public float shotMinDist  = 8f;   // 离门 ≤10m 必射
    public float shotMaxDist  = 20f;   // 离门 >28m 不射
    public float threatMax    = 2.2f;

    [Header("Rotation")]
    public float turnSpeed = 540f;

    [Header("Abilities 0-1")]
    [Range(0.1f, 1f)] public float accuracy = .85f;
    [Range(0.1f, 1f)] public float power    = .9f;

    [Header("Speed Settings")]
    public float baseSpeed       = 3.7f;
    public float sprintMultiplier = 1.9f;
    public bool  alwaysSprint     = true;

    [Header("Defence")]
    [Tooltip("Only drop back when ball is within this distance from our goal (m)")]
    public float defendTriggerDist = 30f;

    /* ───────── Static per-team ball chaser ───────── */
    public static WaterPlayer[] BallChaser   = new WaterPlayer[2];
    private static float[]       chaserExpiry = { 0f, 0f };
    public int TeamIdx => isTeammate ? 0 : 1;

    /* ───────── Runtime ───────── */
    public  bool  requestPass;
    private float holdStart;

    public  Ball       ball  { get; private set; }
    private Rigidbody  rb;
    private WAState    state;
    public  string     stateStr;

    /* ───────── Unity ───────── */
    private void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        ball = FindObjectOfType<Ball>();
    }

    private void Start()
    {
        Change(new WIdle(this));
    }

    private void Update()
    {
        state?.Update();
        stateStr = state.name;

        // 若追球者离球太远则释放
        if (BallChaser[TeamIdx] == this && Vector3.Distance(Pos, ball.Pos) > 2.5f)
            BallChaser[TeamIdx] = null;
    }

    /* ───────── FSM helpers ───────── */
    bool IsPlayerControlled(WaterPlayer p)
    {
        return p.CompareTag("Player") || p.GetComponent<PlayerController>() != null;
    }

    public void Change(WAState s)
    {
        state = s;
        state.Enter();
    }

    public bool ShouldChaseBall()
    {
         // 玩家脚下 → 暂不追
        if (isTeammate && ball.Owner != null && ball.Owner.gameObject.CompareTag("Player"))
        {
            float dist = Vector3.Distance(Pos, ball.Pos);
            if (dist < 3f) return false;
        }

        // ① 已是追球者且锁未到期
        if (BallChaser[TeamIdx] == this && Time.time < chaserExpiry[TeamIdx])
            return true;

        // ② 无人追或锁到期 → 重新评估
        if (BallChaser[TeamIdx] == null || Time.time >= chaserExpiry[TeamIdx])
        {
            if (IsClosestToBall())
            {
                BallChaser[TeamIdx] = this;
                chaserExpiry[TeamIdx] = Time.time + 1.2f; // 锁 1.2 秒
                return true;
            }
        }
        return false;
    }

    /* ===== 呼叫传球 ===== */
    public void AskForPass()
    {
        requestPass = true;
        Invoke(nameof(ClearPassRequest), 0.7f);
    }

    private void ClearPassRequest() => requestPass = false;

    public void StartHold()           => holdStart = Time.time;
    public bool HoldExceeded(float s) => Time.time - holdStart > s;

    /* ───────── Ball control ───────── */
    private static float nextKick;

    public void AttemptKick()
    {
        if (!HasBall) return;
        if (Time.time < nextKick) return;

        if      (CanShoot(out Vector3 g)) Shoot(g);
        else if (CanPass(out Vector3 t, out _)) Pass(t);
        else ClearBall();

        nextKick = Time.time + 0.4f; // 400 ms 全队冷却
    }

    private void ClearBall()
    {
        Vector3 dir = (enemyGoal.transform.position - Pos).normalized;
        dir = Quaternion.Euler(0, Random.Range(-30, 30), 0) * dir;
        ball.Kick(Pos + dir * 6f, 16f);
    }

    /* ───────── Helpers ───────── */
    public bool HasBall =>
    ball.Owner == this ||
    (ball.Owner != null && ball.Owner.gameObject.CompareTag("Player") && Vector3.Distance(Pos, ball.Pos) <= 2.5f);


    public Vector3 Pos => new Vector3(transform.position.x, 0, transform.position.z);

    public bool IsClosestToBall()
    {
        float myDist = Vector3.Distance(Pos, ball.Pos);
        foreach (var mate in team)
        {
            if (mate == this) continue;
            if (Vector3.Distance(mate.Pos, ball.Pos) < myDist - 0.05f)
                return false;
        }
        return true;
    }

    /* ───────── Movement ───────── */
    public void MoveTo(Vector3 target)
    {
        Vector3 dir = target - Pos; dir.y = 0;
        if (dir.sqrMagnitude < 0.05f) return;
        dir.Normalize();

        // Separation
        Vector3 repel = Vector3.zero;
        foreach (var mate in team)
        {
            if (mate == this) continue;
            float dist = Vector3.Distance(Pos, mate.Pos);
            if (dist < 2.5f)
                repel += (Pos - mate.Pos) * (1f / dist);
        }
        dir = (dir + repel * 0.2f).normalized;

        float speed   = baseSpeed * (alwaysSprint ? sprintMultiplier : 1f);
        Vector3 horiz = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(dir * speed - horiz, ForceMode.Acceleration);

        // Rotate model
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation  = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    public void MoveToBallPred(float leadFactor = 1.1f, float maxLead = 1.4f)
    {
        Vector3 v = ball.Rb.velocity; v.y = 0;
        float   d = Vector3.Distance(Pos, ball.Pos);
        float   t = Mathf.Clamp(d / (baseSpeed * leadFactor), 0, maxLead);
        MoveTo(ball.Pos + v * t);
    }

    /* ───────── Threat / Safety helpers ───────── */
    private Vector3 Orthogonal(Vector3 a, Vector3 b, Vector3 p)
    {
        // 投影点
        return a + Vector3.Project(p - a, b - a);
    }

    private bool PassSafeFromOpponent(Vector3 start, Vector3 target, WaterPlayer opp, float ballTime)
    {
        Vector3 foot    = Orthogonal(start, target, opp.Pos);
        float   oppTime = Vector3.Distance(opp.Pos, foot) / opp.baseSpeed;
        return oppTime > ballTime;
    }

    private bool PassSafeAll(Vector3 start, Vector3 target, float powerNeed, float ballTime)
    {
        foreach (var opp in opponents)
            if (!PassSafeFromOpponent(start, target, opp, ballTime))
                return false;
        return true;
    }

    /* ───────── Pass / Shoot evaluation ───────── */
    public bool CanPass(out Vector3 target, out WaterPlayer receiver)
    {
        target   = Vector3.zero;
        receiver = null;
        float bestScore = -1f;

        foreach (var mate in team)
        {
            if (mate == this) continue;

            float dist = Vector3.Distance(Pos, mate.Pos);
            if (dist < distPassMin || dist > distPassMax) continue;

            float powerNeed = ball.FindPower(ball.Pos, mate.Pos, 1.2f);
            float ballTime  = ball.TimeToCover(ball.Pos, mate.Pos, powerNeed);
            if (ballTime < 0) continue;

            if (!PassSafeAll(ball.Pos, mate.Pos, powerNeed, ballTime))
            {
                if (dist > 3f) mate.AskForPass();
                continue;
            }

            // 更靠近敌门且距离适中
            float progress = Vector3.Distance(mate.Pos, enemyGoal.transform.position);
            float myDist    = Vector3.Distance(Pos, enemyGoal.transform.position);
            float score     = (myDist - progress) / dist;

            if (IsPlayerControlled(mate))
                score *= 1.8f;

            if (score > bestScore)
            {
                bestScore = score;
                target    = mate.Pos;
                receiver  = mate;
            }
        }
        return receiver != null && bestScore > 0f;
    }

    public bool CanShoot(out Vector3 goal)
    {
        goal = enemyGoal.transform.position;
        float dist = Vector3.Distance(Pos, goal);

        /* 射门阈值随距离分段 (需求 2) */
        if (dist <= shotMinDist)      return true;      // 绝对要射
        if (dist >= shotMaxDist)      return false;     // 绝不射

        /* 中间距离：概率随距离线性下降 */
        float t = 1f - (dist - shotMinDist) /
                       (shotMaxDist - shotMinDist);     // 1 → 0
        return Random.value < t;
    }

    public void Pass(Vector3 tgt)
    {
        float pow = ball.FindPower(ball.Pos, tgt, 1.2f) * 1.15f;
        ball.Kick(tgt, pow);
    }

    public void Shoot(Vector3 tgt)
    {
        float pow = ball.FindPower(ball.Pos, tgt, 4f) * 1.1f;
        ball.Kick(tgt, pow);
    }

    /* ───────── Support spot ───────── */
    public Transform GetSupportSpot()
    {
        if (spotManager == null) return null;

        /* ① 谁控球？ */
        bool possessionUs = BallUtils.IsFriendly(ball.Owner, isTeammate);

        /* ② 选对应“攻/守”模式；再把持球者位置传入，供 SupportSpotManager
              避开离持球者太近的点（需求 1） */
        return spotManager.GetBestSpot(
            ballPos: ball.Pos,
            friendlyGoalPos: friendlyGoal.transform.position,
            enemyGoalPos: enemyGoal.transform.position,
            possessionUs,
            ballCarrierPos: ball.Owner != null ? (Vector3?)ball.Pos : null, // 玩家只提供球位置
            opponents: opponents,
            mates: team
        );
    }
}
