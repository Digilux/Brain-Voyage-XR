using UnityEngine;

// This will automatically add an AudioSource component when you add this script
[RequireComponent(typeof(AudioSource))]
public class BrainPieceAudioFeedback : MonoBehaviour
{
    // Assign your sound clips in the Inspector
    [Tooltip("Sound to play when the piece is pinched/grabbed.")]
    public AudioClip pinchSound;
    
    [Tooltip("Sound to play when the piece is dropped/released.")]
    public AudioClip dropSound;

    private AudioSource audioSource;

    void Awake()
    {
        // Get the AudioSource component attached to this GameObject
        audioSource = GetComponent<AudioSource>();
        
        // Ensure the audio doesn't play when the scene starts
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Public method to be called by the Interactable's 'OnActivated' event.
    /// </summary>
    public void PlayPinchSound()
    {
        if (pinchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pinchSound);
        }
        else
        {
            Debug.LogWarning("Pinch sound or AudioSource missing on " + gameObject.name);
        }
    }

    /// <summary>
    /// Public method to be called by the Interactable's 'OnDeactivated' event.
    /// </summary>
    public void PlayDropSound()
    {
        if (dropSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dropSound);
        }
        else
        {
            Debug.LogWarning("Drop sound or AudioSource missing on " + gameObject.name);
        }
    }
}