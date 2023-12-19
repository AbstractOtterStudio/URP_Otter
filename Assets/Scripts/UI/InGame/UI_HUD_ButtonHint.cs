using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_HUD_ButtonHint : UIBase
{
    Image buttonHintImg;
    [SerializeField] float showTime = 1.5f;
    Coroutine corou;
    bool isShowing { get { return corou != null; } }

    public override void Init()
    {
        buttonHintImg = GetComponent<Image>();

        EventCenter.AddListener<ButtonHintType>(GameEvents.ShowButtonHint, ShowHint);
        EventCenter.AddListener(GameEvents.HideButtonHint, HideHint);

        HideHint();
    }

    void Update()
    {
        transform.forward = Camera.main.transform.forward;
    }

    void OnDestroy()
    {
        EventCenter.RemoveListener<ButtonHintType>(GameEvents.ShowButtonHint, ShowHint);
        EventCenter.RemoveListener(GameEvents.HideButtonHint, HideHint);
    }

    void HideHint()
    {        
        buttonHintImg.color = buttonHintImg.color.A(0);
        buttonHintImg.sprite = null;
    }

    void ShowHint(ButtonHintType hintType)
    {        
        foreach (var hint in ConfigFile.GetConfigFile().buttonHints)
        {
            if (hint.hintType == hintType)
            {
                buttonHintImg.sprite = hint.hintSprite;
                break;
            }
        }

        if (isShowing)
        {
            StopCoroutine("HidingHint");
        }

        corou = StartCoroutine("HidingHint");
    }

    IEnumerator HidingHint()
    {
        buttonHintImg.color = buttonHintImg.color.A(1);
        yield return new WaitForSeconds(showTime);
        HideHint();
        corou = null;
    }

}
