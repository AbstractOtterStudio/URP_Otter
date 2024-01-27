using System;
using System.Security.AccessControl;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manager Player Input (Except Movement) and Responding Logic
/// </summary>
[RequireComponent(typeof(PlayerStateController))]
[RequireComponent(typeof(PlayerHand))]
[RequireComponent(typeof(PlayerProperty))]
[RequireComponent(typeof(AnimatorManager))]
public class PlayerController : MonoBehaviour
{
    [Tooltip("按下互动键超过多少秒则判断为投掷")]
    [SerializeField]
    private float throwHoldThres = 0.4f;

    [Tooltip("投掷力量增加速度(每秒)")]
    [SerializeField]
    private float throwStrengthIncSpeed = 1.0f;

    [Tooltip("最大投掷力量")]
    [SerializeField]
    private float maxThrowStrength = 10.0f;

    [SerializeField]
    private ParticleSystem knockParticle;

    [DebugDisplay]
    private List<ItemProperties> canTakeList = new List<ItemProperties>();
    //private List<IPullable> canPullList = new List<IPullable>();
    [DebugDisplay]
    private List<ItemProperties> canKnockList = new List<ItemProperties>();
    [DebugDisplay]
    private List<Env_SeaWeed> seaWeedsList = new List<Env_SeaWeed>();
    
    [SerializeField]
    private Material playerMaterial;

    [SerializeField]
    private TrajectoryLine trajectoryLine;

    private PlayerStateController stateController;
    private PlayerProperty playerProperty;
    private PlayerHand hand;
    private AnimatorManager animatorMgr;
    //Use to set lerp value with player material
    private float m_materialFloat;
    //When player level up, set true
    private bool m_isGrow;
    CompareItem compareItem;

    // throw state variabls
    private float m_throwHoldTimer = 0.0f;

    [DebugDisplay]
    private bool m_isThrowing = false;

    [DebugDisplay]
    private bool m_isThrowAiming = false;

    [DebugDisplay]
    private float m_throwStrength = 0;

    void SetIsThrowing(bool throwing)
    {
        if (!m_isThrowing && throwing)
        {
            SetIsThrowAiming(true);
        }
        else if (m_isThrowing && !throwing)
        {
            animatorMgr.OffLockState();
            animatorMgr.playerAnimator.SetTrigger(ValueShortcut.anim_Throw);

            SetIsThrowAiming(false);
            PlayerThrowItem();
        }

        m_isThrowing = throwing;
    }

    void SetIsThrowAiming(bool throwAiming)
    {
        m_isThrowAiming = throwAiming;
        m_throwStrength = 0.0f;
    }

    // List<Renderer> rendererList;
    void Start()
    {
        hand = GetComponent<PlayerHand>();
        //playerMaterial = transform.Find("backup").GetComponent<SkinnedMeshRenderer>().materials[0];

        stateController = GetComponent<PlayerStateController>();
        playerProperty = GetComponent<PlayerProperty>();
        animatorMgr = GetComponent<AnimatorManager>();
        compareItem = new CompareItem(this.transform);
    }

    void Update()
    {
        trajectoryLine.MakeTrajectory(transform.position, transform.forward, maxThrowStrength, 1.0f);

        if (GameManager.instance.GetGameAction())
        {
            Interact();
            EatOrKnockInteract();
            PlayerGrowUp();
        }
    }

    void OnTriggerEnter(Collider other)
    {
       if (other.GetComponent<ItemProperties>() && other.GetComponent<ItemProperties>().CanCatch ) 
       {
           ItemProperties itemBase = other.GetComponent<ItemProperties>();
           Debug.Log("Takable Found");
           if (!canTakeList.Contains(itemBase) && hand.grabItemInHand != itemBase)
                canTakeList.Add(itemBase);
                EventCenter.Broadcast(GameEvents.ShowButtonHint, ButtonHintType.Button_Z);
                canTakeList.Sort(compareItem);
       }


       if (other.GetComponent<ItemProperties>() && other.GetComponent<ItemProperties>().CanKnock
       && stateController.playerPlaceState == PlayerPlaceState.Float) {
           ItemProperties item = other.GetComponent<ItemProperties>();
           if (!canKnockList.Contains(item) && hand.grabItemInHand != item) {
                canKnockList.Add(item);
                canKnockList.Sort(compareItem);
                if (hand.grabItemInHand != null && !hand.grabItemInHand.IsBroken &&
                    stateController.playerPlaceState == PlayerPlaceState.Float)
                {
                    //Hint Player Knock food
                    EventCenter.Broadcast(GameEvents.ShowButtonHint, ButtonHintType.Button_X);
                }
           }
       }

       if (other.GetComponent<Env_SeaWeed>() && stateController.playerPlaceState == PlayerPlaceState.Float) {
           Env_SeaWeed item = other.GetComponent<Env_SeaWeed>();
           if (!seaWeedsList.Contains(item)) {
                seaWeedsList.Add(item);                
           }
       }

    }

