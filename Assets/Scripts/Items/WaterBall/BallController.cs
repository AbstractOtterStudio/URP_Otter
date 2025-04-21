using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    public float buoyancyForce = 9.81f; // upward force to keep ball afloat
    public float hitMultiplier = 2.0f;  // impulse scaling when struck
    public float waterDrag = 1.5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.drag = waterDrag;
    }

    void FixedUpdate()
    {
        // simple buoyancy toward water surface (y = 0)
        if (transform.position.y < 0f)
        {
            float depth = Mathf.Clamp01(-transform.position.y / 4f);
            rb.AddForce(Vector3.up * buoyancyForce * depth, ForceMode.Acceleration);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Player") || col.collider.CompareTag("AI"))
        {
            Vector3 impulse = col.rigidbody.velocity * hitMultiplier;
            rb.AddForce(impulse, ForceMode.Impulse);
        }
    }
}