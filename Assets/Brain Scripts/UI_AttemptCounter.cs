using TMPro;
using UnityEngine;

public class UI_AttemptCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text label;         // optional “Attempts:” text
    [SerializeField] private TMP_Text value;         // “3 / 6”
    [SerializeField] private string labelText = "Attempts:";
    [SerializeField] private int maxAttempts = 3;

    public int Current { get; private set; }

    void Awake()
    {
        if (label) label.text = labelText;
        Set(0, maxAttempts);
    }

    public void Set(int current, int max)
    {
        Current = Mathf.Max(0, current);
        maxAttempts = Mathf.Max(1, max);
        if (value) value.text = $"{Current} / {maxAttempts}";
    }

    public void Increment()
    {
        Set(Current + 1, maxAttempts);
    }

    public void ResetCounter(int max)
    {
        Set(0, max);
    }
}