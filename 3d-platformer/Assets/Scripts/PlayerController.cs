using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    #region Events & Singleton

    public static PlayerController Instance { get; private set; }

    public event Action<PlayerState, PlayerState> OnStateChange;
    public event Action OnJump, OnDoubleJump, OnDash, OnGroundPoundLand;

    #endregion

    #region Component & Variable Declarations

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSmoothing = 15f;
    [SerializeField] private Transform cameraTransform;

    [Header("Jump & Gravity Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;
    [SerializeField, Range(0f, 2f)] private float airMomentumMultiplier = 0.8f;
    [SerializeField] private float airControl = 2.5f;

    [Header("Skill Settings")]
    [SerializeField] private bool canDoubleJump = true;
    [SerializeField] private bool canDash = true;
    [SerializeField] private float dashPower = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private bool canGroundPound = true;
    [SerializeField] private float groundPoundForce = -30f;

    // Components
    private CharacterController controller;
    private Animator animator;

    // Input Actions
    private InputAction moveAction, jumpAction, sprintAction, dashAction, groundPoundAction;

    // State & Movement
    private Vector3 playerVelocity, horizontalVelocity;
    private Vector2 moveInput;
    private bool hasDoubleJumped, isDashing, isGroundPounding, hasLandedFromPound;
    private float dashCooldownTimer;
    
    public enum PlayerState { Idle, Moving, Sprinting, Jumping, Falling, Dashing, GroundPounding }
    [Header("Debug")]
    [SerializeField] private PlayerState currentState;
    private PlayerState previousState;

    // Animator Hashes
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int isFallingHash = Animator.StringToHash("IsFalling");
    private readonly int doubleJumpHash = Animator.StringToHash("DoubleJump");
    private readonly int dashHash = Animator.StringToHash("Dash");
    private readonly int groundPoundHash = Animator.StringToHash("GroundPound");

    #endregion

    #region Initialization & Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CacheReferences();
    }

    private void OnEnable()
    {
        moveAction.Enable(); sprintAction.Enable(); jumpAction.Enable();
        dashAction.Enable(); groundPoundAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable(); sprintAction.Disable(); jumpAction.Disable();
        dashAction.Disable(); groundPoundAction.Disable();
    }

    private void CacheReferences()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Dash"];
        groundPoundAction = playerInput.actions["GroundPound"];
    }

    #endregion

    #region Core Update Loop

    private void Update()
    {
        bool isGrounded = controller.isGrounded;
        
        HandleCooldowns();
        ReadInputs();
        
        Vector3 moveDirection = GetCameraRelativeMovement();

        HandleAbilityInput(isGrounded, moveDirection);
        HandleGroundPoundLanding(isGrounded);

        if (!IsMovementLocked())
        {
            CalculateMovement(isGrounded, moveDirection);
            ApplyGravity();
            HandleRotation(moveDirection);
        }

        ApplyFinalMove();
        UpdateStateAndAnimator(isGrounded);
    }

    #endregion

    #region Input & Abilities

    private void ReadInputs() => moveInput = moveAction.ReadValue<Vector2>();
    private void HandleCooldowns() => dashCooldownTimer -= Time.deltaTime;

    private void HandleAbilityInput(bool isGrounded, Vector3 moveDirection)
    {
        if (dashAction.triggered && canDash && dashCooldownTimer <= 0)
            StartCoroutine(PerformDash(moveDirection));

        if (isGrounded)
        {
            if (jumpAction.triggered) PerformJump();
        }
        else // In-air abilities
        {
            if (groundPoundAction.triggered && canGroundPound && !isGroundPounding)
                PerformGroundPound();
            
            if (jumpAction.triggered && canDoubleJump && !hasDoubleJumped)
                PerformDoubleJump();
        }
    }

    #endregion

    #region State & Physics

    private bool IsMovementLocked() => isDashing || isGroundPounding;
    
    private void HandleGroundPoundLanding(bool isGrounded)
    {
        if (isGrounded && isGroundPounding && !hasLandedFromPound)
        {
            hasLandedFromPound = true;
            OnGroundPoundLand?.Invoke();
        }
    }

    private void CalculateMovement(bool isGrounded, Vector3 moveDirection)
    {
        if (isGrounded)
        {
            hasDoubleJumped = false;
            bool isSprinting = sprintAction.IsPressed();
            float currentSpeed = isSprinting && moveInput.magnitude > 0.1f ? sprintSpeed : moveSpeed;
            horizontalVelocity = moveDirection * currentSpeed;
        }
        else
        {
            Vector3 targetAirVelocity = moveDirection * moveSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetAirVelocity, airControl * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = groundedGravity;
        }
        else
        {
            playerVelocity.y += gravity * Time.deltaTime;
        }
    }

    private void HandleRotation(Vector3 moveDirection)
    {
        if (moveInput.magnitude < 0.1f) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
    }
    
    private void ApplyFinalMove()
    {
        CollisionFlags flags = controller.Move((horizontalVelocity + playerVelocity) * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && playerVelocity.y > 0)
        {
            playerVelocity.y = -1f;
        }
    }

    #endregion

    #region Skills

    private void PerformJump()
    {
        playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        horizontalVelocity *= airMomentumMultiplier;
        OnJump?.Invoke();
    }
    
    private void PerformDoubleJump()
    {
        hasDoubleJumped = true;
        playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        animator.SetTrigger(doubleJumpHash);
        OnDoubleJump?.Invoke();
    }

    private void PerformGroundPound()
    {
        isGroundPounding = true;
        hasLandedFromPound = false;
        animator.SetTrigger(groundPoundHash);
        horizontalVelocity = Vector3.zero;
        playerVelocity.y = groundPoundForce;
    }

    public void EndGroundPound() => isGroundPounding = false;

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
        
        Vector3 moveDirAfterDash = GetCameraRelativeMovement();
        float speedAfterDash = sprintAction.IsPressed() ? sprintSpeed : moveSpeed;
        horizontalVelocity = moveDirAfterDash * speedAfterDash;
    }

    #endregion

    #region Animation & State Enum

    private void UpdateStateAndAnimator(bool isGrounded)
    {
        // Update State Enum
        previousState = currentState;
        if (isGroundPounding) currentState = PlayerState.GroundPounding;
        else if (isDashing) currentState = PlayerState.Dashing;
        else if (isGrounded)
        {
            if (moveInput.magnitude > 0.1f)
                currentState = sprintAction.IsPressed() ? PlayerState.Sprinting : PlayerState.Moving;
            else
                currentState = PlayerState.Idle;
        }
        else currentState = playerVelocity.y > 0 ? PlayerState.Jumping : PlayerState.Falling;
        
        if (previousState != currentState)
            OnStateChange?.Invoke(previousState, currentState);

        // Update Animator
        float horizontalSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        animator.SetFloat(speedHash, horizontalSpeed);
        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetBool(isJumpingHash, currentState == PlayerState.Jumping);
        animator.SetBool(isFallingHash, currentState == PlayerState.Falling);
    }

    #endregion

    #region Helpers

    private Vector3 GetCameraRelativeMovement()
    {
        if (moveInput.magnitude < 0.1f) return Vector3.zero;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();

        return (right * moveInput.x + forward * moveInput.y).normalized;
    }

    #endregion
}