using UnityEngine;
using System.Collections;

public class SfxBus : MonoBehaviour
{
    public static SfxBus Instance { get; private set; }

    [Header("Audio Sources / Clips")]
    public AudioSource oneShot;                 // 建议拖一个 AudioSource 进来
    public AudioClip sfxSteal;                  // 抢断成功
    public AudioClip sfxStealFail;              // 抢断失败/无敌
    public AudioClip sfxKick;                   // 普通踢/传
    public AudioClip sfxShoot;                  // 射门（力度高或AI Shoot）
    
    [Header("Water Splash")]
    public ParticleSystem splashPrefab;         // 水花预制体（将根据高度缩放）
    public float splashMinScale = 0.7f;
    public float splashMaxScale = 2.2f;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!oneShot) oneShot = gameObject.AddComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
    }

    public void PlayKick(bool isShot = false, float power01 = 0.5f, Vector3? at = null)
    {
        if (oneShot)
        {
            oneShot.PlayOneShot(isShot && sfxShoot ? sfxShoot : sfxKick);
        }
        if (splashPrefab != null && at.HasValue)
        {
            var ps = Instantiate(splashPrefab, at.Value, Quaternion.identity);
            float s = Mathf.Lerp(splashMinScale, splashMaxScale, Mathf.Clamp01(power01));
            ps.transform.localScale = Vector3.one * s;
            ps.Play();
            Destroy(ps.gameObject, 3f);
        }
    }

    public void PlaySteal(bool success, Vector3? at = null)
    {
        if (oneShot)
        {
            oneShot.PlayOneShot(success ? sfxSteal : sfxStealFail);
        }
        if (success && at.HasValue && splashPrefab)
        {
            // 成功抢断也来一点小水花
            var ps = Instantiate(splashPrefab, at.Value, Quaternion.identity);
            ps.transform.localScale = Vector3.one * 1.1f;
            ps.Play();
            Destroy(ps.gameObject, 2.5f);
        }
    }

    public void DoSlowMo(float timeScale = 0.6f, float duration = 0.18f)
    {
        StartCoroutine(CoSlowMo(timeScale, duration));
    }

    IEnumerator CoSlowMo(float ts, float dur)
    {
        float old = Time.timeScale;
        Time.timeScale = Mathf.Clamp(ts, 0.05f, 1f);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, dur));
        Time.timeScale = old;
    }
}
