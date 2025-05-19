using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingManager : Singleton<PostProcessingManager>
{

    #region PostProcessing
    //Outline
    public PlayerController playerController;
    [SerializeField] UniversalRendererData renderData;
    //DayNight
    Volume volume;
    LiftGammaGain liftGammaGain;
    [SerializeField] float gammaChangeSpeed = 0.5f;
    [SerializeField] float gainChangeSpeed = 0.5f;
    [SerializeField] Vector4 morningGamma = new Vector4(1, 1, 1, -0.3f);
    [SerializeField] Vector4 morningGain = new Vector4(1, 1, 1, 0);
    [SerializeField] Vector4 eveningGamma = new Vector4(1, 0.5f, 0, -0.2f);
    [SerializeField] Vector4 eveningGain = new Vector4(1, 0.7f, 0.2f, -0.1f);
    [SerializeField] Vector4 nightGamma = new Vector4(0.1f, 0.2f, 1, -0.8f);
    [SerializeField] Vector4 nightGain = new Vector4(1, 1, 1, 0);
    VolumeParameter<Vector4> tempGamma = new VolumeParameter<Vector4>();
    VolumeParameter<Vector4> tempGain = new VolumeParameter<Vector4>();
    #endregion

    [SerializeField] float eveningTime = 30f;

    protected override void Awake()
    {
        base.Awake();

        volume = Camera.main.GetComponent<Volume>();
        volume.profile.TryGet(out liftGammaGain);

        tempGamma.value = morningGamma;
        tempGain.value = morningGain;

        if (liftGammaGain != null)
        {
            liftGammaGain.gamma.SetValue(tempGamma);
            liftGammaGain.gain.SetValue(tempGain);
        }

    }

    #region Weather Postprocessing
    public void DayPassing(float curTime)
    {
        if (curTime >= (GlobalSetting.dayTime + GlobalSetting.nightTime) - (1 / gammaChangeSpeed))
        {
            //Debug.Log("Goint To Morning");
            DayPassHelper_Gamma(morningGamma, gammaChangeSpeed);
            DayPassHelper_Gain(morningGain, gainChangeSpeed);
        }
        else if (curTime >= GlobalSetting.dayTime - (1 / gammaChangeSpeed))
        {
            //Debug.Log("Going To Night");
            DayPassHelper_Gamma(nightGamma, gammaChangeSpeed);
            DayPassHelper_Gain(nightGain, gainChangeSpeed);
        }
        else if (curTime >= (GlobalSetting.dayTime - eveningTime - (1 / gammaChangeSpeed)))
        {
            //Debug.Log("Going to Evening");
            DayPassHelper_Gamma(eveningGamma, gammaChangeSpeed);
            DayPassHelper_Gain(eveningGain, gainChangeSpeed);
        }
    }
    void DayPassHelper_Gamma(Vector4 toGamma, float speed)
    {
        tempGamma.value = Vector4.Lerp(tempGamma.value, toGamma, Time.deltaTime * speed);
        liftGammaGain.gamma.SetValue(tempGamma);
    }
    void DayPassHelper_Gain(Vector4 toGain, float speed)
    {
        tempGain.value = Vector4.Lerp(tempGain.value, toGain, Time.deltaTime * speed);
        liftGammaGain.gain.SetValue(tempGain);
    }
    #endregion
}
