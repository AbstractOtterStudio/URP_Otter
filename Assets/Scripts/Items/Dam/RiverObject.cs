using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RiverObject : MonoBehaviour
{
    public RiverObjectType objectType = RiverObjectType.Material;

    [Header("Spawn physics")]
    public float waterfallDownSpeed = 8f;         // initial vertical drop
    public float angularTumble = 60f;             // random spin while falling

    [Header("Floating physics")]
    public float flowSpeed = 2f;                  // horizontal drift speed on water
    public float buoyancyOffset = 0.3f;           // keeps object slightly above water surface

    Rigidbody rb;
    bool floating = false;

    public void Initialize(float flowSpeedMultiplier)
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.velocity = Vector3.down * waterfallDownSpeed;
        rb.angularVelocity = Random.insideUnitSphere * angularTumble;
        flowSpeed *= flowSpeedMultiplier;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!floating && other.CompareTag("Water"))
        {
            StartFloating();
        }
    }

    void StartFloating()
    {
        floating = true;
        rb.useGravity = false;
        rb.angularVelocity = Vector3.zero;
        //Debug.Log($"Floating! Flow = {Dam_WaterSurface.Instance.flowDirection}");
        // small splash VFX here if desired
    }

    void FixedUpdate()
    {
        if (!floating) return;
        // maintain height
        Vector3 pos = rb.position;
        pos.y = Dam_WaterSurface.Instance.height + buoyancyOffset;
        rb.MovePosition(pos + Dam_WaterSurface.Instance.flowDirection.normalized * flowSpeed * Time.fixedDeltaTime);
    }
}