    void OnTriggerExit(Collider other)
    {
       if (other.GetComponent<ItemProperties>() && other.GetComponent<ItemProperties>().CanCatch) {
           ItemProperties itemBase = other.GetComponent<ItemProperties>();
           if (canTakeList.Contains(itemBase))
               canTakeList.Remove(itemBase);
       }

       if (other.GetComponent<ItemProperties>() && other.GetComponent<ItemProperties>().CanKnock) {
           ItemProperties item = other.GetComponent<ItemProperties>();
           if (canKnockList.Contains(item)) {
               canKnockList.Remove(item);
           }
       } 

        if (other.GetComponent<Env_SeaWeed>()) {
           Env_SeaWeed item = other.GetComponent<Env_SeaWeed>();
           if (seaWeedsList.Contains(item)) {
                seaWeedsList.Remove(item);
           }
        }        
    }

    #region Interact Detect

    /// <summary>
    /// When Player input Interact Keycode
    /// Responding logic
    /// </summary>
    private void Interact() 
    {
        if (hand.grabItemInHand != null)
        {
            if (m_isThrowAiming)
            {
                m_throwStrength = Mathf.Min(
                    m_throwStrength + throwStrengthIncSpeed * Time.deltaTime,
                    maxThrowStrength
                );
            }

            if (Input.GetKey(GlobalSetting.InterectKey))
            {
                m_throwHoldTimer += Time.deltaTime;
                if (m_throwHoldTimer > throwHoldThres)
                {
                    //地面状态：水面/水下
                    //手部状态：手中有物品     
                    //动作：投掷物品
                    SetIsThrowing(true);

                    m_throwHoldTimer = 0.0f;
                }
            }
            else if (Input.GetKeyUp(GlobalSetting.InterectKey))
            {
                if (m_isThrowing)
                {
                    SetIsThrowing(false);
                }
                else
                {
                    //地面状态：水面/水下
                    //手部状态：手中有物品     
                    //动作：放开物品    
                    PlayerReleaseItem();
                }
            }
        }


        if (Input.GetKeyDown(GlobalSetting.InterectKey)) {

            if (hand.grabItemInHand == null) {
                if (stateController.playerCanClean
                    && stateController.playerPlaceState == PlayerPlaceState.Float
                    && canTakeList.Count <= 0)
                {
                    if (GameManager.instance.GetDayState() == DayState.Night)
                    {
                        //水面、环境可睡觉
                        //时间：夜晚
                        //手部状态：手中无物品
                        //动作：睡觉
                        PlayerSleep();
                        return;
                    }
                    //地面状态：水面、环境可清洁
                    //手部状态：手中无物品
                    //动作：清洁                         
                    PlayerClean();
                }
                else if (!m_isThrowing)
                {
                    //地面状态：水面/水下、可抓取物体
                    //手部状态：手中无物品     
                    //动作：抓取                
                    PlayerGrabItemInHand();
                }
            }
        }
    }

    /// <summary>
    /// Player Eat Food
    /// Improve Health Value and Experience Value
    /// </summary>
    private void EatOrKnockInteract() 
    {
        if (stateController.playerAniState == PlayerInteractAniState.Throw)
        {
            return;
        }

        if (Input.GetKeyDown(GlobalSetting.EatOrKnockKey) && hand.grabItemInHand != null) {
            if (stateController.playerPlaceState == PlayerPlaceState.Float) {
                if (hand.grabItemInHand.GetComponent<ItemProperties>().CanEat
                && hand.grabItemInHand.GetComponent<ItemProperties>().IsBroken) 
                {
                   //Eat
                   EatFoodAniPlay();
                }
                else {
                    if (canKnockList.Count > 0) {
                        //Knock
                        Knock();
                    }
                }
            }
        }
    }

