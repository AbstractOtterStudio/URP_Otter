using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EscaperAgent : IAgent
{
    EscaperFsmSystem m_Fsm;
    Vector3 dir;

    #region Stroll Action Vars
    int patrolIndex = 0;
    [SerializeField] Vector3[] m_PatrolPoints;
    [SerializeField] float moveSpeed;
    #endregion

    #region Escape Action Vars        
    public Vector3 fleeVector;
    [SerializeField] float escapeSpeedMultiplier = 1.5f;
    #endregion

    protected override void MakeFsm()
    {                
        m_Fsm = new EscaperFsmSystem();

        StateStroll strollState = new StateStroll(this, m_Fsm);
        strollState.AddTransition(EscaperTransitions.SeePlayer, EscaperStateID.Escape);

        StateEscape escapeState = new StateEscape(this, m_Fsm);
        escapeState.AddTransition(EscaperTransitions.ReachFleePoint, EscaperStateID.Stroll);

        m_Fsm.AddState<IEscaperState>(strollState, escapeState);
    }

    public void SetPatrolPoints(Vector3[] patrolPoints)
    {
        m_PatrolPoints = patrolPoints;
    }

    void FixedUpdate()
    {
        if (!isActivated) 
        {                  
            return; 
        }        

        WallDetect();
        if (isAvoidingWall)
        {
            wallAvoidTimer += Time.fixedDeltaTime;
            GetOutFromWall(moveSpeed);
            if(wallAvoidTimer >= wallAvoidingTime)
            {
                isAvoidingWall = false;
                wallAvoidTimer = 0f;
            }                     
        }

        if(m_Fsm.CurrentState != null)
        {            
            m_Fsm.CurrentState.Reason();
            m_Fsm.CurrentState.Act();
        }        
    }
    
    #region Specify Actions
    public void Strolling()
    {           
        if(m_PatrolPoints.Length == 0 || m_PatrolPoints == null) { return; }
        if(Vector3.Distance(m_PatrolPoints[patrolIndex], transform.position) < 1.5f)
        {
            patrolIndex = (patrolIndex + 1) % m_PatrolPoints.Length;            
        }        
        dir = (m_PatrolPoints[patrolIndex] - transform.position)
            .Y(transform.position.y)
            .normalized;
        Movement(false);
    }

    public void Escaping()
    {
        if (isAvoidingWall) { return; }
        fleeVector = -(m_DetectedPlayerTrans.position - transform.position); 
        dir = fleeVector
            .Y(transform.position.y)
            .normalized;
        Movement(true);
        #region Obsolete
        //Random Direction        
        // turnTimer += Time.deltaTime;
        // if(turnTimer >= turnTime)
        // {
        //     float randAngle = UnityEngine.Random(-1f, 1f);            

        //     dir = new Vector3(dir.x + rand)
        //     turnTimer = 0f;
        // }        

        // Debug.Log(dir);
        // if(Vector3.Distance(m_TargetEscapePosition, transform.position) < 5f)
        // {
        //     deadFightTimer += Time.deltaTime;
        //     if(deadFightTimer >= deadFightTime)
        //     {
        //         m_TargetEscapePosition = 
        //             (DetectedPlayerTrans.position - transform.position).Y(transform.position.y);
        //         deadFightTimer = 0f;
        //     }
        // }    
        #endregion                     
    }

    void Movement(bool isEscape)
    {
        if (!isActivated) { return; }
        if (!isEscape) { m_Rigidbody.velocity = dir * moveSpeed; }
        else { m_Rigidbody.velocity = dir * moveSpeed * escapeSpeedMultiplier; }
        transform.rotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.X(0).Z(0));
    }
    public override void ActivateAI() 
    {
        Debug.Log("AI is Activated");
        isActivated = true;
    }
    public override void DeactivateAI()
    {
        Debug.Log("AI is Deactivated");
        m_Rigidbody.velocity = Vector3.zero;
        isActivated = false;        
    }
    public void ResetState()
    {
        isActivated = true;
        transform.position = m_PatrolPoints[0];
    }
    #endregion
}