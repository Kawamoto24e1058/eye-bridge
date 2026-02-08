using UnityEngine;

namespace EyeTrackingGame.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    public class EyePlayerController : MonoBehaviour
    {
        [Header("Gaze Input (Set via External Script)")]
        [Tooltip("Screen X coordinate (Pixels)")]
        public float gazeX;
        [Tooltip("Screen Y coordinate (Pixels)")]
        public float gazeY;

        [Header("Wink Input (Set via External Script)")]
        [Tooltip("True when left eye is closed")]
        public bool isLeftEyeClosed;
        [Tooltip("True when right eye is closed")]
        public bool isRightEyeClosed;

        [Header("Movement Settings")]
        [SerializeField] private float forwardSpeed = 5.0f; // Speed to move forward (User added)
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float jumpForce = 8.0f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Crouch Settings")]
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float standingHeight = 2.0f;
        [SerializeField] private float crouchTransitionSpeed = 10.0f;

        private CharacterController _characterController;
        private Vector3 _velocity; // Vertical velocity
        private bool _wasLeftEyeClosed;
        private float _currentHeight;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
            // Initialize height based on settings or current component
            _currentHeight = standingHeight;
            _characterController.height = _currentHeight;
            _characterController.center = new Vector3(0, _currentHeight * 0.5f, 0);
        }

        private void Update()
        {
            // --- 1. Gravity & Ground Check ---
            // If grounded and falling, reset vertical velocity to a small value
            if (_characterController.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; 
            }

            // Apply gravity
            _velocity.y += gravity * Time.deltaTime;

            // --- 2. Input Logic ---
            HandleJumpInput();
            HandleCrouch();
            
            // --- 3. Movement Application ---
            HandleMovement();
        }

        [Header("Physics Settings")]
        [SerializeField] private float acceleration = 10.0f; // How fast to reach max speed
        [SerializeField] private float deceleration = 10.0f; // How fast to stop (Damping)

        private Vector3 _currentVelocity; // For smooth damping (Horizontal/Forward)

        private void HandleMovement()
        {
            // --- Determine Target Velocity ---
            Vector3 targetVelocity = Vector3.zero;

            // 1. Forward/Backward Input (Keyboard: W/S or Arrow Keys)
            // Use GetAxis directly for analog-like feel, or GetAxisRaw for digital
            float forwardInput = Input.GetAxis("Vertical"); 
            
            // If user wants auto-run, they can hold W. If they release, it stops.
            if (Mathf.Abs(forwardInput) > 0.1f)
            {
                targetVelocity += transform.forward * forwardInput * forwardSpeed;
            }

            // 2. Strafe Input (Keyboard: A/D + Gaze)
            float strafeInput = Input.GetAxis("Horizontal");
            
            // Combine Gaze (if available) with Keyboard
            // Gaze is usually -1.5 to 1.5, clamp if needed
            float combinedStrafe = strafeInput + gazeX;
            
            if (Mathf.Abs(combinedStrafe) > 0.1f) // Deadzone
            {
                targetVelocity += transform.right * combinedStrafe * moveSpeed;
            }

            // --- Apply Acceleration / Deceleration (Damping) ---
            // Manual implementation of "Linear Damping" for CharacterController
            
            // Move towards target velocity
            // If target is zero (input released), we decelerate.
            // If target is non-zero, we accelerate towards it.
            
            float speedChangeRate = (targetVelocity.magnitude > 0.1f) ? acceleration : deceleration;
            
            // Apply smoothing to X and Z specifically (leave Y for gravity)
            float targetY = _velocity.y; // Preserve gravity velocity
            
            // Use Vector3.MoveTowards for linear deceleration (like friction)
            Vector3 horizontalCurrent = new Vector3(_currentVelocity.x, 0, _currentVelocity.z);
            Vector3 horizontalTarget = new Vector3(targetVelocity.x, 0, targetVelocity.z);
            
            Vector3 newHorizontal = Vector3.MoveTowards(horizontalCurrent, horizontalTarget, speedChangeRate * Time.deltaTime);
            
            _currentVelocity = new Vector3(newHorizontal.x, targetY, newHorizontal.z);

            // Apply Move
            _characterController.Move(_currentVelocity * Time.deltaTime);
        }

        private void HandleJumpInput()
        {
            // Note: Gravity is now handled in Update()
            
            // Jump Trigger (Left Wink Rising Edge)
            // Logic: Is Grounded AND Left Eye Closed AND Wasn't Closed Previous Frame
            if (_characterController.isGrounded && isLeftEyeClosed && !_wasLeftEyeClosed)
            {
                _velocity.y = jumpForce;
            }

            // Update state for next frame
            _wasLeftEyeClosed = isLeftEyeClosed;
        }

        private void HandleCrouch()
        {
            // Crouch Hold (Right Wink)
            float targetH = isRightEyeClosed ? crouchHeight : standingHeight;

            // Smoothly adjust height
            _currentHeight = Mathf.Lerp(_characterController.height, targetH, crouchTransitionSpeed * Time.deltaTime);
            
            _characterController.height = _currentHeight;
            // Adjust center to keep feet on ground (Center is always half of height)
            _characterController.center = new Vector3(0, _currentHeight * 0.5f, 0);
        }
    }
}
