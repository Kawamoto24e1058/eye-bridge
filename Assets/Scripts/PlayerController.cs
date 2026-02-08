using UnityEngine;

namespace EyeTrackingGame.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
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
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float jumpForce = 8.0f;
        [SerializeField] private float gravity = 20.0f;

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
            HandleMovement();
            HandleJump();
            HandleCrouch();
        }

        private void HandleMovement()
        {
            // 1. Calculate Target Point from Gaze (Raycast)
            Vector3 screenPoint = new Vector3(gazeX, gazeY, 0);
            Ray ray = Camera.main.ScreenPointToRay(screenPoint);
            
            Vector3 targetDirection = Vector3.zero;
            
            // Raycast against the world to find where the player is looking
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                // Determine direction on the horizontal plane
                Vector3 targetPoint = hit.point;
                targetPoint.y = transform.position.y; // Flatten to player level

                Vector3 directionToTarget = targetPoint - transform.position;

                // Deadzone to stop moving when very close to cursor
                if (directionToTarget.magnitude > 0.5f)
                {
                    targetDirection = directionToTarget.normalized;
                }
            }
            else
            {
                 // Fallback: If looking at skybox, project ray to player's height plane
                Plane playerPlane = new Plane(Vector3.up, transform.position);
                if (playerPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    Vector3 direction = hitPoint - transform.position;
                    if (direction.magnitude > 0.5f)
                    {
                        targetDirection = direction.normalized;
                    }
                }
            }

            // 2. Rotate Character
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }

            // 3. Move Character (Horizontal)
            Vector3 horizontalMove = targetDirection * moveSpeed;
            
            // Integrate with Vertical Velocity (handled in HandleJump/Gravity)
            Vector3 finalMove = horizontalMove;
            finalMove.y = _velocity.y; // Apply vertical velocity calculated below

            _characterController.Move(finalMove * Time.deltaTime);
        }

        private void HandleJump()
        {
            // Apply Gravity
            if (_characterController.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Stick to ground
            }

            // Jump Trigger (Left Wink Rising Edge)
            // Logic: Is Grounded AND Left Eye Closed AND Wasn't Closed Previous Frame
            if (_characterController.isGrounded && isLeftEyeClosed && !_wasLeftEyeClosed)
            {
                _velocity.y = jumpForce;
            }

            // Update state for next frame
            _wasLeftEyeClosed = isLeftEyeClosed;

            // Apply Gravity over time
            _velocity.y -= gravity * Time.deltaTime;
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
