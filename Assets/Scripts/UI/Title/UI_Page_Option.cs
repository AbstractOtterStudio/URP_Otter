using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UI_Page_Option : UIBase
{
    CanvasGroup canvasGroup;
    Button returnBtn;

    public override void Init()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        returnBtn = transform.Find("Btn_Return").GetComponent<Button>();

        returnBtn.onClick.AddListener(OnReturnBtnClicked);

        EventCenter.AddListener(GameEvents.ShowOption, ShowOption);

        HideOption();
    }

    private void OnDestroy()
    {
        Debug.Log($"UI Option Have Been Destroyed");
        EventCenter.RemoveListener(GameEvents.ShowOption, ShowOption);
    }

    void ShowOption()
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
    }

    void HideOption()
    {
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    void OnReturnBtnClicked()
    {
        HideOption();
        EventCenter.Broadcast(GameEvents.ShowTitle);
    }
}
