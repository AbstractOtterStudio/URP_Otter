using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_CollectableInfo : UIBase
{
    //CanvasGroup canvasGroup;
    //Image collectableImg;
    //Text collectableName;
    //Text collectableDescription;

    //Coroutine corou;
    //bool isCollectableInfoShowing { get { return corou != null; } }
    //[SerializeField] float infoShowTime = 5f;

    //public override void Init()
    //{
    //    canvasGroup = GetComponent<CanvasGroup>();
    //    collectableImg = transform.Find("Img_CollectableIcon").GetComponent<Image>();
    //    collectableName = transform.Find("Txt_CollectableName").GetComponent<Text>();
    //    collectableDescription = transform.Find("Txt_CollectableDescription").GetComponent<Text>();

    //    EventCenter.AddListener<string>(GameEvents.ShowCollectableInfo, ShowCollectableInfo);

    //    HideCollectableInfo();
    //}

    public override void Init()
    {
        
    }

    //void OnDestroy()
    //{
    //    EventCenter.RemoveListener<string>(GameEvents.ShowCollectableInfo, ShowCollectableInfo);    
    //}

    //void ShowCollectableInfo(string itemName)
    //{
    //    canvasGroup.blocksRaycasts = true;
    //    canvasGroup.alpha = 1;

    //    if (isCollectableInfoShowing)
    //    {
    //        StopCoroutine("WaitShowTime");
    //    }
    //    corou = StartCoroutine("WaitShowTime", itemName);
    //}

    //void HideCollectableInfo()
    //{
    //    canvasGroup.alpha = 0;
    //    canvasGroup.blocksRaycasts = false;

    //    collectableImg.sprite = null;
    //    collectableName.text = string.Empty;
    //    collectableDescription.text = string.Empty;
    //}

    //IEnumerator WaitShowTime(string itemName)
    //{
    //    foreach (var collectable in ConfigFile.GetConfigFile().collectables)
    //    {
    //        if (collectable.name.CompareTo(itemName) == 0)
    //        {
    //            collectableImg.sprite = collectable.icon;
    //            collectableName.text = collectable.name;
    //            collectableDescription.text = collectable.description;
    //        }
    //    }
    //    yield return new WaitForSeconds(infoShowTime);
    //    corou = null;
    //    HideCollectableInfo();
    //}
}
