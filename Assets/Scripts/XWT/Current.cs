using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Current : MonoBehaviour
{
    BoxCollider BC;
    [SerializeField]
    float degrees = 0;
    Vector3 dir;
    [SerializeField]
    float forceMag = 1;
    public bool showGizmos = true;


    // Start is called before the first frame update
    void Start()
    {
        BC = transform.GetComponent<BoxCollider>();
        dir = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad));
        //print(BC);
    }

    private void OnTriggerStay(Collider other)
    {
        if ((other.tag == "Player") || (other.tag == "Holdable") || (other.tag == "Floating"))
        {
            Rigidbody rb = other.transform.GetComponent<Rigidbody>();
            rb.AddForce(dir * forceMag, ForceMode.VelocityChange);
            print(rb);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Cache the collider reference
        Collider collider = GetComponent<Collider>();
        if (collider == null) return;  // Safety check
        
        Gizmos.color = Color.red;
        Bounds bounds = collider.bounds;
        Vector3 center = bounds.center;
        Gizmos.DrawWireCube(center, bounds.size);

        degrees %= 360;
        dir = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad));
        Gizmos.DrawRay(center, dir * (bounds.size.x + bounds.size.z)/2);
    }
}
