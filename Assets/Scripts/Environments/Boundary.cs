using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Current only for waterball game boundary
public class Boundary : MonoBehaviour
{
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            rb.velocity = -rb.velocity * 0.8f;
        }
        else if (other.CompareTag("Player") || other.CompareTag("AI"))
        {
            Vector3 pos = other.transform.position;
            pos.x = Mathf.Clamp(pos.x, -15f, 15f);
            pos.z = Mathf.Clamp(pos.z, -10f, 10f);
            other.transform.position = pos;
        }
    }
}
