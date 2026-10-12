using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro; // For TextMeshProUGUI if you use text on buttons
using System.Collections; // For Coroutines
using UnityEngine.EventSystems; // Required for EventTrigger for the slider

public class ShawnVideoPlayerScript : MonoBehaviour
{
    [Header("Video Player Components & Display")]
    // Reference to the VideoPlayer component on this GameObject.
    public VideoPlayer videoPlayerComponent;

    // Reference to the RawImage where the video will be displayed.
    // Based on your hierarchy, this is the "Screen" GameObject under "Card 2 Video Player".
    public RawImage videoDisplayScreen;

    // Reference to the Render Texture asset used by the VideoPlayer and RawImage.
    // Based on your inspector, this is "UI Video Screen".
    public RenderTexture videoRenderTexture;

    [Header("UI Control Elements")]
    // The single button for Play/Pause. Named "Play/PauseButton" in your hierarchy.
    public Button playPauseButton;

    // Optional: If your Play/Pause button uses TextMeshPro for "Play" / "Pause" text.
    // If you're only swapping sprites, you can remove this and its usage.
    public TextMeshProUGUI playPauseButtonText;

    // Buttons for skipping. Named "SkipForwardButton" and "SkipBackwardButton" in your hierarchy.
    public Button skipForwardButton;
    public Button skipBackwardButton;

    // The slider for time scrubbing. Named "TimeScrubSlider" in your hierarchy.
    public Slider timeScrubSlider;

    [Header("Optional UI Visibility")]
    // The Canvas containing all the video controls. Named "Canvas" under "Shawn Video Player".
    public GameObject videoControlsCanvas;

    // Internal state variables
    private bool isVideoPlaying = false;
    private bool isSliderBeingDragged = false;
    private Coroutine sliderUpdateCoroutine;

    void Awake()
    {
        // Get the VideoPlayer component if not assigned in Inspector
        if (videoPlayerComponent == null)
        {
            videoPlayerComponent = GetComponent<VideoPlayer>();
            if (videoPlayerComponent == null)
            {
                Debug.LogError("VideoPlayer component not found on 'Shawn Video Player' GameObject!", this);
            }
        }

        // Assign the Render Texture to the RawImage if not already linked
        if (videoDisplayScreen != null && videoRenderTexture != null)
        {
            videoDisplayScreen.texture = videoRenderTexture;
        }
        else
        {
            Debug.LogWarning("Video Display Screen or Render Texture not fully assigned. Video may not appear on UI.", this);
        }
    }

    void Start()
    {
        // --- Input Validation: Ensure all necessary references are assigned ---
        if (videoPlayerComponent == null || playPauseButton == null || skipForwardButton == null ||
            skipBackwardButton == null || timeScrubSlider == null || videoDisplayScreen == null || videoRenderTexture == null)
        {
            Debug.LogError("One or more required references are NOT assigned in the Inspector for ShawnVideoPlayerScript! Disabling script.", this);
            enabled = false; // Disable script to prevent NullReferenceExceptions
            return;
        }
        // Check for TextMeshProUGUI specifically if it's used
        if (playPauseButtonText == null && playPauseButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            Debug.LogWarning("playPauseButtonText is not assigned, but TextMeshProUGUI found as child. Assign it for text-based play/pause.", this);
        }


        // --- Add Listeners to UI Elements ---
        playPauseButton.onClick.AddListener(TogglePlayPause);
        skipForwardButton.onClick.AddListener(SkipForward);
        skipBackwardButton.onClick.AddListener(SkipBackward);

        // --- Slider setup ---
        AddEventTriggerToSlider(); // Adds EventTrigger component for PointerDown/Up events
        timeScrubSlider.onValueChanged.AddListener(OnSliderScrub); // Listener for when slider value changes

        // --- VideoPlayer setup ---
        videoPlayerComponent.Prepare(); // Start preparing the video
        videoPlayerComponent.prepareCompleted += OnVideoPrepared; // Subscribe to event when video is ready
        videoPlayerComponent.loopPointReached += OnVideoEnd; // Subscribe to event when video finishes playing

        // Set initial UI state (assuming video starts paused)
        isVideoPlaying = false;
        UpdatePlayPauseButtonUI();

        // Start the coroutine that continuously updates the time scrub slider
        sliderUpdateCoroutine = StartCoroutine(UpdateTimeSliderRoutine());
    }

    // Helper to add EventTrigger to the slider for drag detection
    private void AddEventTriggerToSlider()
    {
        EventTrigger trigger = timeScrubSlider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = timeScrubSlider.gameObject.AddComponent<EventTrigger>();
        }

        // Clear existing listeners to prevent duplicates if script is reloaded in editor
        trigger.triggers.Clear();

