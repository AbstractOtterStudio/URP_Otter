using System;
using UnityEngine;

//可吃接口
public class Eatable : MonoBehaviour
{    
    //經驗加成
    [SerializeField] float oxygenIncrease = 0;
    //飽腹值加成
    [SerializeField] float healthIncrease = 0;

    //進食音效
    [SerializeField] SFX_Name eatSFX = SFX_Name.Eat_Hard;

    public virtual (float oxygen, float health) GetFoodNutrition()
    {
        Debug.Log($"You ate a {gameObject.name}.");
        AudioManager.instance.PlayLocalSFX(eatSFX, transform.position);
        if (healthIncrease == 0) { EventCenter.Broadcast(GameEvents.BecomeConfuse); }
        (float oxygen, float health) nutrition;
        nutrition.oxygen = oxygenIncrease;
        nutrition.health = healthIncrease;
        if(GetComponent<Item_Urchin>())
        {
            gameObject.SetActive(false);
        }
        else
        {
            ObjectPool.ReturnToPool(this.gameObject);
            MapAnimalSpawner.AddAnimalToRespawnList(this.gameObject);
        }
        return nutrition;
    }    
}