using UnityEngine;
using System;

public class PlayerProperty : MonoBehaviour
{
    public PlayerStatus Status { get; private set; }

    public event Action OnStatusChanged;

    private PlayerStateController stateController;

    [Header("Health Settings")]
    [SerializeField] private float initialMaxHealth = GlobalSetting.playerInitFull;
    [SerializeField] private float healthDecayRate = GlobalSetting.timelyFullConsume;
    [SerializeField] private float hungerThreshold = GlobalSetting.playerHungryFull;
    [SerializeField] private float agonyThreshold = GlobalSetting.playerAgonyFull;

    [Header("Power Settings")]
    [SerializeField] private float initialMaxPower = GlobalSetting.playerInitPower;
    [SerializeField] private float powerDecayRate = GlobalSetting.timelyPowerConsume;
    [SerializeField] private float powerRecoveryMultiplier = 2f;

    [Header("Cleanliness Settings")]
    [SerializeField] private float initialMaxCleanliness = GlobalSetting.playerInitClean;
    [SerializeField] private float cleanAmount = GlobalSetting.playerInitClean;
    [SerializeField] private float eatDirtyAmount = GlobalSetting.onceDirtyConsume;
    [SerializeField] private float dirtyThreshold = GlobalSetting.dirtyClean;
    [SerializeField] private float veryDirtyThreshold = GlobalSetting.dirtyTwiceClean;
    [SerializeField] private float dangerThreshold = GlobalSetting.dangerClean;

    [Header("Oxygen Settings")]
    [SerializeField] private float initialMaxOxygen = GlobalSetting.playerInitOxy;
    [SerializeField] private float oxygenDecayRate = GlobalSetting.timelyOxyConsume;
    [SerializeField] private float oxygenRecoveryMultiplier = 2f;

    [Header("Speed Ratios")]
    [Range(0.01f, 1f)]
    [SerializeField] private float dirtySpeedRatio = GlobalSetting.playerDirtySpeedRatio;
    [Range(0.01f, 1f)]
    [SerializeField] private float dangerSpeedRatio = GlobalSetting.playerDangerCleanSpeedRatio;
    [Range(0.01f, 1f)]
    [SerializeField] private float agonySpeedRatio = GlobalSetting.playerAgonyFullSpeedRatio;

    [Header("Experience Settings")]
    [SerializeField] private float[] experienceThresholds = { 100f, 200f, 300f, 400f };

    private void Start()
    {
        Status = new PlayerStatus
        {
            MaxHealth = initialMaxHealth,
            Health = initialMaxHealth,
            MaxPower = initialMaxPower,
            Power = initialMaxPower,
            MaxCleanliness = initialMaxCleanliness,
            Cleanliness = initialMaxCleanliness,
            MaxOxygen = initialMaxOxygen,
            Oxygen = initialMaxOxygen,
            Level = 0,
            Experience = 0f,
            HungerThreshold = hungerThreshold,
            AgonyThreshold = agonyThreshold,
            DirtyThreshold = dirtyThreshold,
            VeryDirtyThreshold = veryDirtyThreshold,
            DangerThreshold = dangerThreshold,
            AgonySpeedRatio = agonySpeedRatio,
            DirtySpeedRatio = dirtySpeedRatio,
            DangerSpeedRatio = dangerSpeedRatio
        };

        stateController = GetComponent<PlayerStateController>();
    }

    private void Update()
    {
        if (GameManager.instance.GetGameAction())
        {
            if (stateController.PlayerAniState != PlayerInteractAniState.Sleep)
            {
                ModifyHealth(-healthDecayRate * Time.deltaTime);
            }

            UpdateOxygen();
            UpdatePower();
            CheckExperience();
        }
    }

    #region 状态更新方法

    private void UpdateOxygen()
    {
        if (stateController.PlayerPlaceState == PlayerPlaceState.Dive)
        {
            if (Status.Oxygen > 0)
            {
                ModifyOxygen(-oxygenDecayRate * Time.deltaTime);
            }
        }
        else
        {
            if (Status.Oxygen < Status.MaxOxygen)
            {
                ModifyOxygen(oxygenDecayRate * oxygenRecoveryMultiplier * Time.deltaTime);
            }
        }
    }

    private void UpdatePower()
    {
        if (stateController.PlayerSpeedState == PlayerSpeedState.Fast && Status.Power > 0 && stateController.PlayerPlaceState != PlayerPlaceState.Dive)
        {
            if (!stateController.IsAddSpeedLocked && stateController.PlayerPlaceState == PlayerPlaceState.WaterFall)
            {
                return;
            }

            ModifyPower(-powerDecayRate * Time.deltaTime);

            if (Status.Power <= Status.MaxPower / 10 && stateController.PlayerPlaceState == PlayerPlaceState.WaterFall)
            {
                EventCenter.Broadcast(GameEvents.BecomeSleepy);
            }
        }
        else
        {
            if (Status.Power < Status.MaxPower)
            {
                if (!Input.GetKey(GlobalSetting.AddSpeedKey) && stateController.PlayerSpeedState != PlayerSpeedState.Fast
                    && !stateController.IsAddSpeedLocked && stateController.PlayerPlaceState != PlayerPlaceState.Dive)
                {
                    ModifyPower(powerDecayRate * powerRecoveryMultiplier * Time.deltaTime);
                }
            }
        }
    }

    private void CheckExperience()
    {
        if (Status.Level < experienceThresholds.Length && Status.Experience >= experienceThresholds[Status.Level])
        {
            Status.Experience -= experienceThresholds[Status.Level];
            LevelUp();
            OnStatusChanged?.Invoke();
        }
    }

    #endregion

    #region 属性修改方法

    public void ModifyHealth(float amount)
    {
        Status.Health = Mathf.Clamp(Status.Health + amount, 0, Status.MaxHealth);
        OnStatusChanged?.Invoke();
    }

    public void ModifyPower(float amount)
    {
        Status.Power = Mathf.Clamp(Status.Power + amount, 0, Status.MaxPower);
        OnStatusChanged?.Invoke();
    }

    public void ModifyCleanliness(float amount)
    {
        Status.Cleanliness = Mathf.Clamp(Status.Cleanliness + amount, 0, Status.MaxCleanliness);
        OnStatusChanged?.Invoke();
    }

    public void ModifyOxygen(float amount)
    {
        Status.Oxygen = Mathf.Clamp(Status.Oxygen + amount, 0, Status.MaxOxygen);
        OnStatusChanged?.Invoke();
    }

    public void ModifyExperience(float amount)
    {
        Status.Experience += amount;
        OnStatusChanged?.Invoke();
        CheckExperience();
    }

    public void ModifyMaxOxygen(float amount)
    {
        Status.MaxOxygen += amount;
        Status.Oxygen = Status.MaxOxygen;
        OnStatusChanged?.Invoke();
    }

    #endregion

    #region 等级相关方法

    private void LevelUp()
    {
        Status.Level++;
        Status.MaxPower += GlobalSetting.playerUpLevelPower;
        // Status.MaxOxygen += GlobalSetting.playerUpLevelOxy; // 如果适用
        Status.Power = Status.MaxPower;
        Status.Oxygen = Status.MaxOxygen;
        // 触发升级事件或逻辑
    }

    #endregion
}

