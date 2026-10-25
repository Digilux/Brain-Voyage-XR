using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FingerContactService : MonoBehaviour
{
    /* Handles finger selection by two fingers as well as cone selection by touch controller. */

    [Header("Exclusions")]
    [Tooltip("Structural shell colliders that should never themselves count as a touch target.")]
    public Collider[] excludedShellColliders;

    [Header("Debug")]
    public string currentGoalDebugTag;
        
    public enum GoalSelectionSource { None, Left, Right, Both }

    [Header("Fingertips")]
    public Transform leftIndexTip;
    public Transform rightIndexTip;

    [Tooltip("Consecutive frames a controller hit must remain on the same object before it's accepted.")]
    public int controllerStableFrames = 3;

    private GameObject _lastControllerHit;
    private int _controllerStableCount;

    [Tooltip("Consecutive frames both index fingers must stay on the same wrong object before it counts as a wrong selection.")]
    public int handWrongStableFrames = 6;

    private GameObject _lastHandWrongHit;
    private int _handWrongStableCount;

    [Header("Geometry")]
    [Tooltip("MeshCollider(s) that the fingers can touch.")]
    public Collider[] geometryColliders;

    [Header("Contact Settings")]
    [Tooltip("Numerical tolerance for 'touch' via ClosestPoint (not a probe radius).")]
    public float contactEpsilon = 0.01f;

    [Tooltip("Tighter tolerance specifically for goal-tag priority selection. Must be <= contactEpsilon. Smaller = more precise, less forgiving near boundaries.")]
    public float priorityContactEpsilon = 0.004f;

    [System.Serializable]
    public class TagEpsilonOverride
    {
       public string tag;
       [Tooltip("Priority tolerance for this specific tag, in meters. Use for recessed/hard-to-reach regions like BrocasArea or WernickesArea.")]
       public float priorityEpsilon = 0.01f;
    }

    [Tooltip("Per-tag overrides for priorityContactEpsilon. Use for small/recessed regions that need a looser tolerance than the global default.")]
    public TagEpsilonOverride[] priorityEpsilonOverrides;


    [Tooltip("Frames to ignore on startup to avoid spawn overlap noise.")]
    public int warmupFrames = 8;

    [Tooltip("Consecutive frames both hands must satisfy the gate before reporting true (debounce).")]
    public int requiredStableFrames = 2;

    [Header("Controller Proximity Selection")]
    public Transform leftControllerTip;
    public Transform rightControllerTip;
    public InputActionReference leftTriggerAction;
    public InputActionReference rightTriggerAction;

    // per-frame state
    private readonly Dictionary<GameObject, string> _leftTags = new();
    private readonly Dictionary<GameObject, string> _rightTags = new();
    private readonly Dictionary<GameObject, string> _leftControllerTags = new();
    private readonly Dictionary<GameObject, string> _rightControllerTags = new();
    private int _frame;
    private int _stable;
    private string _lastQueryTag;

    void Update()
    {
        if (_frame++ < warmupFrames) return;
        _leftTags.Clear();
        _rightTags.Clear();
        _leftControllerTags.Clear();
        _rightControllerTags.Clear();

        if (leftIndexTip) Probe(leftIndexTip.position, _leftTags);
        if (rightIndexTip) Probe(rightIndexTip.position, _rightTags);

        // TEMP DEBUG — remove after testing
        string l = "-", r = "-";
        foreach (var kv in _leftTags) l = kv.Key.name;
        foreach (var kv in _rightTags) r = kv.Key.name;

        float dl = float.MaxValue, dr = float.MaxValue;
        string goalName = "NOT FOUND";
        if (!string.IsNullOrEmpty(currentGoalDebugTag) && leftIndexTip && rightIndexTip && geometryColliders != null)
        {
            foreach (var col in geometryColliders)
            {
                if (!col || col.gameObject.tag != currentGoalDebugTag) continue;
                goalName = col.name;
                dl = Mathf.Min(dl, Vector3.Distance(col.ClosestPoint(leftIndexTip.position), leftIndexTip.position));
                dr = Mathf.Min(dr, Vector3.Distance(col.ClosestPoint(rightIndexTip.position), rightIndexTip.position));
            }
        }
        VRDebugText.Log($"{goalName} L:{dl:0.000} R:{dr:0.000} | hit L:{l} R:{r}");

        if (leftControllerTip && leftTriggerAction != null)
        {
            float leftVal = leftTriggerAction.action.ReadValue<float>();
            if (leftVal > 0.5f)
                Probe(leftControllerTip.position, _leftControllerTags, debugTagFilter: currentGoalDebugTag);
        }

        if (rightControllerTip && rightTriggerAction != null)
        {
            float rightVal = rightTriggerAction.action.ReadValue<float>();
            if (rightVal > 0.5f)
                Probe(rightControllerTip.position, _rightControllerTags, debugTagFilter: currentGoalDebugTag);
        }
    }

    private void Probe(Vector3 tipPos, Dictionary<GameObject, string> map, string debugTagFilter = null)
    {
        if (geometryColliders == null) return;
        float eps2 = contactEpsilon * contactEpsilon;

        GameObject closestGo = null;
        string closestTag = null;
        float closestSqrDist = float.MaxValue;

        GameObject priorityGo = null;
        string priorityTag = null;
        float prioritySqrDist = float.MaxValue;

        for (int i = 0; i < geometryColliders.Length; i++)
        {
            var col = geometryColliders[i];
            if (!col) continue;
            if (excludedShellColliders != null && System.Array.IndexOf(excludedShellColliders, col) >= 0)
                continue;

            Vector3 cp = col.ClosestPoint(tipPos);
            float sqrDist = (cp - tipPos).sqrMagnitude;

            if (debugTagFilter != null && col.gameObject.tag == debugTagFilter)
            {
                VRDebugText.Log($"{col.gameObject.name} dist: {Mathf.Sqrt(sqrDist):0.000}");
            }

            var go = col.gameObject;
            if (go.tag == "Untagged") continue;

            if (sqrDist <= eps2 && sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closestGo = go;
                closestTag = go.tag;
            }

            // Separately track the best match for the currently prompted goal tag,
            // using its own (possibly looser) priority epsilon so it can win even
            // when another collider is technically closer.
            if (!string.IsNullOrEmpty(currentGoalDebugTag) && go.tag == currentGoalDebugTag)
            {
                float effectivePriorityEps2 = GetPriorityEps2(currentGoalDebugTag, priorityContactEpsilon * priorityContactEpsilon);
                if (sqrDist <= effectivePriorityEps2 && sqrDist < prioritySqrDist)
                {
                    prioritySqrDist = sqrDist;
                    priorityGo = go;
                    priorityTag = go.tag;
                }
            }
        }

        map.Clear();

        // Prompted goal tag takes priority if it's within its own radius,
        // even if something else was technically closer.
        if (priorityGo != null)
        {
            map[priorityGo] = priorityTag;
            return;
        }

        if (closestGo == null) return;
        map[closestGo] = closestTag;
    }

    private float GetPriorityEps2(string tag, float defaultEps2)
    {
       if (priorityEpsilonOverrides == null || string.IsNullOrEmpty(tag)) return defaultEps2;

       for (int i = 0; i<priorityEpsilonOverrides.Length; i++)
       {
           if (priorityEpsilonOverrides[i].tag == tag)
           {
               float e = priorityEpsilonOverrides[i].priorityEpsilon;
               return e* e;
           }
       }
       return defaultEps2;
    }
    public bool IsCommonTagActive(string requiredTag)
    {
        if (string.IsNullOrEmpty(requiredTag)) { _stable = 0; return false; }

        if (_lastQueryTag != requiredTag) { _lastQueryTag = requiredTag; _stable = 0; }

        bool leftHas = false, rightHas = false;

        foreach (var v in _leftTags.Values) { if (v == requiredTag) { leftHas = true; break; } }
        foreach (var v in _rightTags.Values) { if (v == requiredTag) { rightHas = true; break; } }

        bool instant = leftHas && rightHas;
        if (requiredStableFrames <= 0) return instant;

        _stable = instant ? _stable + 1 : 0;
        return _stable >= requiredStableFrames;
    }

    public bool IsAnyFingerTagActive(string requiredTag)
    {
        if (string.IsNullOrEmpty(requiredTag)) return false;

        foreach (var v in _leftTags.Values) { if (v == requiredTag) return true; }
        foreach (var v in _rightTags.Values) { if (v == requiredTag) return true; }
        return false;
    }

    public bool IsAnyFingerTagActive(string requiredTag, out GameObject hitObject)
    {
        hitObject = null;
        if (string.IsNullOrEmpty(requiredTag)) return false;

        foreach (var kv in _leftTags)
        {
            if (kv.Value == requiredTag) { hitObject = kv.Key; return true; }
        }
        foreach (var kv in _rightTags)
        {
            if (kv.Value == requiredTag) { hitObject = kv.Key; return true; }
        }
        return false;
    }

    public bool IsAnyFingerTouching(out GameObject hitObject, out string hitTag,
        Transform leftHandRoot = null, Transform rightHandRoot = null)
    {
        hitObject = null;
        hitTag = null;

        foreach (var kv in _leftTags)
        {
            if (leftHandRoot && kv.Key.transform.IsChildOf(leftHandRoot)) continue;
            if (rightHandRoot && kv.Key.transform.IsChildOf(rightHandRoot)) continue;
            hitObject = kv.Key;
            hitTag = kv.Value;
            return true;
        }
        foreach (var kv in _rightTags)
        {
            if (leftHandRoot && kv.Key.transform.IsChildOf(leftHandRoot)) continue;
            if (rightHandRoot && kv.Key.transform.IsChildOf(rightHandRoot)) continue;
            hitObject = kv.Key;
            hitTag = kv.Value;
            return true;
        }
        return false;
    }

    public bool IsGoalSelectionActive(string requiredTag)
    {
        if (string.IsNullOrEmpty(requiredTag)) return false;

        foreach (var v in _leftControllerTags.Values) { if (v == requiredTag) return true; }
        foreach (var v in _rightControllerTags.Values) { if (v == requiredTag) return true; }

        return IsCommonTagActive(requiredTag);
    }

    public GoalSelectionSource GetGoalSelectionSource(string requiredTag)
    {
        if (string.IsNullOrEmpty(requiredTag)) return GoalSelectionSource.None;

        bool leftController = false, rightController = false;
        foreach (var v in _leftControllerTags.Values) { if (v == requiredTag) { leftController = true; break; } }
        foreach (var v in _rightControllerTags.Values) { if (v == requiredTag) { rightController = true; break; } }

        if (leftController && rightController) return GoalSelectionSource.Both;
        if (leftController) return GoalSelectionSource.Left;
        if (rightController) return GoalSelectionSource.Right;

        // Hand-tracking requires both index fingers together, so it's always "Both"
        if (IsCommonTagActive(requiredTag)) return GoalSelectionSource.Both;

        return GoalSelectionSource.None;
    }

    public bool TryGetControllerSelection(out GameObject hitObject, out string hitTag, out bool isLeftHand)
    {
        GameObject currentHit = null;
        string currentTag = null;
        bool currentIsLeft = false;

        foreach (var kv in _leftControllerTags) { currentHit = kv.Key; currentTag = kv.Value; currentIsLeft = true; break; }
        if (currentHit == null)
        {
            foreach (var kv in _rightControllerTags) { currentHit = kv.Key; currentTag = kv.Value; currentIsLeft = false; break; }
        }

        if (currentHit == null)
        {
            _lastControllerHit = null;
            _controllerStableCount = 0;
            hitObject = null;
            hitTag = null;
            isLeftHand = false;
            return false;
        }

        if (currentHit == _lastControllerHit)
        {
            _controllerStableCount++;
        }
        else
        {
            _lastControllerHit = currentHit;
            _controllerStableCount = 1;
        }

        if (_controllerStableCount >= controllerStableFrames)
        {
            hitObject = currentHit;
            hitTag = currentTag;
            isLeftHand = currentIsLeft;
            return true;
        }

        hitObject = null;
        hitTag = null;
        isLeftHand = false;
        return false;
    }

    public void ResetControllerStability()
    {
        _lastControllerHit = null;
        _controllerStableCount = 0;
        _lastHandWrongHit = null;
        _handWrongStableCount = 0;
    }

    /// <summary>
    /// True once (on the frame stability is reached) when both index fingers resolve to the same
    /// non-goal object. Uses the same Probe results, exclusions, and priority logic as goal selection.
    /// </summary>
    public bool TryGetTwoFingerWrongSelection(string goalTag, out GameObject hitObject, out string hitTag)
    {
        hitObject = null;
        hitTag = null;

        GameObject common = null;
        string commonTag = null;
        foreach (var kv in _leftTags)
        {
            if (kv.Value == goalTag) continue;
            if (_rightTags.ContainsKey(kv.Key)) { common = kv.Key; commonTag = kv.Value; break; }
        }

        if (common == null)
        {
            _lastHandWrongHit = null;
            _handWrongStableCount = 0;
            return false;
        }

        if (common == _lastHandWrongHit) _handWrongStableCount++;
        else { _lastHandWrongHit = common; _handWrongStableCount = 1; }

        // Fire exactly once per continuous contact
        if (_handWrongStableCount != handWrongStableFrames) return false;

        hitObject = common;
        hitTag = commonTag;
        return true;
    }
}