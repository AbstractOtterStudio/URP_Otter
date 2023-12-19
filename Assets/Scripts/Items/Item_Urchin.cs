using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_Urchin : ItemProperties
{
    bool isSpawned = false;
    bool m_hasShow;

    Vector3 m_spawnPos;
    Vector3 m_arrivePos;

    [SerializeField] float moveSpeed = 2f;

    private void Update()
    {
        if (isSpawned && !catchAttr.isGetCaught)
        {        
            transform.position = Vector3.Lerp(transform.position, m_arrivePos, Time.deltaTime * moveSpeed);
        }
    }

    public void UrchinSpawn(Transform spawnPos, Transform arrivePos)
    {
        if (Vector3.Distance(spawnPos.position, arrivePos.position) < 0.05f) { return; }

        isSpawned = true;
        transform.position = spawnPos.position;    
        m_arrivePos = arrivePos.position;
    }
}
