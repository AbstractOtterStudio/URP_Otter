using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Env_WaterFall : TerrainEffectBase
{

    private Vector3 playerLastPos = Vector3.zero;
    void Start()
    {
        if (forbidWalls == null) {
            Debug.LogError($"Add Forbid Walls On the Script ! : {gameObject.name}");
        } 
    }

    void FixedUpdate()
    {
        if (hasEffect) {
            //EffectPlayer();
            //AccuratePlayerPos();
            WaterFallSpeedEffect();
            waterFallWithPlayer();
        }      
    }

    private void waterFallWithPlayer() 
    {
        if (playerMovement == null) 
        { 
            waterFallParticle.Stop();
            waterFallParticle.gameObject.SetActive(false);
            playerLastPos = Vector3.zero;
            hasEffect = false;
            return; 
        }
        waterFallParticle.gameObject.SetActive(true);
        if (! waterFallParticle.isPlaying) { waterFallParticle.Play(); }
        if (playerLastPos != Vector3.zero) {
            waterFallParticle.transform.position = playerLastPos;
        }
        playerLastPos = playerMovement.transform.position;
    }

    
}