    #endregion

    #region Player Interaction Logic
    private void PlayerThrowItem()
    {

    }

    /// <summary>
    /// Improve Experience Value
    /// </summary>
    private void PlayerSleep() 
    {
        if (stateController.playerStateLock) return;
        stateController.ChangeAniState(PlayerInteractAniState.Sleep);
        int lastLevel = playerProperty.currentLevel;
        playerProperty.ChangeLevelValue(true, 1);
        if (playerProperty.currentLevel > lastLevel)
        {
            m_isGrow = true;
            m_materialFloat = 0;
        }
    }

    /// <summary>
    /// When Player Level up, Show Player Material Changing 
    /// </summary>
    private void PlayerGrowUp()
    {
        if (m_isGrow)
        {
            m_materialFloat += Time.deltaTime / 5;
            if (playerProperty.currentLevel == 2 && playerMaterial.GetFloat("Step1To2") < 0.99f)
            {
                Debug.Log("Grow UP !");
                playerMaterial.SetFloat("Step1To2",m_materialFloat);
            }
            else if (playerProperty.currentLevel == 3 && playerMaterial.GetFloat("Step2To3") < 0.99f)
            {
                playerMaterial.SetFloat("Step2To3",m_materialFloat);
            }
            else
            {
                m_isGrow = false;
            }
        }
    }

    /// <summary>
    /// Improve Clean Value
    /// </summary>
    private void PlayerClean() 
    {
        Debug.Log("Clean !");
        if (stateController.playerStateLock) return;
        playerProperty.ChangeCleanValue(true,playerProperty.cleanOnceValue);
        stateController.ChangeAniState(PlayerInteractAniState.Clean);
        EventCenter.Broadcast(GameEvents.BecomeGrowth);
    }

    private void PlayerGrabItemInHand() 
    {
        if (stateController.playerStateLock) return;
       stateController.ChangeAniState(PlayerInteractAniState.Grab);
    }

    //Unity Animation Event
    //When Player Grab Animation Finished, Handle the Grab Logic
    private void GrabItemInHandLogic() 
    {
       if (canTakeList.Count <= 0) return;
       ItemProperties item = canTakeList[0];
       hand.GrabItem(item);
       canTakeList.Remove(item);
       if (canKnockList.Contains(item)) {
           canKnockList.Remove(item);
       }
       item.Catch(hand.playerHandModel);
    }

    private void PlayerBeginThrowItem()
    {
        if (m_isThrowing)
        {
            return;
        }

        SetIsThrowing(true);
    }

    private void PlayerEndThrowItem()
    {
        Debug.Assert(m_isThrowing, $"{nameof(m_isThrowing)} is not true but this function is called");
        SetIsThrowing(false);
    }

    private void PlayerReleaseItem() {
       stateController.ChangeAniState(PlayerInteractAniState.Release);
       if (hand.grabItemInHand == null) return;
       canTakeList.Add(hand.grabItemInHand);
       hand.grabItemInHand.Release();       
       hand.ReleaseGrabItem();
    }
    
    private void Knock() {
        if (stateController.playerStateLock) return;
        stateController.ChangeAniState(PlayerInteractAniState.Knock);
        transform.rotation = Quaternion.LookRotation(-(canKnockList[0].transform.position - transform.position).Y(transform.position.y).normalized);
        // transform.LookAt(canKnockList[0].transform);
        hand.grabItemInHand.KnockWith(canKnockList[0]);
        if (canKnockList[0].IsBroken)
        {
            canKnockList.Remove(canKnockList[0]);
        }
    }

    private void KnockLogic() {
        knockParticle.Play();
        if (hand.grabItemInHand.IsBroken && !hand.grabItemInHand.CanEat) {
            hand.grabItemInHand = null;
        }
    }

    private void EatFoodAniPlay() {
        if (stateController.playerStateLock) return;
        stateController.ChangeAniState(PlayerInteractAniState.Eat);
    }
    

