using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetButton : MonoBehaviour
{
    [SerializeField] private GameObject goalCanvasRoot;
    [SerializeField] private GameObject failCanvasRoot;
    [SerializeField] private GameObject finalCanvasRoot;

    // There are eight objects here to reset comprising the brain 
    public BackUpResettable[] resettableObjects;

    // Tag for reset button to make sure its the exact finger collider
    [SerializeField] private string allowedResetTag = "L_FingerTip";

    // a simple collider object resets all objects in the scene
    public void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(allowedResetTag))
            return;
            
        StartCoroutine(ResetAllObjectsSafely());
    }

    // If you also call it from a UI button:
    public void ResetAllFromUI()
    {
        VRDebugText.Log("ResetAllFromUI");

        StartCoroutine(ResetAllObjectsSafely());
    }

    private IEnumerator ResetAllObjectsSafely()
    {
 
        // 1) Pause ALL step detection
        StepController.GlobalDetectionPaused = true;


        if (goalCanvasRoot) goalCanvasRoot.SetActive(false);
        if (failCanvasRoot) failCanvasRoot.SetActive(false);
        if (finalCanvasRoot) finalCanvasRoot.SetActive(false);

        // 2) Reset slices
        foreach (var obj in resettableObjects)
        {
            if (obj != null)
                obj.ResetObject();
        }

        // 3) Wait a frame or two to let physics/colliders settle
        //yield return null;
        //yield return null;

        // Give physics/overlaps time to settle (Quest hand colliders are “noisy”)
        yield return new WaitForSecondsRealtime(1f);

        // 4) Re-enable detection
        StepController.GlobalDetectionPaused = false;

    }

    // Old direct reset (no longer used):
    public void ResetAllObjects()
    {
        foreach (var obj in resettableObjects)
        {
            if (obj != null)
                obj.ResetObject();
        }
    }


}
