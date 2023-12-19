using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//場景動物/食物生成
public class MapAnimalSpawner : MonoBehaviour
{
    static AnimalSetting[] animalSettings { get { return ConfigFile.GetConfigFile().animalSettings; } }
    static Dictionary<AnimalPoolName, Destinations[]> animalDict;
    static List<GameObject> respawnList = new List<GameObject>();

    public void Init()
    {
        animalDict = new Dictionary<AnimalPoolName, Destinations[]>();        
        for (int i = 0; i < animalSettings.Length; i++)
        {
            animalDict.Add(animalSettings[i].poolName, animalSettings[i].patrolPoints);
        }
        
        Spawn(false);
    }

    public static void Spawn(bool isRespawn = true)
    {
        foreach (var animalType in animalDict)
        {
            //Fish, StarFish, Urchin not Respawn
            if(isRespawn)
            {
                if (animalType.Key == AnimalPoolName.Fish ||
                    animalType.Key == AnimalPoolName.JellyFish)
                {
                    continue;
                }
            }

            //Traverse all single animal in this animal type
            for (int i = 0; i < animalType.Value.Length; i++)
            {                
                ////Take current Type single animal from pool
                //Transform spawnAnimal = ObjectPool.TakeFromPool(animalType.Key.ToString());
                ////Init EscaperAgent
                ////Have Ai
                //if (spawnAnimal.GetComponent<EscaperAgent>())
                //{
                //    EscaperAgent escaper = spawnAnimal.GetComponent<EscaperAgent>();
                //    escaper.SetPatrolPoints(animalType.Value[i].destinations);                    
                //}
                ////Spawn Animal to its first patrol point
                //spawnAnimal.position = animalType.Value[i].destinations[0];

                SpawnHelper(animalType.Key, animalType.Value, i);
            }
        }
    }
    
    public static void AddAnimalToRespawnList(GameObject animal)
    {
        respawnList.Add(animal);
    }

    public static void RespawnAnimals()
    {
        if(respawnList.Count <= 0) { return; }
        for (int i = 0; i < respawnList.Count; i++)
        {
            if (respawnList[i].GetComponent<EscaperAgent>())
            {
                respawnList[i].GetComponent<EscaperAgent>().ResetState();
            }            
            respawnList[i].GetComponent<ItemProperties>().ResetProperties();
            respawnList[i].SetActive(true);
        }
        respawnList.Clear();
    }

    static void SpawnHelper(AnimalPoolName animalType, Destinations[] destinations, int desIndex)
    {
        //Take current Type single animal from pool
        Transform spawnAnimal = ObjectPool.TakeFromPool(animalType.ToString());
        //Init EscaperAgent
        //Have Ai
        if (spawnAnimal.GetComponent<EscaperAgent>())
        {
            EscaperAgent escaper = spawnAnimal.GetComponent<EscaperAgent>();
            escaper.SetPatrolPoints(destinations[desIndex].destinations);
        }        
        //Spawn Animal to its first patrol point
        spawnAnimal.position = destinations[desIndex].destinations[0];

        if (spawnAnimal.GetComponent<ItemProperties>())
        {
            spawnAnimal.GetComponent<ItemProperties>().InitProperties();
        }        
    }
}
