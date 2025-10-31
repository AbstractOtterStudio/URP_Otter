using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameUIController : MonoBehaviour
{
    public static GameUIController I { get; private set; }

    [Header("Refs")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI scoreText;
    public Image borderImage;                 // 全屏 UI Image（Raycast Target 关掉）
    public Color borderEnemy = new Color(1f, .2f, .2f, .6f);
    public Color borderAlly  = new Color(.2f, 1f, .2f, .6f);
    public Color borderNone  = new Color(0, 0, 0, 0);

    [Header("Pulse")]
    public float last10sPulseScale = 1.25f;
    public float pulseSpeed = 4.0f;
    public float scorePunchScale = 1.35f;
    public float scorePunchTime = 0.22f;

    int prevA = -1, prevB = -1;
    Coroutine pulseCR;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        if (borderImage) borderImage.color = borderNone;
    }

    public void SetTime(float secondsLeft)
    {
        secondsLeft = Mathf.Max(0f, secondsLeft);
        int m = Mathf.FloorToInt(secondsLeft / 60f);
        int s = Mathf.FloorToInt(secondsLeft % 60f);
        if (timeText) timeText.text = $"{m:00}:{s:00}";

        // 最后 10 秒开始脉动
        if (secondsLeft <= 10.0001f)
        {
            if (pulseCR == null) pulseCR = StartCoroutine(CoPulseTime());
        }
        else
        {
            if (pulseCR != null) { StopCoroutine(pulseCR); pulseCR = null; }
            if (timeText) timeText.rectTransform.localScale = Vector3.one;
        }
    }

    IEnumerator CoPulseTime()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * pulseSpeed;
            float a = (Mathf.Sin(t) * 0.5f + 0.5f); // 0..1
            float sc = Mathf.Lerp(1f, last10sPulseScale, a);
            if (timeText) timeText.rectTransform.localScale = new Vector3(sc, sc, 1f);
            yield return null;
        }
    }

    public void SetScore(int my, int opp)
    {
        if (!scoreText) return;
        scoreText.text = $"{my} : {opp}";
        if (prevA >= 0 && (my != prevA || opp != prevB))
            StartCoroutine(CoPunch(scoreText.rectTransform, scorePunchScale, scorePunchTime));
        prevA = my; prevB = opp;
    }

    IEnumerator CoPunch(RectTransform rt, float punch, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f + (punch - 1f) * Mathf.Sin(Mathf.PI * (t / dur));
            rt.localScale = new Vector3(k, k, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // possession:  1=对方持球（红边），-1=我方持球（绿边），0=无边
    public void SetBorderByPossession(int possession)
    {
        if (!borderImage) return;
        Color c = possession == 1 ? borderEnemy : (possession == -1 ? borderAlly : borderNone);
        borderImage.color = c;
    }
}
