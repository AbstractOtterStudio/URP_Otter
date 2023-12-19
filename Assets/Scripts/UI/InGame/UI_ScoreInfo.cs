using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ScoreInfo : UIBase
{
    CanvasGroup canvasGroup;
    Text scoreCountText;
    const string scorePattern = "x ";

    public override void Init()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        scoreCountText = transform.Find("Txt_ScoreCount").GetComponent<Text>();

        scoreCountText.text = scorePattern + "";

        EventCenter.AddListener<int>(GameEvents.UpdateScore, OnScoreCountChanged);

        HideScore();
    }

    void OnDestroy()
    {
        EventCenter.RemoveListener<int>(GameEvents.UpdateScore, OnScoreCountChanged);
    }

    void ShowScore(int score)
    {        
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1;
    }

    void HideScore()
    {        
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    void OnScoreCountChanged(int score)
    {
        scoreCountText.text = scorePattern + score.ToString();
        ShowScore(score);
    }

}
