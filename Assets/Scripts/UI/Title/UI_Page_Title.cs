using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Page_Title : UIBase
{

    CanvasGroup canvasGroup;
    Button startBtn;
    //Button optionBtn;

    public override void Init()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        startBtn = transform.Find("Btn_Start").GetComponent<Button>();
        //optionBtn = transform.Find("Btn_Option").GetComponent<Button>();

        startBtn.onClick.AddListener(OnStartBtnClicked);
        //optionBtn.onClick.AddListener(OnOptionBtnClicked);

        EventCenter.AddListener(GameEvents.ShowTitle, ShowTitle);

        ShowTitle();
    }

    private void OnDestroy()
    {
        Debug.Log($"UI Title Have Been Destroyed");
        EventCenter.RemoveListener(GameEvents.ShowTitle, ShowTitle);
    }

    void ShowTitle()
    {
        Debug.Log("Title Showed");
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;        
    }

    void HideTitle()
    {
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    void OnStartBtnClicked()
    {
        HideTitle();

        //Fade out and prepare to start game        
        Action startAction = GameManager.instance.GameStart;
        EventCenter.Broadcast(GameEvents.FadeOut, startAction);                
    }

    void OnOptionBtnClicked()
    {
        HideTitle();
        //ShowOption
        EventCenter.Broadcast(GameEvents.ShowOption);
    }
}
