using System;
using System.Net.Security;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainEffectBase : TerrainBase
{
    protected float effectHeightRatio;

    //Judge Player State in Terrain
    [SerializeField]
    protected bool hasEffect;
    protected bool upToDown;
    protected bool downToUp;
    [SerializeField] protected ParticleSystem waterFallParticle;
    protected PlayerMovement playerMovement;

    //Use to Correct Player Position
    [SerializeField] protected Transform up;
    [SerializeField] protected Transform down;

    [SerializeField] protected float waterfallForce;

    protected float tempHeight;
    [SerializeField] protected float oriPlayerHeight;

    //Use to Judge Waterfall Height
    [SerializeField] public int waterfallLevel;

    //Use to Forbid Player Enter When Player in Dive PlaceState
    [SerializeField] protected GameObject forbidWalls;

    [SerializeField] private float lastDistFromUp = 0;
    [SerializeField] private float curDistFromUp = -1;
    private Transform playerTrans;
    [SerializeField] protected float vel;
    [SerializeField] protected float minVel;
    [SerializeField] protected float maxVel;

    void Start()
    {
        effectHeightRatio = (up.position.y - down.position.y) / Vector3.Distance(up.position, down.position);
    }

    void Update()
    {
        
    }

    /// <summary>
    /// When enter Enviroment, Open effect on player speed
    /// Close Forbid Wall When Player Enter without Dive PlaceState
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerEnter(Collider other)
    {
        if (!hasEffect && other.GetComponent<PlayerMovement>())
        {
            if (other.GetComponent<PlayerStateController>().playerPlaceState == PlayerPlaceState.Dive)
            {
                return;
            }
            else
            {
                forbidWalls.SetActive(false);
                oriPlayerHeight = other.transform.position.y;
            }
            playerMovement = other.GetComponent<PlayerMovement>();
            if (other.GetComponent<PlayerProperty>().currentLevel < waterfallLevel)
            {
                playerMovement.EffectCurSpeed(0.1f);
            }
            hasEffect = true;
            playerTrans = other.transform;
        }
    }

    /// <summary>
    /// When exit Environemnt, Close effect on player speed
    /// Open Forbid Wall Again
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerExit(Collider other)
    {
        if (hasEffect && other.GetComponent<PlayerMovement>())
        {
            if (other.transform.position.y > (up.position.y + down.position.y) / 2 && oriPlayerHeight > (up.position.y + down.position.y) / 2)
            {
                playerMovement.transform.position = new Vector3(playerMovement.transform.position.x, oriPlayerHeight, playerMovement.transform.position.z);
            }
            else if (other.transform.position.y < (up.position.y + down.position.y) / 2 && oriPlayerHeight < (up.position.y + down.position.y) / 2)
            {
                playerMovement.transform.position = new Vector3(playerMovement.transform.position.x, oriPlayerHeight, playerMovement.transform.position.z);
            }
            else if (other.transform.position.y > (up.position.y + down.position.y) / 2 && oriPlayerHeight < (up.position.y + down.position.y) / 2)
            {
                playerMovement.transform.position = playerMovement.transform.position.Y(oriPlayerHeight + waterfallLevel * GlobalSetting.waterFallHeight);
                //playerMovement.transform.position = new Vector3 (playerMovement.transform.position.x, oriPlayerHeight + waterfallLevel * GlobalSetting.WaterFallHeight, playerMovement.transform.position.z);
                playerMovement.SetDiveOrFloatHeight(true, waterfallLevel * GlobalSetting.waterFallHeight);
            }
            else if (other.transform.position.y < (up.position.y + down.position.y) / 2 && oriPlayerHeight > (up.position.y + down.position.y) / 2)
            {
                playerMovement.transform.position = playerMovement.transform.position.Y(oriPlayerHeight - waterfallLevel * GlobalSetting.waterFallHeight);
                //playerMovement.transform.position = new Vector3 (playerMovement.transform.position.x, oriPlayerHeight - waterfallLevel * GlobalSetting.WaterFallHeight, playerMovement.transform.position.z);
                playerMovement.SetDiveOrFloatHeight(false, waterfallLevel * GlobalSetting.waterFallHeight);
            }
            playerMovement = null;
            forbidWalls.SetActive(true);
            hasEffect = false;
            playerTrans = null;
            curDistFromUp = -1;
            lastDistFromUp = 0;
            waterFallParticle.gameObject.SetActive(false);
        }
    }

    protected void WaterFallSpeedEffect()
    {
        if (hasEffect && playerTrans != null && playerMovement != null)
        {
            vel = playerTrans.GetComponent<PlayerMovement>().GetCurrentSpeed();
            if (curDistFromUp > -1)
            {
                lastDistFromUp = curDistFromUp;
            }
            curDistFromUp = Vector3.Distance(up.position, playerTrans.position);
            if (curDistFromUp <= lastDistFromUp)
            {
                if (vel > minVel)
                {
                    playerMovement.EffectCurSpeed(0.85f);
                }
            }
            else
            {
                if (vel < maxVel)
                {
                    playerMovement.EffectCurSpeed(2f);
                }
            }
            playerTrans.position = new Vector3(playerTrans.position.x, oriPlayerHeight + effectHeightRatio * Vector3.Distance(up.position, playerTrans.position), playerTrans.position.z);
        }
    }
}
