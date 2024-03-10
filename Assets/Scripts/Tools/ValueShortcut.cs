using UnityEngine;
  
public static class ValueShortcut
{
    //struct類型數據修改捷徑
    #region Struct Shortcuts
    static Vector2 vector2;
    static Vector3 vector3;
    static Color color;  

    #region Vector2 Shortcut
    public static Vector2 X(this Vector2 value, float x)
    {
        vector2 = value;
        vector2.x = x;
        return vector2;           
    }
    public static Vector2 Y(this Vector2 value, float y)
    {
        vector2 = value;
        vector2.y = y;
        return vector2;
    }
    #endregion

    #region Vector3 Shortcut
    public static Vector3 X(this Vector3 value, float x)
    {
        vector3 = value;
        vector3.x = x;
        return vector3;
    }
    public static Vector3 Y(this Vector3 value, float y)
    {
        vector3 = value;
        vector3.y = y;
        return vector3;
    }
    public static Vector3 Z(this Vector3 value, float z)
    {
        vector3 = value;
        vector3.z = z;
        return vector3;
    }
    #endregion

    #region Color Shortcut
    public static Color R(this Color value, float r)
    {
        color = value;
        color.r = r;
        return color;
    }
    public static Color G(this Color value, float g)
    {
        color = value;
        color.g = g;
        return color;
    }
    public static Color B(this Color value, float b)
    {
        color = value;
        color.b = b;
        return color;
    }
    public static Color A(this Color value, float a)
    {
        color = value;
        color.a = a;
        return color;
    }
    #endregion
    #endregion
            
    //Layer index捷徑
    #region Layer Index Shortcuts
    public static int LayerIndex_Player { get { return LayerMask.NameToLayer("Player"); }}
    public static int LayerIndex_Ground { get { return LayerMask.NameToLayer("Ground"); }}
    public static int LayerIndex_Water { get { return LayerMask.NameToLayer("Water"); }}
    public static int LayerIndex_WaterSurface { get { return LayerMask.NameToLayer("WaterFace"); }}
    #endregion

    //Tag Name捷徑
    #region Tag Name Shortcuts
    public const string tag_Player = "Player";
    #endregion

    //Animation Parameter 捷徑
    #region Animation Parameter Shortcuts
    public const string anim_Clean = "IsClean";
    public const string anim_Knock = "IsKnock";
    public const string anim_Eat = "IsEat";
    public const string anim_FlipToBreast = "IsFlipToBreast";
    public const string anim_isWalk = "isWalk";
    public const string anim_RandomInt = "RandomInt";
    public const string anim_Grab = "Grab";
    public const string anim_Dive = "Dive";
    public const string anim_Float = "Float";
    public const string anim_OnKnock = "OnKnock";
    public const string anim_Celebrate = "Celebrate";
    public const string anim_Sleep = "Sleep";
    public const string anim_UnderWater = "UnderWater";
    public const string anim_SleepOver = "IsSleepOver";
    public const string anim_Sleep_Single = "Single_Sleep";
    public const string anim_Sleep_Couple = "Couple_Sleep";
    public const string anim_PlayerCome = "PlayerCome";
    public const string anim_HasNPCSleep = "HasNPCSleep";
    public const string anim_ThrowAim = "IsThrowAim";
    public const string anim_Throw = "IsThrow";
    #endregion

    //Animation Name 捷徑
    #region Animation Name Shortcuts
    public const string animName_Dive = "Dive";
    public const string animName_Float = "Float";
    public const string animName_Eat = "Eat";
    public const string animName_KnockLie = "Knock_Lie";
    public const string animName_KnockStand = "Knock_Stand";
    public const string animName_Grab = "Grab";
    public const string animName_CleanHand = "Cleaning_Hand";
    public const string animName_CleanBody = "Cleaning_Body";
    public const string animName_CleanFace = "Cleaning_Face";
    public const string animName_Sleep = "Sleep";
    public const string animName_OtterHappy = "OtterHappyAnim";
    public const string animName_OtterConfuse = "OtterConfuseAnim";
    public const string animName_OtterShock = "OtterShockAnim";
    public const string animName_OtterGrowth = "OtterGrowthAnim";
    public const string animName_OtterTired = "OtterTiredAnim";
    public const string animName_OtterHungry = "OtterHungryAnim";
    public const string animName_OtterSleepToStrong = "OtterSleepToStrongAnim";
    public const string animName_Celebrate = "Celebrate";

    #endregion

    //Object Pool Name 捷徑
    #region Object Pool Name Shortcuts
    public const string pool_ButtonHint = "ButtonPopUp";
    #endregion
}

