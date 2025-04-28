using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Water Physics")]
    public float waterLevel   = 0.1f;
    public float waterDrag    = 2.8f;
    public float airDrag      = 0.025f;
    public float maxDepth     = 0f;

    [Header("Hit Response (speed‑based)")]
    public float hitMultiplier   = 3f;
    public float upwardFactor    = 0.4f;
    public float minHitSpeed     = 2.5f;
    public float randomCone      = 18f;

    [Header("Wall Bounce (power)")]
    public float wallBounceDamping = 0.9f;  // 每次反弹速度保留率
    public float minWallBounceSpeed = 2.5f;   // 最低回弹速度
    public float wallBounceBoost    = 2f;   // 额外速度增益 (m/s)

    Rigidbody rb;
    float     radius;

    void Awake()
    {
        rb     = GetComponent<Rigidbody>();
        radius = GetComponent<SphereCollider>().radius * transform.localScale.x;
        rb.drag = airDrag;
        rb.useGravity = true;
    }

    void FixedUpdate()
    {
        // --- 浮力 & 阻尼 ---
        float d = waterLevel - transform.position.y;
        float subm = d <= -radius ? 0f : d >= radius ? 1f : (d + radius) / (2f * radius);
        if (subm > 0f)
        {
            rb.AddForce(Vector3.up * Mathf.Abs(Physics.gravity.y) * rb.mass * subm, ForceMode.Force);
        }
        rb.drag = Mathf.Lerp(airDrag, waterDrag, subm);

        if (transform.position.y < maxDepth)
        {
            Vector3 p = transform.position; p.y = maxDepth; transform.position = p;
            if (rb.velocity.y < 0) rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }
    }

    // ---------- 撞击玩家 / AI ----------
    void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Player") || col.collider.CompareTag("AI"))
        {
            float speed = Mathf.Max(col.rigidbody.velocity.magnitude, minHitSpeed);
            Vector3 dir = new Vector3(col.rigidbody.velocity.x, 0, col.rigidbody.velocity.z);
            if (dir.sqrMagnitude < 0.01f) dir = col.transform.forward;
            dir += Vector3.up * (speed * upwardFactor);
            dir = Quaternion.Euler(Random.Range(-randomCone, randomCone), Random.Range(-randomCone, randomCone), 0) * dir;
            rb.AddForce(dir.normalized * speed * hitMultiplier, ForceMode.Impulse);
            return;
        }

        // 初次碰墙立即处理
        if (IsWall(col.collider)) ApplyWallBounce(col.contacts[0].normal);
    }

    // 连续被顶在墙上时，每帧保持反弹
    void OnCollisionStay(Collision col)
    {
        if (IsWall(col.collider))
        {
            // 当速度几乎指向墙内或几乎为零时强制反弹
            Vector3 normal = AverageNormal(col);
            if (Vector3.Dot(rb.velocity, -normal) <= 0.2f)
                ApplyWallBounce(normal);
        }
    }

    bool IsWall(Collider c) => c.CompareTag("PoolWall") || c.gameObject.layer == LayerMask.NameToLayer("PoolWall");

    Vector3 AverageNormal(Collision col)
    {
        Vector3 n = Vector3.zero;
        foreach (var ct in col.contacts) n += ct.normal;
        return n.sqrMagnitude > 0 ? n.normalized : Vector3.up;
    }

    void ApplyWallBounce(Vector3 wallNormal)
    {
        Vector3 reflDir = Vector3.Reflect(rb.velocity.normalized, wallNormal);
        float speedIn   = Mathf.Max(rb.velocity.magnitude, minWallBounceSpeed);
        float newSpeed  = speedIn * wallBounceDamping + wallBounceBoost;
        rb.velocity = reflDir * newSpeed;
        rb.position += wallNormal * 0.05f; // 防止卡墙
    }
}