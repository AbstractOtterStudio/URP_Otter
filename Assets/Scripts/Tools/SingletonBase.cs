using UnityEngine;
using System.Collections;
using System;

public abstract class SingletonBase<T> : MonoBehaviour    
{
    public static T instance { get; protected set; }
}
