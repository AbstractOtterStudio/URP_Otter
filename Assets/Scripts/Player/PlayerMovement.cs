using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("==== Basic Movement Settings ====")]
    [SerializeField] private float maxSpeed = 5f;       // 基础最大速度
    [SerializeField] private float acceleration = 10f;  // 加速度 (让角色 0.3~0.5s 加速到 maxSpeed)

    [Header("==== Additional Multipliers ====")]
    [SerializeField] private float fastMultiplier = 1.5f;  // 快速游动倍数
    [SerializeField] private float slowMultiplier = 0.5f;  // 慢速游动倍数
    // 其他 multiplier 可自定义，比如潜水 multiplier,diveSpeedMultiplier 等
    [SerializeField] private float diveSpeedMultiplier = 0.8f; // 潜水时速度倍数（示例）

    [Header("==== Turning Settings ====")]
    [SerializeField] private float minTurningSpeed = 3f;  
    [SerializeField] private float maxTurningSpeed = 6f;  
    [SerializeField] private float brakeAngle = 90f;        // 超过此夹角则进入刹车区
    [SerializeField] private float brakeSpeed = 1.0f;        // 刹车减速度

    [Header("==== Dive & Float Settings ====")]
    [SerializeField] private float diveDepth = 1.5f;
    [SerializeField] private float verticalSpeed = 3f;  // 用于上下浮潜速度

    [Header("==== Collision Settings ====")]
    [SerializeField] private float collisionReboundSpeed = 3f;

    [Header("==== Debug ====")]
    [SerializeField] private float currentSpeed;  // 当前速度（标量）
    private float targetDiveDepth;
    private float targetFloatDepth;

    private Rigidbody rb;
    private PlayerStateController stateController;
    private PlayerInputHandler inputHandler;
    private Animator animator;

    // 用于记录玩家的输入方向（平面）
    private Vector3 movementInput;
    private Vector3 currentVelocity;  // 用于保存实际的运动向量
    public bool IsMoving { get; private set; }

    #region Delegates
    public delegate void PlayerSpeedChangeHandler(PlayerSpeedState speedState);
    public PlayerSpeedChangeHandler OnPlayerSpeedChange { get; set; }
    #endregion

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        stateController = GetComponent<PlayerStateController>();
        inputHandler = GetComponent<PlayerInputHandler>();
        animator = GetComponent<Animator>();

        // 初始速度设为 0，或者设置为一个默认值也可以
        currentSpeed = 0f;
        currentVelocity = Vector3.zero;

        // 潜水/浮水的目标深度
        targetFloatDepth = transform.position.y;
        targetDiveDepth = transform.position.y - diveDepth;

        // 如果有需求，可注册此回调
        OnPlayerSpeedChange = HandlePlayerSpeedChange;
    }

    private void Update()
    {
        // 从输入获取移动方向（不带 Y 轴，主要在 XZ 平面）
        movementInput = inputHandler.MovementInput;
        IsMoving = movementInput != Vector3.zero;
    }

    private void FixedUpdate()
    {
        // 在此处判断游戏是否可交互
        if (GameManager.instance.GetGameAction())
        {
            MovePlayer();
        }
    }

    #region === 玩家移动核心逻辑 ===

    private void MovePlayer()
    {
        if (stateController.IsStateLocked && stateController.PlayerAniState != PlayerInteractAniState.Grab)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        // 1) 计算输入方向
        Vector3 desiredDirection = GetInputDirection();

        // 2) 判断当前朝向与输入方向的夹角
        float deltaAngle = Vector3.Angle(-transform.forward, desiredDirection);

        if (movementInput != Vector3.zero && desiredDirection.sqrMagnitude > 0.01f)
        {

            if (currentSpeed < 0.5f && deltaAngle > 120f)
            {
                TurnFirstThenMove(desiredDirection);
            }
            else 
            {
                // 如果大于 brakeAngle，则执行刹车逻辑
                if (deltaAngle > brakeAngle)
                {
                    BrakeAndTurn(desiredDirection);
                }
                else
                {
                    NormalTurnAndAccelerate(desiredDirection);
                }
            }
        }
        else
        {
            // 无输入时，逐渐减速到 0（也可以保持惯性，看需求）
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);
        }

        // 3) 更新刚体速度
        currentVelocity = desiredDirection.normalized * currentSpeed;
        rb.velocity = currentVelocity;

        // 4) 处理上下浮潜
        HandleDiveAndFloat();
    }

    // private void MovePlayer()
    // {
    //     if (stateController.IsStateLocked && stateController.PlayerAniState != PlayerInteractAniState.Grab)
    //     {
    //         rb.velocity = Vector3.zero;
    //         return;
    //     }

    //     Vector3 desiredVelocity = GetDesiredVelocity();
    //     rb.velocity = Vector3.Lerp(rb.velocity, desiredVelocity, acceleration * Time.deltaTime);
    //     if (movementInput != Vector3.zero)
    //     {
    //         RotatePlayer(desiredVelocity);
    //     }

    //     HandleDiveAndFloat();
    // }

    // private Vector3 GetDesiredVelocity()
    // {
    //     Camera mainCamera = Camera.main;

    //     Vector3 right = mainCamera.transform.right;
    //     Vector3 forward = Vector3.Cross(right, Vector3.up);
        
    //     Vector3 direction = (right * movementInput.x + forward * movementInput.z).normalized;

    //     direction = new Vector3(direction.x, 0, direction.z).normalized * directionSpeed;

    //     return direction * currentSpeed;
    // }


    // private void RotatePlayer(Vector3 desiredVelocity)
    // {
    //     Quaternion targetRotation = Quaternion.LookRotation(-desiredVelocity);
    //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    // }
    /// <summary>
    /// 计算玩家输入方向，基于相机朝向（保持在 XZ 平面）
    /// </summary>
    private Vector3 GetInputDirection()
    {
        Camera mainCamera = Camera.main;
        Vector3 right = mainCamera.transform.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up);

        // 输入方向(不考虑Y，保持在XZ平面)
        Vector3 direction = (right * movementInput.x + forward * movementInput.z).normalized;
        direction.y = 0f;
        return direction;
    }

    /// <summary>
    /// 刹车区逻辑：先快速将当前速度减为 0，再朝目标方向重新加速
    /// </summary>
    private void BrakeAndTurn(Vector3 desiredDirection)
    {
        // 先刹车
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeSpeed * Time.deltaTime);

        // 如果已经几乎停止，再开始朝新的方向加速
        if (currentSpeed < 0.5f)
        {
           TurnSmoothly(desiredDirection, maxTurningSpeed * 2);

            // 再按照普通加速度往 maxSpeed 加
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
        }
    }

    /// <summary>
    /// 普通转向并加速
    /// </summary>
    private void NormalTurnAndAccelerate(Vector3 desiredDirection)
    {
        // 计算 deltaAngle，用于在 minTurningSpeed ~ maxTurningSpeed 之间插值
        float angle = Vector3.Angle(transform.forward, desiredDirection);
        float t = angle / 180f;  // 0 ~ 1, 这里简单做个线性映射
        float turnSpeed = Mathf.Lerp(minTurningSpeed, maxTurningSpeed, t);

        // 平滑转向
        TurnSmoothly(desiredDirection, turnSpeed);

        // 加速到 maxSpeed
        currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
    }

    /// <summary>
    /// 瞬间转向（用于刹车完毕后的快速转向）
    /// </summary>
    private void TurnInstantly(Vector3 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(-desiredDirection, Vector3.up);
        }
    }

    /// <summary>
    /// 当速度很小且需要大角度掉头时，先转向，再移动
    /// </summary>
    private void TurnFirstThenMove(Vector3 desiredDirection)
    {
        // 1) 让速度保持在一个非常小的值（甚至可以直接设置为 0）
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeSpeed * Time.deltaTime);

        // 2) 用一个稍大的转向速度做平滑转向
        float fastTurnSpeed = maxTurningSpeed * 5f;
        TurnSmoothly(desiredDirection, fastTurnSpeed);

        // 3) 判断什么时候“转得差不多”了，可以开始加速
        float angleAfterTurn = Vector3.Angle(transform.forward, desiredDirection);
        if (angleAfterTurn < 15f)
        {
            // 开始加速
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
        }
    }

    /// <summary>
    /// 平滑转向
    /// </summary>
    private void TurnSmoothly(Vector3 desiredDirection, float turnSpeed)
    {
            if (desiredDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-desiredDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 浮潜处理
    /// </summary>
    private void HandleDiveAndFloat()
    {
        if (stateController.PlayerPlaceState == PlayerPlaceState.Dive)
        {
            // 潜水时可附加一个 multiplier
            float diveTargetSpeed = maxSpeed * diveSpeedMultiplier;
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, diveTargetSpeed);

            // 下潜
            if (transform.position.y > targetDiveDepth)
            {
                transform.position += Vector3.down * verticalSpeed * Time.deltaTime;
            }
        }
        else if (stateController.PlayerPlaceState == PlayerPlaceState.Float)
        {
            if (transform.position.y < targetFloatDepth)
            {
                transform.position += Vector3.up * verticalSpeed * Time.deltaTime;
            }
        }
    }

    #endregion

    #region === 速度控制 ===

    private void HandlePlayerSpeedChange(PlayerSpeedState speedState)
    {
        switch (speedState)
        {
            case PlayerSpeedState.Fast:
                // 快速倍数
                currentSpeed = currentSpeed * fastMultiplier;
                break;
            case PlayerSpeedState.Slow:
                currentSpeed = currentSpeed * slowMultiplier;
                break;
            case PlayerSpeedState.Normal:
            default:
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
                break;
        }
    }

    /// <summary>
    /// 对 currentSpeed 做乘法或除法倍数调整（保留原接口）
    /// </summary>
    public void ModifyCurrentSpeed(float multiplier, bool isMultiplying)
    {
        currentSpeed = isMultiplying ? currentSpeed * multiplier : currentSpeed / multiplier;
        // 可根据需要设置上限/下限
        if (currentSpeed > maxSpeed * fastMultiplier)
        {
            currentSpeed = maxSpeed * fastMultiplier;
        }
        if (currentSpeed < 0f)
        {
            currentSpeed = 0f;
        }
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    #endregion

    #region === 碰撞处理 ===

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 normal = collision.contacts[0].normal;
        Vector3 reboundDirection = Vector3.ProjectOnPlane(rb.velocity, normal).normalized;
        rb.velocity = reboundDirection * collisionReboundSpeed;
    }

    #endregion

    #region === 公共方法 ===

    /// <summary>
    /// 设置潜水或浮水的目标水面高度（示例）
    /// </summary>
    public void SetDiveOrFloatHeight(bool increase, float height)
    {
        if (increase)
        {
            targetDiveDepth += height;
            targetFloatDepth += height;
        }
        else
        {
            targetDiveDepth -= height;
            targetFloatDepth -= height;
        }
    }

    #endregion
}