using UnityEngine;
using System;

public class PlayerStateController : MonoBehaviour
{
    public PlayerPlaceState PlayerPlaceState { get; private set; } = PlayerPlaceState.Float;
    public PlayerSpeedState PlayerSpeedState { get; private set; } = PlayerSpeedState.Normal;
    public PlayerFullState PlayerFullState { get; private set; } = PlayerFullState.Strong;
    public PlayerInteractAniState PlayerAniState { get; private set; } = PlayerInteractAniState.Idle;
    public PlayerCleanState PlayerCleanState { get; private set; } = PlayerCleanState.Clean;

    // public bool IsStateLocked { get; private set; }
    public bool IsStateLocked;
    public bool IsAddSpeedLocked { get; private set; }
    public bool CanPassGame { get; private set; }

    public bool CanClean { get; private set; }
    public bool CanSleep { get; private set; }
    public bool IsKnocking { get; private set; }

    private PlayerMovement playerMovement;
    private PlayerProperty playerProperty;
    private PlayerHand playerHand;
    private PlayerInputHandler inputHandler;

    private AudioSource playerAudio;

    [SerializeField]
    private ParticleSystem playerFloatParticle;
    [SerializeField]
    private Material playerMaterial;

    public event Action OnStateChanged;

    private void Start()
    {
        if (playerFloatParticle == null)
        {
            Debug.LogError("Player Float Particle System is not assigned!");
        }

        playerMovement = GetComponent<PlayerMovement>();
        playerProperty = GetComponent<PlayerProperty>();
        playerHand = GetComponent<PlayerHand>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerAudio = GetComponent<AudioSource>();

        playerProperty.OnStatusChanged += HandleStatusChanged;
    }

    private void Update()
    {
        if (GameManager.instance.GetGameAction())
        {
            UpdatePlayerStates();
        }
    }

    private void HandleStatusChanged()
    {
        UpdateFullState();
        UpdateCleanState();
        OnStateChanged?.Invoke();
    }

    #region 状态更新方法

    private void UpdatePlayerStates()
    {
        UpdatePlaceState();
        UpdateSpeedState();
        HandleKnockState();
        HandleSleepState();
    }

    private void UpdatePlaceState()
    {
        if (IsStateLocked) return;

        if (playerProperty.Status.Oxygen <= 0 && PlayerPlaceState == PlayerPlaceState.Dive)
        {
            ChangePlaceState(PlayerPlaceState.Float);
            ExitDiveMode();
            return;
        }

        if (PlayerPlaceState != PlayerPlaceState.WaterFall && inputHandler.IsDiving)
        {
            if (PlayerPlaceState == PlayerPlaceState.Dive)
            {
                ChangePlaceState(PlayerPlaceState.Float);
                ExitDiveMode();
            }
            else if (PlayerPlaceState == PlayerPlaceState.Float && playerProperty.Status.Oxygen > 0)
            {
                ChangePlaceState(PlayerPlaceState.Dive);
                EnterDiveMode();
            }
        }
    }

    private void UpdateSpeedState()
    {
        if (playerMovement.IsMoving)
        {
            if (playerProperty.Status.Power <= 0 && PlayerPlaceState != PlayerPlaceState.Dive)
            {
                ChangeSpeedState(PlayerSpeedState.Normal);
                EventCenter.Broadcast(GameEvents.BecomeTired);
                return;
            }

            if (PlayerSpeedState != PlayerSpeedState.Fast &&
                (PlayerPlaceState == PlayerPlaceState.Dive || inputHandler.IsAddingSpeed) &&
                playerHand.grabItemInHand == null)
            {
                ChangeSpeedState(PlayerSpeedState.Fast);
            }
            else if (PlayerSpeedState != PlayerSpeedState.Normal &&
                (!inputHandler.IsAddingSpeed && PlayerPlaceState != PlayerPlaceState.Dive || playerHand.grabItemInHand != null))
            {
                ChangeSpeedState(PlayerSpeedState.Normal);
            }
        }
        else if (playerMovement.GetCurrentSpeed() < 0.1f)
        {
            ChangeSpeedState(PlayerSpeedState.Stop);
        }
    }

