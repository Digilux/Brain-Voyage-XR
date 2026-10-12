using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Translates and Y-rotates this GameObject when grabbed/pinched via XR Interaction Toolkit.
/// Uses a one-frame delay after grab (same pattern as PinchRotateInteractable) to capture
/// the true offset AFTER XRIT has applied its internal attach snap — so the object never
/// jumps to the interactor's center point.
///
/// Inspector fix required: Uncheck "Track Rotation" on the XRGrabInteractable base.
///
/// Setup:
///   1. Attach to GlobalMoveObject.
///   2. Ensure a Rigidbody exists with Is Kinematic = true (handled in Awake).
///   3. In the Inspector, verify Track Rotation is UNCHECKED.
/// </summary>
public class PinchTranslateInteractable : XRGrabInteractable
{
    [Header("Translation")]
    [Tooltip("1 = instant follow. Lower = smoother but laggy.")]
    [SerializeField, Range(0.01f, 1f)] private float followSharpness = 1f;

    [Tooltip("Lock vertical (Y) movement — object slides on a horizontal plane only.")]
    [SerializeField] private bool lockYAxis = false;

    [Tooltip("Max distance from grab-start position. 0 = unlimited.")]
    [SerializeField] private float maxDriftDistance = 0f;

    [Header("Y-Axis Rotation")]
    [Tooltip("Mirror the interactor's yaw so twisting your wrist rotates the object.")]
    [SerializeField] private bool enableYRotation = true;

    [Tooltip("1 = instant rotation. Lower = smoother/damped.")]
    [SerializeField, Range(0.01f, 1f)] private float rotationSharpness = 1f;

    // ------------------------------------------------------------------ //
    //  Private state
    // ------------------------------------------------------------------ //
    private XRBaseInteractor _grabbingInteractor;

    // Set to true only AFTER the one-frame delay; guards ProcessInteractable
    private bool _offsetReady = false;

    private Vector3 _positionOffset;   // world-space offset captured post-snap
    private Vector3 _originPosition;   // object world position at grab start (for drift clamp)
    private float _lockedY;          // held Y when lockYAxis is true
    private float _rotationOffset;   // yaw delta (object yaw - interactor yaw) at grab time

    // ------------------------------------------------------------------ //
    //  XRGrabInteractable overrides
    // ------------------------------------------------------------------ //
    protected override void Awake()
    {
        base.Awake();

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // We drive position/rotation ourselves every frame
        movementType = MovementType.Instantaneous;
        trackRotation = false;  // also set this in the Inspector to be safe
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        _grabbingInteractor = args.interactorObject as XRBaseInteractor;
        _offsetReady = false;

        if (_grabbingInteractor != null)
            StartCoroutine(CaptureOffsetNextFrame());
    }

    /// <summary>
    /// Wait one frame so XRIT finishes its internal attach/snap logic, then
    /// restore the object to where it physically was and record the true offsets.
    /// Mirrors the pattern used in PinchRotateInteractable.
    /// </summary>
    private IEnumerator CaptureOffsetNextFrame()
    {
        // Remember where the object actually was before XRIT moved it
        Vector3 truePosition = transform.position;
        Quaternion trueRotation = transform.rotation;

        yield return null;  // let XRIT apply its snap

        // Snap back to the real position so there's no visible jump
        transform.position = truePosition;
        transform.rotation = trueRotation;

        // Now capture offsets relative to the (possibly moved) interactor
        _positionOffset = truePosition - _grabbingInteractor.transform.position;
        _originPosition = truePosition;
        _lockedY = truePosition.y;

        float interactorYaw = _grabbingInteractor.transform.eulerAngles.y;
        float objectYaw = trueRotation.eulerAngles.y;
        _rotationOffset = objectYaw - interactorYaw;

        _offsetReady = true;
        VRDebugText.Log("[PinchTranslate] Offsets captured — grab active");
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        _grabbingInteractor = null;
        _offsetReady = false;

        VRDebugText.Log("[PinchTranslate] Grab END");
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        // Don't drive the transform until offsets are ready (avoids the snap frame)
        if (!isSelected || _grabbingInteractor == null || !_offsetReady) return;
        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;

        // ---- Position ------------------------------------------------- //
        Vector3 targetPos = _grabbingInteractor.transform.position + _positionOffset;

        if (lockYAxis)
            targetPos.y = _lockedY;

        if (maxDriftDistance > 0f)
        {
            Vector3 delta = targetPos - _originPosition;
            if (delta.magnitude > maxDriftDistance)
                targetPos = _originPosition + delta.normalized * maxDriftDistance;
        }

        transform.position = followSharpness >= 1f
            ? targetPos
            : Vector3.Lerp(transform.position, targetPos, followSharpness);

        // ---- Y-Axis Rotation ------------------------------------------ //
        if (enableYRotation)
        {
            float targetYaw = _grabbingInteractor.transform.eulerAngles.y + _rotationOffset;
            Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);

            transform.rotation = rotationSharpness >= 1f
                ? targetRot
                : Quaternion.Slerp(transform.rotation, targetRot, rotationSharpness);
        }
        // If Y-rotation is disabled we leave rotation exactly as it was at grab time
    }
}