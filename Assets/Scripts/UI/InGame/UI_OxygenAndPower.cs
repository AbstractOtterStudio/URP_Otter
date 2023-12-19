using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//氧氣值及耐力值UI
public class UI_OxygenAndPower : UIBase
{
    CanvasGroup canvas;
    Image oxygenFill;
    Image powerFill;    
    

    public override void Init()
    {
        canvas = GetComponent<CanvasGroup>();
        oxygenFill = transform.Find("Img_Oxygen_Fill").GetComponent<Image>();
        powerFill = transform.Find("Img_Power_Fill").GetComponent<Image>();

        // EventCenter.AddListener<float>(GameEvents.UpdateOxygen, UpdateOxygen);
        // EventCenter.AddListener<float>(GameEvents.UpdatePower, UpdatePower);
        // EventCenter.AddListener(GameEvents.HideOxygenAndPower, Hide);

        Hide();        
    }

    void Update()
    {                
        transform.forward = Camera.main.transform.forward;        
    }

    void OnDestroy()
    {
        // EventCenter.RemoveListener<float>(GameEvents.UpdateOxygen, UpdateOxygen);
        // EventCenter.RemoveListener<float>(GameEvents.UpdatePower, UpdatePower);
        // EventCenter.RemoveListener(GameEvents.HideOxygenAndPower, Hide);
    }

    void ShowOxygen()
    {        
        canvas.alpha = 1;        
    }

    void Hide()
    {
        canvas.alpha = 0;        
    }

    void UpdateOxygen(float value)
    {
        ShowOxygen();
        //Left Side
        oxygenFill.fillAmount = 0.5f + value * 0.5f;
    }

    void UpdatePower(float value)
    {
        ShowOxygen();
        //Right Side
        powerFill.fillAmount = 0.5f + value * 0.5f;
    }
}
