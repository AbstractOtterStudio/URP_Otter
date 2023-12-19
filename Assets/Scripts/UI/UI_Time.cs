using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//時間條UI
public class UI_Time : UIBase
{
    Slider m_Slider;    
    public override void Init()
    {        
        //valueChangeEvent = GameEvents.UpdateTime;
        //m_Slider = transform.Find("slide").GetComponent<Slider>();
        //base.Init();
    }

    //protected override void ValueChange(float value)
    //{        
    //    m_Slider.value = value;
    //}
}
