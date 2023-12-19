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

    //Time for Player Pass the Terrain
    [SerializeField]
    [Range(1,3)]
    protected float effectTime;
    protected float effectHeight;
    protected Vector2 effectLength = Vector2.zero;

    protected float tempHeight;
    [SerializeField] protected float oriPlayerHeight;

    //Use to Judge Waterfall Height
    [SerializeField] public int waterfallLevel;

    //Use to Forbid Player Enter When Player in Dive PlaceState
    [SerializeField] protected GameObject forbidWalls;
    void Start()
    {
        if (forbidWalls == null) {
            Debug.LogError($"Add Forbid Walls On the Script ! : {gameObject.name}");
        }

        effectTime = effectTime * 0.7f + effectTime * 0.3f / waterfallLevel;
        effectHeight = (up.position.y - down.position.y) / effectTime;
        //effectLength = new Vector2(up.position.x - down.position.x, up.position.z - down.position.z) / effectTime;
        //effectHeight = ( up.transform.position.y - down.transform.position.y ) / effectParam;
        // effectHeight = Mathf.Sin(Math.Min(Mathf.Abs(waterfallMode.rotation.x * 180),180 - Mathf.Abs(waterfallMode.rotation.x * 180))) * waterfallMode.lossyScale.y / effectParam;
        // effectLength = Mathf.Cos(Math.Min(Mathf.Abs(waterfallMode.rotation.x * 180),180 - Mathf.Abs(waterfallMode.rotation.x * 180))) * waterfallMode.lossyScale.y / effectParam;

    }

    void FixedUpdate()
    {
        if (hasEffect) {
            EffectPlayer();
            AccuratePlayerPos();
        }
    }

    /// <summary>
    /// When enter Enviroment, Open effect on player speed
    /// Close Forbid Wall When Player Enter without Dive PlaceState
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerEnter(Collider other)
    {
        if (!hasEffect && other.GetComponent<PlayerMovement>()) {
            if (other.GetComponent<PlayerStateController>().playerPlaceState == PlayerPlaceState.Dive) {
                return;
            }
            else {
                forbidWalls.SetActive(false);
                oriPlayerHeight = other.transform.position.y;
            }
            playerMovement = other.GetComponent<PlayerMovement>();
            if (other.GetComponent<PlayerProperty>().currentLevel < waterfallLevel)
            {
                playerMovement.EffectCurSpeed(0.1f);
            }
            hasEffect = true;
            // lastDistance = Mathf.Abs(up.transform.position.x - playerMovement.transform.position.x)
            // * Mathf.Abs(up.transform.position.x - playerMovement.transform.position.x)
            // + Mathf.Abs(up.transform.position.z - playerMovement.transform.position.z)
            // *Mathf.Abs(up.transform.position.z - playerMovement.transform.position.z);
            if (Vector3.Distance(up.position, playerMovement.transform.position) > Vector3.Distance(down.position, playerMovement.transform.position)) {
                effectHeightRatio = (up.position.y - playerMovement.transform.position.y) / Vector3.Distance(up.position, playerMovement.transform.position);
            }
            else {
                effectHeightRatio = - (down.position.y - playerMovement.transform.position.y) / Vector3.Distance(down.position, playerMovement.transform.position);
            }
        }
    }

    /// <summary>
    /// When exit Environemnt, Close effect on player speed
    /// Open Forbid Wall Again
    /// </summary>
    /// <param name="other"></param>
    protected void OnTriggerExit(Collider other)
    {
        if (hasEffect && other.GetComponent<PlayerMovement>()) {
            //playerMovement.ReturnCurSpeed(effectPlayerRatio);
            if (other.GetComponent<PlayerProperty>().currentLevel < waterfallLevel)
            {
                playerMovement.ReturnCurSpeed(0.1f);
            }
            if (other.transform.position.y > (up.position.y + down.position.y) / 2 && oriPlayerHeight > (up.position.y + down.position.y) / 2) {
                playerMovement.transform.position = new Vector3 (playerMovement.transform.position.x, oriPlayerHeight, playerMovement.transform.position.z);
            }
            else if (other.transform.position.y < (up.position.y + down.position.y) / 2 && oriPlayerHeight < (up.position.y + down.position.y) / 2) {
                playerMovement.transform.position = new Vector3 (playerMovement.transform.position.x, oriPlayerHeight, playerMovement.transform.position.z);
            }
            else if (other.transform.position.y > (up.position.y + down.position.y) / 2 && oriPlayerHeight < (up.position.y + down.position.y) / 2) {
                playerMovement.transform.position = playerMovement.transform.position.Y(oriPlayerHeight + waterfallLevel * GlobalSetting.waterFallHeight);
                //playerMovement.transform.position = new Vector3 (playerMovement.transform.position.x, oriPlayerHeight + waterfallLevel * GlobalSetting.WaterFallHeight, playerMovement.transform.position.z);
                playerMovement.SetDiveOrFloatHeight(true,waterfallLevel * GlobalSetting.waterFallHeight);
            }
            else if (other.transform.position.y < (up.position.y + down.position.y) / 2 && oriPlayerHeight > (up.position.y + down.position.y) / 2){
                playerMovement.transform.position = playerMovement.transform.position.Y(oriPlayerHeight - waterfallLevel * GlobalSetting.waterFallHeight);
                //playerMovement.transform.position = new Vector3 (playerMovement.transform.position.x, oriPlayerHeight - waterfallLevel * GlobalSetting.WaterFallHeight, playerMovement.transform.position.z);
                playerMovement.SetDiveOrFloatHeight(false,waterfallLevel * GlobalSetting.waterFallHeight);
            }
            playerMovement = null;
            forbidWalls.SetActive(true);
            hasEffect = false;
            waterFallParticle.gameObject.SetActive(false);
        }
    }

    protected void AccuratePlayerPos() {
        tempHeight = up.position.y - effectHeightRatio * Vector3.Distance (up.position, playerMovement.transform.position);
        playerMovement.transform.position = new Vector3 (playerMovement.transform.position.x, tempHeight, playerMovement.transform.position.z);
    }

    protected void EffectPlayer() {
        if (playerMovement.GetComponent<PlayerStateController>().playerSpeedState != PlayerSpeedState.Fast) {
            playerMovement.transform.position -= new Vector3 (effectLength.x, effectHeight, effectLength.y) * Time.deltaTime; 
        }
	}
}
