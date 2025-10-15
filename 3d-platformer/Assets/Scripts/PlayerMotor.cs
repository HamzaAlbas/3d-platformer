using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    #region State Management

    public enum PlayerState { Idle, Moving, Sprinting, Jumping, Falling, Dashing, GroundPounding, LedgeClimbing }
    public PlayerState CurrentState { get; private set; }
    private PlayerState previousState;
    public event Action<PlayerState, PlayerState> OnStateChange;
    
    #endregion

    #region Movement Properties

    public Vector3 HorizontalVelocity { get; private set; }
    public Vector3 PlayerVelocity { get; private set; }
    public bool IsGrounded => controller.isGrounded;
    public bool IsMovementLocked => CurrentState is PlayerState.Dashing or PlayerState.GroundPounding or PlayerState.LedgeClimbing;
    
    #endregion

    #region Configuration
    
    [Header("Dependencies")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float rotationSmoothing = 15f; 
    public float gravity = -20f; 
    [SerializeField] private float groundedGravity = -2f; 
    
    #endregion

    // Component references
    private CharacterController controller;
    private Animator animator;
    
    // Optimized animation parameter hashes (avoid string lookups)
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int isJumpingHash = Animator.StringToHash("IsJumping");
    private readonly int isFallingHash = Animator.StringToHash("IsFalling");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    } 

    private void Update()
    {
        // Skip processing when in ledge climbing state
        if (CurrentState == PlayerState.LedgeClimbing) return;
        
        ApplyGravity();
        
        ApplyFinalMove();
        
        UpdateState();
        
        HandleAnimation();
    }
    
    public void SetHorizontalVelocity(Vector3 velocity) => HorizontalVelocity = velocity;
    
    public void SetVerticalVelocity(float yVelocity)
    {
        var currentVelocity = PlayerVelocity;
        currentVelocity.y = yVelocity;
        PlayerVelocity = currentVelocity;
    }

    /// <summary>
    /// Smoothly rotates player towards given direction
    /// </summary>
    public void RotatePlayer(Vector3 moveDirection)
    {
        if (moveDirection == Vector3.zero) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
    }

    /// <summary>
    /// Applies gravity effects based on ground state
    /// - When grounded and falling: applies slow grounded gravity
    /// - When airborne: applies normal gravity over time
    /// </summary>
    private void ApplyGravity()
    {
        if (IsGrounded && PlayerVelocity.y < 0)
        {
            SetVerticalVelocity(groundedGravity);
        }
        else
        {
            SetVerticalVelocity(PlayerVelocity.y + gravity * Time.deltaTime);
        }
    }

    /// <summary>
    /// Moves character with combined velocity and handles ceiling collisions
    /// - Stops upward movement when hitting ceiling
    /// </summary>
    private void ApplyFinalMove()
    {
        // Apply movement with combined velocity
        CollisionFlags flags = controller.Move((HorizontalVelocity + PlayerVelocity) * Time.deltaTime);
        
        // If hit ceiling while moving up, reverse vertical velocity
        if ((flags & CollisionFlags.Above) != 0 && PlayerVelocity.y > 0)
        {
            SetVerticalVelocity(-1f);
        }
    }

    /// <summary>
    /// Updates current player state based on movement conditions
    /// - Grounded: checks horizontal speed for Idle/Moving/Sprinting
    /// - Airborne: checks vertical velocity for Jumping/Falling
    /// </summary>
    private void UpdateState()
    {
        previousState = CurrentState;
        
        // Skip state updates during locked movement states
        if (IsMovementLocked) return;

        if (IsGrounded)
        {
            // Calculate movement speed once for efficiency
            float speed = HorizontalVelocity.magnitude;
            
            // Determine grounded state based on speed thresholds
            CurrentState = speed > 5.1f ? PlayerState.Sprinting :
                           speed > 0.1f ? PlayerState.Moving :
                           PlayerState.Idle;
        }
        else
        {
            // Determine airborne state based on vertical velocity
            CurrentState = PlayerVelocity.y > 0 ? PlayerState.Jumping : PlayerState.Falling;
        }
        
        // Trigger state change event if state changed
        if (previousState != CurrentState)
            OnStateChange?.Invoke(previousState, CurrentState);
    }
    
    /// <summary>
    /// Sets new state and triggers state change events
    /// </summary>
    public void SetState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        
        previousState = CurrentState;
        CurrentState = newState;
        OnStateChange?.Invoke(previousState, CurrentState);
    }
    
    /// <summary>
    /// Updates animator parameters based on current state and movement
    /// - Sets horizontal speed for movement animations
    /// - Updates ground/jumping/falling boolean states
    /// </summary>
    private void HandleAnimation()
    {
        // Calculate planar speed (X and Z only) for animations
        float horizontalSpeed = new Vector2(HorizontalVelocity.x, HorizontalVelocity.z).magnitude;
        
        // Update all animation parameters
        animator.SetFloat(speedHash, horizontalSpeed);
        animator.SetBool(isGroundedHash, IsGrounded);
        animator.SetBool(isJumpingHash, CurrentState == PlayerState.Jumping);
        animator.SetBool(isFallingHash, CurrentState == PlayerState.Falling);
    }
}