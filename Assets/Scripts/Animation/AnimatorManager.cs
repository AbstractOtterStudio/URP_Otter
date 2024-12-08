using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorManager : MonoBehaviour
{
    public static AnimatorManager instance { get; private set; }

    [SerializeField]
    private Animator m_playerAnimator;
    [SerializeField]
    private PlayerStateController stateController;
    public Animator playerAnimator { get => m_playerAnimator; set => m_playerAnimator = value; }
    private bool m_HasNPCSleep;

    void Start()
    {
        if (playerAnimator == null) {
            playerAnimator = FindObjectOfType<PlayerController>().GetComponent<Animator>();
        }

        if (stateController == null) {
            stateController = FindObjectOfType<PlayerStateController>();
        }

        if (instance == null) { instance = this; }
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        DetectMoveAniPlay();
        DetectInteractAniPlay();
        ChangeKnockAnimState();
        //DetectDiveOrFloatAniPlay();
    }

    private void DetectInteractAniPlay() 
    {
        switch (stateController.PlayerAniState) {
            case PlayerInteractAniState.Idle:
                playerAnimator.SetBool(ValueShortcut.anim_SleepOver,true);
                return; 

            case PlayerInteractAniState.Eat:
                if (!stateController.IsStateLocked) {
                    stateController.StateOnLock();
                    playerAnimator.SetTrigger(ValueShortcut.anim_Eat);
                }
                break;

            case PlayerInteractAniState.Knock:
                if (!stateController.IsStateLocked) {
                    stateController.StateOnLock();
                    playerAnimator.SetTrigger(ValueShortcut.anim_Knock);
                }               
                break;
            
            case PlayerInteractAniState.Grab:
                if (!stateController.IsStateLocked) {
                    stateController.StateOnLock();
                    playerAnimator.SetTrigger(ValueShortcut.anim_Grab);
                }            
                break;

            case PlayerInteractAniState.Release:
                // playerAnimator.SetBool(ValueShortcut.anim_Grab,false);
                stateController.ChangeAniState(PlayerInteractAniState.Idle);
                break;

            case PlayerInteractAniState.Clean:
                if (!stateController.IsStateLocked) 
                {
                    stateController.StateOnLock();
                    playerAnimator.SetInteger(ValueShortcut.anim_RandomInt,Random.Range(0,3));
                    playerAnimator.SetTrigger(ValueShortcut.anim_Clean);
                }
                break;

            case PlayerInteractAniState.Sleep:
                if (!stateController.IsStateLocked) {
                    stateController.StateOnLock();
                    if (m_HasNPCSleep)
                    {
                        playerAnimator.SetBool(ValueShortcut.anim_HasNPCSleep,true);
                    }
                    else
                    {
                        playerAnimator.SetBool(ValueShortcut.anim_HasNPCSleep,false);
                    }
                    playerAnimator.SetTrigger(ValueShortcut.anim_Sleep);
                    playerAnimator.SetBool(ValueShortcut.anim_SleepOver,false);
                    
                }
                break;

            case PlayerInteractAniState.Celebrate:
                if (!stateController.IsStateLocked) {
                    stateController.StateOnLock();
                    playerAnimator.SetTrigger(ValueShortcut.animName_Celebrate);
                }
                break;

            case PlayerInteractAniState.Throw:
                if (!stateController.IsStateLocked)
                {
                    stateController.StateOnLock();
                    playerAnimator.SetTrigger(ValueShortcut.anim_Throw);
                }
                break;

            default:
                break;
        }
    }

    private void DetectMoveAniPlay() 
    {
        switch (stateController.PlayerSpeedState) 
        {
            case PlayerSpeedState.Stop:
                playerAnimator.SetBool(ValueShortcut.anim_isWalk, false);
                break;

            case PlayerSpeedState.Normal:
                playerAnimator.SetBool(ValueShortcut.anim_isWalk, true);
                playerAnimator.SetBool(ValueShortcut.anim_FlipToBreast, false);                
                break;

            case PlayerSpeedState.Fast:
                playerAnimator.SetBool(ValueShortcut.anim_isWalk, true);
                playerAnimator.SetBool(ValueShortcut.anim_FlipToBreast, true);          
                break;

            default:
                break;
        }

    }

    public void DetectDiveOrFloatAniPlay() 
    {
        switch (stateController.PlayerPlaceState) {
            case PlayerPlaceState.Dive :
                    if (!stateController.IsStateLocked) {
                        stateController.StateOnLock();
                        playerAnimator.SetTrigger(ValueShortcut.anim_Dive);
                        playerAnimator.SetBool(ValueShortcut.anim_UnderWater,true);
                    }

                break;
            
            case PlayerPlaceState.Float :
                if (!stateController.IsStateLocked) {
                    stateController.StateOnLock();
                    playerAnimator.SetTrigger(ValueShortcut.anim_Float);
                    playerAnimator.SetBool(ValueShortcut.anim_UnderWater,false);
                }

                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Detest When Player Switch Knock Anim State to Others
    /// </summary>
    private void ChangeKnockAnimState()
    {
        if (stateController.IsKnocking) {
            playerAnimator.SetBool(ValueShortcut.anim_OnKnock, true);
        }
        else {
            playerAnimator.SetBool(ValueShortcut.anim_OnKnock, false);
        }
    }

    public bool IsSpecifyAnimationPlaying(string animName)
    {        
        bool playingAnim = playerAnimator.GetCurrentAnimatorStateInfo(0).IsName(animName);
        Debug.Log($"{animName} Playing State: {playingAnim}");
        return playingAnim;
    }

    public void OffLockState() 
    {
        stateController.StateOffLock();
        stateController.ChangeAniState(PlayerInteractAniState.Idle);
    }

    public void HasOtterNpc(bool hasNPC)
    {
        m_HasNPCSleep = hasNPC;
    }

    public void PlayerCelebrate()
    {
        playerAnimator.SetTrigger(ValueShortcut.anim_Celebrate);
    }

}
