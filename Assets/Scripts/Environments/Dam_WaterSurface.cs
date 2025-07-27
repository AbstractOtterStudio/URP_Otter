using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Dam_WaterSurface : MonoBehaviour
{
    public static Dam_WaterSurface Instance { get; private set; }
    [Tooltip("Flat water plane Y height")] public float height = 0f;
    [Tooltip("World‑space flow direction along the water surface")] public Vector3 flowDirection = Vector3.forward;

    void Awake()
    {
        if (Instance && Instance != this) Destroy(gameObject);
        else Instance = this;
        // Ensure the collider is trigger for OnTriggerEnter events
        GetComponent<Collider>().isTrigger = true;
    }
}
