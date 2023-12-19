using UnityEngine;
using System;
using System.Collections.Generic;

public class StateEscape : IEscaperState
{
    public StateEscape(EscaperAgent agent, EscaperFsmSystem fsm)
        : base(agent, fsm)
    {
        m_Agent = agent;
        m_StateID = EscaperStateID.Escape;
    }

    public override void Reason()
    {
        if(m_Agent.m_DetectedPlayerTrans == null)
        {
            m_Fsm.PerformTransition(EscaperTransitions.ReachFleePoint);
        }
    }

    public override void Act()
    {        
        if(m_Agent.m_DetectedPlayerTrans == null) { return; }
        m_Agent.Escaping();        
    }
}