using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeInOut : MonoBehaviour
{

    public float fadeInDuration = 5f;
    public float fadeOutDuration = 5f;
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
        //wait 
        yield return new WaitForSeconds(displayTime);

        //fade out
        time = 0f;
        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, time / fadeOutDuration);
            SetAlphaAllSprites(alpha);
            yield return null;
        }
    }

    public void Awake()
    {
        //StartCoroutine(FadeIn());
    }
}
