using UnityEngine;

//場景元素配置
[CreateAssetMenu(fileName = "ConfigFile", menuName = "Otter/ConfigFile", order = 0)]
public class ConfigFile : ScriptableObject 
{
    public static ConfigFile GetConfigFile()
    {
        return Resources.Load<ConfigFile>("ConfigFile");
    }
    public AnimalSetting[] animalSettings;
    public ButtonHint[] buttonHints;
    public SFX[] soundEffects;
    public BGM[] backgroundMusics;
}

#region Stage Settings
public enum AnimalPoolName
{
    Fish,
    JellyFish,
    FanShell,
    SpiralShell,
    Snail,
    Lobster,
    Crab,
    Octopus,    
}
[System.Serializable]
public struct AnimalSetting
{
    public AnimalPoolName poolName;    
    public Destinations[] patrolPoints;
}

[System.Serializable]
public struct Destinations
{
    public Vector3[] destinations;    
}
#endregion

#region Game Infos
public enum ButtonHintType
{
    Button_Z,
    Button_X,
    Button_Shift,
    Button_Space
}
[System.Serializable]
public class ButtonHint
{
    public ButtonHintType hintType;
    public Sprite hintSprite;    
}

#endregion

#region Music Settings
public enum SFX_Name
{
    DiveAndFloat,
    Eat_Soft,
    Eat_Hard,
    Knock_Hard,
    Swim_A,
    Swim_B,
    Swim_C,
    Swim_D,
    UI_Click,
    Waterfall,
    Happy,
    Growth,
    Surprise,
    Wondering,
    Bubble_A,
    Bubble_B,
    Bubble_C,
    Tired,
    Hungry
}
[System.Serializable]
public struct SFX
{
    public SFX_Name name;
    public AudioClip clip;
}

public enum BGM_Name
{
    Normal
}
[System.Serializable]
public struct BGM
{
    public BGM_Name name;
    public AudioClip clip;
}
#endregion