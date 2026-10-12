using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit;
#if XR_HANDS_1_1_OR_NEWER
using UnityEngine.XR.Hands;
#endif

/* This script drives the pinch interaction of each of the brain slices.
 * The script pulls from the XR Interaction Toolkit, but you have to access
   the base library of the script. The script is setup to allow pinch rotate
   and offset rotations, with optional per-axis rotation locking. */

public class PinchRotateInteractable : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
{
    [Header("Rotation Axis Lock")]
    [Tooltip("Prevent rotation on the X axis (pitch). Only Y and/or Z will rotate.")]
    [SerializeField] private bool lockXRotation = false;

    [Tooltip("Prevent rotation on the Y axis (yaw). Only X and/or Z will rotate.")]
    [SerializeField] private bool lockYRotation = false;

    [Tooltip("Prevent rotation on the Z axis (roll). Only X and/or Y will rotate.")]
    [SerializeField] private bool lockZRotation = false;

    private Quaternion rotationOffset;
    private Vector3 positionOffset;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor grabbingInteractor;

    private Quaternion initialRotation;
    private Vector3 initialPosition;

    // Rotation at the moment of grab — locked axes are held at this value
    private Vector3 grabbedEuler;
    private bool offsetReady = false;

    protected override void Awake()
    {
        base.Awake();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        grabbingInteractor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
        offsetReady = false;

        if (grabbingInteractor != null)
        {
            Quaternion preservedRotation = transform.rotation;
            Vector3 preservedPosition = transform.position;

            StartCoroutine(ResetAndCaptureOffset(preservedRotation, preservedPosition));
        }
    }

    private IEnumerator ResetAndCaptureOffset(Quaternion originalRotation, Vector3 originalPosition)
    {
        yield return null; // Wait for Unity to apply its default attach transform

        // Restore our original rotation and position
        transform.rotation = originalRotation;
        transform.position = originalPosition;

        // Snapshot the euler angles on locked axes — we'll hold these throughout the grab
        grabbedEuler = originalRotation.eulerAngles;

        // Calculate the offsets
        rotationOffset = Quaternion.Inverse(grabbingInteractor.transform.rotation) * transform.rotation;
        positionOffset = Quaternion.Inverse(grabbingInteractor.transform.rotation) * (transform.position - grabbingInteractor.transform.position);

        offsetReady = true;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (!isSelected || grabbingInteractor == null || !offsetReady) return;
        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;

        // Apply full rotation and position from interactor
        Quaternion newRotation = grabbingInteractor.transform.rotation * rotationOffset;
        Vector3 newEuler = newRotation.eulerAngles;

        // Clamp any locked axes back to their value at grab time
        if (lockXRotation) newEuler.x = grabbedEuler.x;
        if (lockYRotation) newEuler.y = grabbedEuler.y;
        if (lockZRotation) newEuler.z = grabbedEuler.z;

        transform.rotation = Quaternion.Euler(newEuler);
        transform.position = grabbingInteractor.transform.position + grabbingInteractor.transform.rotation * positionOffset;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        grabbingInteractor = null;
        offsetReady = false;
    }
}