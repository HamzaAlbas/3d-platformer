using UnityEngine;

public class EventProxy : MonoBehaviour
{
    public PlayerEffectsManager effectsManager;
    
    public void PlayFootstep()
    {
        if (effectsManager != null)
        {
            effectsManager.PlayFootstep();
        }
        else
        {
            Debug.LogWarning("Effects Manager not assigned on the AnimationEventProxy!", this);
        }
    }
}
