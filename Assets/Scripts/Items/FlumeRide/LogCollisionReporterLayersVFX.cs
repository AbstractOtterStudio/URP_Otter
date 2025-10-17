using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class LogCollisionReporterLayersVFX : MonoBehaviour
{
    [Header("计数规则（用 Layer）")]
    [Tooltip("所有会累计次数的层（把 Obstacle、两岸墙等都勾上）。")]
    public LayerMask dangerLayers;   // 例：Obstacle、RiverWall 等

    [Tooltip("障碍物所在层（仅用于触发VFX/SFX）。")]
    public string obstacleLayerName = "Obstacle";

    [Header("特效/音效")]
    public ParticleSystem obstacleHitVfxPrefab; // 可为空
    public AudioClip obstacleHitSfx;          // 可为空
    public float sfxVolume = 0.9f;

    [Tooltip("两次计数的最小间隔(秒)，避免一次长时间挤压重复加。")]
    public float minInterval = 0.25f;

    int obstacleLayer;
    float lastHitTime = -999f;
    AudioSource asrc;

    void Awake()
    {
        obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);
        asrc = GetComponent<AudioSource>();
        if (!asrc) asrc = gameObject.AddComponent<AudioSource>();
        asrc.playOnAwake = false;
        asrc.spatialBlend = 1f; // 3D 声音
    }

    void OnCollisionEnter(Collision c)
    {
        if (Time.time - lastHitTime < minInterval) return;

        int otherLayer = c.collider.gameObject.layer;
        int otherMaskBit = 1 << otherLayer;

        // 1) 在危险层里：计数
        if ((dangerLayers.value & otherMaskBit) != 0)
        {
            lastHitTime = Time.time;
        }

        // 2) 若是障碍物层：播特效+音效
        if (otherLayer == obstacleLayer)
        {
            Vector3 pos = c.contacts != null && c.contacts.Length > 0 ? c.contacts[0].point : c.collider.bounds.ClosestPoint(transform.position);

            if (obstacleHitVfxPrefab)
            {
                ParticleSystem ps = Instantiate(obstacleHitVfxPrefab, pos, Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, 3f);
            }

            if (obstacleHitSfx && asrc)
            {
                asrc.transform.position = pos;
                asrc.PlayOneShot(obstacleHitSfx, sfxVolume);
            }
        }
    }
}