    private void UpdateFullState()
    {
        float health = playerProperty.Status.Health;
        float hungerThreshold = playerProperty.Status.HungerThreshold;
        float agonyThreshold = playerProperty.Status.AgonyThreshold;

        if (PlayerFullState != PlayerFullState.Strong && health > hungerThreshold)
        {
            if (PlayerFullState == PlayerFullState.Agony)
            {
                playerMovement.ModifyCurrentSpeed(1 - playerProperty.Status.AgonySpeedRatio, false);
            }
            PlayerFullState = PlayerFullState.Strong;
        }
        else if (PlayerFullState != PlayerFullState.Hungry && health <= hungerThreshold && health > agonyThreshold)
        {
            if (PlayerFullState == PlayerFullState.Agony)
            {
                playerMovement.ModifyCurrentSpeed(1 - playerProperty.Status.AgonySpeedRatio, false);
            }
            PlayerFullState = PlayerFullState.Hungry;
            EventCenter.Broadcast(GameEvents.BecomeHungry);
        }
        else if (PlayerFullState != PlayerFullState.Agony && health <= agonyThreshold)
        {
            playerMovement.ModifyCurrentSpeed(1 - playerProperty.Status.AgonySpeedRatio, true);
            PlayerFullState = PlayerFullState.Agony;
            EventCenter.Broadcast(GameEvents.BecomeHungry);
        }
    }

    private void UpdateCleanState()
    {
        float cleanliness = playerProperty.Status.Cleanliness;
        float dirtyThreshold = playerProperty.Status.DirtyThreshold;
        float veryDirtyThreshold = playerProperty.Status.VeryDirtyThreshold;
        float dangerThreshold = playerProperty.Status.DangerThreshold;

        if (cleanliness > dirtyThreshold)
        {
            if (PlayerCleanState != PlayerCleanState.Clean)
            {
                ResetCleanStateEffects();
                PlayerCleanState = PlayerCleanState.Clean;
            }
        }
        else if (cleanliness <= dirtyThreshold && cleanliness > veryDirtyThreshold)
        {
            if (PlayerCleanState != PlayerCleanState.Dirty)
            {
                ApplyCleanStateEffects(PlayerCleanState.Dirty);
                PlayerCleanState = PlayerCleanState.Dirty;
            }
        }
        else if (cleanliness <= veryDirtyThreshold && cleanliness > dangerThreshold)
        {
            if (PlayerCleanState != PlayerCleanState.TwiceDirty)
            {
                ApplyCleanStateEffects(PlayerCleanState.TwiceDirty);
                PlayerCleanState = PlayerCleanState.TwiceDirty;
            }
        }
        else if (cleanliness <= dangerThreshold)
        {
            if (PlayerCleanState != PlayerCleanState.Weak)
            {
                ApplyCleanStateEffects(PlayerCleanState.Weak);
                PlayerCleanState = PlayerCleanState.Weak;
            }
        }
    }

    private void HandleKnockState()
    {
        if (!IsKnocking && PlayerAniState == PlayerInteractAniState.Knock)
        {
            IsKnocking = true;
        }
        else if (IsKnocking && Input.anyKeyDown && !Input.GetKeyDown(GlobalSetting.InterectKey))
        {
            AnimatorManager.instance.OffLockState();
            IsKnocking = false;
        }
    }

    private void HandleSleepState()
    {
        if (PlayerAniState != PlayerInteractAniState.Sleep) return;

        if (GameManager.instance.GetCurTime() >= 0 && GameManager.instance.GetDayState() == DayState.Day)
        {
            AnimatorManager.instance.OffLockState();
        }
    }

    #endregion

    #region 状态变化方法

    private void ChangePlaceState(PlayerPlaceState newState)
    {
        PlayerPlaceState = newState;
        OnStateChanged?.Invoke();
    }

    private void ChangeSpeedState(PlayerSpeedState newState)
    {
        PlayerSpeedState = newState;
        //playerMovement.OnPlayerSpeedChange?.Invoke(newState);
        OnStateChanged?.Invoke();
    }

