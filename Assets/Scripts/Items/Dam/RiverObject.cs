using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ItemProperties))]
public class RiverObject : MonoBehaviour
{
    public RiverObjectType objectType = RiverObjectType.Material;

    [Header("Spawn physics")]
    public float waterfallDownSpeed = 8f;
    public float angularTumble = 60f;

    [Header("Floating physics")]
    public float flowSpeed = 2f;
    public float buoyancyOffset = 0.3f;

    Rigidbody rb;
    bool floating = false;
    bool isHeld = false;               // ★ 新增：被玩家拿在手里

    public void Initialize(float flowSpeedMultiplier)
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.velocity = Vector3.down * waterfallDownSpeed;
        rb.angularVelocity = Random.insideUnitSphere * angularTumble;
        flowSpeed *= flowSpeedMultiplier;
        floating = false;
        isHeld = false;
    }

    // 由 Catchable 调用
    public void SetHeld(bool held)
    {
        isHeld = held;
        if (held) floating = false; // 拿在手里就不再执行漂浮
    }

    void OnTriggerEnter(Collider other)
    {
        if (isHeld) return; // 手持状态不响应水
        if (!floating && other.CompareTag("Water"))
        {
            StartFloating();
        }
    }

    void StartFloating()
    {
        floating = true;
        if (!rb) rb = GetComponent<Rigidbody>();
        rb.useGravity = false;           // 漂浮阶段不用重力
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (!floating || isHeld) return;

        // 维持在水面高度，并沿流向漂移
        Vector3 pos = rb.position;
        pos.y = Dam_WaterSurface.Instance.height + buoyancyOffset;
        rb.MovePosition(pos + Dam_WaterSurface.Instance.flowDirection.normalized
                        * flowSpeed * Time.fixedDeltaTime);
    }
}
