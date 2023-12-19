using System;
using UnityEngine;

//可抓接口
public class Catchable : MonoBehaviour
{
    //是否為收集物
    [SerializeField] bool isCollection = false;
    public bool isGetCaught;
    Transform catcher;

    public void OnCatch(Transform catcher)
    {
        this.catcher = catcher;
        isGetCaught = true;
        Debug.Log("Item Caught");
    }

    private void Update()
    {
        if (isGetCaught && catcher != null && !isCollection)
        {
            transform.position = catcher.transform.position;
        }
    }

    public void OnRelease()
    {
        Debug.Log("Item Released");
        catcher = null;
        isGetCaught = false;
    }   

    public bool IsCollection() { return isCollection; }     
}
