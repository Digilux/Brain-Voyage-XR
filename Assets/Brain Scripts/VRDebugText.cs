using TMPro;
using UnityEngine;

public class VRDebugText : MonoBehaviour
{
    public TMP_Text label;

    [Header("Release Builds")]
    [Tooltip("Object hidden in non-development builds. Leave empty to hide just the label; assign the panel root if the label has a background.")]
    [SerializeField] private GameObject rootToHide;
    [Tooltip("Force the overlay on in release builds (testing only — keep OFF for the store build).")]
    [SerializeField] private bool showInReleaseBuilds = false;

    [Header("Log Mode")]
    [Tooltip("Append each message on a new line instead of replacing. Turn on temporarily for multi-line captures.")]
    [SerializeField] private bool appendMode = false;
    [Tooltip("Lines kept in append mode before the oldest are dropped.")]
    [SerializeField] private int maxLines = 12;

    private static VRDebugText _instance;

    // True in the Editor and in builds with "Development Build" ticked
    private static bool Enabled => Debug.isDebugBuild || (_instance && _instance.showInReleaseBuilds);

    void Awake()
    {
        _instance = this;

        if (!Enabled)
        {
            var root = rootToHide ? rootToHide : (label ? label.gameObject : null);
            if (root) root.SetActive(false);
        }
    }

    public static void Log(string msg)
    {
        if (!_instance || !_instance.label || !Enabled) return;

        var lbl = _instance.label;

        if (!_instance.appendMode)
        {
            lbl.text = msg;
            return;
        }

        string text = string.IsNullOrEmpty(lbl.text) ? msg : lbl.text + "\n" + msg;
        var lines = text.Split('\n');
        int keep = Mathf.Max(1, _instance.maxLines);
        if (lines.Length > keep)
            text = string.Join("\n", lines, lines.Length - keep, keep);

        lbl.text = text;
    }
}