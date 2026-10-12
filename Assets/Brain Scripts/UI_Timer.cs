using UnityEngine;
using TMPro;

public class UI_Timer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private bool showMillis = false;
    [SerializeField] private bool startHidden = true;

    [Header("Goal Stop Condition")]
    [SerializeField] private bool enableGoalAutoStop = false; // NEW: toggle auto-stop
    [SerializeField] private GameObject goalObject;            // if enabled: stop when this becomes active

    // removing audio on stop here bc it overlaps with fail audio
    //[Header("Audio on Stop")]
    //[SerializeField] private AudioSource audioSource;
    //[SerializeField] private AudioClip stopClip;

    [Header("Timing")]
    [SerializeField] private bool useUnscaledTime = true;      // recommended for VO/training

    // State
    private bool running;
    private float elapsed;
    private CanvasGroup cg;

    // Latches/flags
    private bool stopLatched;   // prevents repeated auto-stops
    private bool shown;         // prevents redundant Show/Hide

    // Public read-only accessors
    public bool IsRunning => running;
    public float Elapsed => elapsed;

    void Awake()
    {
        if (!timerText) timerText = GetComponent<TextMeshProUGUI>();

        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        if (startHidden)
        {
            // prime so Hide() always applies on first call
            shown = true;
            Hide();
        }
        else
        {
            Show();
        }
        UpdateUI();
    }

    void OnEnable()
    {
        // If re-enabled while configured to start hidden and not running, enforce hidden state
        if (startHidden && !running)
        {
            shown = true; // allow Hide() to execute its body
            Hide();
        }
    }

    void Update()
    {
        if (!running) return;

        // Optional auto-stop watcher (guarded + latched)
        if (enableGoalAutoStop && goalObject && goalObject.activeInHierarchy && !stopLatched)
        {
            Stop();             // idempotent
            stopLatched = true; // latch so it only fires once per run
            return;
        }

        elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        UpdateUI();
    }

    /// <summary>Start counting up from 0. Idempotent (does nothing if already running).</summary>
    public void Begin()
    {
        if (running) return;

        stopLatched = false; // fresh run
        elapsed = 0f;
        running = true;

        // ensure visible and interactive
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Show();
        UpdateUI();
    }

    /// <summary>Stop counting. Idempotent (does nothing if already stopped).</summary>
    public void Stop()
    {
        if (!running) return;

        running = false;
        
        //removing play stop audio to prevent both goal and fail audio to play
        //PlayStopAudio();
        // Keep visible by default after stop; comment-in below to auto-hide:
        // Hide();
    }

    /// <summary>Reset elapsed time to 0 (does not change running state).</summary>
    public void ResetTimer()
    {
        elapsed = 0f;
        UpdateUI();
    }

    /// <summary>Clear the auto-stop latch so goal-based auto-stop can fire again.</summary>
    public void ResetStopLatch()
    {
        stopLatched = false;
    }

    private void UpdateUI()
    {
        if (!timerText) return;

        int m = Mathf.FloorToInt(elapsed / 60f);
        int s = Mathf.FloorToInt(elapsed % 60f);

        if (showMillis)
        {
            int ms = Mathf.FloorToInt((elapsed - Mathf.Floor(elapsed)) * 1000f);
            timerText.text = $"{m:00}:{s:00}.{ms:000}";
        }
        else
        {
            timerText.text = $"{m:00}:{s:00}";
        }

        if (running && Time.frameCount % 30 == 0)
            Debug.Log($"[UI_Timer] running, elapsed={Elapsed:0.00}");
    }

    private void Hide()
    {
        shown = false;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (timerText) timerText.enabled = false;
    }

    private void Show()
    {
        if (shown) return;
        shown = true;
        if (timerText) timerText.enabled = true;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    public string GetFormattedTime()
    {
        int m = Mathf.FloorToInt(elapsed / 60f);
        int s = Mathf.FloorToInt(elapsed % 60f);

        if (showMillis)
        {
            int ms = Mathf.FloorToInt((elapsed - Mathf.Floor(elapsed)) * 1000f);
            return $"{m:00}:{s:00}.{ms:000}";
        }
        else
        {
            return $"{m:00}:{s:00}";
        }
    }

#if UNITY_EDITOR
    // Handy context-menu items for testing from the Inspector
    [ContextMenu("DEBUG/Begin")]
    private void DebugBegin() => Begin();

    [ContextMenu("DEBUG/Stop")]
    private void DebugStop() => Stop();

    [ContextMenu("DEBUG/Reset Timer")]
    private void DebugReset() => ResetTimer();
#endif
}