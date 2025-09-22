using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
* 非完全基于物理的浮动物体
* Y坐标根据crest水面变化以transform调整
* XZ坐标根据flow变化以rg body或者transform调整
*/
public class FloatingObject : MonoBehaviour
{
    enum FlowAdjustmentType
    {
        None,
        Force,
        Transform
    }

    [Tooltip("超过这个delta我们才根据水位调整y坐标")]
    [SerializeField] private float minWaterAdjustmentDelta = 0.2f;

    [Tooltip("水位采样宽度")]
    [SerializeField] private float waterSamplingObjectWidth = 1.0f;

    [Tooltip("水流互动调整方式")]
    [SerializeField] private FlowAdjustmentType flowAdjustmentType = FlowAdjustmentType.Transform;

    [Tooltip("水流影响幅度")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float flowAdjustmentScale = 1.0f;

    public float VerticalAdjustmentSpeed { get; set; } = 1.0f;

    public Vector3 CurrentFlowXZ { get => currentFlowXZ; }

    private Crest.SampleHeightHelper sampleHeightHelper = new Crest.SampleHeightHelper();
    private Crest.SampleFlowHelper sampleFlowHelper = new Crest.SampleFlowHelper();

    private Vector3 currentFlowXZ;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    private void HandleWaterHeightAdjustment()
    {
        if (VerticalAdjustmentSpeed <= Mathf.Epsilon)
        {
            return;
        }

        sampleHeightHelper.Init(transform.position, waterSamplingObjectWidth, true);
        sampleHeightHelper.Sample(out Vector3 disp, out _, out _);

        if (Mathf.Abs(disp.y) > minWaterAdjustmentDelta)
        {
            float height = disp.y + Crest.OceanRenderer.Instance.SeaLevel;
            var pos = transform.position;
            pos.y = Mathf.MoveTowards(pos.y, height, VerticalAdjustmentSpeed * Time.deltaTime);
            transform.position = pos;
        }
    }

    private void HandleWaterFlowAdjustment()
    {
        if (flowAdjustmentScale < Mathf.Epsilon)
        {
            return;
        }

        sampleFlowHelper.Init(transform.position, waterSamplingObjectWidth);
        sampleFlowHelper.Sample(out Vector2 flow);
        currentFlowXZ = new Vector3(flow.x, 0f, flow.y) * flowAdjustmentScale;

        if (flow.sqrMagnitude > 0.001f)
        {
            switch (flowAdjustmentType) // your enum
            {
                case FlowAdjustmentType.Force:
                    rb.velocity = new Vector3(flow.x, 0f, flow.y) * flowAdjustmentScale;
                    break;

                case FlowAdjustmentType.Transform:
                    var pos = transform.position;
                    Vector3 target = pos + new Vector3(flow.x, 0f, flow.y) * flowAdjustmentScale * Time.deltaTime;
                    pos.x = Mathf.MoveTowards(pos.x, target.x, Time.deltaTime);
                    pos.z = Mathf.MoveTowards(pos.z, target.z, Time.deltaTime);
                    transform.position = pos;
                    break;

                case FlowAdjustmentType.None:
                default:
                    break;
            }
        }
    }


    private void FixedUpdate()
    {
        if (GameManager.Instance.GetGameAction())
        {
            HandleWaterHeightAdjustment();
            HandleWaterFlowAdjustment();
        }
    }

    void OnDrawGizmosSelected()
    {
        var backup = Gizmos.color;
        try
        {
            Gizmos.color = Color.red;
            Handles.Label(transform.position, $"浮动物体大小: {waterSamplingObjectWidth}");
            Gizmos.DrawWireCube(transform.position, new Vector3(waterSamplingObjectWidth, 0.1f, waterSamplingObjectWidth));
        }
        finally
        {
            Gizmos.color = backup;
        }
    }

    void OnValidate()
    {
        if (flowAdjustmentType == FlowAdjustmentType.Force)
        {
            if (!TryGetComponent(out Rigidbody _))
            {
                Debug.LogError("FloatingObject requires a Rigidbody component when flowAdjustmentType is Force");
                flowAdjustmentType = FlowAdjustmentType.Transform;
            }
        }
    }
}