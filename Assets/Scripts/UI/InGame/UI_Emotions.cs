using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Emotions : UIBase
{
    Animator happyEmotion;
    Animator confuseEmotion;
    Animator shockEmotion;
    Animator growthEmotion;
    Animator tiredEmotion;
    Animator hungryEmotion;
    Animator sleepToStrongEmotion;

    public override void Init()
    {
        happyEmotion = transform.Find("Emotion_Happy").GetComponent<Animator>();
        confuseEmotion = transform.Find("Emotion_Confuse").GetComponent<Animator>();
        shockEmotion = transform.Find("Emotion_Shock").GetComponent<Animator>();
        growthEmotion = transform.Find("Emotion_Growth").GetComponent<Animator>();
        tiredEmotion = transform.Find("Emotion_Tired").GetComponent<Animator>();
        hungryEmotion = transform.Find("Emotion_Hungry").GetComponent<Animator>();
        sleepToStrongEmotion = transform.Find("Emotion_SleepToStrong").GetComponent<Animator>();

        happyEmotion.gameObject.SetActive(false);
        confuseEmotion.gameObject.SetActive(false);
        shockEmotion.gameObject.SetActive(false);
        growthEmotion.gameObject.SetActive(false);
        tiredEmotion.gameObject.SetActive(false);
        hungryEmotion.gameObject.SetActive(false);
        sleepToStrongEmotion.gameObject.SetActive(false);

        EventCenter.AddListener(GameEvents.BecomeHappy, BecomeHappy);
        EventCenter.AddListener(GameEvents.BecomeConfuse, BecomeConfuse);
        EventCenter.AddListener(GameEvents.BecomeShock, BecomeShock);
        EventCenter.AddListener(GameEvents.BecomeGrowth, BecomeGrowth);
        EventCenter.AddListener(GameEvents.BecomeTired, BecomeTired);
        EventCenter.AddListener(GameEvents.BecomeHungry, BecomeHungry);
        EventCenter.AddListener(GameEvents.BecomeSleepy, BecomeSleepy);
    }

    private void OnDestroy()
    {
        EventCenter.RemoveListener(GameEvents.BecomeHappy, BecomeHappy);
        EventCenter.RemoveListener(GameEvents.BecomeConfuse, BecomeConfuse);
        EventCenter.RemoveListener(GameEvents.BecomeShock, BecomeShock);
        EventCenter.RemoveListener(GameEvents.BecomeGrowth, BecomeGrowth);
        EventCenter.RemoveListener(GameEvents.BecomeTired, BecomeTired);
        EventCenter.RemoveListener(GameEvents.BecomeHungry, BecomeHungry);
        EventCenter.RemoveListener(GameEvents.BecomeSleepy, BecomeSleepy);
    }

    // Update is called once per frame
    void Update()
    {
        transform.forward = Camera.main.transform.forward;
        if(Input.GetKeyDown(KeyCode.O)) { BecomeSleepy(); }
    }

    #region Emotion Function
    void BecomeHappy()
    {        
        happyEmotion.gameObject.SetActive(true);
        happyEmotion.Play(ValueShortcut.animName_OtterHappy);
        AudioManager.instance.PlayLocalSFX(SFX_Name.Happy, transform.position);
    }

    void BecomeConfuse()
    {
        confuseEmotion.gameObject.SetActive(true);
        confuseEmotion.Play(ValueShortcut.animName_OtterConfuse);
        AudioManager.instance.PlayLocalSFX(SFX_Name.Wondering, transform.position);
    }

    void BecomeShock()
    {
        shockEmotion.gameObject.SetActive(true);
        shockEmotion.Play(ValueShortcut.animName_OtterShock);
        AudioManager.instance.PlayLocalSFX(SFX_Name.Surprise, transform.position);
    }

    void BecomeGrowth()
    {
        growthEmotion.gameObject.SetActive(true);
        growthEmotion.Play(ValueShortcut.animName_OtterGrowth);
        AudioManager.instance.PlayLocalSFX(SFX_Name.Growth, transform.position);
    }

    void BecomeTired()
    {
        tiredEmotion.gameObject.SetActive(true);
        tiredEmotion.Play(ValueShortcut.animName_OtterTired);
        AudioManager.instance.PlayLocalSFX(SFX_Name.Tired, transform.position);
    }
    
    void BecomeHungry()
    {
        hungryEmotion.gameObject.SetActive(true);
        hungryEmotion.Play(ValueShortcut.animName_OtterHungry);
        AudioManager.instance.PlayLocalSFX(SFX_Name.Hungry, transform.position);
    }

    void BecomeSleepy()
    {
        sleepToStrongEmotion.gameObject.SetActive(true);
        sleepToStrongEmotion.Play(ValueShortcut.animName_OtterSleepToStrong);
    }
    #endregion
}
