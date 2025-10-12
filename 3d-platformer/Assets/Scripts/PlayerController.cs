using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The speed at which the player moves horizontally.")]
    public float moveSpeed = 5f;
    [Tooltip("The speed at which the player moves when sprinting.")]
    public float sprintSpeed = 8f;
    [Tooltip("How fast the player rotates to face the movement direction. Higher is faster.")]
    public float rotationSmoothing = 15f;
    [Tooltip("The transform of the camera that follows the player.")]
    public Transform cameraTransform;

    [Header("Jump & Gravity Settings")]
    [Tooltip("The maximum height the player can jump.")]
    public float jumpHeight = 2f;
    [Tooltip("The strength of gravity.")]
    public float gravity = -20f;
    [Tooltip("A small downward force applied when grounded to prevent bouncing.")]
    public float groundedGravity = -2f;
    [Tooltip("Controls how much ground momentum is carried into a jump. 1 = full momentum, 0.5 = half, etc.")]
    [Range(0f, 2f)] 
    public float airMomentumMultiplier = 0.8f;
    [Tooltip("How much control the player has to change direction mid-air. Higher is more responsive.")]
    public float airControl = 2.5f;

    // Components
    private CharacterController controller;
    private PlayerInput playerInput;

    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    // State & Movement 
    private Vector3 playerVelocity;
    private Vector3 horizontalVelocity;
    private Vector2 moveInput;
    
    public enum PlayerState
    {
        Idle,
        Moving,
        Sprinting,
        Jumping,
        Falling
    }

    [Header("Debug")]
    [SerializeField] private PlayerState currentState;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
        jumpAction = playerInput.actions["Jump"];
    }

    private void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
        jumpAction.Disable();
    }

    private void Update()
    {
        bool isGrounded = controller.isGrounded;
        moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = GetCameraRelativeMovement();

        if (isGrounded)
        {
            if (playerVelocity.y < 0)
            {
                playerVelocity.y = groundedGravity;
            }

            bool isSprinting = sprintAction.IsPressed();
            float currentSpeed = isSprinting && moveInput.magnitude > 0.1f ? sprintSpeed : moveSpeed;
            horizontalVelocity = moveDirection * currentSpeed;

            if (jumpAction.triggered)
            {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                horizontalVelocity *= airMomentumMultiplier;
            }
        }
        else 
        {
            Vector3 targetAirVelocity = moveDirection * moveSpeed;
            

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetAirVelocity, airControl * Time.deltaTime);
        }
        
        playerVelocity.y += gravity * Time.deltaTime;

        if (moveInput.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
        }
        
        CollisionFlags flags = controller.Move((horizontalVelocity + playerVelocity) * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && playerVelocity.y > 0)
        {
            playerVelocity.y = -1f;
        }

        UpdateStateEnum(isGrounded, sprintAction.IsPressed());
    }

    private Vector3 GetCameraRelativeMovement()
    {
        if (moveInput.magnitude < 0.1f)
        {
            return Vector3.zero;
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        return (right * moveInput.x + forward * moveInput.y).normalized;
    }

    private void UpdateStateEnum(bool isGrounded, bool isSprinting)
    {
        if (isGrounded)
        {
            if (moveInput.magnitude >= 0.1f)
            {
                currentState = isSprinting ? PlayerState.Sprinting : PlayerState.Moving;
            }
            else
            {
                currentState = PlayerState.Idle;
            }
        }
        else
        {
            currentState = playerVelocity.y > 0 ? PlayerState.Jumping : PlayerState.Falling;
        }
    }
}