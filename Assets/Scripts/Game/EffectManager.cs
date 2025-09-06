using System;
using System.Collections;
using Crest;
using UnityEngine;

public class EffectManager : Singleton<EffectManager>
{
    public class ShaderState
    {
        public ShaderState(float fade, Vector3 fogDensity)
        {
            this.fade = fade;
            this.fogDensity = fogDensity;
        }

        public ShaderState(OceanRenderer oceanRenderer)
        {
            From(oceanRenderer);
        }

        public void Apply(OceanRenderer oceanRenderer)
        {
            oceanRenderer.OceanMaterial.SetFloat("_Fade", fade);
            oceanRenderer.OceanMaterial.SetVector("_DepthFogDensity", fogDensity);
        }

        public void From(OceanRenderer oceanRenderer)
        {
            fade = oceanRenderer.OceanMaterial.GetFloat("_Fade");
            fogDensity = oceanRenderer.OceanMaterial.GetVector("_DepthFogDensity");
        }

        public bool Equals(ShaderState other)
        {
            return Mathf.Abs(fade - other.fade) < 0.01f && Vector3.SqrMagnitude(fogDensity - other.fogDensity) < 0.0001f;
        }

        public float fade;
        public Vector3 fogDensity;
    }

    [SerializeField]
    [UnityEngine.Range(0.0f, 1.0f)]
    float _underwaterTargetFade = 0.24f;
    [SerializeField]
    Vector3 _underwaterTargetFogDensity = new Vector3(0.05f, 0.05f, 0.05f);

    [SerializeField]
    float _fadeDuration = 1.0f;

    OceanRenderer _oceanRenderer = null;

    IEnumerator _fadeCoroutine = null;

    ShaderState _initialShaderState = null;

    ShaderState _underwaterShaderState = null;

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        _oceanRenderer = OceanRenderer.Instance;

        if (_oceanRenderer == null)
        {
            Debug.LogError("OceanRenderer not found; cannot set underwater effect");
            return;
        }

        _initialShaderState = new ShaderState(_oceanRenderer);
        _underwaterShaderState = new ShaderState(_underwaterTargetFade, _underwaterTargetFogDensity);
    }

    void OnDisable()
    {
        if (_initialShaderState != null)
        {
            _initialShaderState.Apply(_oceanRenderer);
        }
    }

    public void SetUnderwaterEffect(bool isUnderwater)
    {
        IEnumerator FadeCoroutine()
        {
            ShaderState currentShaderState = new ShaderState(_oceanRenderer);
#if UNITY_EDITOR
            ShaderState targetShaderState = isUnderwater ? new ShaderState(_underwaterTargetFade, _underwaterTargetFogDensity) : _initialShaderState;
#else
            ShaderState targetShaderState = isUnderwater ? _underwaterShaderState : _initialShaderState;
#endif

            float fadeSpeed = (targetShaderState.fade - currentShaderState.fade) / _fadeDuration;
            Vector3 fogDensitySpeed = (targetShaderState.fogDensity - currentShaderState.fogDensity) / _fadeDuration;
            do
            {
                currentShaderState.fade += fadeSpeed * Time.deltaTime;
                currentShaderState.fogDensity += fogDensitySpeed * Time.deltaTime;
                currentShaderState.Apply(_oceanRenderer);
                yield return null;
            } while (!currentShaderState.Equals(targetShaderState));

            targetShaderState.Apply(_oceanRenderer);
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

#if UNITY_EDITOR
    static ShaderState s_initialShaderState = null;
    public static void SetUnderwaterEffectImmediate(bool isUnderwater)
    {
        if (isUnderwater)
        {
            if (s_initialShaderState == null)
            {
                s_initialShaderState = new ShaderState(OceanRenderer.Instance);
            }
            new ShaderState(Instance._underwaterTargetFade, Instance._underwaterTargetFogDensity).Apply(OceanRenderer.Instance);
        }
        else
        {
            if (s_initialShaderState != null)
            {
                s_initialShaderState.Apply(OceanRenderer.Instance);
                s_initialShaderState = null;
            }
        }
    }
#endif
}