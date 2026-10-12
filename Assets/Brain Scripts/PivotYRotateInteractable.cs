using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Grabbable pivot for the brain. Never tracks position (pivot stays fixed in place).
/// Rotation is NOT matched to the hand's absolute orientation (that causes a snap on
/// grab) — instead we track only the CHANGE in the hand's yaw since grab started, and
/// apply that delta on top of the brain's rotation at grab time. Clamped to Y-axis only,
/// checked in both Dynamic and OnBeforeRender phases since XRI can re-apply pose in
/// OnBeforeRender, which runs after Unity's own LateUpdate.
/// The brain (2025_Brain Shawns Voyage) should be parented under this object.
/// </summary>
public class PivotYRotateInteractable : XRGrabInteractable
{
    [Header("Joystick Fine Rotation")]
    [Tooltip("Thumbstick Vector2 action from XRI Left Interaction.")]
    [SerializeField] private InputActionReference leftJoystickAction;
    [Tooltip("Thumbstick Vector2 action from XRI Right Interaction.")]
    [SerializeField] private InputActionReference rightJoystickAction;
    [Tooltip("Degrees per second at full joystick deflection.")]
    [SerializeField] private float joystickRotateSpeed = 60f;
    [SerializeField] private float joystickDeadzone = 0.15f;

    private Transform grabbingTransform;
    private float startInteractorYaw;
    private Quaternion startRotation;

    protected override void Awake()
    {
        base.Awake();

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        movementType = MovementType.Instantaneous;
        trackPosition = false;   // never follow the hand's position
        trackRotation = false;   // we compute rotation manually below — no snap-to-hand
    }

    private void OnEnable()
    {
        if (leftJoystickAction != null) leftJoystickAction.action.Enable();
        if (rightJoystickAction != null) rightJoystickAction.action.Enable();
    }

    private void OnDisable()
    {
        if (leftJoystickAction != null) leftJoystickAction.action.Disable();
        if (rightJoystickAction != null) rightJoystickAction.action.Disable();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        grabbingTransform = args.interactorObject.transform;
        startRotation = transform.rotation;                 // brain's CURRENT rotation, not reset to zero
        startInteractorYaw = grabbingTransform.eulerAngles.y; // hand's yaw right now

        //VRDebugText.LogPriority("[PivotYRotate] Grab START", 2f);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        grabbingTransform = null;
        //VRDebugText.LogPriority("[PivotYRotate] Grab END", 2f);
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (!isSelected || grabbingTransform == null) return;

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic ||
            updatePhase == XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender)
        {
            float currentYaw = grabbingTransform.eulerAngles.y;
            float delta = Mathf.DeltaAngle(startInteractorYaw, currentYaw); // handles 360° wraparound safely

            transform.rotation = Quaternion.AngleAxis(delta, Vector3.up) * startRotation;
        }
    }

    private void Update()
    {
        // Don't fight pinch-grab rotation — it overwrites transform.rotation
        // absolutely every frame, so joystick input during a grab would be wasted.
        if (isSelected) return;

        float x = 0f;
        if (leftJoystickAction != null)
        {
            float lx = leftJoystickAction.action.ReadValue<Vector2>().x;
            if (Mathf.Abs(lx) > Mathf.Abs(x)) x = lx;
        }
        if (rightJoystickAction != null)
        {
            float rx = rightJoystickAction.action.ReadValue<Vector2>().x;
            if (Mathf.Abs(rx) > Mathf.Abs(x)) x = rx;
        }

        if (Mathf.Abs(x) < joystickDeadzone) return;

        transform.Rotate(Vector3.up, x * joystickRotateSpeed * Time.deltaTime, Space.World);
    }

    // Extra safety net for anything outside XRI's own update loop.
    private void LateUpdate()
    {
        Vector3 e = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, e.y, 0f);
    }
}