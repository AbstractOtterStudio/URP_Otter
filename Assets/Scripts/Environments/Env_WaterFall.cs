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

        effectTime = effectTime * 0.7f + effectTime * 0.3f / waterfallLevel;
        effectHeight = (up.position.y - down.position.y) / effectTime;
        effectLength = new Vector2(up.position.x - down.position.x, up.position.z - down.position.z) / effectTime;    
    }

    void FixedUpdate()
    {
        if (hasEffect) {
            EffectPlayer();
            AccuratePlayerPos();
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
