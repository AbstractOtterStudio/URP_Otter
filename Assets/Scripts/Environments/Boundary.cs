using UnityEngine;

/// <summary>Attach to 4 墙的 BoxCollider（Is Trigger）</summary>
public class Boundary : MonoBehaviour
{
    [Tooltip("0-1，碰墙后保留的水平速度比例")]
    public float damping = 0.9f;

    void OnTriggerEnter(Collider other)  => Bounce(other);
    void OnTriggerStay (Collider other)  => Bounce(other);

    void Bounce(Collider col)
    {
        Ball ball = col.GetComponent<Ball>();
        if (!ball) return;

        Vector3 pos = ball.Pos;
        Vector3 vel = ball.Rb.velocity;

        // 通过比较位置 → 判断是哪一面墙；反转对应速度分量
        float halfX = transform.lossyScale.x * 0.5f;
        float halfZ = transform.lossyScale.z * 0.5f;

        bool bounced = false;
        if (pos.x >  halfX && vel.x > 0) { vel.x = -vel.x; bounced = true; }
        if (pos.x < -halfX && vel.x < 0) { vel.x = -vel.x; bounced = true; }
        if (pos.z >  halfZ && vel.z > 0) { vel.z = -vel.z; bounced = true; }
        if (pos.z < -halfZ && vel.z < 0) { vel.z =  vel.z * -1; bounced = true; }

        if (bounced)
        {
            ball.Rb.velocity = vel * damping;
            // 把球轻推回场内 0.05 m
            ball.Pos = new Vector3(
                Mathf.Clamp(pos.x, -halfX + .05f, halfX - .05f),
                pos.y,
                Mathf.Clamp(pos.z, -halfZ + .05f, halfZ - .05f));
        }
    }
}

