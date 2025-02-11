using UnityEngine;

/* Super simple movement controller. I don't recommend using this in production, it's just included to get a sense of how the movement
 * of LilDude feels. 
 */

namespace FluffyExample {
    [RequireComponent(typeof(CharacterController))]
    public class SimpleThirdPersonController : MonoBehaviour {
        [Header("Player")] [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")] [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)] [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)] [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")] [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")] public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        private const float CameraLerpSpeed = 10f;
        private float speed;
        private float animationBlend;
        private float targetRotation;
        private float rotationVelocity;
        private float verticalVelocity;
        private float terminalVelocity = 53.0f;
 
        private float jumpTimeoutDelta;
        private float fallTimeoutDelta;
 
        private int animIDSpeed;
        private int animIDGrounded;
        private int animIDJump;
        private int animIDFreeFall;
        private int animIDMotionSpeed;

        private Animator animator;
        private CharacterController controller;
        private SimpleCharacterAssetsInputs input;
        private GameObject mainCamera;

        private void Awake() {
            if (mainCamera == null) {
                mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start() {
            animator = GetComponent<Animator>();
            controller = GetComponent<CharacterController>();
            input = GetComponent<SimpleCharacterAssetsInputs>();

            AssignAnimationIDs();

            jumpTimeoutDelta = JumpTimeout;
            fallTimeoutDelta = FallTimeout;
        }

        private void Update() {
            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate() {
            CameraRotation();
        }

        private void AssignAnimationIDs() {
            animIDSpeed = Animator.StringToHash("Speed");
            animIDGrounded = Animator.StringToHash("Grounded");
            animIDJump = Animator.StringToHash("Jump");
            animIDFreeFall = Animator.StringToHash("FreeFall");
            animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck() {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            animator.SetBool(animIDGrounded, Grounded);
        }


        private void CameraRotation() {
            var cameraPivotPosition = getCameraMovementTransform().position;
            cameraPivotPosition += (transform.position - cameraPivotPosition) * (CameraLerpSpeed * Time.deltaTime);
            getCameraMovementTransform().position = cameraPivotPosition;
        }

        private Transform getCameraMovementTransform() {
            var parent = mainCamera.transform.parent;
            return parent == null ? mainCamera.transform : parent;
        }

        private void Move() {
            float targetSpeed = input.sprint ? SprintSpeed : MoveSpeed;

            if (input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset) {
                speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                speed = Mathf.Round(speed * 1000f) / 1000f;
            }
            else {
                speed = targetSpeed;
            }

            animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

            Vector3 inputDirection = new Vector3(input.move.x, 0.0f, input.move.y).normalized;

            if (input.move != Vector2.zero) {
                targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, RotationSmoothTime);

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

            controller.Move(targetDirection.normalized * (speed * Time.deltaTime) + new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime);

            animator.SetFloat(animIDSpeed, animationBlend);
            animator.SetFloat(animIDMotionSpeed, inputMagnitude);
        }

        private void JumpAndGravity() {
            if (Grounded) {
                fallTimeoutDelta = FallTimeout;


                animator.SetBool(animIDJump, false);
                animator.SetBool(animIDFreeFall, false);

                if (verticalVelocity < 0.0f) {
                    verticalVelocity = -2f;
                }

                if (input.jump && jumpTimeoutDelta <= 0.0f) {
                    verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    animator.SetBool(animIDJump, true);
                }

                if (jumpTimeoutDelta >= 0.0f) {
                    jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else {
                jumpTimeoutDelta = JumpTimeout;
                if (fallTimeoutDelta >= 0.0f) {
                    fallTimeoutDelta -= Time.deltaTime;
                }
                else {
                    animator.SetBool(animIDFreeFall, true);
                }

                input.jump = false;
            }

            if (verticalVelocity < terminalVelocity) {
                verticalVelocity += Gravity * Time.deltaTime;
            }
        }
    }
}