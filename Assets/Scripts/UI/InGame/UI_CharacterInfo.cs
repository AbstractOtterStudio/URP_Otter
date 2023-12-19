using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UI_CharacterInfo : UIBase
{
    CanvasGroup canvasGroup;
    Image healthImg;

    public override void Init()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        healthImg = transform.Find("Img_HpBg/Img_HpFill").GetComponent<Image>();

        EventCenter.AddListener(GameEvents.ShowCharacterInfo, ShowPlayerInfo);
        EventCenter.AddListener<float>(GameEvents.UpdateHealth, OnHealthValueChanged);

        healthImg.fillAmount = 1;
        HidePlayerInfo();
    }

    void OnDestroy()
    {
        EventCenter.RemoveListener(GameEvents.ShowCharacterInfo, ShowPlayerInfo);
        EventCenter.RemoveListener<float>(GameEvents.UpdateHealth, OnHealthValueChanged);
    }

    void ShowPlayerInfo()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1;        
    }

    void HidePlayerInfo()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    void OnHealthValueChanged(float value)
    {
        healthImg.fillAmount = value;
    }
}
