using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR;

/// <summary>
/// Pauses gameplay when the app loses focus (Universal Menu opened) or is paused
/// (headset removed). Needed for VRC.Quest.Functional.2.
///
/// Setup: add to an empty GameObject in the gameplay scene. No references required.
/// </summary>
public class AppPauseHandler : MonoBehaviour
{
    [Header("What to pause")]
    [SerializeField] private bool pauseTimeScale = true;
    [SerializeField] private bool pauseAudio = true;
    [SerializeField] private bool pauseVideo = true;

    [Header("Optional overlay")]
    [Tooltip("Shown while paused. Leave empty for none.")]
    [SerializeField] private GameObject pausedOverlay;

    [Header("Editor")]
    [Tooltip("Off by default so clicking outside the Game view doesn't pause Play mode.")]
    [SerializeField] private bool activeInEditor = false;

    [Header("Debug")]
    [SerializeField] private bool logToVRDebug = true;

    // Track both signals: on Quest they can arrive in either order.
    private bool hasFocus = true;
    private bool appPaused;
    private bool userPresent = true;

    [Header("Headset removal")]
    [Tooltip("Pause the moment the headset reports nobody is wearing it, instead of waiting for the OS to sleep the display.")]
    [SerializeField] private bool useUserPresence = true;


    private bool isPaused;
    private float savedTimeScale = 1f;
    private readonly List<UI_Timer> pausedTimers = new();
    private readonly List<VideoPlayer> pausedVideos = new();
    private Coroutine resumeRoutine;

    private void Update()
    {
        if (!useUserPresence) return;
        if (Application.isEditor && !activeInEditor) return;

        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!head.isValid) return;
        if (!head.TryGetFeatureValue(CommonUsages.userPresence, out bool present)) return;
        if (present == userPresent) return;

        userPresent = present;
        if (logToVRDebug) VRDebugText.Log($"EVENT Presence={present}");
        Evaluate();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (logToVRDebug) VRDebugText.Log($"EVENT Focus={focus}");
        hasFocus = focus;
        Evaluate();
    }

    private void OnApplicationPause(bool paused)
    {
        if (logToVRDebug) VRDebugText.Log($"EVENT Pause={paused}");
        appPaused = paused;
        Evaluate();
    }

    private void Evaluate()
    {
        if (Application.isEditor && !activeInEditor) return;

        if (!hasFocus || appPaused || !userPresent) EnterPause();
        else RequestResume();
    }

    private void EnterPause()
    {
        // Cancel a pending resume if focus is lost again before it ran
        if (resumeRoutine != null) { StopCoroutine(resumeRoutine); resumeRoutine = null; }
        if (isPaused) return;
        isPaused = true;

        StepController.AppPaused = true;

        // Freeze only timers that are actually running, so we resume exactly those
        pausedTimers.Clear();
        foreach (var t in FindObjectsByType<UI_Timer>(FindObjectsSortMode.None))
        {
            if (t.IsRunning) { t.Stop(); pausedTimers.Add(t); }
        }

        pausedVideos.Clear();
        if (pauseVideo)
        {
            foreach (var v in FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None))
            {
                if (v.isPlaying) { v.Pause(); pausedVideos.Add(v); }
            }
        }

        if (pauseAudio) AudioListener.pause = true;   // also pauses PlayOneShot clips

        if (pauseTimeScale)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (pausedOverlay) pausedOverlay.SetActive(true);
        if (logToVRDebug) VRDebugText.Log("APP PAUSED");
    }

    private void RequestResume()
    {
        if (!isPaused) return;
        if (resumeRoutine != null) StopCoroutine(resumeRoutine);
        resumeRoutine = StartCoroutine(ResumeAfterFrames());
    }

    private IEnumerator ResumeAfterFrames()
    {
        // The first frame back carries the whole paused gap in unscaledDeltaTime.
        // Skip it so timers don't jump forward.
        yield return null;
        yield return null;

        if (pauseTimeScale) Time.timeScale = savedTimeScale;
        if (pauseAudio) AudioListener.pause = false;

        foreach (var v in pausedVideos) if (v) v.Play();
        foreach (var t in pausedTimers) if (t) t.Resume();
        pausedVideos.Clear();
        pausedTimers.Clear();

        if (pausedOverlay) pausedOverlay.SetActive(false);

        StepController.AppPaused = false;
        isPaused = false;
        resumeRoutine = null;

        if (logToVRDebug) VRDebugText.Log("APP RESUMED");
    }

    private void OnDestroy()
    {
        // Never leave global state stuck if the scene unloads while paused
        if (!isPaused) return;
        StepController.AppPaused = false;
        AudioListener.pause = false;
        if (pauseTimeScale) Time.timeScale = savedTimeScale;
    }
}
