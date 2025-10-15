using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerDash : MonoBehaviour
{
    #region Events
    
    public event Action OnDash;
    
    #endregion

    #region Settings
    
    [Header("Settings")]
    [SerializeField] private bool canDash = true;
    [SerializeField] private float dashPower = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    
    #endregion

    #region Internal State
    
    private PlayerMotor motor;
    private Animator animator;
    private readonly int dashHash = Animator.StringToHash("Dash");
    private float dashCooldownTimer;
    
    #endregion

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Initiates dash sequence if conditions are met
    /// </summary>
    public void TryDash(Vector3 moveDirection)
    {
        // Check if dash is allowed and cooldown timer has expired
        if (canDash && dashCooldownTimer <= 0)
        {
            // Start coroutine to handle dash sequence
            StartCoroutine(PerformDash(moveDirection));
        }
    }

    /// <summary>
    /// Handles all dash mechanics during active dash sequence
    /// </summary>
    private IEnumerator PerformDash(Vector3 moveDirection)
    {
        // Notify listeners that dash has started
        OnDash?.Invoke();
        
        // Set state to Dashing for state management
        motor.SetState(PlayerMotor.PlayerState.Dashing);
        animator.SetTrigger(dashHash);
        
        // Activate cooldown timer
        dashCooldownTimer = dashCooldown;
        
        // Determine dash direction: use input direction if available, fallback to forward
        Vector3 dashDirection = moveDirection != Vector3.zero ? moveDirection : transform.forward;
        
        // Store current vertical velocity to restore after dash
        float originalYVelocity = motor.PlayerVelocity.y;
        
        // Reset vertical velocity for pure horizontal dash effect
        motor.SetVerticalVelocity(0);
        
        // Apply dash force in direction
        motor.SetHorizontalVelocity(dashDirection * dashPower);
        
        // Temporarily halt coroutine for duration
        yield return new WaitForSeconds(dashDuration);
        
        // Restore original vertical velocity
        motor.SetVerticalVelocity(originalYVelocity);
        
        // Reset horizontal velocity to zero (could adjust based on movement input)
        motor.SetHorizontalVelocity(Vector3.zero);
        
        // Set state to Falling to handle post-dash behavior (e.g., landing)
        motor.SetState(PlayerMotor.PlayerState.Falling);
    }

    /// <summary>
    /// Decrements cooldown timer each frame until it reaches zero
    /// </summary>
    private void Update()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;
    }
}