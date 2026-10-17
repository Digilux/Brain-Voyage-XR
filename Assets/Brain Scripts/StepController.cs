using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

[RequireComponent(typeof(Collider))]
public class StepController : MonoBehaviour
{

    public static bool GlobalDetectionPaused = false;
    public static bool AppPaused = false;   // set by AppPauseHandler
    public event System.Action<StepController, bool> OnStepFinished;   
    public GameObject startButton; // blue button

    private bool pausedForReset;

    // Add near armDelay/requireRisingEdge:
    [Tooltip("Grace window after arming where wrong-touch penalties are suppressed, so residual hand/controller position from the previous step doesn't get punished.")]
    [SerializeField] private float wrongTouchArmGrace = 0.35f;

    [Header("Haptics")]
    [SerializeField] private HapticImpulsePlayer leftHapticPlayer;
    [SerializeField] private HapticImpulsePlayer rightHapticPlayer;
    [SerializeField] private float correctHapticAmplitude = 0.5f;
    [SerializeField] private float correctHapticDuration = 0.15f;
    [SerializeField] private float incorrectHapticAmplitude = 0.3f;
    [SerializeField] private float incorrectHapticDuration = 0.1f;

    [Header("Goal Sequence")]
    [SerializeField] private GoalSequencePlayer goalSequencePlayer;

    [Header("Color Feedback")]
    [SerializeField] private GoalObjectColorFeedback colorFeedback;

    [Header("Scoring")]
    [SerializeField] private int goalPoints = 0;
    public int GoalPoints => goalPoints;

    [Header("Scoring")]
    [SerializeField] private int basePoints = 30;
    [SerializeField] private int failPenaltyPerWrongTouch = 10; // each wrong touch reduces score

    [Tooltip("Points awarded per second saved. Faster completion earns more bonus.")]
    [SerializeField] private int timeBonusPerSecond = 0;        // optional: faster = higher

    [Tooltip("Maximum number of seconds considered for time bonus calculation. After this time, no bonus is awarded.")]
    [SerializeField] private int maxTimeBonusSeconds = 0;     // cap bonus

    [Header("Cleanup Visuals")]
    [SerializeField] private GameObject[] objectsToDestroy;
    [SerializeField] private GameObject visualsRoot;

    [Header("References")]
    [SerializeField] private FingerContactService fingerService;
    [SerializeField] private TwoFingerWrongTouchDetector wrongTouchDetector;
    [SerializeField] private UI_AttemptCounter attemptCounter;

    [Header("Audio Instruction")]
    [SerializeField] private AudioSource voOutputSource;
    [SerializeField] private AudioClip instructionClip;
    [Tooltip("Delay before VO + instruction text appear (seconds).")]
    [SerializeField] private float instructionDelay = 0.5f;

    [Header("Instruction UI")]
    [SerializeField] private GameObject instructionCanvas;
    [SerializeField] private TMP_Text instructionTMP;
    [TextArea] [SerializeField] private string instructionText;

