
/// <summary>
/// Player Move in Different Places with Different Speed State
/// </summary>
public enum PlayerPlaceState
{
    Dive,
    Float,
    WaterFall
}

/// <summary>
/// Player Speed State : 
/// Normal    Player with no Input
/// Fast    Player with Add Speed KeyCode
/// Slow    Player with Decrease Speed
/// </summary>
public enum PlayerSpeedState
{
    Normal,
    Fast,
    Slow,
    Stop
}

/// <summary>
/// Player Health State:
/// Strong  Player Power Value and Clean Value is health
/// Weak    Player Power Value or Clean Value is not good (slow)
/// Agony   Player Power Value or Clean Value is bad (very slow)
/// Dead    Player Dead
/// </summary>
public enum PlayerFullState 
{
    Strong,
    Hungry,
    Agony,
    Dead
}

public enum PlayerCleanState
{
    Clean,
    Dirty,
    TwiceDirty,
    Weak
}

public enum PlayerInteractAniState
{
    Idle,
    Eat,
    Knock,
    Grab,
    Release,
    Clean,
    Sleep,
    Celebrate,
    Throw,
}

public class PlayerStatus
{
    public float Health { get; set; }
    public float MaxHealth { get; set; }

    public float Power { get; set; }
    public float MaxPower { get; set; }

    public float Cleanliness { get; set; }
    public float MaxCleanliness { get; set; }

    public float Oxygen { get; set; }
    public float MaxOxygen { get; set; }

    public int Level { get; set; }
    public float Experience { get; set; }

    // 添加需要的阈值和速率属性
    public float HungerThreshold { get; set; }
    public float AgonyThreshold { get; set; }
    public float DirtyThreshold { get; set; }
    public float VeryDirtyThreshold { get; set; }
    public float DangerThreshold { get; set; }

    public float AgonySpeedRatio { get; set; }
    public float DirtySpeedRatio { get; set; }
    public float DangerSpeedRatio { get; set; }
}


