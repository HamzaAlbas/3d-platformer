using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System; 

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public event Action<PlayerState, PlayerState> OnStateChange;
    public event Action OnJump;
    public event Action OnDoubleJump;
    public event Action OnDash;
    
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

    
    [Header("Skill Settings")]
    [Tooltip("Determines if the player has unlocked the ability to double jump.")]
    public bool canDoubleJump = true;
    [Tooltip("Determines if the player has unlocked the ability to dash.")]
    public bool canDash = true; 
    public float dashPower = 20f; 
    public float dashDuration = 0.2f; 
    public float dashCooldown = 1f;
    
    // Animations
    private Animator animator;
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int isFallingHash = Animator.StringToHash("IsFalling");
    private readonly int doubleJumpHash = Animator.StringToHash("DoubleJump");
    private readonly int dashHash = Animator.StringToHash("Dash");

    // Components & Input
    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction dashAction;

    // Private Variables 
    private Vector3 playerVelocity;
    private Vector3 horizontalVelocity;
    private Vector2 moveInput;
    private bool hasDoubleJumped = false; 
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;
    private PlayerState previousState;
    [Header("Debug")]
    [SerializeField] private PlayerState currentState;
    
    public enum PlayerState
    {
        Idle,
        Moving,
        Sprinting,
        Jumping,
        Falling,
        Dashing
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Dash"];
    }

    private void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
    }

private void Update()
    {
        bool isGrounded = controller.isGrounded;
        moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = GetCameraRelativeMovement();

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (dashAction.triggered && canDash && dashCooldownTimer <= 0)
        {
            StartCoroutine(PerformDash(moveDirection));
        }
        
        if (!isDashing)
        {
            if (isGrounded)
            {
                hasDoubleJumped = false;
                if (playerVelocity.y < 0) playerVelocity.y = groundedGravity;
                
                bool isSprinting = sprintAction.IsPressed();
                float currentSpeed = isSprinting && moveInput.magnitude > 0.1f ? sprintSpeed : moveSpeed;
                horizontalVelocity = moveDirection * currentSpeed;

                if (jumpAction.triggered)
                {
                    playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    horizontalVelocity *= airMomentumMultiplier;
                    OnJump?.Invoke();
                }
            }
            else
            {
                if (jumpAction.triggered && canDoubleJump && !hasDoubleJumped)
                {
                    hasDoubleJumped = true;
                    playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    animator.SetTrigger(doubleJumpHash);
                    OnDoubleJump?.Invoke();
                }

                Vector3 targetAirVelocity = moveDirection * moveSpeed;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetAirVelocity, airControl * Time.deltaTime);
            }
        }
        
        if (!isDashing)
        {
            playerVelocity.y += gravity * Time.deltaTime;
        }

        if (moveInput.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
        }
        
        controller.Move((horizontalVelocity + playerVelocity) * Time.deltaTime);

        UpdateStateEnum(isGrounded, sprintAction.IsPressed());
        HandleAnimation();
    }
    
    private IEnumerator PerformDash(Vector3 moveDirection)
    {
        OnDash?.Invoke();
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        animator.SetTrigger(dashHash);

        Vector3 dashDirection = moveDirection != Vector3.zero ? moveDirection : transform.forward;
    
        float originalYVelocity = playerVelocity.y;
        playerVelocity.y = 0;

        horizontalVelocity = dashDirection * dashPower;

        yield return new WaitForSeconds(dashDuration);
    
        isDashing = false;
        playerVelocity.y = originalYVelocity; 

        Vector3 moveDirectionAfterDash = GetCameraRelativeMovement();
        bool isSprintingAfterDash = sprintAction.IsPressed();
        float speedAfterDash = isSprintingAfterDash && moveInput.magnitude > 0.1f ? sprintSpeed : moveSpeed;
        horizontalVelocity = moveDirectionAfterDash * speedAfterDash;
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
        previousState = currentState;
        
        if (isDashing)
        {
            currentState = PlayerState.Dashing;
            return;
        }

        if (isGrounded)
        {
            if (moveInput.magnitude >= 0.1f)
                currentState = isSprinting ? PlayerState.Sprinting : PlayerState.Moving;
            else
                currentState = PlayerState.Idle;
        }
        else
        {
            currentState = playerVelocity.y > 0 ? PlayerState.Jumping : PlayerState.Falling;
        }
        
        if (previousState != currentState)
        {
            OnStateChange?.Invoke(previousState, currentState);
        }
    }
    
    private void HandleAnimation()
    {
        float horizontalSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        
        animator.SetFloat(speedHash, horizontalSpeed);
        animator.SetBool(isGroundedHash, controller.isGrounded);
        animator.SetBool(isJumpingHash, currentState == PlayerState.Jumping);
        animator.SetBool(isFallingHash, currentState == PlayerState.Falling);
    }
}