    [Header("Instruction Fade")]
    [SerializeField] private CanvasGroup instructionGroup;   // assign the CanvasGroup on your Instruction Canvas
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("Timer UI")]
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private UI_Timer timer;
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private string labelText = "Time Elapsed:";

    [Header("All Goals / Steps State")]
    [SerializeField] private string requiredCommonTag = "Goal";
    [SerializeField] private AudioClip goalClip;
    [SerializeField] private GameObject goalCanvas;
    [SerializeField] private float armDelay = 1f;
    [SerializeField] private bool requireRisingEdge = true;

    [Header("Goal Display Name")]
    [Tooltip("Human-readable name shown on the goal/fail canvases, e.g. \"Broca's Area\".")]
    [SerializeField] private string goalDisplayName;
    [SerializeField] private TMP_Text goalNameTextOnGoalCanvas;
    [SerializeField] private TMP_Text goalNameTextOnFailCanvas;

    [Header("Attempts and Fail State")]
    [SerializeField] public int maxAttempts = 3;
    [SerializeField] private AudioClip failClip;        // your fail audio clip
    [SerializeField] private AudioClip wrongTouchClip;
    [SerializeField] private GameObject failCanvas;     // assign your fail canvas in Inspector
    [SerializeField] private float failCooldown = 1.0f; // seconds between counting attempts

    [Header("Start Trigger Filter")]
    [SerializeField] private LayerMask allowedLayers = ~0;
    [SerializeField] private string[] allowedStartTags;

    [Header("Options")]
    [SerializeField] private bool singleUse = true;
    [SerializeField] private bool hideSelfOnComplete = true;


    private bool hasTriggered;
    private bool watching;
    private bool stepRunning;
    private Collider myTrigger;   
    private float voTimer;
    private bool voPlaying;
    private bool stepCompleted;
    private float lastFailTime;
    private int failedAttempts;                              
    private float wrongTouchGraceEndTime;

    private enum GateState { Idle, WaitingForFalse, WaitingForTrue }
    private GateState gateState = GateState.Idle;

    private void Awake()
    {
        SetInstructionVisibleImmediate(false);

        myTrigger = GetComponent<Collider>();
        myTrigger.isTrigger = true;

        if (goalCanvas) goalCanvas.SetActive(false);
        if (timerRoot) timerRoot.SetActive(false);
        if (instructionCanvas) instructionCanvas.SetActive(false);
        if (attemptCounter) attemptCounter.gameObject.SetActive(false);
        if (timerLabel) timerLabel.text = labelText;

        if (wrongTouchDetector)
            wrongTouchDetector.OnWrongTwoFingerTouch += HandleWrongTwoFingerTouch;

        // In Awake() after subscribing:
        if (wrongTouchDetector)
        {
            wrongTouchDetector.IsArmed = false;                    // start safe
            wrongTouchDetector.correctTag = requiredCommonTag;     // keep tags in sync
        }
    }


    // TEMP DEBUG — remove after confirming Awake/OnEnable timing
    private void OnEnable()
    {
        VRDebugText.Log($"[{name}] OnEnable | myTrigger={(myTrigger != null ? "SET" : "NULL")} | wrongTouchDetector={(wrongTouchDetector != null ? "SET" : "NULL")}");
    }
    public void StartStep()
    {
        StartCoroutine(LogDelayed($"StartStep: {name} run={stepRunning} trig={hasTriggered}"));


        if (wrongTouchDetector)
            wrongTouchDetector.correctTag = requiredCommonTag;

       // if (startButton) startButton.SetActive(false);

        if (stepRunning) return;
        if (singleUse && hasTriggered) return;

        hasTriggered = true;
        stepRunning = true;

        if (myTrigger) myTrigger.enabled = false;

        StartCoroutine(RunStep());
    }

    private void PlayHaptics(float amplitude, float duration)
    {
        leftHapticPlayer?.SendHapticImpulse(amplitude, duration);
        rightHapticPlayer?.SendHapticImpulse(amplitude, duration);
    }

    private void PlayHaptics(float amplitude, float duration, bool leftHand)
    {
        if (leftHand)
            leftHapticPlayer?.SendHapticImpulse(amplitude, duration);
        else
            rightHapticPlayer?.SendHapticImpulse(amplitude, duration);
    }

    private void OnDestroy()
    {
        if (wrongTouchDetector)
            wrongTouchDetector.OnWrongTwoFingerTouch -= HandleWrongTwoFingerTouch;
    }

    private IEnumerator RunStep()
    {
        //VRDebugText.Log($"RunStep: {name}");

        if (wrongTouchDetector) wrongTouchDetector.IsArmed = false;
        
        failedAttempts = 0;
        ScoreManager.Instance?.ResetGoalScore();

        // Hide all UI 
        if (timerRoot) timerRoot.SetActive(false);
        if (attemptCounter) attemptCounter.gameObject.SetActive(false);
        if (goalCanvas) goalCanvas.SetActive(false);
        if (instructionCanvas) instructionCanvas.SetActive(false);

        // --- Cleanup old visuals ---
        foreach (var o in objectsToDestroy)
            if (o) Destroy(o);

        HideSelf(); // disables renderers/colliders under visualsRoot or self

        // Delay before showing instruction text + playing audio
        if (instructionDelay > 0f)
            yield return new WaitForSecondsRealtime(instructionDelay);

        // Show instruction text
        if (instructionTMP) instructionTMP.text = instructionText;
        FadeInstruction(true);   // Fades in the instruction canvas

        // Play VO once
        float wait = 0f;
        if (instructionClip)
        {
            if (voOutputSource)
            {
                voOutputSource.PlayOneShot(instructionClip);
                wait = instructionClip.length;
            }
        }
        // Wait until VO is done
        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        // --- Safety buffer after coaching VO ---
        yield return new WaitForSecondsRealtime(1.0f);

        // Hide instruction *decided to keep it on always
        //FadeInstruction(false);

        // Now show timer + attempts, start timer
        if (timerRoot) timerRoot.SetActive(true);
        
        if (timer)
        {
            //timer.Stop();
            timer.Begin();
        }
        if (attemptCounter)
        {
            attemptCounter.gameObject.SetActive(true);
            attemptCounter.ResetCounter(maxAttempts);
            attemptCounter.Set(0, maxAttempts); // forces the "/ max" display to refresh
        }

        // --- Arm the goal after small delay ---
        if (armDelay > 0f)
            yield return new WaitForSecondsRealtime(armDelay);

        ScoreManager.Instance?.BeginGoal();
        watching = true;
        wrongTouchGraceEndTime = Time.unscaledTime + wrongTouchArmGrace;
        fingerService?.ResetControllerStability();
        wrongTouchDetector?.ResetDwellState();

        if (fingerService != null)
            fingerService.currentGoalDebugTag = requiredCommonTag;

        // Only now allow wrong touches to be detected
        if (wrongTouchDetector)
            wrongTouchDetector.IsArmed = true;

        // Set gate state for rising/falling edge detection
        bool gateNow = fingerService && fingerService.IsGoalSelectionActive(requiredCommonTag);
        gateState = requireRisingEdge
            ? (gateNow ? GateState.WaitingForFalse : GateState.WaitingForTrue)
            : GateState.WaitingForTrue;
    }

    private void Update()
    {
        // global pause so goal detection isn't triggered
        if (GlobalDetectionPaused || AppPaused || !watching || !fingerService)
            return;

        var selectionSource = fingerService.GetGoalSelectionSource(requiredCommonTag);
        bool gate = selectionSource != FingerContactService.GoalSelectionSource.None;
        //VRDebugText.Log($"gate={gate} tag={requiredCommonTag}");

        // Controller-based wrong touch detection
        if (fingerService.TryGetControllerSelection(out GameObject hitObj, out string hitTag, out bool isLeftHand) &&
    hitTag != requiredCommonTag)
        {
            TriggerControllerWrongTouch(hitObj, isLeftHand);
        }

        if (!requireRisingEdge)
        {
            if (gate) CompleteStep(selectionSource);
            return;
        }

        switch (gateState)
        {
            case GateState.WaitingForFalse:
                if (!gate) gateState = GateState.WaitingForTrue;
                break;
            case GateState.WaitingForTrue:
                if (gate) CompleteStep(selectionSource);
                break;
        }
    }

    private void CompleteStep(FingerContactService.GoalSelectionSource source)
    {
        // Guards must be first
        if (GlobalDetectionPaused) return;
        if (stepCompleted) return;

        stepCompleted = true;
        watching = false;

        // Disarm and update UI
        if (wrongTouchDetector) wrongTouchDetector.IsArmed = false;
        attemptCounter?.Set(failedAttempts + 1, maxAttempts);

        // Snap instruction canvas visible (cancel any in-flight fade)
        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
        if (instructionCanvas) instructionCanvas.SetActive(true);
        if (instructionGroup) instructionGroup.alpha = 1f;

        // Visual feedback
        colorFeedback?.ShowCorrect();

        switch (source)
        {
            case FingerContactService.GoalSelectionSource.Left:
                PlayHaptics(correctHapticAmplitude, correctHapticDuration, leftHand: true);
                break;
            case FingerContactService.GoalSelectionSource.Right:
                PlayHaptics(correctHapticAmplitude, correctHapticDuration, leftHand: false);
                break;
            default: // Both or None (shouldn't reach here as None, but safe fallback)
                PlayHaptics(correctHapticAmplitude, correctHapticDuration);
                break;
        };
        if (hideSelfOnComplete) HideSelf();

        // Score — must happen before OnStepFinished fires
        int goalScore = ComputeScore();
        ScoreManager.Instance?.AddScore(goalScore, name);
        ScoreManager.Instance?.CommitGoalScore(name);

        //VRDebugText.Log($"COMPLETE: goalScore={goalScore}, total={ScoreManager.Instance?.TotalScore}");

        if (goalNameTextOnGoalCanvas) goalNameTextOnGoalCanvas.text = goalDisplayName;

        // Sequence last — its onComplete fires OnStepFinished asynchronously
        if (goalSequencePlayer)
        {
            if (goalCanvas) goalCanvas.SetActive(true);  // make sure canvas is active
            goalSequencePlayer.Play(voOutputSource,
                onComplete: () => OnStepFinished?.Invoke(this, true));
        }
        else
        {
            if (goalCanvas) goalCanvas.SetActive(true);
            OnStepFinished?.Invoke(this, true);
        }
    }

    public int ComputeScore()
    {
        float t = timer ? timer.Elapsed : 0f;

        int timeBonus = 0;

        if (timeBonusPerSecond > 0 && maxTimeBonusSeconds > 0)
        {
            float capped = Mathf.Min(t, maxTimeBonusSeconds);
            timeBonus = Mathf.Max(
                0,
                Mathf.RoundToInt((maxTimeBonusSeconds - capped) * timeBonusPerSecond)
            );
        }

        // No attempt penalty here (already applied live)
        return Mathf.Max(0, goalPoints + basePoints + timeBonus);
    }

    private void HandleWrongTwoFingerTouch(Collider wrongCollider)
    {
        if (Time.unscaledTime < wrongTouchGraceEndTime) return;
        if (AppPaused) return;
        // Add IsShowingFeedback check to existing guards
        if (!stepRunning || stepCompleted) return;
        if (wrongTouchDetector && !wrongTouchDetector.IsArmed) return;
        if (colorFeedback != null && colorFeedback.IsShowingFeedback) return;

        // Debounce fail counting: ignore if last counted too recently
        if (Time.unscaledTime - lastFailTime < failCooldown)
            return;
        lastFailTime = Time.unscaledTime;

        failedAttempts++;
        attemptCounter?.Set(failedAttempts, maxAttempts);

        if (voOutputSource && wrongTouchClip)
        {
            voOutputSource.Stop();
            voOutputSource.PlayOneShot(wrongTouchClip);
        }
        PlayHaptics(incorrectHapticAmplitude, incorrectHapticDuration);
        // live penalty
        ScoreManager.Instance?.ApplyDelta(-failPenaltyPerWrongTouch);

        var wrongFeedback = wrongCollider.GetComponent<GoalObjectColorFeedback>();
        if (wrongFeedback != null)
        {
            wrongFeedback.ShowIncorrect();
        }
        else
        {
            var renderers = wrongCollider.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                StartCoroutine(FlashRed(r));
        }


        if (failedAttempts >= maxAttempts)
        {
            watching = false;
            stepRunning = false;

            if (instructionCanvas) instructionCanvas.SetActive(false);
            if (goalCanvas) goalCanvas.SetActive(false);
            if (wrongTouchDetector) wrongTouchDetector.IsArmed = false;

            if (voOutputSource && failClip)
            {
                voOutputSource.Stop();
                voOutputSource.PlayOneShot(failClip);
            }

            if (goalNameTextOnFailCanvas) goalNameTextOnFailCanvas.text = goalDisplayName;
            if (failCanvas) failCanvas.SetActive(true);
            OnStepFinished?.Invoke(this, false);
        }



        // VRDebugText.Log($"WRONG TOUCH: -{failPenaltyPerWrongTouch}, goal={ScoreManager.Instance?.CurrentGoalScore}, total={ScoreManager.Instance?.TotalScore}");
    }

    private IEnumerator FlashRed(Renderer r)
    {
        if (!r) yield break;
        var mat = r.material;
        Color original = mat.color;
        mat.color = Color.red;
        yield return new WaitForSecondsRealtime(0.5f);
        if (r) mat.color = original;
    }
    private void HideSelf()
    {
        var root = visualsRoot ? visualsRoot : gameObject;

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (goalCanvas && r.transform.IsChildOf(goalCanvas.transform)) continue;
            if (instructionCanvas && r.transform.IsChildOf(instructionCanvas.transform)) continue;
            r.enabled = false;
        }

        foreach (var c in root.GetComponentsInChildren<Collider>(true))
        {
            if (goalCanvas && c.transform.IsChildOf(goalCanvas.transform)) continue;
            if (instructionCanvas && c.transform.IsChildOf(instructionCanvas.transform)) continue;
            c.enabled = false;
        }

    }

    public void SetFailPenalty(int penalty)
    {
        failPenaltyPerWrongTouch = penalty;
    }

    private bool PassesFilter(Collider other)
    {
        if ((allowedLayers.value & (1 << other.gameObject.layer)) == 0)
            return false;

        if (allowedStartTags != null && allowedStartTags.Length > 0)
        {
            string t = other.tag;
            foreach (var tag in allowedStartTags)
            {
                if (!string.IsNullOrEmpty(tag) && t == tag)
                    return true;
            }
            return false;
        }
        return true;
    }

    public void ResetStep()
    {
        //VRDebugText.Log("ResetStep: stopping coroutines");
        StopAllCoroutines();
        if (voOutputSource) voOutputSource.Stop();

        watching = false;
        stepRunning = false;
        hasTriggered = false;
        stepCompleted = false;

        failedAttempts = 0;
        //VRDebugText.Log("ResetStep: resetting goal score");
        ScoreManager.Instance?.ResetGoalScore();
        lastFailTime = -999f;
        //VRDebugText.Log("ResetStep: setting attempt counter");
        attemptCounter?.Set(0, maxAttempts);
        //VRDebugText.Log("ResetStep: hiding canvases");
        if (instructionCanvas) instructionCanvas.SetActive(false);
        if (timerRoot) timerRoot.SetActive(false);
        if (attemptCounter) attemptCounter.gameObject.SetActive(false);
        if (goalCanvas) goalCanvas.SetActive(false);
        if (failCanvas) failCanvas.SetActive(false);
        //VRDebugText.Log("ResetStep: disarming detector");
        if (wrongTouchDetector) wrongTouchDetector.IsArmed = false;
        //VRDebugText.Log("ResetStep: resetting color");
        colorFeedback?.ResetColor();
        //VRDebugText.Log("ResetStep: resetting sequence player");
        if (goalSequencePlayer) goalSequencePlayer.ResetPlayer();
        //VRDebugText.Log("ResetStep: enabling trigger");
        if (myTrigger) myTrigger.enabled = true;
        //VRDebugText.Log("ResetStep: DONE");
    }

    private Coroutine fadeRoutine;

    private void SetInstructionVisibleImmediate(bool visible)
    {
        if (instructionCanvas) instructionCanvas.SetActive(visible);
        if (instructionGroup)
        {
            instructionGroup.alpha = visible ? 1f : 0f;
            instructionGroup.interactable = visible;
            instructionGroup.blocksRaycasts = visible;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float to, float duration, Action onComplete = null)
    {
        if (!cg || duration <= 0f)
        {
            if (cg) cg.alpha = to;
            onComplete?.Invoke();
            yield break;
        }

        float from = cg.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }
        cg.alpha = to;
        onComplete?.Invoke();
    }

    private void FadeInstruction(bool show)
    {
        // stop any in-flight fade
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        if (!instructionCanvas)
            return;

        // ensure object is active before fading in
        if (show)
        {
            instructionCanvas.SetActive(true);
            if (instructionGroup == null)
            {
                // fallback: just show if no CanvasGroup assigned
                return;
            }
            instructionGroup.interactable = true;
            instructionGroup.blocksRaycasts = true;
            fadeRoutine = StartCoroutine(FadeCanvasGroup(
                instructionGroup, 1f, fadeInDuration));
        }
        else
        {
            if (instructionGroup == null)
            {
                // fallback: just hide if no CanvasGroup
                instructionCanvas.SetActive(false);
                return;
            }
            instructionGroup.interactable = false;
            instructionGroup.blocksRaycasts = false;
            fadeRoutine = StartCoroutine(FadeCanvasGroup(
                instructionGroup, 0f, fadeOutDuration,
                onComplete: () => instructionCanvas.SetActive(false)));
        }
    }

    public void PauseGoalDetectionForReset()
    {
        if (pausedForReset) return;
        pausedForReset = true;

        watching = false;
        if (wrongTouchDetector)
            wrongTouchDetector.IsArmed = false;
    }

    public void ResumeGoalDetectionAfterReset()
    {
        if (!pausedForReset) return;
        pausedForReset = false;

        // If the step is over, don't rearm anything
        if (stepCompleted || !fingerService)
            return;

        // Re-arm detection in a safe "rising edge" state,
        // same logic as at the end of RunStep()
        watching = true;
        wrongTouchGraceEndTime = Time.unscaledTime + wrongTouchArmGrace;
        fingerService?.ResetControllerStability();
        wrongTouchDetector?.ResetDwellState();
        if (wrongTouchDetector)
            wrongTouchDetector.IsArmed = true;

        bool gateNow = fingerService.IsGoalSelectionActive(requiredCommonTag);
        gateState = requireRisingEdge
            ? (gateNow ? GateState.WaitingForFalse : GateState.WaitingForTrue)
            : GateState.WaitingForTrue;
    }

    private IEnumerator LogDelayed(string msg)
    {
        yield return new WaitForSecondsRealtime(1.5f);
        VRDebugText.Log(msg);
    }

    private void TriggerControllerWrongTouch(GameObject wrongObject, bool isLeftHand)
    {
        if (Time.unscaledTime < wrongTouchGraceEndTime) return;
        if (!stepRunning || stepCompleted) return;
        if (colorFeedback != null && colorFeedback.IsShowingFeedback) return;

        if (Time.unscaledTime - lastFailTime < failCooldown)
            return;
        lastFailTime = Time.unscaledTime;

        failedAttempts++;
        attemptCounter?.Set(failedAttempts, maxAttempts);

        if (voOutputSource && wrongTouchClip)
        {
            voOutputSource.Stop();
            voOutputSource.PlayOneShot(wrongTouchClip);
        }
        PlayHaptics(incorrectHapticAmplitude, incorrectHapticDuration, isLeftHand);

        ScoreManager.Instance?.ApplyDelta(-failPenaltyPerWrongTouch);

        var wrongFeedback = wrongObject.GetComponent<GoalObjectColorFeedback>();
        if (wrongFeedback != null)
        {
            wrongFeedback.ShowIncorrect();
        }
        else
        {
            var renderers = wrongObject.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                StartCoroutine(FlashRed(r));
        }

        if (failedAttempts >= maxAttempts)
        {
            watching = false;
            stepRunning = false;

            if (instructionCanvas) instructionCanvas.SetActive(false);
            if (goalCanvas) goalCanvas.SetActive(false);
            if (wrongTouchDetector) wrongTouchDetector.IsArmed = false;

            if (voOutputSource && failClip)
            {
                voOutputSource.Stop();
                voOutputSource.PlayOneShot(failClip);
            }

            if (goalNameTextOnFailCanvas) goalNameTextOnFailCanvas.text = goalDisplayName;
            if (failCanvas) failCanvas.SetActive(true);
            OnStepFinished?.Invoke(this, false);
        }
    }
}