using UnityEngine;

public class PlayerEffectsManager : MonoBehaviour
{
    //[Header("Effect References")]
    //public ParticleSystem dashVFX;
    //public ParticleSystem doubleJumpVFX;
    //public AudioSource audioSource;

    //[Header("Sound Clips")]
    //public AudioClip jumpSound; 
    //public AudioClip doubleJumpSound; 
    //public AudioClip dashSound;
    //public AudioClip landSound;
    

    private void Start()
    {
        if (PlayerController.Instance == null) return;
        
        PlayerController.Instance.OnStateChange += HandleStateChange;
        PlayerController.Instance.OnDash += HandleDash;
        PlayerController.Instance.OnDoubleJump += HandleDoubleJump;
    }

    private void OnDestroy()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnStateChange -= HandleStateChange;
            PlayerController.Instance.OnDash -= HandleDash;
            PlayerController.Instance.OnDoubleJump -= HandleDoubleJump;
        }
    }

    private void HandleStateChange(PlayerController.PlayerState previousState, PlayerController.PlayerState newState)
    {
        Debug.Log($"State changed from {previousState} to {newState}");
        
        if (newState == PlayerController.PlayerState.Jumping)
        {
            HandleJump();
        }
        
        bool wasAirborne = previousState == PlayerController.PlayerState.Jumping || previousState == PlayerController.PlayerState.Falling;
        bool isNowGrounded = newState == PlayerController.PlayerState.Idle || newState == PlayerController.PlayerState.Moving;

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
        //dashVFX.Play();
        //audioSource.PlayOneShot(dashSound);
        Debug.Log("DASH");
    }

    private void HandleDoubleJump()
    {
        //doubleJumpVFX.Play();
        //audioSource.PlayOneShot(jumpSound);
        Debug.Log("DOUBLE JUMP");
    }

    public void PlayFootstep()
    {
        
        Debug.Log("FOOTSTEP");
    }
}