        // Pointer Down event (when user starts dragging)
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { isSliderBeingDragged = true; Debug.Log("Slider Drag Started"); });
        trigger.triggers.Add(entryDown);

        // Pointer Up event (when user stops dragging)
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { isSliderBeingDragged = false; Debug.Log("Slider Drag Ended"); });
        trigger.triggers.Add(entryUp);
    }

    // Called when the video is prepared and ready to be played
    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("Video is prepared and ready to play!");
        // We can ensure the slider's max value is set correctly, though 0-1 normalized is common
        // if (timeScrubSlider != null && vp.length > 0)
        // {
        //     timeScrubSlider.maxValue = (float)vp.length; // If you want absolute time on slider
        // }
    }

    // Called when the video reaches its end (if not looping)
    void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("Video has ended.");
        isVideoPlaying = false;
        UpdatePlayPauseButtonUI();
        // You can add logic here to hide controls, or restart video etc.
        // For example, if you want it to restart and pause at the beginning:
        // vp.time = 0;
        // vp.Pause();
    }

    // Coroutine to continuously update the time scrub slider as the video plays
    IEnumerator UpdateTimeSliderRoutine()
    {
        while (true) // This loop runs indefinitely as long as the GameObject is active
        {
            // Only update the slider if video is prepared, has a valid length, and the user isn't currently dragging it
            if (videoPlayerComponent.isPrepared && videoPlayerComponent.length > 0 && !isSliderBeingDragged)
            {
                // Normalize video time to a 0-1 range for the slider
                timeScrubSlider.value = (float)(videoPlayerComponent.time / videoPlayerComponent.length);
            }
            yield return null; // Wait for the next frame before continuing the loop
        }
    }

    // --- UI Button Functions ---

    // Toggles between Play and Pause states
    void TogglePlayPause()
    {
        if (videoPlayerComponent.isPlaying)
        {
            videoPlayerComponent.Pause();
            isVideoPlaying = false;
            Debug.Log("Video Paused.");
        }
        else
        {
            // If video finished playing, reset to start before playing
            if (videoPlayerComponent.time >= videoPlayerComponent.length - 0.1f) // Small buffer for floating point inaccuracies
            {
                videoPlayerComponent.time = 0;
            }
            videoPlayerComponent.Play();
            isVideoPlaying = true;
            Debug.Log("Video Playing.");
        }
        UpdatePlayPauseButtonUI(); // Update the button's appearance
    }

    // Updates the Play/Pause button's text or sprite
    void UpdatePlayPauseButtonUI()
    {
        if (playPauseButtonText != null)
        {
            playPauseButtonText.text = isVideoPlaying ? "Pause" : "Play";
        }
        // --- Implement your icon swapping here if using image-based buttons ---
        // You mentioned you collected icons. This is where you'd use them.
        // For example, if your Play/PauseButton has an Image component:
        // Image buttonImage = playPauseButton.GetComponent<Image>();
        // if (buttonImage != null)
        // {
        //     // You would have public Sprite variables for your play and pause icons
        //     // public Sprite playIconSprite;
        //     // public Sprite pauseIconSprite;
        //     // buttonImage.sprite = isVideoPlaying ? pauseIconSprite : playIconSprite;
        // }
    }

    // Skips the video forward by 15 seconds
    void SkipForward()
    {
        if (videoPlayerComponent.isPrepared && videoPlayerComponent.canSetTime)
        {
            double newTime = videoPlayerComponent.time + 15.0;
            // Ensure we don't skip past the end of the video
            videoPlayerComponent.time = System.Math.Min(newTime, videoPlayerComponent.length - 0.1);
            Debug.Log($"Skipped forward to: {videoPlayerComponent.time:F2}s");
        }
    }

    // Skips the video backward by 15 seconds
    void SkipBackward()
    {
        if (videoPlayerComponent.isPrepared && videoPlayerComponent.canSetTime)
        {
            double newTime = videoPlayerComponent.time - 15.0;
            // Ensure we don't skip before the beginning of the video (0 seconds)
            videoPlayerComponent.time = System.Math.Max(newTime, 0.0);
            Debug.Log($"Skipped backward to: {videoPlayerComponent.time:F2}s");
        }
    }

    // Called when the TimeScrubSlider's value changes (either by video or by user drag)
    void OnSliderScrub(float normalizedValue)
    {
        // Only seek the video if the user is actively dragging the slider
        // This prevents the video playback from "fighting" with the slider's value updates
        if (isSliderBeingDragged && videoPlayerComponent.isPrepared && videoPlayerComponent.canSetTime && videoPlayerComponent.length > 0)
        {
            videoPlayerComponent.time = normalizedValue * videoPlayerComponent.length;
            Debug.Log($"Scrubbed to: {videoPlayerComponent.time:F2}s ({(normalizedValue * 100):F0}%)");
        }
    }

    // --- Cleanup on Destroy ---
    void OnDestroy()
    {
        // Remove all listeners to prevent memory leaks, especially important in long-running applications
        if (playPauseButton != null) playPauseButton.onClick.RemoveListener(TogglePlayPause);
        if (skipForwardButton != null) skipForwardButton.onClick.RemoveListener(SkipForward);
        if (skipBackwardButton != null) skipBackwardButton.onClick.RemoveListener(SkipBackward);
        if (timeScrubSlider != null) timeScrubSlider.onValueChanged.RemoveListener(OnSliderScrub);

        // Detach from VideoPlayer events
        if (videoPlayerComponent != null)
        {
            videoPlayerComponent.prepareCompleted -= OnVideoPrepared;
            videoPlayerComponent.loopPointReached -= OnVideoEnd;
        }

        // Stop the slider update coroutine
        if (sliderUpdateCoroutine != null)
        {
            StopCoroutine(sliderUpdateCoroutine);
        }

        // Clean up EventTrigger listeners on the slider
        EventTrigger trigger = timeScrubSlider.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.triggers.Clear(); // Clears all EventTrigger entries
        }
    }
}
