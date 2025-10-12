using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The speed at which the player moves horizontally.")]
    public float moveSpeed = 5f;
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

    // Components
    private CharacterController controller;
    private PlayerInput playerInput;

    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;

    // State & Movement Vectors
    private Vector3 playerVelocity;
    private Vector2 moveInput;
    
    public enum PlayerState
    {
        Idle,
        Moving,
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
        jumpAction = playerInput.actions["Jump"];
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    private void Update()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = groundedGravity;
        }

        moveInput = moveAction.ReadValue<Vector2>();
        
        Vector3 moveDirection = GetCameraRelativeMovement();

        if (jumpAction.triggered && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (moveInput.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
        }

        playerVelocity.y += gravity * Time.deltaTime;
        
        CollisionFlags flags = controller.Move((moveDirection * moveSpeed + playerVelocity) * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && playerVelocity.y > 0)
        {
            playerVelocity.y = -1f;
        }

        UpdateStateEnum(isGrounded);
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

    private void UpdateStateEnum(bool isGrounded)
    {
        if (isGrounded)
        {
            currentState = moveInput.magnitude >= 0.1f ? PlayerState.Moving : PlayerState.Idle;
        }
        else
        {
            currentState = playerVelocity.y > 0 ? PlayerState.Jumping : PlayerState.Falling;
        }
    }
}