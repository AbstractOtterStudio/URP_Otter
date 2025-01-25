using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RippleCreator : MonoBehaviour
{
    [SerializeField] private ParticleSystem rippleParticle;
    [SerializeField] private float createGapTime;
    [SerializeField] private float minCreateDist;
    private Vector3 rippleInitPos = Vector3.zero;
    private float createTimer = 0;

    private PlayerStateController playerStateController;
    private PlayerMovement playerMovement;
    private float createGapTimeWithMove;
    private float playerOriSpeed;

    private List<ParticleSystem> rippleList = new List<ParticleSystem>();
    private bool hasCreateParent;
    private GameObject rippleParent;
    private int listCapacity;
    private int reuseIndex = 0;
    void Start()
    {
        if(rippleParticle == null)
        {
            Debug.LogError($"No RippleParticle On {gameObject.name} !");
        }

        if (GetComponent<PlayerStateController>())
        {
            playerStateController = GetComponent<PlayerStateController>();
        }

        if(GetComponent<PlayerMovement>())
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
        playerOriSpeed = GlobalSetting.playerInitSpeed;
        createGapTimeWithMove = createGapTime;
        listCapacity = 2 * (1 + (int)(rippleParticle.main.duration / createGapTime)) - 1;
        hasCreateParent = false;
    }

    // Update is called once per frame
    void Update()
    {
        createTimer += Time.deltaTime;
        createGapTimeWithMove = createGapTime / Mathf.Max(1, playerMovement.GetCurrentSpeed()/playerOriSpeed);
        if (createTimer > createGapTimeWithMove)
        {
            createTimer = 0;
            if(minCreateDist < Vector3.Distance(transform.position, rippleInitPos))
            {
                if (playerStateController != null && playerStateController.PlayerPlaceState != PlayerPlaceState.Dive)
                {
                    if(!hasCreateParent)
                    {
                        hasCreateParent = true;
                        rippleParent = new GameObject("Ripple List");
                    }

                    if (rippleList.Count < listCapacity)
                    {
                        ParticleSystem ripple = Instantiate(rippleParticle, transform.position, Quaternion.Euler(new Vector3(90, 0, 0)));
                        ripple.transform.parent = rippleParent.transform;
                        rippleParticle.Play();
                        rippleList.Add(ripple);
                    }
                    else
                    {
                        if (reuseIndex >= listCapacity) reuseIndex = 0;
                        rippleList[reuseIndex].transform.position = transform.position;
                        rippleList[reuseIndex].Play();
                        ++reuseIndex;
                    }
                }
            }
            rippleInitPos = transform.position;
        }
    }
}
