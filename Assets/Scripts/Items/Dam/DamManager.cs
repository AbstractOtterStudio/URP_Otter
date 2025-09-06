using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class DamManager : MonoBehaviour
{
    [Range(0,1f)] public float progress = 0f;
    public float progressPerMaterial = 0.05f;
    public float damageAmount = 0.05f;

    [Header("UI")] public Slider progressBar;
    public UnityEvent<float> OnProgressChanged;
    public UnityEvent OnDamCompleted;

    void Start() => UpdateUI();

    public void AddMaterial()
    {
        progress = Mathf.Clamp01(progress + progressPerMaterial);
        UpdateUI();
        if (progress >= 1f) OnDamCompleted?.Invoke();
    }

    public void DamageDam()
    {
        progress = Mathf.Clamp01(progress - damageAmount);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (progressBar) progressBar.value = progress;
        OnProgressChanged?.Invoke(progress);
    }
}
