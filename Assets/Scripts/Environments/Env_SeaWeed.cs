using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Env_SeaWeed : TerrainBase
{
    Material seaGrass_Mat;

    void Start()
    {
        canClean = true;
        canSleep = true;

        seaGrass_Mat = GetComponentInChildren<SkinnedMeshRenderer>().materials[0];        

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ValueShortcut.tag_Player))
        {
            seaGrass_Mat.SetFloat("Brighten", 2.5f);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ValueShortcut.tag_Player))
        {
            seaGrass_Mat.SetFloat("Brighten", 1);
        }
    }
}
