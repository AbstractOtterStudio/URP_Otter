using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crest;

[RequireComponent(typeof(TimeProviderCustom))]
public class OceanTimeTicker : MonoBehaviour
{
    public float TimeScale = 1.0f;
    private TimeProviderCustom _timeProvider;

    void Awake()
    {
        _timeProvider = GetComponent<TimeProviderCustom>();
    }

    // Update is called once per frame
    void Update()
    {
        _timeProvider._time += Time.deltaTime * TimeScale;
    }
}
