using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Current.cs
 * 
 * Purpose: Simulates a water current that affects physics objects within its trigger zone.
 * The current applies a directional force to any Rigidbody with specific tags 
 * (Player, Holdable) that enters its BoxCollider trigger area.
 * 
 * Features:
 * - Configurable force magnitude and direction (in degrees)
 * - Visual debugging with Gizmos
 * - Simple force application
 * 
 * Usage:
 * 1. Attach to a GameObject with a BoxCollider (set to trigger)
 * 2. Set the desired force magnitude and direction in degrees
 * 3. Objects entering the trigger will be affected by the current
 */

public class Current : MonoBehaviour
{
    BoxCollider BC;
    [SerializeField] float degrees = 0;
    Vector3 dir;
    [SerializeField] float forceMag = 5f;
    public bool showGizmos = true;

    void Start()
    {
        BC = transform.GetComponent<BoxCollider>();
        dir = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad));
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Holdable"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(dir * forceMag, ForceMode.VelocityChange);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Collider collider = GetComponent<Collider>();
        if (collider == null) return;
        
        Gizmos.color = Color.red;
        Bounds bounds = collider.bounds;
        Vector3 center = bounds.center;
        Gizmos.DrawWireCube(center, bounds.size);

        degrees %= 360;
        dir = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad));
        Gizmos.DrawRay(center, dir * (bounds.size.x + bounds.size.z)/2);
    }
}
