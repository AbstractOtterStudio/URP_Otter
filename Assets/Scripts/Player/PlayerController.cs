using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerStateController))]
[RequireComponent(typeof(PlayerHand))]
[RequireComponent(typeof(PlayerProperty))]
[RequireComponent(typeof(AnimatorManager))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    [Tooltip("按下互动键超过多少秒则判断为投掷")]
    [SerializeField]
    private float throwHoldThreshold = 0.4f;

    [Tooltip("投掷力量增加速度(每秒)")]
    [SerializeField]
    private float throwStrengthIncrement = 1.0f;

    [Tooltip("最大投掷力量")]
    [SerializeField]
    private float maxThrowStrength = 10.0f;

    [Tooltip("这些layer[不]可以作为投掷目的地")]
    [SerializeField]
    private LayerMask nonThrowableLayers;

    [SerializeField]
    private float throwOffset = 1.2f;

    [SerializeField]
    private ParticleSystem knockParticle;

    private List<ItemProperties> availableItems = new List<ItemProperties>();
    private List<ItemProperties> knockableItems = new List<ItemProperties>();
    private List<Env_SeaWeed> seaWeeds = new List<Env_SeaWeed>();

    [SerializeField]
    private Material playerMaterial;

    [SerializeField]
    private TrajectoryLine trajectoryLine;

    private PlayerStateController stateController;
    private PlayerProperty playerProperty;
    private PlayerHand hand;
    private AnimatorManager animatorManager;
    private PlayerMovement playerMovement;
    private PlayerInputHandler inputHandler;

    private float materialBlendValue;
    private bool isGrowing;
    private ItemComparer itemComparer;

    // 投掷状态变量
    private float throwHoldTimer = 0.0f;
    private bool isThrowing = false;
    private bool isThrowAiming = false;
    private float throwStrength = 0;
    private ItemProperties lastThrownItem = null;

    private void Start()
    {
        hand = GetComponent<PlayerHand>();
        stateController = GetComponent<PlayerStateController>();
        playerProperty = GetComponent<PlayerProperty>();
        animatorManager = GetComponent<AnimatorManager>();
        playerMovement = GetComponent<PlayerMovement>();
        inputHandler = GetComponent<PlayerInputHandler>();

        itemComparer = new ItemComparer(this.transform);

        trajectoryLine.SetColor(Color.white);
    }

    private void Update()
    {
        if (GameManager.instance.GetGameAction())
        {
            HandleInteractions();
            HandleEatOrKnock();
            UpdatePlayerGrowth();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponent<ItemProperties>();
        if (item != null)
        {
            if (item.CanCatch)
            {
                AddToListIfNotExists(availableItems, item);
                EventCenter.Broadcast(GameEvents.ShowButtonHint, ButtonHintType.Button_Z);
            }

            if (item.CanKnock && stateController.PlayerPlaceState == PlayerPlaceState.Float)
            {
                AddToListIfNotExists(knockableItems, item);
                if (hand.GrabItemInHand != null && !hand.GrabItemInHand.IsBroken &&
                    stateController.PlayerPlaceState == PlayerPlaceState.Float)
                {
                    EventCenter.Broadcast(GameEvents.ShowButtonHint, ButtonHintType.Button_X);
                }
            }
        }

        var seaWeed = other.GetComponent<Env_SeaWeed>();
        if (seaWeed != null && stateController.PlayerPlaceState == PlayerPlaceState.Float)
        {
            AddToListIfNotExists(seaWeeds, seaWeed);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var item = other.GetComponent<ItemProperties>();
        if (item != null)
        {
            RemoveFromList(availableItems, item);
            RemoveFromList(knockableItems, item);
        }

        var seaWeed = other.GetComponent<Env_SeaWeed>();
        if (seaWeed != null)
        {
            seaWeeds.Remove(seaWeed);
        }
    }

    #region 交互处理

    private void HandleInteractions()
    {
        if (hand.GrabItemInHand != null)
        {
            HandleItemInHandInteractions();
        }
        else
        {
            HandleNoItemInHandInteractions();
        }
    }

    private void HandleItemInHandInteractions()
    {
        if (isThrowAiming)
        {
            AccumulateThrowStrength();
            UpdateTrajectoryLine();
        }

        if (Input.GetKey(GlobalSetting.InterectKey))
        {
            throwHoldTimer += Time.deltaTime;
            if (throwHoldTimer > throwHoldThreshold && !isThrowing)
            {
                BeginThrowingItem();
                throwHoldTimer = 0.0f;
            }
        }
        else if (Input.GetKeyUp(GlobalSetting.InterectKey))
        {
            if (isThrowing)
            {
                EndThrowingItem();
            }
            else
            {
                ReleaseItem();
            }
        }
    }

    private void HandleNoItemInHandInteractions()
    {
        if (inputHandler.IsInteracting)
        {
            if (stateController.CanClean
                && stateController.PlayerPlaceState == PlayerPlaceState.Float
                && availableItems.Count <= 0)
            {
                if (GameManager.instance.GetDayState() == DayState.Night)
                {
                    Sleep();
                    return;
                }
                Clean();
            }
            else if (!isThrowing)
            {
                GrabItem();
            }
        }
    }

    private void HandleEatOrKnock()
    {
        if (stateController.PlayerAniState == PlayerInteractAniState.Throw)
            return;

        if (inputHandler.IsEatingOrKnocking && hand.GrabItemInHand != null)
        {
            if (stateController.PlayerPlaceState == PlayerPlaceState.Float)
            {
                var item = hand.GrabItemInHand.GetComponent<ItemProperties>();
                if (item.CanEat && item.IsBroken)
                {
                    PlayEatAnimation();
                }
                else
                {
                    if (knockableItems.Count > 0)
                    {
                        Knock();
                    }
                }
            }
        }
    }

    #endregion

    #region 交互方法

    private void AccumulateThrowStrength()
    {
        throwStrength = Mathf.Min(
            throwStrength + throwStrengthIncrement * Time.deltaTime,
            maxThrowStrength
        );
    }

    private void UpdateTrajectoryLine()
    {
        trajectoryLine.MakeTrajectory(
            GetThrowStartPosition(),
            GetThrowDirection(),
            throwStrength, 1.0f
        );

        if (CanThrow())
        {
            trajectoryLine.SetColor(Color.white);
        }
        else
        {
            trajectoryLine.SetColor(Color.red);
        }
    }

    private void BeginThrowingItem()
    {
        lastThrownItem = hand.GrabItemInHand;
        SetIsThrowing(true);
    }

    private void EndThrowingItem()
    {
        if (!CanThrow())
        {
            SetIsThrowing(false);
            trajectoryLine.ClearTrajectory();
            return;
        }

        // 开始投掷协程（此处省略具体实现）

        SetIsThrowing(false);
        hand.GrabItemInHand.Release();
        hand.ReleaseGrabItem();
        trajectoryLine.ClearTrajectory();
    }

    private void ReleaseItem()
    {
        stateController.ChangeAniState(PlayerInteractAniState.Release);
        if (hand.GrabItemInHand == null) return;
        availableItems.Add(hand.GrabItemInHand);
        hand.GrabItemInHand.Release();
        hand.ReleaseGrabItem();
    }

    private void GrabItem()
    {
        if (stateController.IsStateLocked) return;
        stateController.ChangeAniState(PlayerInteractAniState.Grab);
    }

    private void GrabItemLogic()
    {
        if (availableItems.Count <= 0) return;
        ItemProperties item = availableItems[0];
        hand.GrabItem(item);
        availableItems.Remove(item);
        if (knockableItems.Contains(item))
        {
            knockableItems.Remove(item);
        }
        item.Catch(hand.PlayerHandModel);
    }

    private void Knock()
    {
        if (stateController.IsStateLocked) return;
        stateController.ChangeAniState(PlayerInteractAniState.Knock);
        Vector3 direction = -(knockableItems[0].transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);

        hand.GrabItemInHand.KnockWith(knockableItems[0]);
        if (knockableItems[0].IsBroken)
        {
            knockableItems.RemoveAt(0);
        }
    }

    private void PlayEatAnimation()
    {
        if (stateController.IsStateLocked) return;
        stateController.ChangeAniState(PlayerInteractAniState.Eat);
    }

    private void EatFood()
    {
        var foodAdd = hand.GrabItemInHand.Eat();

        if (hand.GrabItemInHand.GetComponent<Item_Urchin>())
        {
            AnimatorManager.instance.PlayerCelebrate();
        }
        hand.GrabItemInHand.transform.parent = null;
        hand.GrabItemInHand = null;
        playerProperty.ModifyHealth(foodAdd.health);
        playerProperty.ModifyMaxOxygen(foodAdd.Oxygen);
        playerProperty.ModifyCleanliness(-playerProperty.Status.eatDirtyAmount / 2);
        EventCenter.Broadcast(GameEvents.BecomeGrowth);
    }

    #endregion

    #region 辅助方法

    private bool CanThrow()
    {
        var colliders = Physics.OverlapSphere(trajectoryLine.EndPos, 0.5f);
        foreach (var collider in colliders)
        {
            if (ReferenceEquals(collider.gameObject, hand.GrabItemInHand.gameObject))
                continue;

            if (((1 << collider.gameObject.layer) & nonThrowableLayers.value) != 0)
                return false;
        }

        return true;
    }

    private void SetIsThrowing(bool throwing)
    {
        if (!isThrowing && throwing)
        {
            animatorManager.OffLockState();
            animatorManager.playerAnimator.SetTrigger(ValueShortcut.anim_ThrowAim);
            playerMovement.OnPlayerSpeedChange?.Invoke(PlayerSpeedState.Slow);
            SetIsThrowAiming(true);
        }
        else if (isThrowing && !throwing)
        {
            animatorManager.OffLockState();
            animatorManager.playerAnimator.SetTrigger(ValueShortcut.anim_Throw);
            playerMovement.OnPlayerSpeedChange?.Invoke(PlayerSpeedState.Normal);
            SetIsThrowAiming(false);
        }

        isThrowing = throwing;
    }

    private void SetIsThrowAiming(bool throwAiming)
    {
        isThrowAiming = throwAiming;
        throwStrength = 0.0f;
    }

    private Vector3 GetThrowStartPosition() => transform.position + transform.up * throwOffset;
    private Vector3 GetThrowDirection() => -transform.forward + transform.up;

    private void AddToListIfNotExists<T>(List<T> list, T item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);
            list.Sort(itemComparer);
        }
    }

    private void RemoveFromList<T>(List<T> list, T item)
    {
        if (list.Contains(item))
        {
            list.Remove(item);
        }
    }

    #endregion

    #region 玩家成长和清洁

    private void UpdatePlayerGrowth()
    {
        if (isGrowing)
        {
            materialBlendValue += Time.deltaTime / 5;
            if (playerProperty.Status.Level == 2 && playerMaterial.GetFloat("Step1To2") < 0.99f)
            {
                playerMaterial.SetFloat("Step1To2", materialBlendValue);
            }
            else if (playerProperty.Status.Level == 3 && playerMaterial.GetFloat("Step2To3") < 0.99f)
            {
                playerMaterial.SetFloat("Step2To3", materialBlendValue);
            }
            else
            {
                isGrowing = false;
            }
        }
    }

    private void Clean()
    {
        if (stateController.IsStateLocked) return;
        playerProperty.ModifyCleanliness(playerProperty.Status.Cleanliness);
        stateController.ChangeAniState(PlayerInteractAniState.Clean);
        EventCenter.Broadcast(GameEvents.BecomeGrowth);
    }

    private void Sleep()
    {
        if (stateController.IsStateLocked) return;
        stateController.ChangeAniState(PlayerInteractAniState.Sleep);
        int previousLevel = playerProperty.Status.Level;
        playerProperty.ModifyExperience(experienceThresholds[playerProperty.Status.Level]); // 假设睡眠直接增加足够经验升级

        if (playerProperty.Status.Level > previousLevel)
        {
            isGrowing = true;
            materialBlendValue = 0;
        }
    }

    #endregion

    public class ItemComparer : IComparer<ItemProperties>
    {
        private Transform player;

        public ItemComparer(Transform player)
        {
            this.player = player;
        }

        public int Compare(ItemProperties itemA, ItemProperties itemB)
        {
            float distanceA = Vector3.Distance(itemA.transform.position, player.position);
            float distanceB = Vector3.Distance(itemB.transform.position, player.position);
            return distanceA.CompareTo(distanceB);
        }
    }
}

