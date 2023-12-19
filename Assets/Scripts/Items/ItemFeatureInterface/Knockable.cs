using System.Net.Http;
using System;
using UnityEngine;

//可敲接口
public class Knockable : MonoBehaviour
{
    //敲撃次數（最大/當前）
    [SerializeField] protected int maxKnockNum = 5;
    [SerializeField] protected int currKnockNum = 0;

    //敲撃音效
    [SerializeField] SFX_Name knockSFX = SFX_Name.Knock_Hard;

    public bool IsBroken { get; private set; }
    void Start()
    {
        if (currKnockNum >= maxKnockNum) {
            IsBroken = true;
        }
    }
    public void OnBreak() 
    {
        if (IsBroken) { return; }
        if (maxKnockNum > currKnockNum)
        {
            //還可以能敲
            Debug.Log($"You are knocking {gameObject.name}.");
            AudioManager.instance.PlayLocalSFX(knockSFX, transform.position);
            currKnockNum++;
        }

        if (currKnockNum >= maxKnockNum)
        {
            Debug.Log($"Item {gameObject.name} is Broken!");
            //敲爛了
            BreakDown();
            EventCenter.Broadcast(GameEvents.ShowButtonHint, ButtonHintType.Button_X);
        }
    }

    void BreakDown()
    {
        IsBroken = true;
    }

    public void ResetKnockTime()
    {
        currKnockNum = 0;
    }
}
