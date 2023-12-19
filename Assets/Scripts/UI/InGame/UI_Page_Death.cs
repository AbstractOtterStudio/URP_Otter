using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI_Page_Death : UIBase
{
    CanvasGroup canvasGroup;
    Button retryBtn;
    Button menuBtn;
    Text gameTimeText;
    Text consumeCountText;
    Text processText;

    public override void Init()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        retryBtn = transform.Find("Btn_Retry").GetComponent<Button>();
        menuBtn = transform.Find("Btn_Menu").GetComponent<Button>();

        gameTimeText = transform.Find("GameTime/Txt_GameTime").GetComponent<Text>();
        consumeCountText = transform.Find("TotalConsume/Txt_Consume").GetComponent<Text>();
        processText = transform.Find("GameProcess/Txt_Process").GetComponent<Text>();

        retryBtn.onClick.AddListener(OnRetryBtnClicked);
        menuBtn.onClick.AddListener(OnMenuBtnClicked);

        EventCenter.AddListener(GameEvents.ShowDeathPage, Show);
        //EventCenter.AddListener(GameEvents.HideDeathPage, Hide);

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    void OnDestroy()
    {
        EventCenter.RemoveListener(GameEvents.ShowDeathPage, Show);
        //EventCenter.RemoveListener(GameEvents.HideDeathPage, Hide);
    }

    void Show()
    {
        StartCoroutine("ShowingDeathPage");
    }

    //void Hide()
    //{
    //    Action hideDeathPage = () =>
    //    {
    //        canvasGroup.alpha = 0f;
    //        canvasGroup.blocksRaycasts = false;
    //    };
    //    EventCenter.Broadcast(GameEvents.FadeIn, hideDeathPage);        
    //}    

    IEnumerator ShowingDeathPage()
    {
        while(canvasGroup.alpha < 0.95f)
        {
            canvasGroup.alpha += Time.deltaTime;
            if(canvasGroup.alpha >= 0.95f)
            {
                canvasGroup.alpha = 1;
            }
            yield return null;
        }


        canvasGroup.blocksRaycasts = true;
        yield return null;
    }

    void OnRetryBtnClicked()
    {
        Action onRetryBtnClicked = () => SceneManager.LoadScene(0);
        EventCenter.Broadcast(GameEvents.FadeOut, onRetryBtnClicked);
    }

    void OnMenuBtnClicked()
    {
        Action onMenuBtnClicked = () => SceneManager.LoadScene(0);
        EventCenter.Broadcast(GameEvents.FadeOut, onMenuBtnClicked);
    }
}
