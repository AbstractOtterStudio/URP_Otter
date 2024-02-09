using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteTransparencyToggle : MonoBehaviour
{
    public float activeTransparency = 1.0f; // Fully opaque when active
    public float inactiveTransparency = 0f; // Semi-transparent when inactive

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // Set sprite to be fully opaque when GameObject is enabled
        //SetTransparency(activeTransparency);
        StartCoroutine(FadeIn());
    }

    private void OnDisable()
    {
        // Set sprite to be semi-transparent when GameObject is disabled
        //SetTransparency(inactiveTransparency);
        StartCoroutine(FadeOut());
    }

    private void SetTransparency(float transparency)
    {
        Color color = spriteRenderer.color;
        color.a = transparency;
        spriteRenderer.color = color;
    }

    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1f;
    public float displayTime = 5f;
    // Start is called before the first frame update
    public void SetAlphaAllSprites(float alpha) {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in sprites)
        {
            Color color = sprite.color;
            color.a = alpha;
            sprite.color = color;
        }
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

    IEnumerator FadeIn()
    {
        //fade in
        float time = 0f;
        while (time < fadeInDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, time / fadeInDuration);
            SetAlphaAllSprites(alpha);
            yield return null;
        }
    }

    IEnumerator FadeOut()
    {
        //fade out
        float time = 0f;
        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, time / fadeOutDuration);
            SetAlphaAllSprites(alpha);
            yield return null;
        }
    }
}