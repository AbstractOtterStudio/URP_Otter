using UnityEngine;
using System;
using System.Collections.Generic;

public class StateStroll : IEscaperState
{
    public StateStroll(EscaperAgent agent, EscaperFsmSystem fsm) 
        : base(agent, fsm)
    {
        m_Agent = agent;
        m_StateID = EscaperStateID.Stroll;        
    }
    
    public override void Reason()
    {
        if(m_Agent.m_DetectedPlayerTrans != null)
        {
            m_Fsm.PerformTransition(EscaperTransitions.SeePlayer);
        }
    }

    public override void Act()
    {         
        if(m_Agent.m_DetectedPlayerTrans != null) { return; }
        m_Agent.Strolling();    
    }
}