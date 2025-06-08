using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crest;

/**
* Responsible for:
* 1. dynamically updating the weight of the player based on their speed.
*/
[RequireComponent(typeof(SphereWaterInteraction))]
public class WaterInteraction : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement = null;

    [SerializeField]
    [UnityEngine.Range(0.1f, 1.5f)]
    private float speedToWeightRatio = 1f; // 1 m/s speed == 1 kg weight

    [Tooltip("Diameter of object, for physics purposes. The larger this value, the more filtered/smooth the wave response will be.")]
    [SerializeField] private float _objectWidth = 3f;
    [Tooltip("Strength of buoyancy force per meter of submersion in water.")]
    [SerializeField] private float _buoyancyCoeff = 3f;
    [Tooltip("Offsets center of object to raise it (or lower it) in the water.")]
    [SerializeField] private float _raiseObject = 1f;


    private float basePlayerWaterWeight = 10.0f;
    private float? lastPlayerSpeed = null;
    private SphereWaterInteraction sphereWaterInteraction = null;

    private SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();
    private bool _inWater = false;

    [DebugDisplay]
    private float CurWeight => sphereWaterInteraction == null ? 0 : sphereWaterInteraction._weight;

    void Awake()
    {
        sphereWaterInteraction = GetComponent<SphereWaterInteraction>();
        basePlayerWaterWeight = sphereWaterInteraction._weight;
    }

    // Update is called once per frame
    void Update()
    {
        if (lastPlayerSpeed == null || !Mathf.Approximately(playerMovement.GetCurrentSpeed(), lastPlayerSpeed.Value))
        {
            lastPlayerSpeed = playerMovement.GetCurrentSpeed();
            sphereWaterInteraction._weight = basePlayerWaterWeight + lastPlayerSpeed.Value * speedToWeightRatio;
        }
    }
}
