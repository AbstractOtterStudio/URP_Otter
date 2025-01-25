using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtterNPC : MonoBehaviour
{
    [SerializeField] private bool isSingle;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject urchin;
    [SerializeField] private Transform playerSleepPosition;
    
    bool m_hasGiven;
    void Start()
    {   
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (urchin == null)
        {
            Debug.LogError("Please Drog the Urchi to the Otter NPC !");
        }
        urchin.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        NPCLogicDetect();
    }

    private void NPCLogicDetect()
    {
        if (GameManager.instance.GetDayState() == DayState.Day)
        {
            if (isSingle)
            {
                animator.SetBool(ValueShortcut.anim_Sleep_Single, false);
            }
            else
            {
                animator.SetBool(ValueShortcut.anim_Sleep_Couple, false);
            }
        }
        else
        {
            if (isSingle)
            {
                animator.SetBool(ValueShortcut.anim_Sleep_Single, true);
            }
            else
            {
                animator.SetBool(ValueShortcut.anim_Sleep_Couple, true);
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (m_hasGiven == false && other.GetComponent<PlayerStateController>())
        {
            animator.SetBool(ValueShortcut.anim_PlayerCome, true);
            AnimatorManager.instance.HasOtterNpc(true);
            if (other.GetComponent<PlayerStateController>().PlayerAniState == PlayerInteractAniState.Sleep)
            {
                other.transform.position = playerSleepPosition.position;
                other.transform.rotation = playerSleepPosition.rotation;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (m_hasGiven == false && other.GetComponent<PlayerMovement>())
        {
            animator.SetBool(ValueShortcut.anim_PlayerCome, false);
            AnimatorManager.instance.HasOtterNpc(false);
        }
    }

    private void OtterNPCKnockLogic()
    {
        urchin.SetActive(true);
        urchin.transform.parent = null;
        urchin.GetComponent<Item_Urchin>().UrchinSpawn(urchin.transform,urchin.transform);
        animator.SetBool(ValueShortcut.anim_PlayerCome, false);
        m_hasGiven = true;
    }
}
