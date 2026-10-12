using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class OldStepController : MonoBehaviour
{
    [Header("Detectors")]
    [SerializeField] private FingerContactService fingerService;
    [SerializeField] private string requiredCommonTag = "Triggerable";

    [Header("Audio")]
    [SerializeField] private AudioSource voOutputSource;
    [SerializeField] private AudioClip instructionClip;
    [SerializeField] private float instructionDelay = 0f;

    [Header("Timer UI")]
    [SerializeField] private GameObject timerRoot;     // parent that holds the UI_Timer + text (kept active)
    [SerializeField] private UI_Timer timer;           // count-up stopwatch
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private string labelText = "Time Remaining:";

    [Header("Goal UI")]
    [SerializeField] private GameObject goalCanvas;    // success panel (starts hidden, shown on completion)

    [Header("Cleanup visuals")]
    [SerializeField] private GameObject[] objectsToDestroy;
    [SerializeField] private GameObject visualsRoot;

    [Header("Trigger filter")]
    [SerializeField] private LayerMask allowedLayers = ~0;
    [SerializeField] private string[] allowedHandTags = new string[0];

    [Header("Behavior")]
    [SerializeField] private bool singleUse = true;

    [Header("Gate Behavior")]
    [SerializeField] private bool requireRisingEdge = true;
    [SerializeField, Tooltip("Delay (seconds) after timer start before we begin watching the gate.")]
    private float armDelay = 0.25f;  // set 0 if you don't want a delay

    private bool hasTriggered;
    private bool watching;
    private Collider ownCollider;

    private enum GateState { Idle, WaitingForFalse, WaitingForTrue }
    private GateState gateState = GateState.Idle;

    void Awake()
    {
        if (goalCanvas) goalCanvas.SetActive(false); // own the goal UI
        ownCollider = GetComponent<Collider>();
        if (ownCollider) ownCollider.isTrigger = true;
    }

    void OnValidate()
    {
        ownCollider = GetComponent<Collider>();
        if (ownCollider) ownCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (singleUse && hasTriggered) return;
        if (!PassesFilter(other)) return;

        hasTriggered = true;
        StartCoroutine(RunStep());
    }

    private IEnumerator RunStep()
    {
        // remove old UI / hide local visuals
        foreach (var o in objectsToDestroy) if (o) Destroy(o);
        HideSelf();

        // Optional delay then VO
        if (instructionDelay > 0f)
            yield return new WaitForSecondsRealtime(instructionDelay);

        float wait = 0f;
        if (instructionClip)
        {
            if (voOutputSource)
            {
                voOutputSource.PlayOneShot(instructionClip);
                wait = instructionClip.length;
            }
            else
            {
                Transform at = Camera.main ? Camera.main.transform : transform;
                var temp = new GameObject("OneShotVO");
                temp.transform.position = at.position;
                var src = temp.AddComponent<AudioSource>();
                src.spatialBlend = 0f;
                src.clip = instructionClip;
                src.Play();
                Destroy(temp, instructionClip.length + 0.25f);
                wait = instructionClip.length;
            }
        }

        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);

        // Label & ensure timer root is active
        if (timerLabel) timerLabel.text = labelText;
        if (timerRoot && !timerRoot.activeSelf) timerRoot.SetActive(true);

        // Force a clean slate, then start
        if (timer)
        {
            timer.Stop();   // resets any latches or prior state
            Debug.Log("[StepController] Timer.Begin()");
            timer.Begin();
        }

        // Start watching the gate after an optional delay
        StartCoroutine(ArmAfterDelay());
    }

    private IEnumerator ArmAfterDelay()
    {
        if (armDelay > 0f)
            yield return new WaitForSecondsRealtime(armDelay);

        watching = true;

        // If we require a rising edge, decide initial state based on current gate
        if (requireRisingEdge && fingerService)
        {
            bool gateNow = fingerService.IsCommonTagActive(requiredCommonTag);
            gateState = gateNow ? GateState.WaitingForFalse : GateState.WaitingForTrue;
        }
        else
        {
            gateState = GateState.WaitingForTrue;
        }
    }

    void Update()
    {
        if (!watching) return;
        if (!fingerService) return;

        bool gate = fingerService.IsCommonTagActive(requiredCommonTag);

        if (!requireRisingEdge)
        {
            if (gate)
            {
                watching = false;
                CompleteStep();
            }
            return;
        }

        // Rising-edge state machine
        switch (gateState)
        {
            case GateState.WaitingForFalse:
                if (!gate) gateState = GateState.WaitingForTrue;
                break;

            case GateState.WaitingForTrue:
                if (gate)
                {
                    watching = false;
                    CompleteStep();
                }
                break;

            default:
                gateState = GateState.WaitingForTrue;
                break;
        }
        
    }

    private void CompleteStep()
    {
        if (goalCanvas && !goalCanvas.activeSelf)
            goalCanvas.SetActive(true);

        if (timer) timer.Stop();
    }

    private void HideSelf()
    {
        if (ownCollider) ownCollider.enabled = false;
        var root = visualsRoot ? visualsRoot : gameObject;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        foreach (var c in root.GetComponentsInChildren<Collider>(true)) c.enabled = false;
    }

    private bool PassesFilter(Collider other)
    {
        if ((allowedLayers.value & (1 << other.gameObject.layer)) == 0) return false;

        if (allowedHandTags != null && allowedHandTags.Length > 0)
        {
            bool ok = false;
            foreach (var t in allowedHandTags)
                if (!string.IsNullOrEmpty(t) && other.CompareTag(t)) { ok = true; break; }
            if (!ok) return false;
        }

        return true;
    }
}