using UnityEngine;

/// <summary>
/// 负责渲染拖拽移动的 UI：
///   • 箭头（arrowUI）始终跟随指针位置 & 朝向角色正前方。
///   • 内圈 / 外圈用 LineRenderer 一次性绘制，挂在角色子物体即可。
/// </summary>
public class UI_PointerDriveUIDrawer : MonoBehaviour
{
    [Header("Refs")]
    public PlayerInputHandler input;        // 必填
    public RectTransform canvasRect;        // Screen-Space Canvas 的根
    public RectTransform innerCircle;       // Image (圆)
    public RectTransform outerCircle;       // Image (圆)
    public RectTransform arrow;             // Image (剪头)

    [Header("Smoothing (sec) 0 = 没缓动")]
    [Range(0f,0.2f)] public float smoothTime = 0.06f;

    Vector2 innerVel, outerVel, arrowVel;   // SmoothDamp 用
    Vector2 innerPos, outerPos, arrowPos;   // 缓动后的最终位置

    private Vector2 virtualDelta   = Vector2.zero; // 虚拟鼠标(相对圆心)
    private Vector2 prevMousePixel = Vector2.zero; // 上一帧真实鼠标屏幕坐标

    [Tooltip("鼠标 → UI箭头朝向的额外角度偏移（顺时针为正）")]
    public float arrowAngleBiasDeg = 0f;
    public float arrowRadialBiasCv = 0f;

    void Awake()
    {
        if (!input) Debug.LogError("PlayerPointerUI 缺 PlayerInputHandler");
        prevMousePixel = Input.mousePosition;
    }
    


     void LateUpdate()
    {
        float scale = canvasRect.GetComponent<Canvas>().scaleFactor;

        // 1. 圆心 = 角色投影
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, input.PlayerScreenPos, null, out var center);

        // 2. ScreenDelta → Canvas Δ（只做方向，用来放置箭头）
        Vector2 deltaCv = input.ScreenDelta / scale;

        // 如果几乎没有拖动，就保持箭头在中心
        if (deltaCv.sqrMagnitude < 1e-4f) deltaCv = Vector2.zero;

        // 3. 保持在 outerRadius 内 + 位置 Bias
        float outer = input.outerRadiusPx / scale;
        if (deltaCv.sqrMagnitude > outer * outer)
            deltaCv = deltaCv.normalized * outer;

        if (Mathf.Abs(arrowRadialBiasCv) > 0.01f && deltaCv.sqrMagnitude > 1e-6f)
        {
            float r = Mathf.Clamp(deltaCv.magnitude + arrowRadialBiasCv, 0f, outer);
            deltaCv = deltaCv.normalized * r;
        }

        Vector2 targetInner = center;
        Vector2 targetOuter = center;
        Vector2 targetArrow = center + deltaCv;

        // 4. 位置平滑
        if (smoothTime > 0f)
        {
            innerPos = Vector2.SmoothDamp(innerPos, targetInner, ref innerVel, smoothTime);
            outerPos = Vector2.SmoothDamp(outerPos, targetOuter, ref outerVel, smoothTime);
            arrowPos = Vector2.SmoothDamp(arrowPos, targetArrow, ref arrowVel, smoothTime);
        }
        else
        {
            innerPos = targetInner;
            outerPos = targetOuter;
            arrowPos = targetArrow;
        }

        // 5. 写回 UI
        innerCircle.anchoredPosition = innerPos;
        outerCircle.anchoredPosition = outerPos;
        arrow.anchoredPosition       = arrowPos;

        // 箭头旋转 = 自己指向圆心 + 角度 Bias
        Vector2 dirCv = arrowPos - innerPos;
        if (dirCv.sqrMagnitude > 1e-4f)
        {
            float ang = Mathf.Atan2(dirCv.y, dirCv.x) * Mathf.Rad2Deg + arrowAngleBiasDeg;
            arrow.localEulerAngles = new Vector3(0, 0, ang);
        }

        // 圆直径同步
        float dOuter = input.outerRadiusPx * 2f / scale;
        float dInner = input.innerRadiusPx * 2f / scale;
        outerCircle.sizeDelta = new Vector2(dOuter, dOuter);
        innerCircle.sizeDelta = new Vector2(dInner, dInner);

    }
}
