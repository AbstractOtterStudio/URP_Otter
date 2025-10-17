using UnityEngine;

/* 基类 */
public abstract class WAState
{
    public string name = "State";
    protected readonly WaterPlayer P;
    protected WAState(WaterPlayer p) { P = p; }
    public virtual void Enter() { }
    public virtual void Update() { }
}

/* Idle：无球 → 跑位；有球 → 决策；最近者 → 追球 */
public class WIdle : WAState
{
    public WIdle(WaterPlayer p) : base(p) { name = "Idle"; }

    public override void Update()
    {
        if (WaterPlayer.isPaused) return;

        if (P.HasBall && P.CanChangeState) { P.StartHold(); P.Change(new WDecision(P), P.stateMinHold_Decision); return; }
        if (P.ShouldChaseBall() && P.CanChangeState) { P.Change(new WChase(P), P.stateMinHold_Chase); return; }

        bool usHave = P.TeamHasBall || (!P.ball || P.ball.Owner == null && P.IsClosestToBallInTeam());

        // 先取角色锚点，再尝试支援点
        Vector3 anchor = P.GetRoleAnchor(usHave);
        var spot = P.GetSupportSpot();
        Vector3 target = spot ? Vector3.Lerp(anchor, spot.position, 1f - P.anchorBias) : anchor;

        P.MoveTo(target);
        name = spot ? "Support" : "RoleAnchor";
    }
}

/* Chase：预测抢球，接触到球切换 Decision */
public class WChase : WAState
{
    public WChase(WaterPlayer p) : base(p) { name = "Chase"; }

    public override void Update()
    {
        if (WaterPlayer.isPaused) return;
        if (P.HasBall && P.CanChangeState) 
        {
            P.StartHold();
            P.Change(new WDecision(P), P.stateMinHold_Decision);
            return;
        }
        if ((P.TeamHasBall && !P.HasBall) || !P.ShouldChaseBall())
        { if (P.CanChangeState) P.Change(new WIdle(P), P.stateMinHold_Idle); return; }

        P.MoveToBallPred();

        if (P.ball && Vector3.Distance(P.Pos, P.ball.Pos) < 1.1f && P.CanChangeState)
        { P.StartHold(); P.Change(new WDecision(P), P.stateMinHold_Decision); }
    }
}

/* Decision：拿球后的完整行为树（先“能射必射”，再传，再解卡，最后带球） */
public class WDecision : WAState
{
    Vector3 driveDir;
    float nextDriveAt, nextTick;

    public WDecision(WaterPlayer p) : base(p) { name = "Decision"; }

    public override void Enter()
    {
        nextDriveAt = Time.time;
        nextTick = Time.time + P.decisionTickRange.x;

        // ★ 首帧就计算一次带球方向，避免 DribbleAlong 用到 0 向量的回退（导致朝前一脚）
        driveDir = P.ComputeDriveDir();

        // 拿球瞬间：先做合理动作（能射→射；能传→传；门后/贴墙→快处置）
        if (P.FirstTouchPlay(out string why)) { name = "First:" + why; }
    }


    public override void Update()
    {
        if (WaterPlayer.isPaused) return;

        if (!P.HasBall) { if (P.CanChangeState) P.Change(new WIdle(P), P.stateMinHold_Idle); return; }

        // 0) 若路径可达且在射程 → 立即射门
        if (P.CanShootSmart(out Vector3 g1)) { P.Shoot(g1); name = "Shoot"; return; }

        // 1) 墙/门后等快速处理（传/回做/中路清）
        if (P.TryBoundaryEscapeOrQuickRelease(out string why2)) { name = why2; return; }

        // 2) 周期性：传球机会
        if (Time.time >= nextTick)
        {
            bool preferForward = P.role != RG_ROLE.Mid ? true : P.IsInAttackingHalf();
            if (P.FindBestPassOption(out Vector3 pass, out _, preferForward)) { P.Pass(pass); name = "PassTick"; return; }

            float k = Mathf.Clamp01(1f - P.passAggression);
            float dt = Mathf.Lerp(P.decisionTickRange.x, P.decisionTickRange.y, k);
            nextTick = Time.time + dt;
        }

        // 3) 节流计算带球方向 + 平滑
        if (Time.time >= nextDriveAt)
        {
            Vector3 fresh = P.ComputeDriveDir();
            driveDir = (driveDir == Vector3.zero) ? fresh : Vector3.Slerp(driveDir, fresh, P.driveDirSmoothing);
            nextDriveAt = Time.time + P.driveRecalcInterval;
        }

        // 4) 带球
        P.DribbleAlong(driveDir);
        name = "Drive";
    }
}
