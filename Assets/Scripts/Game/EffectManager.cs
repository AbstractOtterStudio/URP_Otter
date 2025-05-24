using System.Collections;
using Crest;
using UnityEngine;

public class EffectManager : Singleton<EffectManager>
{
    [SerializeField]
    float _fadeDuration = 1.0f;

    OceanRenderer _oceanRenderer = null;

    IEnumerator _fadeCoroutine = null;

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        _oceanRenderer = OceanRenderer.Instance;
    }

    public void SetUnderwaterEffect(bool isUnderwater)
    {
        IEnumerator FadeCoroutine()
        {

            float targetFade = isUnderwater ? 0.5f : 0.0f;
            float fadeSpeed = 0.5f / _fadeDuration;
            float currentFade;
            do
            {
                currentFade = _oceanRenderer.OceanMaterial.GetFloat("_Fade");
                float fadeSpeedSign = currentFade < targetFade ? 1.0f : -1.0f;
                float newFade = currentFade + fadeSpeed * fadeSpeedSign * Time.deltaTime;
                _oceanRenderer.OceanMaterial.SetFloat("_Fade", newFade);
                yield return null;
            } while (Mathf.Abs(currentFade - targetFade) > 0.01f);
            _oceanRenderer.OceanMaterial.SetFloat("_Fade", targetFade);
        }

        if (_oceanRenderer == null)
        {
            Debug.LogError("OceanRenderer not found; cannot set underwater effect");
            return;
        }

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = FadeCoroutine();
        StartCoroutine(_fadeCoroutine);
    }
}