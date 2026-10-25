using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class GoalSequenceController : MonoBehaviour
{
    [Header("Intro Audio")]
    [SerializeField] private AudioSource introVoSource;
    [SerializeField] private AudioClip introClip;
    [SerializeField] private GameObject introInstructionCanvas;
    [SerializeField] private TMP_Text introInstructionTMP;
    [TextArea] [SerializeField] private string introInstructionText;

    [Header("Brain")]
    [SerializeField] private GameObject globalMoveBrainObject;

    [System.Serializable]
    public class StepEntry
    {
        public StepController step;
        public bool enabled = true;   // uncheck to skip this goal during playtesting
    }

    [Header("Steps in order")]
    [SerializeField] private StepEntry[] stepEntries;

    [Header("Randomization")]
    [SerializeField] private bool randomizeOrder = false;

    [Header("Navigation Buttons")]
    [SerializeField] private Button nextGoalButtonGoalCanvas;
    [SerializeField] private Button nextGoalButtonFailCanvas;

    [Header("FINAL CANVAS  (appears after the last goal)")]
    [Tooltip("Root of the Final Canvas.")]
    [SerializeField] private GameObject finalCanvas;
    [Tooltip("Final Canvas → the TIME number (e.g. UI Timer Digits).")]
    [SerializeField] private TMP_Text finalTimeText;
    [Tooltip("Final Canvas → the SCORE number (e.g. UI Score Digits).")]
    [SerializeField] private TMP_Text finalScoreText;

    [Header("HIGH SCORE CANVAS  (initials + leaderboard)")]
    [Tooltip("Root of the High Score Canvas.")]
    [SerializeField] private GameObject scoreCanvas;
    [Tooltip("High Score Canvas → the player's SCORE text (shows \"Score: 123\").")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("High Score Canvas → the player's TIME text. Optional: leave as None to skip.")]
    [SerializeField] private TMP_Text scoreCanvasTimeText;
    [Tooltip("High Score Canvas → the leaderboard list.")]
    [SerializeField] private UI_HighScoreDisplay highScoreDisplay;
    [Tooltip("High Score Canvas → the initials entry panel.")]
    [SerializeField] private UI_InitialsInput initialsInput;

    [Header("SESSION DATA  (sources, not displays)")]
    [Tooltip("The timer that measures the whole session.")]
    [SerializeField] private UI_Timer sessionTimer;
    [SerializeField] private ScoreManager scoreManager;

    [Header("Setup")]
    [SerializeField] private bool autoFindSteps = false;

    private string currentDifficulty = "";  // ADD: tracks difficulty for leaderboard filtering
    public void StartEasy() { currentDifficulty = "Easy"; StartSequence(6, 5); }
    public void StartMedium() { currentDifficulty = "Medium"; StartSequence(3, 10); }
    public void StartHard() { currentDifficulty = "Hard"; StartSequence(1, 30); }

    private int currentIndex = 0;

    private List<StepController> activeSteps = new List<StepController>();

    // Stores the original Inspector order so re-shuffles start fresh
    private StepEntry[] originalSteps;


    private void Awake()
    {
        if (autoFindSteps)
        {
            var found = GetComponentsInChildren<StepController>(true);
            stepEntries = new StepEntry[found.Length];
            for (int i = 0; i < found.Length; i++)
                stepEntries[i] = new StepEntry { step = found[i], enabled = true };
        }

        // Snapshot original order before any shuffle
        originalSteps = (StepEntry[])stepEntries.Clone();
    }

    private void Start()
    {
        if (scoreCanvas) scoreCanvas.SetActive(false);
        if (finalCanvas) finalCanvas.SetActive(false);

        if (!scoreManager) scoreManager = ScoreManager.Instance;

        if (randomizeOrder)
            ShuffleSteps();

        for (int i = 0; i < stepEntries.Length; i++)
        {
            var step = stepEntries[i].step;
            if (!step) continue;
            step.gameObject.SetActive(false);
            step.OnStepFinished += HandleStepFinished;
        }
    }

    public void StartSequence(int maxAttempts, int penaltyPerWrongTouch)
    {
        if (globalMoveBrainObject) globalMoveBrainObject.SetActive(true);

        ScoreManager.Instance?.ResetScore();   // ensure TotalScore starts at 0 (or startingScore)

        foreach (var entry in stepEntries)
        {
            if (!entry.step) continue;
            entry.step.maxAttempts = maxAttempts;
            entry.step.SetFailPenalty(penaltyPerWrongTouch);
        }
        StartSequence();
    }

    // In GoalSequenceController.cs — add this method
    // NEW
    public void StartSequence()
    {
        currentIndex = 0;

        if (stepEntries == null || stepEntries.Length == 0)
            return;

        // Reset/deactivate everything first (including disabled ones)
        foreach (var entry in stepEntries)
        {
            if (!entry.step) continue;
            entry.step.ResetStep();
            entry.step.gameObject.SetActive(false);
        }

        // Build the active (enabled) list in inspector order
        activeSteps.Clear();
        foreach (var entry in stepEntries)
        {
            if (entry.step && entry.enabled)
                activeSteps.Add(entry.step);
        }

        if (activeSteps.Count == 0)
        {
            //VRDebugText.Log("ERROR: no enabled steps in sequence!");
            return;
        }

        activeSteps[0].gameObject.SetActive(true);
        StartCoroutine(PlayIntroThenStart(activeSteps[0]));
    }

    private IEnumerator PlayIntroThenStart(StepController firstStep)
    {
        if (introClip && introVoSource)
        {
            if (introInstructionCanvas)
            {
                if (introInstructionTMP) introInstructionTMP.text = introInstructionText;
                introInstructionCanvas.SetActive(true);
            }

            introVoSource.PlayOneShot(introClip);
            yield return new WaitForSecondsRealtime(introClip.length);

            if (introInstructionCanvas)
                introInstructionCanvas.SetActive(false);
        }

        firstStep.StartStep();
    }

    private void OnDestroy()
    {
        if (stepEntries == null) return;

        for (int i = 0; i < stepEntries.Length; i++)
        {
            if (!stepEntries[i].step) continue;
            stepEntries[i].step.OnStepFinished -= HandleStepFinished;
        }
    }

    // Fisher-Yates shuffle — uniform, unbiased, O(n)
    private void ShuffleSteps()
    {
        for (int i = stepEntries.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (stepEntries[i], stepEntries[j]) = (stepEntries[j], stepEntries[i]);
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder("Shuffled order: ");
        for (int i = 0; i < stepEntries.Length; i++)
            sb.Append(stepEntries[i].step ? stepEntries[i].step.name : "null").Append(i < stepEntries.Length - 1 ? " → " : "");
        //VRDebugText.Log(sb.ToString());
    }

    private void HandleStepFinished(StepController step, bool success)
    {
        if (!success) return;

        //VRDebugText.Log($"Goal complete: {step.name} | TOTAL {scoreManager?.TotalScore}");
    }

    public void NextGoal()
    {
        //VRDebugText.Log($"NextGoal: index={currentIndex}");

        if (activeSteps == null || activeSteps.Count == 0)
            return;

        if (currentIndex < activeSteps.Count)
        {
            activeSteps[currentIndex].ResetStep();
            activeSteps[currentIndex].gameObject.SetActive(false);
        }

        currentIndex++;
        if (currentIndex >= activeSteps.Count)
        {
            if (sessionTimer) sessionTimer.Stop();

            RefreshResultTexts();

            //if (finalTimeText && sessionTimer)
            //    finalTimeText.text = sessionTimer.GetFormattedTime();

            //if (scoreText && ScoreManager.Instance)
            //    scoreText.text = $"Score: {ScoreManager.Instance.TotalScore}";

            if (finalCanvas)
                finalCanvas.SetActive(true);

            return;
        }

        var nextStep = activeSteps[currentIndex];

        nextStep.ResetStep();
        nextStep.gameObject.SetActive(true);
        nextStep.StartStep();
    }

    public void ResetSequence(bool activateFirstStepOnly = true)
    {
        if (scoreCanvas) scoreCanvas.SetActive(false);
        if (finalCanvas) finalCanvas.SetActive(false);

        ScoreManager.Instance?.ResetScore();

        // Restore original order before re-shuffling
        if (originalSteps != null)
            stepEntries = (StepEntry[])originalSteps.Clone();

        if (randomizeOrder)
            ShuffleSteps();

        activeSteps.Clear();
        if (stepEntries != null)
        {
            foreach (var entry in stepEntries)
            {
                if (!entry.step) continue;

                entry.step.ResetStep();
                entry.step.gameObject.SetActive(false);

                if (entry.enabled)
                    activeSteps.Add(entry.step);
            }

            if (activateFirstStepOnly && activeSteps.Count > 0)
                activeSteps[0].gameObject.SetActive(true);
        }

        currentIndex = 0;
    }

    public void StartFromBeginning()
    {
        ResetSequence(activateFirstStepOnly: true);

        if (activeSteps != null && activeSteps.Count > 0 && activeSteps[0] != null)
            StartCoroutine(PlayIntroThenStart(activeSteps[0]));
    }

    public void ShowScore()
    {
        //VRDebugText.Log($"ShowScore called | manager={scoreManager != null} | score={scoreManager?.TotalScore}");

        RefreshResultTexts();
        //if (scoreText && scoreManager)
        //    scoreText.text = $"Score: {scoreManager.TotalScore}";

        // Filter the leaderboard to match the difficulty just played
        if (highScoreDisplay)
            highScoreDisplay.ShowForDifficulty(currentDifficulty);

        if (scoreCanvas) scoreCanvas.SetActive(true);
        if (finalCanvas) finalCanvas.SetActive(false);

    }

    private void RefreshResultTexts()
    {
        int total = ScoreManager.Instance ? ScoreManager.Instance.TotalScore : 0;
        string time = sessionTimer ? sessionTimer.GetFormattedTime() : "00:00";

        if (finalScoreText) finalScoreText.text = total.ToString();
        if (finalTimeText) finalTimeText.text = time;
        if (scoreText) scoreText.text = total.ToString();
        if (scoreCanvasTimeText) scoreCanvasTimeText.text = time;
    }
    public void OpenInitialsInput()
    {
        //VRDebugText.Log($"OpenInitialsInput called | initialsInput={initialsInput != null} | score={ScoreManager.Instance?.TotalScore} | difficulty={currentDifficulty}");

        // Set the filter BEFORE enabling the canvas so OnEnable's Refresh is already filtered
        if (highScoreDisplay)
            highScoreDisplay.ShowForDifficulty(currentDifficulty);

        if (scoreCanvas) scoreCanvas.SetActive(true);   // make sure parent is active first
        if (finalCanvas) finalCanvas.SetActive(false);

        if (initialsInput)
            initialsInput.Show(ScoreManager.Instance.TotalScore, currentDifficulty);
    }

}