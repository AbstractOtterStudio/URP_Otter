using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OurLineCompment : MonoBehaviour
{
    public Renderer other;

    private void Awake()
    {
        OutLineManager.renderer2 =  other; 
    }
    private void Start()
    {
        OutLineManager.renderer =  gameObject.GetComponent<Renderer>(); 
        

    }
    private void OnMouseEnter()
    {
        OutLineManager.renderer = gameObject.GetComponent<Renderer>(); 
        Debug.Log(gameObject.name);
    }
    private void OnMouseExit()
    {
        OutLineManager.renderer = null; 
    }
}
