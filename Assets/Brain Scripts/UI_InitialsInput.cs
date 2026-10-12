using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_InitialsInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField initialsField;
    [SerializeField] private Button submitButton;
    [SerializeField] private UI_VRKeyboard keyboard;
    [SerializeField] private int scoreToSubmit;
    [SerializeField] private Button openKeyboardButton;
    [SerializeField] private UI_HighScoreDisplay highScoreDisplay; // assign in Inspector
    [SerializeField] private GoalSequenceController goalSequence;

    private string difficultyToSubmit = "";
    private void Awake()
    {
        initialsField.characterLimit = 3;
        initialsField.onValueChanged.AddListener(OnValueChanged);
       // initialsField.onSelect.AddListener(OnFieldSelected);
        submitButton.onClick.AddListener(OnSubmit);

        // Wire this up to a separate "open keyboard" button in the Inspector
        if (openKeyboardButton)
            openKeyboardButton.onClick.AddListener(OpenKeyboard);
    }
    public void OpenKeyboard()
    {
        if (keyboard == null) return;
        keyboard.SetTarget(initialsField);
        keyboard.Show();
    }
    private void OnEnable()
    {
        initialsField.text = "";
        if (keyboard) keyboard.Hide();
    }

    private void OnValueChanged(string value)
    {
        initialsField.text = value.ToUpper();
    }

    private void OnFieldSelected(string _)
    {
        if (keyboard == null) return;
        keyboard.SetTarget(initialsField);
        keyboard.Show();
    }

    public void Show(int score, string difficulty = "")
    {
        scoreToSubmit = score;
        difficultyToSubmit = difficulty;
        gameObject.SetActive(true);
    }

    private void OnSubmit()
    {
        string initials = initialsField.text.Trim();
        if (initials.Length == 0) return;

        if (keyboard) keyboard.Hide();
        HighScoreManager.Instance?.SubmitScore(scoreToSubmit, initials, difficultyToSubmit);

        //if (highScoreDisplay) highScoreDisplay.Refresh();
        
        gameObject.SetActive(false);
        goalSequence?.ShowScore();
    }
}