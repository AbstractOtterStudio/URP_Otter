using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    [SerializeField] UIBase[] uis;


    public void Init() 
    {
        if (uis == null || uis.Length == 0) 
        {            
            throw new System.Exception("UI List is Empty");
        }
        foreach (UIBase ui in uis)
        {
            Debug.Log(ui.name);
            ui.Init();
        }
    }
}
