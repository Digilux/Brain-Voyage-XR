using UnityEngine;
using TMPro;

public class UI_HighScoreDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text scoresText;
    [SerializeField] private int maxEntries = 10;

    [Tooltip("Easy / Medium / Hard. Leave empty on the end-of-game board; it's set at runtime by ShowForDifficulty.")]
    [SerializeField] private string filterDifficulty = "";

    [Header("Headers")]
    [Tooltip("Header when no difficulty filter is set.")]
    [SerializeField] private string allScoresHeader = "All Scores";

    private void Awake()
    {
        if (!scoresText) scoresText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.OnHighScoresChanged += Refresh;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (HighScoreManager.Instance != null)
            HighScoreManager.Instance.OnHighScoresChanged -= Refresh;
    }

    /// <summary>Call this before showing the display to filter by difficulty.</summary>
    public void ShowForDifficulty(string difficulty)
    {
        filterDifficulty = difficulty;
        Refresh();
    }

    public void ShowAll()
    {
        filterDifficulty = "";
        Refresh();
    }

    public void Refresh()
    {
        var allEntries = HighScoreManager.Instance != null
        ? HighScoreManager.Instance.Entries
        : new System.Collections.Generic.List<HighScoreManager.Entry>();

        // Only ever show the current difficulty — never a mixed list
        var entries = allEntries.FindAll(e => e.difficulty == filterDifficulty);
        var header = string.IsNullOrEmpty(filterDifficulty)
            ? "--- No difficulty set ---"
            : $"--- {filterDifficulty} ---";

        VRDebugText.Log($"HighScore Refresh: filter='{filterDifficulty}', matches={entries.Count}");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(header);

        for (int i = 0; i < maxEntries; i++)
        {
            if (i < entries.Count)
                sb.AppendLine($"{i + 1,2}. {entries[i].initials,-4}  {entries[i].score,6}");
            else
                sb.AppendLine($"{i + 1,2}. {"---",-4}  {"000",6}");
        }

        scoresText.text = sb.ToString();
    }
}