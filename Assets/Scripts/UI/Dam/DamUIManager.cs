using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class DamUIManager : MonoBehaviour
{
    public Image tipIcon;
    public Sprite okSprite, badSprite;
    public float showDuration = 1f;
    float timer;
    public void ShowTip(bool positive)
    {
        tipIcon.sprite = positive ? okSprite : badSprite;
        tipIcon.enabled = true;
        timer = showDuration;
    }
    void Update()
    {
        if (!tipIcon.enabled) return;
        timer -= Time.deltaTime;
        if (timer <= 0) tipIcon.enabled = false;
    }
}
