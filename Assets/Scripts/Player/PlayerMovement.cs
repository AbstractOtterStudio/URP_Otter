using System;
using System.Collections;
using System.Collections.Generic;
using Crest;
using Crest.Internal;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    enum State
    {
        NotInWater,
        Floating,
        Diving,
    }

    [Header("==== Basic Movement Settings ====")]
    [SerializeField] private float _maxSpeed = 5f;       // 基础最大速度
    [SerializeField] private float _maxAccel = 10f;
    [SerializeField] private float _maxAngularSpeedDeg = 10f;
    [SerializeField]
    [Tooltip("An animation curve that represents the response of the player's movement to the input. The x axis is the input strength, the y axis is the response strength.")]
    private AnimationCurve _responseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [SerializeField, UnityEngine.Range(0f, 20f)] private float _yawDeadzoneDeg = 2f;
    [SerializeField]
    [Tooltip("Yaw response curve. X: normalized yaw error (0-1), Y: response strength.")]
    private AnimationCurve _yawResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("==== Dive & Float Settings ====")]
    [SerializeField] private float _diveTransitionSpeed = 3f;
    [SerializeField] private float _diveDepth = 1.5f;

    [Header("==== Collision Settings ====")]
    [SerializeField] private float _collisionReboundImpulse = 3f;

    [Header("Facing")]
    [SerializeField] private bool _flipModelForward = true;

    private float _speedMultiplier = 1f;

    private float _targetDiveDepth;
    private float _targetFloatDepth;

    private Rigidbody _rb;
    private PlayerStateController _stateController;
    private PlayerProperty _playerProperty;
    private PlayerInputHandler _inputHandler;

    // 用于记录玩家的输入方向（平面）
    private Vector3 _movementInput;
    public bool IsMoving { get; private set; }

    #region Delegates
    public delegate void PlayerSpeedChangeHandler(PlayerSpeedState speedState);
    public PlayerSpeedChangeHandler OnPlayerSpeedChange { get; set; }
    #endregion

    #region Coroutine
    private Coroutine _diveCoroutine = null;
    private Coroutine _floatCoroutine = null;
    #endregion

    [DebugDisplay]
    private State _state = State.NotInWater;


    [Header("==== Water Check Settings ====")]
    [SerializeField] private float _waterSampleWidth = 1f;

    [Header("==== Buoyancy Settings ====")]
    [SerializeField, Tooltip("The transform to sample the water height and flow and test if the player is in water. The position is the sample point.")] Transform _waterProbe;
    [SerializeField, UnityEngine.Range(0f, 2.0f)]
    private float _waterCheckTolerance = 0.1f;
    [SerializeField] private float _buoyancyCoeff = 3f;
    [SerializeField] private float _maximumBuoyancyForce = Mathf.Infinity;
    [SerializeField] private float _dragInWaterUp = 3f;
    [SerializeField] private float _buoyancyTorque = 8f;
    [SerializeField] private float _dragInWaterRotational = 0.2f;
    [SerializeField, UnityEngine.Range(0f, 60f)] private float _maxRollPitchDeg = 20f;


    #region Floating State Variables
    private readonly SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();
    private readonly SampleFlowHelper _sampleFlowHelper = new SampleFlowHelper();
    // displacement from water surface (down is positive)
    private float _submersionHeight;
    private Vector3 _waterSurfaceVel;
    private Vector3 _waterNormal = Vector3.up;
    #endregion

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _stateController = GetComponent<PlayerStateController>();
        _playerProperty = GetComponent<PlayerProperty>();
        _inputHandler = GetComponent<PlayerInputHandler>();

        // 潜水/浮水的目标深度
        _targetFloatDepth = transform.position.y;
        _targetDiveDepth = transform.position.y - _diveDepth;

        // 如果有需求，可注册此回调
        OnPlayerSpeedChange = HandlePlayerSpeedChange;

        _stateController.OnStateChanged += HandleStateChanged;
        UpdateSpeedMultiplier();
    }

    private void Update()
    {
        // 从输入获取移动方向（不带 Y 轴，主要在 XZ 平面）
        _movementInput = _inputHandler.MovementInput;
        IsMoving = _movementInput != Vector3.zero;
    }

    private void FixedUpdate()
    {
        UpdateWaterState();

        if (_state == State.Floating)
        {
            ApplyBuoyancyForce();
        }

        if (GameManager.Instance.GetGameAction())
        {
            MovePlayer();
        }
    }

    #region === 玩家移动核心逻辑 ===
    private void MovePlayer()
    {
        // disable movement when in the middle of diving
        if (_diveCoroutine != null || _floatCoroutine != null)
        {
            _rb.velocity = Vector3.zero;
            return;
        }

        // disable movement when state is locked or in interaction animation
        if (_stateController.IsStateLocked && _stateController.PlayerAniState != PlayerInteractAniState.Grab)
        {
            _rb.velocity = Vector3.zero;
            return;
        }

        Vector3 desiredDirection = GetInputDirection();
        Debug.DrawLine(transform.position, transform.position + desiredDirection, Color.red);

        float inputStrength = _movementInput.magnitude;
        float responseStrength = _responseCurve.Evaluate(inputStrength);
        if (responseStrength <= 0f || desiredDirection == Vector3.zero)
        {
            ApplyLinearDamping();
            return;
        }

        ApplyYawRotation(desiredDirection);

        Vector3 desiredVelocity = desiredDirection * (_maxSpeed * _speedMultiplier * responseStrength);
        Vector3 currentVelocityXZ = Vector3.ProjectOnPlane(_rb.velocity, Vector3.up);
        Vector3 accel = (desiredVelocity - currentVelocityXZ) / Time.fixedDeltaTime;
        accel = Vector3.ClampMagnitude(accel, _maxAccel);
        _rb.AddForce(accel, ForceMode.Acceleration);
    }

    private void ApplyYawRotation(Vector3 desiredDirection)
    {
        Vector3 yawDirection = _flipModelForward ? -desiredDirection : desiredDirection;
        Vector3 currentForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 targetForward = Vector3.ProjectOnPlane(yawDirection, Vector3.up).normalized;
        if (currentForward.sqrMagnitude < 1e-6f || targetForward.sqrMagnitude < 1e-6f)
        {
            return;
        }

        float yawError = Vector3.SignedAngle(currentForward, targetForward, Vector3.up);
        if (Mathf.Abs(yawError) <= _yawDeadzoneDeg)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetForward, Vector3.up);
        float normalizedError = Mathf.Clamp01(Mathf.Abs(yawError) / 180f);
        float responseStrength = _yawResponseCurve.Evaluate(normalizedError);
        float slerpT = Mathf.Clamp01(_maxAngularSpeedDeg * responseStrength * Time.fixedDeltaTime);
        _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, slerpT));
    }

    private void ApplyLinearDamping()
    {
        Vector3 currentVelocityXZ = Vector3.ProjectOnPlane(_rb.velocity, Vector3.up);
        if (currentVelocityXZ.sqrMagnitude < 1e-6f)
        {
            return;
        }

        Vector3 dampingAccel = -currentVelocityXZ / Time.fixedDeltaTime;
        dampingAccel = Vector3.ClampMagnitude(dampingAccel, _maxAccel);
        _rb.AddForce(dampingAccel, ForceMode.Acceleration);
    }

    private void UpdateWaterState()
    {
        if (OceanRenderer.Instance == null)
        {
            _state = State.NotInWater;
            _submersionHeight = 0f;
            _waterSurfaceVel = Vector3.zero;
            return;
        }

        _sampleHeightHelper.Init(_waterProbe.position, _waterSampleWidth, true);
        _sampleHeightHelper.Sample(out Vector3 disp, out var normal, out var waterSurfaceVel);


        _submersionHeight = disp.y + OceanRenderer.Instance.SeaLevel - _waterProbe.position.y;
        Debug.DrawLine(_waterProbe.position, _waterProbe.position + Vector3.up * _submersionHeight, Color.green);

        _state = _submersionHeight <= -_waterCheckTolerance ? State.NotInWater : State.Floating;
        _waterNormal = normal;

        _sampleFlowHelper.Init(_waterProbe.position, _waterSampleWidth);
        _sampleFlowHelper.Sample(out var surfaceFlow);
        _waterSurfaceVel = waterSurfaceVel + new Vector3(surfaceFlow.x, 0f, surfaceFlow.y);
    }

    private void ApplyBuoyancyForce()
    {
        Vector3 buoyancy = _buoyancyCoeff * _submersionHeight * _submersionHeight * _submersionHeight * -Physics.gravity.normalized;
        if (_maximumBuoyancyForce < Mathf.Infinity)
        {
            buoyancy = Vector3.ClampMagnitude(buoyancy, _maximumBuoyancyForce);
        }
        _rb.AddForce(buoyancy, ForceMode.Acceleration);

        var velocityRelativeToWater = _rb.velocity - _waterSurfaceVel;
        float verticalDrag = _dragInWaterUp * Vector3.Dot(Vector3.up, -velocityRelativeToWater);
        _rb.AddForce(verticalDrag * Vector3.up, ForceMode.Acceleration);

        Vector3 limitedNormal = _waterNormal;
        float normalAngle = Vector3.Angle(Vector3.up, _waterNormal);
        if (normalAngle > _maxRollPitchDeg && normalAngle > 0f)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, _waterNormal).normalized;
            limitedNormal = Quaternion.AngleAxis(_maxRollPitchDeg, axis) * Vector3.up;
        }

        Vector3 torqueWidth = Vector3.Cross(transform.up, limitedNormal);
        _rb.AddTorque(torqueWidth * _buoyancyTorque, ForceMode.Acceleration);
        _rb.AddTorque(-_dragInWaterRotational * _rb.angularVelocity, ForceMode.Acceleration);

        Debug.DrawLine(transform.position, transform.position + buoyancy, Color.blue);
    }

    public void PlayerPause()
    {
        _rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void PlayerResume()
    {
        _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
    }

    /// <summary>
    /// 计算玩家输入方向，基于相机朝向（保持在 XZ 平面）
    /// </summary>
    private Vector3 GetInputDirection()
    {
        Camera mainCamera = Camera.main;
        Vector3 right = mainCamera.transform.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up);

        // 输入方向(不考虑Y，保持在XZ平面)
        Vector3 direction = (right * _movementInput.x + forward * _movementInput.z).normalized;
        direction.y = 0f;
        return direction;
    }

    /// <summary>
    /// 浮潜处理
    /// </summary>
    private void HandleStateChanged()
    {
        IEnumerator diveCoroutine()
        {
            // 下潜
            while (transform.position.y > _targetDiveDepth)
            {
                float newY = Mathf.Max(transform.position.y - _diveTransitionSpeed * Time.deltaTime, _targetDiveDepth);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                yield return null;
            }

            _state = State.Diving;
            _diveCoroutine = null;
        }

        IEnumerator floatCoroutine()
        {
            if (_state != State.Diving)
            {
                yield break; // already in floating state
            }

            while (transform.position.y < _targetFloatDepth)
            {
                float newY = Mathf.Min(transform.position.y + _diveTransitionSpeed * Time.deltaTime, _targetFloatDepth);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                yield return null;
            }

            _state = State.Floating; // will be updated in UpdateWaterState
            _floatCoroutine = null;
        }

        PlayerPlaceState curPlaceState = _stateController.PlayerPlaceState;
        if (curPlaceState == PlayerPlaceState.Dive || curPlaceState == PlayerPlaceState.Float)
        {
            if (_diveCoroutine != null)
            {
                StopCoroutine(_diveCoroutine);
                _diveCoroutine = null;
            }
            if (_floatCoroutine != null)
            {
                StopCoroutine(_floatCoroutine);
                _floatCoroutine = null;
            }
        }

        if (curPlaceState == PlayerPlaceState.Dive)
        {
            _diveCoroutine = StartCoroutine(diveCoroutine());
        }
        else if (curPlaceState == PlayerPlaceState.Float)
        {
            _floatCoroutine = StartCoroutine(floatCoroutine());
        }

        UpdateSpeedMultiplier();
    }

    #endregion

    #region === 速度控制 ===

    private void HandlePlayerSpeedChange(PlayerSpeedState speedState)
    {
        UpdateSpeedMultiplier();
    }


    /// <summary>
    /// 设置状态速度倍率（来自饥饿、清洁度等状态）
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0f, multiplier);
    }

    private void UpdateSpeedMultiplier()
    {
        float statusMultiplier = 1f;
        if (_stateController != null && _playerProperty != null)
        {
            float fullMultiplier = 1f;
            if (_stateController.PlayerFullState == PlayerFullState.Agony)
            {
                fullMultiplier = 1f - _playerProperty.Status.AgonySpeedRatio;
            }

            float cleanMultiplier = 1f;
            switch (_stateController.PlayerCleanState)
            {
                case PlayerCleanState.Dirty:
                    cleanMultiplier = 1f - _playerProperty.Status.DirtySpeedRatio;
                    break;
                case PlayerCleanState.TwiceDirty:
                    cleanMultiplier = 1f - _playerProperty.Status.DirtySpeedRatio * 2f;
                    break;
                case PlayerCleanState.Weak:
                    cleanMultiplier = 1f - _playerProperty.Status.DangerSpeedRatio;
                    break;
            }

            statusMultiplier = Mathf.Max(0f, fullMultiplier * cleanMultiplier);
        }
        _speedMultiplier = statusMultiplier;
    }

    public float GetCurrentSpeed()
    {
        return _rb.velocity.magnitude;
    }

    #endregion

    #region === 碰撞处理 ===

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 normal = collision.contacts[0].normal;
        Vector3 reboundDirection = Vector3.ProjectOnPlane(_rb.velocity, normal).normalized;
        _rb.AddForce(reboundDirection * _collisionReboundImpulse, ForceMode.Impulse);
    }

    #endregion
}