    public void ChangeAniState(PlayerInteractAniState newState)
    {
        PlayerAniState = newState;
        OnStateChanged?.Invoke();
    }

    #endregion

    #region 潜水和浮出处理

    private void EnterDiveMode()
    {
        AudioManager.instance.ChangeAudioLowpassCutoff(true);
        PostProcessingManager.instance.ChangeSeaAlpha(true);
        AnimatorManager.instance.DetectDiveOrFloatAniPlay();
        AudioManager.instance.PlayLocalSFX(SFX_Name.DiveAndFloat, transform.position, 1);
    }

    private void ExitDiveMode()
    {
        AudioManager.instance.ChangeAudioLowpassCutoff(false);
        PostProcessingManager.instance.ChangeSeaAlpha(false);
        AnimatorManager.instance.DetectDiveOrFloatAniPlay();
        PlayFloatParticle();
    }

    private void PlayFloatParticle()
    {
        if (playerFloatParticle.isPlaying) return;
        playerFloatParticle.Play();
    }

    #endregion

    #region 清洁状态效果处理

    private void ResetCleanStateEffects()
    {
        playerMovement.ModifyCurrentSpeed(1 - playerProperty.Status.DirtySpeedRatio, false);
        playerMaterial.SetFloat("Dirt1_Lerp", 0);
        playerMaterial.SetFloat("Dirt2_Lerp", 0);
        playerMaterial.SetFloat("Dirt3_Lerp", 0);
    }

    private void ApplyCleanStateEffects(PlayerCleanState newState)
    {
        switch (newState)
        {
            case PlayerCleanState.Dirty:
                playerMovement.ModifyCurrentSpeed(1 - playerProperty.Status.DirtySpeedRatio, true);
                playerMaterial.SetFloat("Dirt1_Lerp", 1);
                playerMaterial.SetFloat("Dirt2_Lerp", 0);
                playerMaterial.SetFloat("Dirt3_Lerp", 0);
                break;
            case PlayerCleanState.TwiceDirty:
                playerMovement.ModifyCurrentSpeed(1 - playerProperty.Status.DirtySpeedRatio * 2, true);
                playerMaterial.SetFloat("Dirt1_Lerp", 1);
                playerMaterial.SetFloat("Dirt2_Lerp", 1);
                playerMaterial.SetFloat("Dirt3_Lerp", 0);
                break;
            case PlayerCleanState.Weak:
                playerMovement.ModifyCurrentSpeed(1 - playerProperty.Status.DangerSpeedRatio, true);
                playerMaterial.SetFloat("Dirt1_Lerp", 1);
                playerMaterial.SetFloat("Dirt2_Lerp", 1);
                playerMaterial.SetFloat("Dirt3_Lerp", 1);
                break;
        }
    }

    #endregion

    #region 碰撞和触发器处理

    private void OnTriggerEnter(Collider other)
    {
        if (PlayerPlaceState == PlayerPlaceState.Dive) return;

        var terrain = other.GetComponent<TerrainBase>();
        if (terrain != null)
        {
            if (terrain.GetComponent<Env_WaterFall>())
            {
                ChangePlaceState(PlayerPlaceState.WaterFall);
                //TODO: Waterfall logic
            }

            CanClean = terrain.canClean;
            CanSleep = terrain.canSleep;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var terrain = other.GetComponent<TerrainBase>();
        if (terrain != null)
        {
            if (PlayerPlaceState == PlayerPlaceState.WaterFall && terrain.GetComponent<Env_WaterFall>())
            {
                ChangePlaceState(PlayerPlaceState.Float);
                IsAddSpeedLocked = false;
            }

            if (terrain.canClean)
            {
                CanClean = false;
            }

            if (terrain.canSleep)
            {
                CanSleep = false;
            }
        }
    }

    #endregion

    #region 状态锁定

    public void StateOnLock()
    {
        IsStateLocked = true;
    }

    public void StateOffLock()
    {
        IsStateLocked = false;
    }

    #endregion

    public void SetCanPassGame(bool value)
    {
        CanPassGame = value;
    }
}
