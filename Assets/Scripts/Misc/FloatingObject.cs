using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
* 非基于物理的浮动物体
* 目前实现仅根据crest水面变化自动调整Y坐标
*/
public class FloatingObject : MonoBehaviour
{
    [SerializeField] private float minWaterAdjustmentDelta = 0.2f; // 超过这个delta我们才根据水位调整y坐标
    [SerializeField] private float waterSamplingObjectWidth = 1.0f; // 水位采样宽度

    public float AdjustmentSpeed { get; set; } = 1.0f; // 反应速度/调整速度

    private Crest.SampleHeightHelper sampleHeightHelper = new Crest.SampleHeightHelper();

    private void HandleWaterHeightAdjustment()
    {
        if (AdjustmentSpeed <= Mathf.Epsilon)
        {
            return;
        }

        sampleHeightHelper.Init(transform.position, waterSamplingObjectWidth, true);
        sampleHeightHelper.Sample(out Vector3 disp, out _, out _);

        if (Mathf.Abs(disp.y) > minWaterAdjustmentDelta)
        {
            float height = disp.y + Crest.OceanRenderer.Instance.SeaLevel;
            var pos = transform.position;
            pos.y = Mathf.MoveTowards(pos.y, height, AdjustmentSpeed * Time.deltaTime);
            transform.position = pos;
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.GetGameAction())
        {
            HandleWaterHeightAdjustment();
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
}