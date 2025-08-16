using System;
using UnityEngine;

//可抓接口
public class Catchable : MonoBehaviour
{
    [SerializeField] bool isCollection = false;

    public bool isGetCaught { get; private set; }
    Transform catcher;

    [Header("Thrown Gate")]
    [Tooltip("玩家投出后，多少秒内视为“有效命中”窗口")]
    [SerializeField] private float thrownValidSeconds = 2.0f;

    float thrownExpireTime = -1f;
    public bool WasThrownByPlayer => Time.time <= thrownExpireTime;

    Rigidbody rb;
    RiverObject riverObj;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        riverObj = GetComponent<RiverObject>();
    }

    public void OnCatch(Transform catcher)
    {
        this.catcher = catcher;
        isGetCaught = true;
        thrownExpireTime = -1f;

        // 1) 停止物理
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        // 2) 告诉 RiverObject：现在被拿在手里，别再做“漂浮”位移
        if (riverObj) riverObj.SetHeld(true);
    }

    public void OnRelease()
    {
        // 从手里离开（放下或将要投掷）
        catcher = null;
        isGetCaught = false;

        // 立刻恢复物理（投掷/放下都会走这里）
        if (rb)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            // 重力是否开启由上层决定（投掷时通常为 true；若要平放到水面，可再关）
            rb.useGravity = true;
        }
        if (riverObj) riverObj.SetHeld(false);
    }

    /// 玩家真正“投出”时调用（有效命中窗口开始）
    public void MarkThrownByPlayer()
    {
        thrownExpireTime = Time.time + thrownValidSeconds;
    }

    private void Update()
    {
        // 跟手（非收集物才跟手）
        if (isGetCaught && catcher != null && !isCollection)
        {
            transform.position = catcher.position;
        }
    }

    public bool IsCollection() => isCollection;
}
