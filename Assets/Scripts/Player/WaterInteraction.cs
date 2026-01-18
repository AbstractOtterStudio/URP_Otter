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
    [SerializeField] private Rigidbody playerRigidbody = null;

    [SerializeField]
    [UnityEngine.Range(0.1f, 1.5f)]
    private float speedToWeightRatio = 1f; // 1 m/s speed == 1 kg weight

    private float speed = 0;

    private float basePlayerWaterWeight = 10.0f;
    private float? lastPlayerSpeed = null;
    private SphereWaterInteraction sphereWaterInteraction = null;

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
        speed = playerMovement != null
            ? playerMovement.GetCurrentSpeed()
            : playerRigidbody.velocity.magnitude;

        if (!lastPlayerSpeed.HasValue || !Mathf.Approximately(speed, lastPlayerSpeed.Value))
        {
            lastPlayerSpeed = speed;
            sphereWaterInteraction._weight = basePlayerWaterWeight + speed * speedToWeightRatio;
        }
    }
}
