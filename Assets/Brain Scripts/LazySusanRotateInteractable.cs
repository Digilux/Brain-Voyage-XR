using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Lazy-susan style rotation. Grab this object (the pole mesh) and swing your hand around
/// the pivot to spin pivotTarget around a fixed world axis (Y by default). No translation,
/// no free rotation — only the swept angle around the pivot matters.
/// </summary>
public class LazySusanRotateInteractable : XRGrabInteractable
{
    [Header("Pivot")]
    [Tooltip("The transform that actually rotates. Leave empty to rotate this object itself.")]
    [SerializeField] private Transform pivotTarget;

    [Tooltip("World-space axis to rotate around. Usually Vector3.up for a lazy susan.")]
    [SerializeField] private Vector3 worldRotationAxis = Vector3.up;

    [Header("Joystick Fine Rotation")]
    [Tooltip("Thumbstick Vector2 action from XRI Left Interaction.")]
    [SerializeField] private InputActionReference leftJoystickAction;
    [Tooltip("Thumbstick Vector2 action from XRI Right Interaction.")]
    [SerializeField] private InputActionReference rightJoystickAction;
    [Tooltip("Degrees per second at full joystick deflection.")]
    [SerializeField] private float joystickRotateSpeed = 60f;
    [SerializeField] private float joystickDeadzone = 0.15f;

    private XRBaseInteractor grabbingInteractor;
    private Transform attachPoint;
    private float lastAngle;

    protected override void Awake()
    {
        base.Awake();

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // We drive rotation manually — don't let XRIT move/rotate this object itself
        movementType = MovementType.Instantaneous;
        trackRotation = false;
        trackPosition = false;

        if (pivotTarget == null)
            pivotTarget = transform;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (leftJoystickAction != null) leftJoystickAction.action.Enable();
        if (rightJoystickAction != null) rightJoystickAction.action.Enable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (leftJoystickAction != null) leftJoystickAction.action.Disable();
        if (rightJoystickAction != null) rightJoystickAction.action.Disable();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        grabbingInteractor = args.interactorObject as XRBaseInteractor;

        if (grabbingInteractor != null)
            attachPoint = grabbingInteractor.GetAttachTransform(this);

        VRDebugText.Log($"LazySusan grab: interactorType={args.interactorObject.GetType().Name}, castOK={grabbingInteractor != null}, attachOK={attachPoint != null}");

        if (attachPoint != null && pivotTarget != null)
            lastAngle = GetSignedAngle(attachPoint.position);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        grabbingInteractor = null;
        attachPoint = null;
    }

    private void Update()
    {
        // Don't fight the lazy-susan grab rotation, which already drives pivotTarget every frame.
        if (isSelected || pivotTarget == null) return;

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

        pivotTarget.Rotate(worldRotationAxis, x * joystickRotateSpeed * Time.deltaTime, Space.World);
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (!isSelected || grabbingInteractor == null || attachPoint == null || pivotTarget == null) return;
        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;

        float currentAngle = GetSignedAngle(attachPoint.position);
        float frameDelta = Mathf.DeltaAngle(lastAngle, currentAngle);

        Vector3 toPoint = Vector3.ProjectOnPlane(attachPoint.position - pivotTarget.position, worldRotationAxis.normalized);
        VRDebugText.Log($"angle={currentAngle:0.00} delta={frameDelta:0.00} radius={toPoint.magnitude:0.000} pos={attachPoint.position}");

        lastAngle = currentAngle;

        pivotTarget.Rotate(worldRotationAxis, frameDelta, Space.World);
    }

    private float GetSignedAngle(Vector3 worldPos)
    {
        Vector3 axis = worldRotationAxis.normalized;
        Vector3 toPoint = Vector3.ProjectOnPlane(worldPos - pivotTarget.position, axis);
        Vector3 reference = Vector3.ProjectOnPlane(Vector3.forward, axis); // fixed world reference

        return Vector3.SignedAngle(reference, toPoint, axis);
    }
}