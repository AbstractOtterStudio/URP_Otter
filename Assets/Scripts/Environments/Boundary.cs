using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Current only for waterball game boundary
public class Boundary : MonoBehaviour
{
    [Header("Pool half-extents (world units)")]
    public float halfX = 15f;          // X 正负边界
    public float halfZ = 10f;          // Z 正负边界
    public float bounceDamping = 0.9f; // 0-1 反弹时保留的速度比例

    void OnTriggerExit(Collider other)
    {
        // ───────────── 球：反弹 ─────────────
        if (other.CompareTag("Ball"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb == null) return;

            Vector3 v   = rb.velocity;        // 当前速度
            Vector3 pos = other.transform.position;
            bool bounced = false;

            // 根据离开哪条边反转速度分量
            if (pos.x >  halfX && v.x >  0f) { v.x = -v.x; bounced = true; }
            if (pos.x < -halfX && v.x <  0f) { v.x = -v.x; bounced = true; }
            if (pos.z >  halfZ && v.z >  0f) { v.z = -v.z; bounced = true; }
            if (pos.z < -halfZ && v.z <  0f) { v.z = -v.z; bounced = true; }

            if (bounced)
            {
                rb.velocity = v * bounceDamping;

                // 轻推回池内，避免卡在边界反复触发
                pos.x = Mathf.Clamp(pos.x, -halfX + 0.05f, halfX - 0.05f);
                pos.z = Mathf.Clamp(pos.z, -halfZ + 0.05f, halfZ - 0.05f);
                other.transform.position = pos;
            }
        }
        else if (other.CompareTag("Player") || other.CompareTag("AI"))
        {
            Vector3 pos = other.transform.position;
            pos.x = Mathf.Clamp(pos.x, -halfX, halfX);
            pos.z = Mathf.Clamp(pos.z, -halfZ, halfZ);
            other.transform.position = pos;
        }
    }
}
