using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeaverAI : MonoBehaviour
{
    public DamManager dam;
    public float catchRadius = 1.2f;
    Animator anim;
    bool isBuilding;
    public float buildTimer = 0.5f;
    const float buildCooldown = 0.6f;

    void Awake() => anim = GetComponent<Animator>();

    void Update()
    {
        if (isBuilding)
        {
            buildTimer -= Time.deltaTime;
            if (buildTimer <= 0)
            {
                isBuilding = false;
                if (anim) anim.SetBool("Building", false);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        RiverObject ro = other.GetComponent<RiverObject>();
        if (!ro) return;
        switch (ro.objectType)
        {
            case RiverObjectType.Material:
                dam.AddMaterial();
                BeginBuild();
                Debug.Log("Add Material Success!!!!!");
                break;
            case RiverObjectType.Junk:
                // optional penalty
                break;
            case RiverObjectType.Hazard:
                dam.DamageDam();
                break;
        }
        Destroy(other.gameObject);
    }

    void BeginBuild()
    {
        isBuilding = true;
        buildTimer = buildCooldown;
        if (anim) anim.SetBool("Building", true);
    }
}
