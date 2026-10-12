using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }


    [Header("Debug Output")]
    [SerializeField] private VRDebugText vrDebugText;
    [SerializeField] private bool logToVRDebug = true;

    [Header("Score")]
    [SerializeField] private int startingScore = 0;

    public int CurrentGoalScore { get; private set; }
    public int TotalScore { get; private set; }
    public int LastGoalScore { get; private set; }  // keeps the just-earned points after committing
    public bool IsGoalActive { get; private set; }

    // Keep this, but don't assume the int means "total"
    public event Action<int> OnScoreChanged;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        TotalScore = startingScore;
        CurrentGoalScore = 0;
        LastGoalScore = 0;
        IsGoalActive = false;

        PushScoreToDebug();
        NotifyScoreChanged();
    }

    public void ResetScore()
    {
        IsGoalActive = false;
        TotalScore = startingScore;
        CurrentGoalScore = 0;
        PushScoreToDebug("RESET");
        NotifyScoreChanged();
    }

    public void ResetGoalScore()
    {
        CurrentGoalScore = 0;
        PushScoreToDebug("GOAL RESET");
        NotifyScoreChanged();
    }

    public void BeginGoal()
    {
        IsGoalActive = true;
        CurrentGoalScore = 0;
        PushScoreToDebug("GOAL BEGIN");
        NotifyScoreChanged();
    }

    // Use this for "goal completion points" and any positive scoring for the current goal
    public void AddScore(int amount, string source = null)
    {
        if (amount == 0) return;

        CurrentGoalScore += amount;
        
        string label = string.IsNullOrEmpty(source) ? "" : $" ({source})";
        PushScoreToDebug($"{(amount > 0 ? "+" : "")}{amount}{label} | GOAL {CurrentGoalScore}");

        NotifyScoreChanged();
    }

    // Use this for penalties / bonuses during the goal
    public void ApplyDelta(int delta)
    {
        AddScore(delta);
    }

    // Call this ONCE when a goal ends successfully
    public void CommitGoalScore(string source = null)
    {
        IsGoalActive = false;             
        LastGoalScore = CurrentGoalScore;

        TotalScore += CurrentGoalScore;

        string label = string.IsNullOrEmpty(source) ? "" : $" ({source})";
        PushScoreToDebug($"COMMIT {CurrentGoalScore}{label} | TOTAL {TotalScore}");

        CurrentGoalScore = 0; // IMPORTANT: prevents accidental double-commit

        NotifyScoreChanged();
    }

    private void NotifyScoreChanged()
    {
        OnScoreChanged?.Invoke(TotalScore);
    }

    private void PushScoreToDebug(string msg = null)
    {
        if (!logToVRDebug || !vrDebugText) return;

        if (!string.IsNullOrEmpty(msg))
            VRDebugText.Log($"SCORE {msg}");
        else
            VRDebugText.Log($"TOTAL {TotalScore} | GOAL {CurrentGoalScore}");
    }
}