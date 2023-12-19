using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_CutScene : UIBase
{
    Image cutSceneImg;
    Material cutSceneMat;    

    Coroutine corou;
    [SerializeField] float cutSpeed = 1f;
    bool isSceneCutting { get { return corou != null; } }

    public override void Init()
    {
        cutSceneImg = GetComponent<Image>();
        cutSceneMat = cutSceneImg.material;

        cutSceneImg.raycastTarget = false;
        cutSceneMat.SetFloat("Fade", -1f);

        EventCenter.AddListener<Action>(GameEvents.FadeIn, FadeIn);
        EventCenter.AddListener<Action>(GameEvents.FadeOut, FadeOut);
    }

    void OnDestroy()
    {
        EventCenter.RemoveListener<Action>(GameEvents.FadeIn, FadeIn);
        EventCenter.RemoveListener<Action>(GameEvents.FadeOut, FadeOut);
    }

    void FadeIn(Action onFadeInFinishEvt)
    {
        PlayCutScene(false, onFadeInFinishEvt);
    }

    void FadeOut(Action onFadeOutFinishEvt)
    {
        Debug.Log("Fading Out");
        PlayCutScene(true, onFadeOutFinishEvt);
    }

    void PlayCutScene(bool isFadeOut, Action onFadeFinishedEvt)
    {
        if(isSceneCutting)
        {
            StopCoroutine("SceneCutting");
        }
        corou = StartCoroutine(SceneCutting(isFadeOut, onFadeFinishedEvt));
    }

    IEnumerator SceneCutting(bool isFadeOut, Action onFadeFinishedEvt)
    {
        float currentMatFade = cutSceneMat.GetFloat("Fade");

        if ((isFadeOut && currentMatFade >= 1f) ||
            (!isFadeOut && currentMatFade <= -1f))
        {
            corou = null;
            yield break;
        }

        if (isFadeOut)
        {
            while(currentMatFade < 1f)
            {
                currentMatFade += Time.deltaTime * cutSpeed;
                if(currentMatFade >= 0.95f)
                {
                    currentMatFade = 1f;
                }
                cutSceneMat.SetFloat("Fade", currentMatFade);
                yield return null;
            }            
        }
        else
        {
            while(currentMatFade > -1f)
            {
                currentMatFade -= Time.deltaTime * cutSpeed;
                if(currentMatFade < -0.05f)
                {
                    currentMatFade = -1f;
                }
                cutSceneMat.SetFloat("Fade", currentMatFade);
                yield return null;
            }
        }

        onFadeFinishedEvt?.Invoke();
        corou = null;
        yield return null;
    }


}
