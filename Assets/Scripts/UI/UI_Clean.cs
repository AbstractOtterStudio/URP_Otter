using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//清潔度UI
public class UI_Clean : UIBase
{
    Slider m_Slider;

    public override void Init()
    {        
        //valueChangeEvent = GameEvents.UpdateClean;
        //m_Slider = transform.Find("slide").GetComponent<Slider>();
        //base.Init();
    }

    //protected override void ValueChange(float value)
    //{        
    //    m_Slider.value = value;
    //}
}
