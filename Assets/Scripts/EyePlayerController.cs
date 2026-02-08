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

        private void HandleMovement()
        {
            // Gazexが 0.1 以下の小さな値の時は動かないようにする（遊びを作る）
            float moveThreshold = 0.1f;
            Vector3 move = Vector3.zero;

            // Note: gazeX is assumed to be normalized (-1 to 1) for strafing
            if (Mathf.Abs(gazeX) > moveThreshold)
            {
                move = transform.right * gazeX * moveSpeed; 
            }

            // 前進（常に進む設定なら、ここも条件を付ける）
            move += transform.forward * forwardSpeed; 

            _characterController.Move(move * Time.deltaTime);
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
