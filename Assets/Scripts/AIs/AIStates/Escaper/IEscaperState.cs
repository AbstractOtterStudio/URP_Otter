

//轉換條件
public enum EscaperTransitions
{
    Null,
    SeePlayer,
    ReachFleePoint,
}

//具體狀態
public enum EscaperStateID
{
    Null,
    Stroll,
    Escape,
}

public abstract class IEscaperState : IStateBase<EscaperTransitions, EscaperStateID>
{
    protected new EscaperAgent m_Agent;

    public IEscaperState(EscaperAgent agent, EscaperFsmSystem fsm) 
        : base(agent, fsm)
    {
        m_Agent = agent;
    }

    public override void Reason() { }
    public override void Act() { }

}