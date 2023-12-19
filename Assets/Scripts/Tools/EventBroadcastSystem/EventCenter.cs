using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//事件廣播中心
public class EventCenter
{
    static Dictionary<GameEvents, Delegate> m_EventDictionary = new Dictionary<GameEvents, Delegate>();

    #region Add Listener
    static void BeforeListenerAdd(GameEvents eventType, Delegate callBack)
    {
        if(!m_EventDictionary.ContainsKey(eventType))
        {
            m_EventDictionary.Add(eventType, null);
        }

        Delegate d = m_EventDictionary[eventType];

        if(d != null && d.GetType() != callBack.GetType())
        {
            throw new Exception($"Listener Adding Failed: {eventType}");
        }

    }
    public static void AddListener(GameEvents eventType, CallBack callBack)
    {
        BeforeListenerAdd(eventType, callBack);
        Debug.Log($"Adding Event: {eventType} With Callback: {callBack}");
        m_EventDictionary[eventType] = (CallBack)m_EventDictionary[eventType] + callBack;
    }
    public static void AddListener<T1>(GameEvents eventType, CallBack<T1> callBack)
    {
        BeforeListenerAdd(eventType, callBack);
        m_EventDictionary[eventType] = (CallBack<T1>)m_EventDictionary[eventType] + callBack;
    }
    public static void AddListener<T1, T2>(GameEvents eventType, CallBack<T1, T2> callBack)
    {
        BeforeListenerAdd(eventType, callBack);
        m_EventDictionary[eventType] = (CallBack<T1, T2>)m_EventDictionary[eventType] + callBack;
    }
    #endregion

    #region Remove Listener
    static void BeforeListenerRemove(GameEvents eventType, Delegate callBack)
    {
        if(m_EventDictionary.ContainsKey(eventType))
        {
            Delegate d = m_EventDictionary[eventType];
            if(d == null || d.GetType() != callBack.GetType())
            {
                throw new Exception($"Remove Listener Failed: {eventType}");
            }
        }
        else
        {
            throw new Exception($"Can't Remove Empty Listener: {eventType}");
        }
    }
    public static void RemoveListener(GameEvents eventType, CallBack callBack)
    {
        BeforeListenerRemove(eventType, callBack);
        m_EventDictionary[eventType] = (CallBack)m_EventDictionary[eventType] - callBack;
        AfterListenerRemoved(eventType);
    }
    public static void RemoveListener<T1>(GameEvents eventType, CallBack<T1> callBack)
    {
        BeforeListenerRemove(eventType, callBack);
        m_EventDictionary[eventType] = (CallBack<T1>)m_EventDictionary[eventType] - callBack;
        AfterListenerRemoved(eventType);
    }
    public static void RemoveListener<T1, T2>(GameEvents eventType, CallBack<T1, T2> callBack)
    {
        BeforeListenerRemove(eventType, callBack);
        m_EventDictionary[eventType] = (CallBack<T1, T2>)m_EventDictionary[eventType] - callBack;
        AfterListenerRemoved(eventType);
    }
    static void AfterListenerRemoved(GameEvents eventType)
    {
        if(m_EventDictionary[eventType] == null){ m_EventDictionary.Remove(eventType); }
    }
    #endregion

    #region Broadcast Event
    public static void Broadcast(GameEvents eventType)
    {
        Delegate d;
        if(m_EventDictionary.TryGetValue(eventType, out d))
        {
            CallBack cb = d as CallBack;
            cb?.Invoke();
        }
    }
    public static void Broadcast<T1>(GameEvents eventType, T1 arg1)
    {
        Delegate d;
        if(m_EventDictionary.TryGetValue(eventType, out d))
        {
            CallBack<T1> cb = d as CallBack<T1>;
            cb?.Invoke(arg1);
        }
    }
    public static void Broadcast<T1, T2>(GameEvents eventType, T1 arg1, T2 arg2)
    {
        Delegate d;
        if(m_EventDictionary.TryGetValue(eventType, out d))
        {
            CallBack<T1, T2> cb = d as CallBack<T1, T2>;
            cb?.Invoke(arg1, arg2);
        }
    }
    #endregion
}
