using UnityEngine;

public class PlayerEffectsManager : MonoBehaviour
{
    [Header("Target Player Components")]
    [Tooltip("Drag the Player GameObject here to link the effects.")]
    public GameObject playerObject;

    //[Header("Effect References")]
    //public ParticleSystem dashVFX;
    //public ParticleSystem doubleJumpVFX;
    //public ParticleSystem groundPoundVFX;
    //public AudioSource audioSource;

    //[Header("Sound Clips")]
    //public AudioClip jumpSound;
    //public AudioClip doubleJumpSound;
    //public AudioClip dashSound;
    //public AudioClip landSound;
    //public AudioClip groundPoundLandSound;

    // Cached references to the player's ability components
    private PlayerMotor motor;
    private PlayerJump jump;
    private PlayerDash dash;
    private PlayerGroundPound groundPound;

    private void Awake()
    {
        // Find and store the components from the player object.
        if (playerObject == null)
        {
            Debug.LogError("Player Object is not assigned in the PlayerEffectsManager!", this);
            return;
        }

        motor = playerObject.GetComponent<PlayerMotor>();
        jump = playerObject.GetComponent<PlayerJump>();
        dash = playerObject.GetComponent<PlayerDash>();
        groundPound = playerObject.GetComponent<PlayerGroundPound>();
    }

    private void OnEnable()
    {
        // Subscribe to the events from each individual component.
        if (motor != null) motor.OnStateChange += HandleStateChange;
        if (jump != null)
        {
            jump.OnJump += HandleJump;
            jump.OnDoubleJump += HandleDoubleJump;
        }
        if (dash != null) dash.OnDash += HandleDash;
        if (groundPound != null) groundPound.OnGroundPoundLand += HandleGroundPoundLand;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors and memory leaks.
        if (motor != null) motor.OnStateChange -= HandleStateChange;
        if (jump != null)
        {
            jump.OnJump -= HandleJump;
            jump.OnDoubleJump -= HandleDoubleJump;
        }
        if (dash != null) dash.OnDash -= HandleDash;
        if (groundPound != null) groundPound.OnGroundPoundLand -= HandleGroundPoundLand;
    }

    // The state enum is now part of PlayerMotor.
    private void HandleStateChange(PlayerMotor.PlayerState previousState, PlayerMotor.PlayerState newState)
    {
        Debug.Log($"State changed from {previousState} to {newState}");

        bool wasAirborne = previousState == PlayerMotor.PlayerState.Jumping || previousState == PlayerMotor.PlayerState.Falling;
        bool isNowGrounded = newState == PlayerMotor.PlayerState.Idle || newState == PlayerMotor.PlayerState.Moving || newState == PlayerMotor.PlayerState.Sprinting;

        if (wasAirborne && isNowGrounded)
        {
            //audioSource.PlayOneShot(landSound);
            Debug.Log("LANDED");
        }
    }

    private void HandleJump()
    {
       //audioSource.PlayOneShot(jumpSound);
        Debug.Log("JUMP");
    }

    private void HandleDash()
    {
        //if (dashVFX != null) dashVFX.Play();
        //audioSource.PlayOneShot(dashSound);
        Debug.Log("DASH");
    }

    private void HandleDoubleJump()
    {
        //if (doubleJumpVFX != null) doubleJumpVFX.Play();
        //audioSource.PlayOneShot(doubleJumpSound);
        Debug.Log("DOUBLE JUMP");
    }

    private void HandleGroundPoundLand()
    {
        //if (groundPoundVFX != null) groundPoundVFX.Play();
        //audioSource.PlayOneShot(groundPoundLandSound);
        Debug.Log("GROUND POUND LANDED!");
    }

    public void PlayFootstep()
    {
        Debug.Log("FOOTSTEP");
        // Add footstep audio logic here
    }
}