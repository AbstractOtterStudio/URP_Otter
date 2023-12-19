using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    #region Managers
    [SerializeField] GameManager gameManager;
    [SerializeField] UIManager uiManager;
    [SerializeField] AudioManager audioManager;
    [SerializeField] PostProcessingManager postProcessingManager;
    [SerializeField] ObjectPool objectPool;
    [SerializeField] MapAnimalSpawner mapSpawner;
    #endregion

    void Awake()
    {
        gameManager.Init();
        uiManager.Init();
        audioManager.Init();
        postProcessingManager.Init();
        objectPool.Init();
        mapSpawner.Init();
    }
}
