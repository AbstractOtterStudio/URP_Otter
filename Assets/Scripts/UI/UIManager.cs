using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    UIBase[] uis;


    public void Init() 
    {
        uis = FindObjectsOfType<UIBase>();
        
        foreach (UIBase ui in uis)
        {
            Debug.Log($"UI Manager: Initializing {ui.GetType().Name} on {ui.gameObject.name}");
            ui.Init();
        }
    }
}
