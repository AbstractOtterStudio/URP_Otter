using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Ball : MonoBehaviour
{
     [Header("Ground Physics")]
    [Min(0)] public float friction = 1.5f;      // ↓ from 3 → travels further
    [SerializeField] string groundMaskName = "Ground";

    [Header("Player Bounce")]
    [Tooltip("Velocity imparted when the user‑controlled player体碰撞球 (m/s)")]
    public float playerBounceSpeed = 18f;        // slight buff

    [Header("Motion Particles")]
    public ParticleSystem[] motionParticles;
    public AnimationCurve speedSize;
    public Gradient speedColor;

    public delegate void BallLaunched(float flightTime, float velocity, Vector3 initial, Vector3 target);
    public event BallLaunched OnBallLaunched;
    public event BallLaunched OnBallShot;

    public Rigidbody      Rb   { get; private set; }
    public SphereCollider Col  { get; private set; }
    public Component Owner;

    public float speed;
    int   groundMask; float rayDist;

    void Awake()
    {
        Rb  = GetComponent<Rigidbody>();
        Col = GetComponent<SphereCollider>();
        groundMask = LayerMask.GetMask(groundMaskName);
        rayDist = Col.radius + 0.05f;
        Rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        Rb.useGravity = false;

        // 初始化 size 曲线
        speedSize = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.5f, 0.6f),
            new Keyframe(1f, 1.2f)
        );

        // 初始化亮度渐变（灰度）
        speedColor = new Gradient();
        speedColor.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.9f, 0.9f, 0.9f), 0f),  // 白灰：慢速
                new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 0.5f), // 中速
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)                   // 黑色：高速
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );

    }

    void FixedUpdate()
    {
        Vector3 v = Rb.velocity; 
        v.y = 0;
        if (v.sqrMagnitude > 0.01f)
            Rb.AddForce(-v.normalized * friction, ForceMode.Acceleration);

        //UpdateMotionEffects(v);
    }

    // void UpdateMotionEffects(Vector3 velocity)
    // {
    //     if (motionParticles == null || motionParticles.Length == 0) return;

    //     speed = velocity.magnitude;
    //     bool show = speed > 4.5f;
    //     Vector3 dir = velocity.normalized;

    //     foreach (var ps in motionParticles)
    //     {
    //         if (!ps) continue;

    //         if (!show)
    //         {
    //             if (ps.isPlaying) ps.Stop();
    //             continue;
    //         }
    //         else
    //         {
    //             if (!ps.isPlaying) ps.Play();
    //         }

    //         if (dir.sqrMagnitude > 0.01f)
    //             ps.transform.rotation = Quaternion.LookRotation(-dir);

    //         var main = ps.main;

    //         float normalizedSpeed = Mathf.Clamp01(speed / 10f);
    //         float size = speedSize != null ? speedSize.Evaluate(normalizedSpeed) : 1f;
    //         main.startSize = Mathf.Max(0.1f, size);
    //         Debug.Log($"Speed: {speed} → Size: {size}");

    //         Color color = speedColor.Evaluate(normalizedSpeed);
    //         main.startColor = color;

    //     }
    // }


    // ───────── Collision with players─────────
    void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Player"))
        {
            Vector3 dir = (Pos - new Vector3(col.transform.position.x, 0, col.transform.position.z)).normalized;
            Rb.velocity = dir * playerBounceSpeed;

            Owner = col.collider.GetComponent<PlayerController>();
            return;
        }

        if (col.collider.GetComponent<Boundary>() && !col.collider.isTrigger)
        {
            ContactPoint contact = col.contacts[0];

            // 原始反射方向
            Vector3 bounceDir = Vector3.Reflect(Rb.velocity.normalized, contact.normal);

            // 兜底：如果方向太小，就用“从碰撞点推开球”的方向
            if (bounceDir.sqrMagnitude < 0.01f)
            {
                bounceDir = (Pos - contact.point).normalized;
                if (bounceDir.sqrMagnitude < 0.01f) // 还是太小，就用反法线
                    bounceDir = contact.normal;
            }

            Rb.velocity = bounceDir * 10f;
            return;
        }

        var wp = col.collider.GetComponent<WaterPlayer>();
        if (wp)
        {
            if (Rb.velocity.magnitude > 0.5f &&
                (Owner == null || (Owner is WaterPlayer owp && wp.isTeammate != owp.isTeammate)))
            {
                Rb.velocity = Vector3.zero;
            }

            Owner = wp;
        }
    }


    public void Kick(Vector3 tgt, float power)
    {
        Vector3 dir = (tgt - Pos).normalized;
        Rb.velocity = dir * power;
        OnBallLaunched?.Invoke(0, power, Pos, tgt);
        Owner = null;
    }

    // ───────── utils ─────────
    public Vector3 Pos
    {
        get => new Vector3(transform.position.x, 0, transform.position.z);
        set => transform.position = new Vector3(value.x, transform.position.y, value.z);
    }

    /// <summary>Find initial speed U (m/s) so that after friction it reaches vEnd at target.</summary>
    public float FindPower(Vector3 from, Vector3 to, float vEnd) =>
        Mathf.Sqrt(vEnd * vEnd - 2 * -friction * Vector3.Distance(from, to));

    public float TimeToCover(Vector3 from, Vector3 to, float u) => Vector3.Distance(from, to) / u;
}