using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
   [System.Serializable] public class SpawnEntry { public RiverObject prefab; [Range(0,1f)] public float weight = 1f; }
    public List<SpawnEntry> earlyPhase = new();
    public List<SpawnEntry> midPhase   = new();
    public List<SpawnEntry> latePhase  = new();
    public float spawnInterval = 1.2f;
    float timer;
    DamGameManager gm;
    void Start() => gm = FindObjectOfType<DamGameManager>();
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0) return;
        timer = spawnInterval;
        SpawnOne();
    }
    void SpawnOne()
    {
        List<SpawnEntry> table = gm.Progress < 0.3f ? earlyPhase : gm.Progress < 0.6f ? midPhase : latePhase;
        RiverObject prefab = Pick(table);
        Vector3 pos = transform.position + new Vector3(Random.Range(-3f, 3f), 0, 0);
        RiverObject inst = Instantiate(prefab, pos, Quaternion.identity);
        inst.Initialize(gm.FlowMultiplier);
    }
    RiverObject Pick(List<SpawnEntry> list)
    {
        float total = 0f; foreach (var e in list) total += e.weight;
        float roll = Random.Range(0, total);
        foreach (var e in list) { roll -= e.weight; if (roll <= 0) return e.prefab; }
        return list[Random.Range(0, list.Count)].prefab;
    }
}
