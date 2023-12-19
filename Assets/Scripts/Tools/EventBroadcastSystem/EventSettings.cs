//事件列表
public enum GameEvents
{
    //Title Event
    ShowTitle,
    ShowOption,    

    //In Game Event
    ShowCharacterInfo,
    UpdateHealth, //更新健康值

    UpdateScore, //更新收集物UI
    
    ShowToolTip, //顯示屏幕下方提示

    ShowCollectableInfo, //顯示收集物信息

    ShowButtonHint, //顯示按鍵提示
    HideButtonHint, //隱藏按鍵提示

    UpdateOxygen, //更新氧氣值
    UpdatePower, //更新耐力值
    HideOxygenAndPower, //隱藏氧氣及耐力值條

    ShowDeathPage, //顯示死亡結算畫面
    //HideDeathPage, //隱藏死亡結算畫面

    BecomeHappy, //開心
    BecomeConfuse, //疑惑
    BecomeShock, //驚訝
    BecomeGrowth, //成長
    BecomeTired, //沒力
    BecomeHungry, //餓了
    BecomeSleepy, //睡覺變強

    //過場
    FadeIn,
    FadeOut,
}

public delegate void CallBack();
public delegate void CallBack<T1>(T1 arg1);
public delegate void CallBack<T1, T2>(T1 arg1, T2 arg2);