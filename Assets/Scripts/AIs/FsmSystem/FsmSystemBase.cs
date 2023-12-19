using System.Collections.Generic;
using System;

//狀態抽象泛型基類
public abstract class FsmSystemBase<TTransition, TStateID> 
    where TTransition : System.Enum, IComparable
    where TStateID : System.Enum, IComparable
{    
    Dictionary<TStateID, IStateBase<TTransition, TStateID>> m_StateMap =
         new Dictionary<TStateID, IStateBase<TTransition, TStateID>>();
    IStateBase<TTransition, TStateID> m_CurrentState;
    public IStateBase<TTransition, TStateID> CurrentState { get { return m_CurrentState; }}

    public void AddState<T>(T state)
        where T : IStateBase<TTransition, TStateID>
    {
        if(state == null) { return; }
        if(m_StateMap.Count == 0)
        {
            //Add and Become default State            
            m_StateMap.Add(state.StateID, (T)state);
            m_CurrentState = state;
            m_CurrentState.DoBeforeEntering();
        }
        else
        {
            if(m_StateMap.ContainsKey(state.StateID)) { return; }
            m_StateMap.Add(state.StateID, (T)state);
        }
    }
    public void AddState<T>(params T[] states)
        where T : IStateBase<TTransition, TStateID>
    {
        foreach(var s in states)
        {
            AddState<T>(s);
        }
    }

    public void RemoveState<T>(T state)
        where T : IStateBase<TTransition, TStateID>
    {
        if(m_StateMap.ContainsKey(state.StateID))
        {
            m_StateMap.Remove(state.StateID);
        }
    }

    public void PerformTransition(TTransition transition)
    {
        if(transition.Equals(default(TTransition))) { return; }
        TStateID nextStateID = m_CurrentState.GetStateByTransition(transition);
        if(nextStateID.Equals(default(TStateID))) { return; }

        if(m_StateMap.ContainsKey(nextStateID))
        {
            m_CurrentState.DoBeforeLeaving();
            m_CurrentState = m_StateMap[nextStateID];            
            m_CurrentState.DoBeforeEntering();
        }
    }
}