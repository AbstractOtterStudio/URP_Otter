using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: 无意义class，可以删除
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

    void Awake() // TODO: 去掉，直接manager里awake
    {
        gameManager.Init();
        uiManager.Init();
        audioManager.Init();
        postProcessingManager.Init();
        objectPool.Init();
        mapSpawner.Init();
    }
}
