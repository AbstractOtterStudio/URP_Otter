using UnityEngine;

public static class BallUtils
{
    public static bool IsEnemy(Component owner, bool selfIsTeammate)
    {
        if (owner == null) return false;

        if (owner.TryGetComponent<WaterPlayer>(out var wp))
            return wp.isTeammate != selfIsTeammate;

        // 明确检查 GameObject
        if (owner.gameObject.CompareTag("Player"))
            return !selfIsTeammate;

        return false;
    }

    public static bool IsFriendly(Component owner, bool selfIsTeammate)
    {
        return !IsEnemy(owner, selfIsTeammate);
    }
}


/* ---------- 基类 ---------- */
public abstract class WAState
{
    public string name;
    protected readonly WaterPlayer P;

    protected WAState(WaterPlayer p) { P = p; }

    public virtual void Enter()  { }
    public virtual void Update() { }
}

/* ───────── Idle ───────── */
public class WIdle : WAState
{
    public WIdle(WaterPlayer p) : base(p) { name = "Idle"; }

    public override void Update()
    {
        // 1. 拿到球 → 进入决策
        if (P.HasBall)
        {
            P.StartHold();
            P.Change(new WDecision(P));
            return;
        }

        bool enemyHas = BallUtils.IsEnemy(P.ball.Owner, P.isTeammate);

        if (enemyHas)
        {
            // ───── 我方无球
            float danger = Vector3.Distance(P.ball.Pos, P.friendlyGoal.transform.position);

            if (danger <= P.defendTriggerDist)
            {
                // 球在警戒范围 → 回防 / 追球
                if (P.ShouldChaseBall())
                {
                    P.Change(new WChase(P));
                    return;
                }

                // 否则占位防守
                Vector3 defendPos = Vector3.Lerp(P.ball.Pos, P.friendlyGoal.transform.position, 0.4f);
                P.MoveTo(defendPos);
                name = "Defend";
                return;
            }
        }

        // ───── 进攻或球在远处
        if (P.ShouldChaseBall())
        {
            P.Change(new WChase(P));
            return;
        }

        Transform spot = P.GetSupportSpot();
        if (spot && Vector3.Distance(P.Pos, spot.position) > 1f)
        {
            P.Change(new WSupport(P, spot));
            return;
        }

        name = "Idle";
    }
}

/* ───────── Chase ───────── */
public class WChase : WAState
{
    public WChase(WaterPlayer p) : base(p) { name = "Chase"; }

    public override void Update()
    {
        if (!P.ShouldChaseBall() && !P.HasBall)
        {
            P.Change(new WIdle(P));
            return;
        }

        P.MoveToBallPred();

        // 若更近队友呼叫则让球
        foreach (var mate in P.team)
        {
            if (mate == P) continue;
            if (mate.requestPass && Vector3.Distance(mate.Pos, P.ball.Pos) < 1.5f)
            {
                WaterPlayer.BallChaser[mate.TeamIdx] = mate;
                break;
            }
        }

        // 到球
        if (Vector3.Distance(P.Pos, P.ball.Pos) < 1.1f)
        {
            P.StartHold();
            P.Change(new WDecision(P));
        }
    }
}

/* ───────── Support ───────── */
public class WSupport : WAState
{
    private readonly Transform target;

    public WSupport(WaterPlayer p, Transform t) : base(p)
    {
        target = t;
        name   = "Support";
    }

    public override void Update()
    {
        if (P.HasBall)
        {
            P.StartHold();
            P.Change(new WDecision(P));
            return;
        }

        if (P.ShouldChaseBall())
        {
            P.Change(new WChase(P));
            return;
        }

        if (!target)
        {
            P.Change(new WIdle(P));
            return;
        }

        P.MoveTo(target.position);

        if (Vector3.Distance(P.Pos, target.position) < 1f)
            P.Change(new WIdle(P));
    }
}

/* ───────── Decision (with ball) ───────── */
public class WDecision : WAState
{
    private const float ThreatRadius = 4f; // 敌方靠近判定半径
    public WDecision(WaterPlayer p) : base(p) { name = "Decision"; }

    public override void Enter() => Decide();

    public override void Update()
    {
        if (!P.HasBall)
        {
            P.Change(new WIdle(P));
            return;
        }
        Decide();
    }

    private void Decide()
    {
        bool threatened = false;
        foreach (var opp in P.opponents)
        {
            if (Vector3.Distance(opp.Pos, P.Pos) <= ThreatRadius)
            {
                threatened = true;
                break;
            }
        }

        if (threatened)
        {
            if (P.CanShoot(out Vector3 goalDir))
            {
                P.Shoot(goalDir);
                name = "QuickShoot";
                return;
            }
            if (P.CanPass(out Vector3 tgt, out _))
            {
                P.Pass(tgt);
                name = "QuickPass";
                return;
            }
            // 无安全传球亦无法射门 → 清球
            Vector3 dir = (P.enemyGoal.transform.position - P.Pos).normalized;
            P.ball.Kick(P.Pos + dir * 6f, 12f);
            name = "Clear";
            return;
        }

        // 1. 刚得球先推
        if (!P.HoldExceeded(0.5f))
        {
            Vector3 dir = (P.enemyGoal.transform.position - P.Pos).normalized;
            P.MoveTo(P.Pos + dir * 8f);
            name = "Drive";
            return;
        }

        // 2. 传球
        foreach (var mate in P.team)
        {
            if (mate == P) continue;
            if (mate.requestPass)
            {
                P.Pass(mate.Pos);
                name = "Pass";
                return;
            }
        }

        if (P.CanPass(out Vector3 tgtDir, out _))
        {
            P.Pass(tgtDir);
            name = "Pass";
            return;
        }

        // 3. 射门
        if (P.CanShoot(out Vector3 goal))
        {
            P.Shoot(goal);
            name = "Shoot";
            return;
        }

        // 4. 继续带
        Vector3 push = (P.enemyGoal.transform.position - P.Pos).normalized;
        P.MoveTo(P.Pos + push * 8f);
        name = "Drive";
    }
}


