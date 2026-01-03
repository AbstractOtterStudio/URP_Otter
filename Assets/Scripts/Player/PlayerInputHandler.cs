using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector3 MovementInput { get; private set; }   // 长度 ∈ [0,1]
    public Vector2 ScreenDelta   { get; private set; }   // 提供给 UI
    public Vector2 PlayerScreenPos { get; private set; } // 提供给 UI

    public bool IsInteracting { get; private set; }
    public bool IsEatingOrKnocking { get; private set; }
    public bool IsDiving { get; private set; }
    public bool IsAddingSpeed { get; private set; }

    [Header("Camera & Plane")]
    public Camera cam;                       // 俯视相机；为空自动用 Camera.main

    [Header("Radius (像素)")]
    public float innerRadiusPx = 120f;                // r₁
    public float outerRadiusPx = 200f;               // r₂

    [Header("Fine‑Tune (deg)")]
    [Tooltip("鼠标 → 角色行进方向的额外角度偏移（顺时针为正）")]
    public float moveAngleBiasDeg = 0f;

    [Header("Movement Smoothing (sec)")]
    [Range(0f,0.3f)] public float moveSmoothTime = 0.05f;
    private Vector3 moveVel;
    private Vector3 smoothDir;
    private Plane dynPlane;
    private static readonly Vector3 Up = Vector3.up;
    private bool isMouseInDeadZone;

    public bool ExternalBlockMovement { get; set; }

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        dynPlane = new Plane(Up, Vector3.zero);
        //Cursor.visible = false;
    }

    private void Start() {
        moveAngleBiasDeg = -cam.transform.eulerAngles.y;
    }

    private void Update()
    {
        if (ExternalBlockMovement)
        {
            MovementInput = Vector3.zero;
            return;
        }
        CalcScreenDelta();
        CalcMovementInput();
        HandleActionInput();
    }

     // 在屏幕平面上得到 Δ
    private void CalcScreenDelta()
    {
        PlayerScreenPos = cam.WorldToScreenPoint(transform.position);
        ScreenDelta = (Vector2)Input.mousePosition - PlayerScreenPos;
    }

    // Δ → 强度，Δ → 世界方向
    private void CalcMovementInput()
    {
        float enterRadius = Mathf.Max(0f, innerRadiusPx);
        if (enterRadius > 0f)
        {
            float exitRadius = Mathf.Max(outerRadiusPx, enterRadius + 1f)/4; // exit threshold must be larger to avoid jitter
            float sqrEnter = enterRadius * enterRadius/16;
            float sqrExit = exitRadius * exitRadius;
            float sqrDist = ScreenDelta.sqrMagnitude;

            if (isMouseInDeadZone)
            {
                if (sqrDist < sqrExit)
                {
                    ResetMovementInput();
                    return;
                }

                isMouseInDeadZone = false;
            }

            if (!isMouseInDeadZone && sqrDist <= sqrEnter)
            {
                isMouseInDeadZone = true;
                ResetMovementInput();
                return;
            }
        }
        // ── 1. 用射线把鼠标投到“角色所处水平面” ─────────────
        dynPlane.distance = -transform.position.y;      // 让平面通过角色
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!dynPlane.Raycast(ray, out float enter))
        {
            MovementInput = Vector3.zero;
            return;
        }

        Vector3 hit = ray.GetPoint(enter);
        Vector3 dir = hit - transform.position;
        dir.y = 0f;

        // 没有方向量（鼠标在角色正上方）
        if (dir.sqrMagnitude < 1e-6f)
        {
            MovementInput = Vector3.zero;
            return;
        }

        // ── 2. 施加角度偏移（顺时针为正） ─────────────────
        if (Mathf.Abs(moveAngleBiasDeg) > 0.01f)
            dir = Quaternion.AngleAxis(moveAngleBiasDeg, Up) * dir;

        dir.Normalize();          // 只保留方向

        // ── 3. 平滑（可选） ───────────────────────────────
        if (moveSmoothTime > 0f)
            smoothDir = Vector3.SmoothDamp(smoothDir, dir, ref moveVel, moveSmoothTime);
        else
            smoothDir = dir;

        MovementInput = smoothDir; // 大小恒为 1（或 0）
    }


    private void ResetMovementInput()
    {
        MovementInput = Vector3.zero;
        smoothDir = Vector3.zero;
        moveVel = Vector3.zero;
    }

    // private void HandleMovementInput()
    // {
    //     float h = Input.GetAxisRaw("Horizontal");
    //     float v = Input.GetAxisRaw("Vertical");
    //     MovementInput = new Vector3(h, 0, v).normalized;
    // }

    private void HandleActionInput()
    {
        IsInteracting = Input.GetKeyDown(GlobalSetting.InterectKey);
        IsEatingOrKnocking = Input.GetKeyDown(GlobalSetting.EatOrKnockKey);
        IsDiving = Input.GetKeyDown(GlobalSetting.DiveKey);
        IsAddingSpeed = Input.GetKey(GlobalSetting.AddSpeedKey);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
    Gizmos.color = Color.green;
    Gizmos.DrawLine(transform.position,
                    transform.position + MovementInput * 5f);
    }

}
