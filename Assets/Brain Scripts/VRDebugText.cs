using TMPro;
using UnityEngine;

public class VRDebugText : MonoBehaviour
{
    public TMP_Text label;

    private static VRDebugText _instance;
    private static float _holdUntil = -1f;

    void Awake() => _instance = this;

    /// <summary>Normal log — can be overwritten immediately by anything, including spam.</summary>
    public static void Log(string msg)
    {
        if (Time.unscaledTime < _holdUntil) return; // a priority message is still holding
        if (_instance && _instance.label)
            _instance.label.text = msg;
    }

    /// <summary>Priority log — holds the display for 'holdSeconds', ignoring normal Log() calls
    /// (including per-frame spam) until the hold expires.</summary>
    public static void LogPriority(string msg, float holdSeconds = 5f)
    {
        if (_instance && _instance.label)
            _instance.label.text = msg;
        _holdUntil = Time.unscaledTime + holdSeconds;
    }
}