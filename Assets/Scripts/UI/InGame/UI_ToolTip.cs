using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ToolTip : UIBase
{    
    Text toolTipText;

    Coroutine corou;
    bool isTipShowing { get { return corou != null; } }
    [SerializeField] float tipShowTime = 5f;

    public override void Init()
    {        
        toolTipText = GetComponent<Text>();

        toolTipText.text = string.Empty;

        EventCenter.AddListener<string>(GameEvents.ShowToolTip, ShowToolTip);

        HideToopTip();
    }

    void OnDestroy()
    {
        EventCenter.RemoveListener<string>(GameEvents.ShowToolTip, ShowToolTip);
    }

    void ShowToolTip(string msg)
    {
        toolTipText.color = toolTipText.color.A(1);

        if (isTipShowing)
        {
            StopCoroutine("WaitShowTime");
        }
        corou = StartCoroutine("WaitShowTime", msg);
    }

    void HideToopTip()
    {
        toolTipText.color = toolTipText.color.A(0);
        toolTipText.text = string.Empty;
    }

    IEnumerator WaitShowTime(string msg)
    {
        toolTipText.text = msg;
        yield return new WaitForSeconds(tipShowTime);
        corou = null;
        HideToopTip();
    }    

}
