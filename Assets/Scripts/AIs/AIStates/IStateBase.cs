using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

//狀態機狀態抽象泛型基類
public abstract class IStateBase<TTransition, TStateID>    
    where TTransition : System.Enum, IComparable
    where TStateID : System.Enum, IComparable
{
    protected IAgent m_Agent; //該狀態所屬角色（用於操控角色行動）
    //該狀態所屬的狀態機（用於狀態切換）
    protected FsmSystemBase<TTransition, TStateID> m_Fsm;

    //轉換條件及目標狀態映射
    protected Dictionary<TTransition, TStateID> m_Map = new Dictionary<TTransition, TStateID>();

    //當前狀態的標識
    protected TStateID m_StateID;
    public TStateID StateID { get { return m_StateID; }}


    //Constructor
    public IStateBase(IAgent agent, 
        FsmSystemBase<TTransition, TStateID> fsm)        
    {
        m_Agent = agent;
        m_Fsm = fsm;
    }

    #region 狀態切換條件及切換至狀態的映射增刪取
    public void AddTransition(TTransition trans, TStateID stateID)
    {
        //Transition:0 ; State:0 must be Null Transition/State
        if(trans.Equals(default(TTransition))) { return; } //Null Trans
        if(stateID.Equals(default(TStateID))) { return; } //Null State
        if(m_Map.ContainsKey(trans)) { return; }
        m_Map.Add(trans, stateID);
    }

    public void RemoveTransition(TTransition trans, TStateID stateID)
    {
        if(!m_Map.ContainsKey(trans)){ return; }
        m_Map.Remove(trans);
    }

    public TStateID GetStateByTransition(TTransition trans)
    {
        TStateID state = default(TStateID);
        m_Map.TryGetValue(trans, out state);
        return state;
    }
    #endregion
    #region 狀態執行
    public virtual void DoBeforeEntering() { }
    public virtual void DoBeforeLeaving() { }
    public abstract void Reason();
    public abstract void Act();    
    #endregion
}