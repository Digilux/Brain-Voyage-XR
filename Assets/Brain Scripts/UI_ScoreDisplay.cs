using UnityEngine;
using TMPro;

public class UI_ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    [SerializeField] private string currentPrefix = "Goal Score: ";
    [SerializeField] private string totalPrefix = "Total Score: ";

    private void Awake()
    {
        if (!scoreText) scoreText = GetComponent<TMP_Text>();
    }

    private void Start()
    {

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;

        Refresh();
    }

    private void OnEnable()
    {
        // Only re-subscribe if already initialized (handles re-activation mid-game)
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            Refresh();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe safely
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
    }

    private void HandleScoreChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (ScoreManager.Instance == null) { SetText(0, 0); return; }

        int current = ScoreManager.Instance.IsGoalActive
            ? ScoreManager.Instance.CurrentGoalScore   // live — goes negative on wrong touches
            : ScoreManager.Instance.LastGoalScore;     // snapshot after goal completed

        int total = ScoreManager.Instance.TotalScore;

        SetText(current, total);
    }
    private void SetText(int current, int total)
    {
        if (!scoreText) return;

        scoreText.text =
            $"<color=#43A4FF>{currentPrefix}</color>{current}\n" +
        $"<color=#43A4FF>{totalPrefix}</color>{total}";
    }
}