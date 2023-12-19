using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainBase : MonoBehaviour
{
    [SerializeField]
    public bool canClean {get; protected set;}
    [SerializeField]
    public bool canSleep {get; protected set;}
}
