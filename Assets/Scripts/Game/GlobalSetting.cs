using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalSetting
{
    #region KeyCode
    public const KeyCode ForwardKey = KeyCode.UpArrow;
    public const KeyCode BackwardKey = KeyCode.DownArrow;
    public const KeyCode LeftKey = KeyCode.LeftArrow;
    public const KeyCode RightKey = KeyCode.RightArrow;
    public const KeyCode DiveKey = KeyCode.Space;
    public const KeyCode AddSpeedKey = KeyCode.LeftShift;
    public const KeyCode InterectKey = KeyCode.Z;
    public const KeyCode EatOrKnockKey = KeyCode.X;
    #endregion
    
    #region Player Const Value
    //海獭数值

    //Power
    public const float playerInitPower = 120;
    public const float playerUpLevelPower = 15;
    public const float timelyPowerConsume = 30;

    //Oxygen
    public const float playerInitOxy = 4 + playerDiveSepth / playerDiveSpeed + 1;
    public const float playerUpLevelOxy = 1;
    public const float timelyOxyConsume = 1;
    public const float playerDiveSepth = 1;
    public const float playerDiveSpeed = 2;

    //Full
    public const float playerInitFull = 100;
    public const float playerHungryFull = 60;
    public const float playerAgonyFull = 0;
    public const float timelyFullConsume = 1f;

    //Clean
    public const float playerInitClean = 3;
    public const float dirtyClean = 2;
    public const float dirtyTwiceClean = 1;
    public const float dangerClean = 0;
    public const float onceDirtyConsume = 1;

    //Movement
    public const float playerInitSpeed = 5;
    public const float playerAddSpeedRatio = 1.5f;
    public const float playerDirtySpeedRatio = 0.05f;
    public const float playerDangerCleanSpeedRatio = 0.1f;
    public const float playerAgonyFullSpeedRatio = 0.3f;
    

    
    #endregion

    #region Environment Const Value
    public const float waterFallHeight = 2;
    public const float oneDayTime = 200;
    public const float dayTime = 120;
    public const float nightTime = 80;

    #endregion
}