    //Unity Animation Event
    //When Player Eat Animation Finished, Handle the Eat food Logic
    private void EatFood() {
        //if (! hand.grabItemInHand.CanEat) return;
        (float Oxygen, float health) foodAdd = hand.grabItemInHand.Eat();
        
        if (hand.grabItemInHand.GetComponent<Item_Urchin>())
        {
            AnimatorManager.instance.PlayerCelebrate();
        }
        hand.grabItemInHand.transform.parent = null;
        // hand.grabItemInHand.Release();
        hand.grabItemInHand = null;
        playerProperty.ChangeHealthValue(true, foodAdd.health);
        playerProperty.ChangeMaxOxygenValue(foodAdd.Oxygen);
        //playerProperty.ReActiveCounter((int)foodAdd.health);
        playerProperty.ChangeCleanValue(false,playerProperty.eatOnceCleanValue / 2);
        // rendererList.Clear();
        //UI Emotion
        EventCenter.Broadcast(GameEvents.BecomeGrowth);
    }
    // private void KnockItemsInHand() {
    //     hand.leftHand.BreakWith(hand.rightHand);
    // }

    #endregion


    #region items render setting

    // public List<Renderer> GetTargetItemRendererList(OutlineTarget target)
    // {
    //     switch (target)
    //     {
    //         case OutlineTarget.TakeAndEat:
    //             return GetPlayerCanTakeItemRendererList();                
    //         case OutlineTarget.SeaGrass:
    //             return GetPlayerCanCleanRendererList();                
    //         default:
    //             return null;
    //     }
    // }

    // public List<Renderer> GetPlayerCanCleanRendererList()
    // {
    //     if (seaWeedsList.Count <= 0) { return null; }
    //     List<Renderer> rendererList = new List<Renderer>();
    //     GameObject seaWeedTarget = seaWeedsList[0].gameObject;
    //     if (seaWeedTarget.GetComponent<Renderer>())
    //     {
    //         rendererList.Add(seaWeedTarget.GetComponent<Renderer>());
    //     }

    //     Renderer[] renderInChild = seaWeedTarget.GetComponentsInChildren<Renderer>();
    //     foreach (var render in renderInChild)
    //     {
    //         rendererList.Add(render);
    //     }
    //     return rendererList;
    // }

    // public List<Renderer> GetPlayerCanTakeItemRendererList()
    // {        
    //     if((canTakeList.Count <= 0 && hand.grabItemInHand == null)) { return null; }
    //     rendererList = new List<Renderer>();        

    //     if (canTakeList.Count > 0) {
    //         GameObject closestTarget = canTakeList[0].gameObject;
    //         Debug.Log(closestTarget.name);
    //         if (closestTarget.GetComponent<Renderer>())
    //         {
    //             rendererList.Add(closestTarget.GetComponent<Renderer>());
    //         }

    //         Renderer[] renderInChild = closestTarget.GetComponentsInChildren<Renderer>();
    //         foreach (var render in renderInChild)
    //         {
    //             rendererList.Add(render);
    //         }
    //     }
    //     if (hand.grabItemInHand != null) {
    //         if (hand.grabItemInHand.GetComponent<ItemProperties>()
    //             &&hand.grabItemInHand.GetComponent<ItemProperties>().CanEat
    //             && hand.grabItemInHand.GetComponent<ItemProperties>().IsBroken)
    //             {
    //                 if (hand.grabItemInHand.GetComponent<Renderer>()) {
    //                     rendererList.Add(hand.grabItemInHand.GetComponent<Renderer>());
    //                 }
    //                 Renderer[] renderInChild2 = hand.grabItemInHand.GetComponentsInChildren<Renderer>();
    //                 foreach (var render in renderInChild2)
    //                 {
    //                     rendererList.Add(render);
    //                 }
    //             }
    //     }

    //     Debug.Log($"Render List Count: {rendererList.Count}");

    //     return rendererList;
    // }

    #endregion

    
    public class CompareItem : IComparer<ItemProperties>
    {
        Transform player;
        public CompareItem(Transform player) {
            this.player = player;
        }

        public int Compare(ItemProperties itemA, ItemProperties itemB) {
            if (Vector3.Distance(itemA.transform.position, player.position) > Vector3.Distance(itemB.transform.position,player.position))
            {
                return 1;
            }
            else {
                return -1;
            }
        }
    }

    

}
