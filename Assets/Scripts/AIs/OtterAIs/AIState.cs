using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIState
{
    protected AIController controller;
    public AIState(AIController c) => controller = c;
    public virtual void Enter() {}
    public virtual void Update() {}
    public virtual void Exit() {}
}

// ---- concrete states ----
public class IdleState : AIState
{
    public IdleState(AIController c) : base(c) {}
    public override void Update()
    {
        if (controller.HasBall) controller.ChangeState(new PassOrShootState(controller));
        else controller.ChangeState(new ChaseBallState(controller));
    }
}

public class ChaseBallState : AIState
{
    public ChaseBallState(AIController c) : base(c) {}
    public override void Update()
    {
        controller.MoveToward(controller.Ball.position);
        if (controller.DistanceToBall < 1.2f) controller.StartSprint();
        if (controller.HasBall) controller.ChangeState(new PassOrShootState(controller));
    }
}

public class PassOrShootState : AIState
{
    Transform goal;
    public PassOrShootState(AIController c) : base(c)
    {
        goal = c.IsTeammate ? c.EnemyGoal : c.FriendlyGoal;
    }
    public override void Update()
    {
        // simple heuristic: if clear line -> shoot, else pass
        if (controller.LineClear(controller.transform.position, goal.position))
            controller.Shoot(goal.position);
        else
            controller.PassToTeammate();
        controller.ChangeState(new RetreatState(controller));
    }
}

public class InterruptState : AIState
{
    AIController target;
    public InterruptState(AIController c, AIController opponent) : base(c)
    { target = opponent; }
    public override void Update()
    {
        controller.MoveToward(target.transform.position);
        if (controller.DistanceTo(target.transform.position) < 1.3f)
            controller.StartSprint();
        if (!target.HasBall) controller.ChangeState(new ChaseBallState(controller));
    }
}

public class RetreatState : AIState
{
    public RetreatState(AIController c) : base(c) {}
    public override void Update()
    {
        controller.MoveToward(controller.HomePosition);
        if (controller.InHomeArea) controller.ChangeState(new IdleState(controller));
    }
}