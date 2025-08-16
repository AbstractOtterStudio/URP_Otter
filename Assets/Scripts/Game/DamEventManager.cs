using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class DamEventManager : MonoBehaviour
{
    public DamManager dam;
    public ObjectSpawner spawner;
    public RiverObject bigLogPrefab;
    bool triggered;
    void Update()
    {
        if (!triggered && dam.progress > 0.6f)
        {
            triggered = true;
            StartCoroutine(BigLogRoutine());
        }
    }
    IEnumerator BigLogRoutine()
    {
        CameraShaker.Shake(0.8f, 0.4f);
        yield return new WaitForSeconds(1f);
        RiverObject log = Instantiate(bigLogPrefab, spawner.transform.position, Quaternion.identity);
        log.Initialize(1.8f);
        dam.DamageDam();
    }
}
