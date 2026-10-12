using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_VRKeyboard : MonoBehaviour
{
    [SerializeField] private TMP_InputField targetField;

    private static readonly string row1 = "ABCDEFG";
    private static readonly string row2 = "HIJKLMN";
    private static readonly string row3 = "OPQRST";
    private static readonly string row4 = "UVWXYZ";
    private static readonly string row5 = "1234567890";

    [Header("Layout")]
    [SerializeField] private Transform row1Parent;
    [SerializeField] private Transform row2Parent;
    [SerializeField] private Transform row3Parent;
    [SerializeField] private Transform row4Parent;
    [SerializeField] private Transform row5Parent;
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button clearButton;

    [Header("Show Keyboard")]
    [SerializeField] private Button showKeyboardButton;

    [Header("Confirm")]
    [SerializeField] private Button confirmButton;

    [Header("Key Style")]
    [SerializeField] private TMP_FontAsset keyFont;
    [SerializeField] private Color keyColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color keyTextColor = Color.white;
    [SerializeField] private Vector2 keySize = new Vector2(60f, 60f);

    /// <summary>Fired when the player presses Confirm. Subscribe to handle name submission.</summary>
    public event System.Action<string> OnConfirmed;

    private void Start()
    {
        BuildRow(row1, row1Parent);
        BuildRow(row2, row2Parent);
        BuildRow(row3, row3Parent);
        BuildRow(row4, row4Parent);
        BuildRow(row5, row5Parent);

        if (showKeyboardButton) showKeyboardButton.onClick.AddListener(Show);
        if (backspaceButton) backspaceButton.onClick.AddListener(OnBackspace);
        if (clearButton) clearButton.onClick.AddListener(OnClear);
        if (confirmButton) confirmButton.onClick.AddListener(OnConfirm);
    }
    private void Awake()
    {
        gameObject.SetActive(false);
    }
    public void SetTarget(TMP_InputField field)
    {
        targetField = field;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    private void BuildRow(string letters, Transform parent)
    {
        foreach (char c in letters)
            CreateKey(c.ToString(), parent);
    }

    private void CreateKey(string letter, Transform parent)
    {
        GameObject keyObj = new GameObject($"Key_{letter}");
        keyObj.transform.SetParent(parent, false);

        RectTransform rt = keyObj.AddComponent<RectTransform>();
        rt.sizeDelta = keySize;

        Image img = keyObj.AddComponent<Image>();
        img.color = keyColor;

        Button btn = keyObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(keyColor.r + 0.15f, 1f),
            Mathf.Min(keyColor.g + 0.15f, 1f),
            Mathf.Min(keyColor.b + 0.15f, 1f));
        cb.pressedColor = new Color(
            Mathf.Max(keyColor.r - 0.1f, 0f),
            Mathf.Max(keyColor.g - 0.1f, 0f),
            Mathf.Max(keyColor.b - 0.1f, 0f));
        btn.colors = cb;

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(keyObj.transform, false);

        RectTransform trt = textObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = letter;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = keyTextColor;
        tmp.fontSize = 24;
        if (keyFont) tmp.font = keyFont;

        btn.onClick.AddListener(() => OnKeyPressed(letter));
    }

    private void OnKeyPressed(string letter)
    {
        if (targetField == null) return;
        if (targetField.text.Length >= targetField.characterLimit) return;
        targetField.text += letter;
    }

    private void OnBackspace()
    {
        if (targetField == null || targetField.text.Length == 0) return;
        targetField.text = targetField.text.Substring(0, targetField.text.Length - 1);
    }

    private void OnClear()
    {
        if (targetField == null) return;
        targetField.text = "";
    }
    private void OnConfirm()
    {
        string entered = targetField != null ? targetField.text : "";
        OnConfirmed?.Invoke(entered);
    }

}