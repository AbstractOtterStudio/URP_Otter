using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float baseSpeed = GlobalSetting.playerInitSpeed;
    [SerializeField]
    private float acceleration = 0.8f;
    [SerializeField]
    private float directionSpeed = 0.65f;
    [SerializeField]
    private float rotationSpeed = 3f;

    [Header("Speed Ratios")]
    [SerializeField]
    private float addSpeedRatio = GlobalSetting.playerAddSpeedRatio;
    [SerializeField]
    private float slowSpeedRatio = 0.3f;
    [SerializeField]
    private float swimTimelyRatio = 2.5f;

    [Header("Dive Settings")]
    [SerializeField]
    private float diveDepth = 1.5f;
    [SerializeField]
    private float diveSpeed = 3f;

    [Header("Collision Settings")]
    [SerializeField]
    private float collisionReboundSpeed = 3f;

     [Header("Debug")]
    [SerializeField] private float currentSpeed;
    private float targetDiveDepth;
    private float targetFloatDepth;
    private Rigidbody rb;
    private PlayerStateController stateController;
    private PlayerInputHandler inputHandler;
    private Animator animator;
    private Vector3 movementInput;
    public bool IsMoving { get; private set; }

    public delegate void PlayerSpeedChangeHandler(PlayerSpeedState speedState);
    public PlayerSpeedChangeHandler OnPlayerSpeedChange { get; set; }

    private void Start()
    {
        targetFloatDepth = transform.position.y;
        targetDiveDepth = transform.position.y - diveDepth;
        currentSpeed = baseSpeed;

        OnPlayerSpeedChange = HandlePlayerSpeedChange;
        rb = GetComponent<Rigidbody>();
        stateController = GetComponent<PlayerStateController>();
        inputHandler = GetComponent<PlayerInputHandler>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        movementInput = inputHandler.MovementInput;
        IsMoving = movementInput != Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (GameManager.instance.GetGameAction())
        {
            MovePlayer();
        }
    }

    #region 玩家移动

    private void MovePlayer()
    {
        if (stateController.IsStateLocked && stateController.PlayerAniState != PlayerInteractAniState.Grab)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        Vector3 desiredVelocity = GetDesiredVelocity();
        rb.velocity = Vector3.Lerp(rb.velocity, desiredVelocity, acceleration * Time.deltaTime);
        if (movementInput != Vector3.zero)
        {
            RotatePlayer(desiredVelocity);
        }

        HandleDiveAndFloat();
    }

    private Vector3 GetDesiredVelocity()
    {
        Camera mainCamera = Camera.main;

        Vector3 right = mainCamera.transform.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up);
        
        Vector3 direction = (right * movementInput.x + forward * movementInput.z).normalized;

        direction = new Vector3(direction.x, 0, direction.z).normalized * directionSpeed;

        return direction * currentSpeed;
    }


    private void RotatePlayer(Vector3 desiredVelocity)
    {
        Quaternion targetRotation = Quaternion.LookRotation(-desiredVelocity);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void HandleDiveAndFloat()
    {
        if (stateController.PlayerPlaceState == PlayerPlaceState.Dive)
        {
            AdjustPlayerDepth(true, targetDiveDepth);
        }
        else if (stateController.PlayerPlaceState == PlayerPlaceState.Float)
        {
            AdjustPlayerDepth(false, targetFloatDepth);
        }
    }

    private void AdjustPlayerDepth(bool isDiving, float targetDepth)
    {
        if (isDiving && transform.position.y > targetDepth)
        {
            transform.position += Vector3.down * diveSpeed * Time.deltaTime;
        }
        else if (!isDiving && transform.position.y < targetDepth)
        {
            transform.position += Vector3.up * diveSpeed * Time.deltaTime;
        }
    }

    #endregion

    #region 速度控制

    private void HandlePlayerSpeedChange(PlayerSpeedState speedState)
    {
        switch (speedState)
        {
            case PlayerSpeedState.Fast:
                currentSpeed = baseSpeed * addSpeedRatio;
                break;
            case PlayerSpeedState.Slow:
                currentSpeed = baseSpeed * slowSpeedRatio;
                break;
            case PlayerSpeedState.Normal:
            default:
                currentSpeed = baseSpeed;
                break;
        }
    }

    public void ModifyCurrentSpeed(float ratio, bool isMultiplying)
    {
        currentSpeed = isMultiplying ? currentSpeed * ratio : currentSpeed / ratio;
        ClampSpeed();
    }

    private void ClampSpeed()
    {
        float maxSpeed = baseSpeed * swimTimelyRatio * (stateController.PlayerSpeedState == PlayerSpeedState.Fast ? addSpeedRatio : 1);
        if (currentSpeed > maxSpeed)
        {
            currentSpeed = maxSpeed;
        }
    }

    #endregion

    #region 碰撞处理

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 normal = collision.contacts[0].normal;
        Vector3 reboundDirection = Vector3.ProjectOnPlane(rb.velocity, normal).normalized;
        rb.velocity = reboundDirection * collisionReboundSpeed;
    }

    #endregion

    #region 公共方法

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

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

