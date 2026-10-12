using System;
using System.Collections.Generic;
using UnityEngine;

public class TwoFingerWrongTouchDetector : MonoBehaviour
{

    [Header("Finger tips (colliders)")]
    public Collider leftFingerTip;
    public Collider rightFingerTip;

    [Header("Exclusions")]
    [Tooltip("Structural shell colliders that should never themselves count as a touch target.")]
    public Collider[] excludedShellColliders;

    [Header("Detection")]
    public string correctTag = "Goal";   // your valid tag on the correct target
    public float minDwell = 0.05f;       // debounce dwell (s) to avoid flicker

    public bool IsArmed { get; set; }    // StepController toggles this

    public event Action<Collider> OnWrongTwoFingerTouch; // StepController subscribes action

    readonly Dictionary<Collider, float> dwellTimer = new();
    Collider lastCounted;

    void Reset() { minDwell = 0.05f; }

    void Update()
    {
       
        if (!IsArmed || leftFingerTip == null || rightFingerTip == null) return;


        var leftHits = new List<Collider>(GetOverlaps(leftFingerTip));
        var rightHits = new List<Collider>(GetOverlaps(rightFingerTip));

        Collider common = null;
        float closestSqrDist = float.MaxValue;
        Vector3 midpoint = (leftFingerTip.bounds.center + rightFingerTip.bounds.center) * 0.5f;

        Collider correctCommon = null; // NEW: tracks the current goal's collider if both fingers are on it

        foreach (var l in leftHits)
        {
            if (l == leftFingerTip || l == rightFingerTip) continue;
            if (!rightHits.Contains(l)) continue;
            if (excludedShellColliders != null && System.Array.IndexOf(excludedShellColliders, l) >= 0) continue;
            if (l.transform == leftFingerTip.transform || l.transform == rightFingerTip.transform)
                continue;
            if (l.transform.IsChildOf(leftFingerTip.transform.root) ||
                l.transform.IsChildOf(rightFingerTip.transform.root))
                continue;

            if (l.CompareTag(correctTag))
            {
                correctCommon = l;
                continue;
            }


            Vector3 cp = l.ClosestPoint(midpoint);
            float sqrDist = (cp - midpoint).sqrMagnitude;

            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                common = l;
            }
        }

        if (correctCommon != null)
        {
            lastCounted = null;
            dwellTimer.Clear();
            return;
        }

        if (common != null)
            //VRDebugText.Log($"Common: {common.gameObject.name} dist={Mathf.Sqrt(closestSqrDist):0.000}");


        // no common collider => reset latch
        if (common == null)
        {
            lastCounted = null;
            dwellTimer.Clear(); // clear timers when no shared contact
            return;
        }

        float t = Time.unscaledTime;

        // dwell timer logic
        if (!dwellTimer.ContainsKey(common)) dwellTimer[common] = t;

        // trigger only once per distinct contact session
        if (t - dwellTimer[common] >= minDwell && lastCounted != common)
        {
            lastCounted = common;
            OnWrongTwoFingerTouch?.Invoke(common);
        }

        // if both fingers stay on the same wrong collider,
        // do NOT repeatedly trigger; only reset when they separate
        if (lastCounted != null && common != lastCounted)
        {
            dwellTimer.Clear();
            dwellTimer[common] = t;
        }

    }

    public void ResetDwellState()
    {
        lastCounted = null;
        dwellTimer.Clear();
    }

    static Collider[] GetOverlaps(Collider tip)
    {
        // Use the collider’s bounds as an overlap region; more reliable than sphere casts in tight UI.
        var center = tip.bounds.center;
        var halfExt = tip.bounds.extents + new Vector3(0.002f, 0.002f, 0.002f);
        return Physics.OverlapBox(center, halfExt, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
    }
}