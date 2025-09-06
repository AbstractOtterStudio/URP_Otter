using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DamGameManager : MonoBehaviour
{
   public DamManager dam;
    public float FlowMultiplier => Mathf.Lerp(1f, 2.8f, Mathf.InverseLerp(0f, 0.6f, dam.progress));
    public float Progress => dam.progress;
    void Start() => Application.targetFrameRate = 60;
}

public enum RiverObjectType { Material, Junk, Hazard }
