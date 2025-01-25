using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameManager : SingletonBase<GameManager>
{        
    [SerializeField]
    private float oneDayTime = GlobalSetting.oneDayTime;
    [SerializeField]
    private float curTime;
    [SerializeField]
    private DayState dayState;

    [SerializeField]
    private PlayerStateController playerState;
    [SerializeField]
    private bool isGameAction;

    private float dayTime = GlobalSetting.dayTime;
    private float nightTime = GlobalSetting.nightTime;


    public void Init()
    {
        if(instance == null) { instance = this; }    
    }

    void Update()
    {
        GameOverDetect();
        TimeLapse();
        PostProcessingManager.instance.DayPassing(curTime);
    }

    #region Time Lapse
    public float GetCurTime() 
    {
        return curTime;
    }

    public DayState GetDayState() 
    {
        return dayState;
    }

    public float GetDayTime()
    {
        return oneDayTime;
    }

    /// <summary>
    /// Record Time Lapse and Change Day State
    /// </summary>
    private void TimeLapse() 
    {
        if (curTime <= dayTime && dayState != DayState.Day)
        {
            dayState = DayState.Day;
        }
        else if(curTime > dayTime && curTime < oneDayTime && dayState != DayState.Night)
        {
            dayState = DayState.Night;
        }
        if (playerState.PlayerAniState == PlayerInteractAniState.Sleep)
        {
            Time.timeScale = 5;
        }
        else
        {
            Time.timeScale = 1;
        }
        curTime += Time.deltaTime;

        if (curTime >= oneDayTime) {
            // MapAnimalSpawner.RespawnAnimals();
            curTime = 0;
        }        
    }
    #endregion

    #region Game Logic
    private void GameOverDetect() 
    {
       
    }
    public void GameStart()
    {
        Debug.Log("Start Game");
        isGameAction = true;
        Action onFadeInFinish = SetPlayerState;
        EventCenter.Broadcast(GameEvents.FadeIn, onFadeInFinish);
        EventCenter.Broadcast(GameEvents.ShowCharacterInfo);
    }
    void SetPlayerState()
    {
        //Player Can Move After Fadein finished
    }
    /// <summary>
    /// Game Over Logic
    /// </summary>
    private void GameOver() {
        Debug.Log("Game Over !");
        isGameAction = false;
        EventCenter.Broadcast(GameEvents.ShowDeathPage);        
        //Application.Quit();
    }
    #endregion

    public bool GetGameAction()
    {
        return isGameAction;
    }
}
