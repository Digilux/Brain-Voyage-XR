using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// This is the current version of the resettable object script in order to reset each brain slice.
// This uses the XR.Interaction.Toolkit.Interactables.XRGrabInteractable.
public class BackUpResettable : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;
    private Collider col;
    private PinchRotateInteractable pinchRotateInteractable;

    [SerializeField] private XRInteractionManager interactionManager;

    void Start()
    {

        // Store initial position and rotation if the parent exists
        if (transform.parent != null)
        {
            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
        }
        else
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        pinchRotateInteractable = GetComponent<PinchRotateInteractable>();


        // Make sure objects are kinematic at the start
        rb.isKinematic = true;
    }

    public void ResetObject()
    {
        var grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable && grabInteractable.isSelected)
        {
            grabInteractable.interactionLayers = 0; // disables all interaction layers
        }

        // Disable collider and physics before teleporting
        rb.isKinematic = true;
        rb.detectCollisions = false;
        col.enabled = false;

        // Disable custom interactions
        if (pinchRotateInteractable != null)
            pinchRotateInteractable.enabled = false;

        // Reset transform
        if (transform.parent != null)
        {
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;
        }
        else
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        // Move rigidbody to match new position � this helps physics register the change
        rb.position = (transform.parent != null) ? transform.parent.TransformPoint(initialPosition) : initialPosition;
        rb.rotation = (transform.parent != null) ? transform.parent.rotation * initialRotation : initialRotation;

        // Wait before reenabling physics so the object doesn't collide and fall through or sit incorrectly
        StartCoroutine(ReenablePhysics(grabInteractable));
    }

    private IEnumerator ReenablePhysics(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable)
    {
        yield return new WaitForSeconds(0.5f); // Small delay to stabilize the object

        // Re-enable physics interactions
        rb.isKinematic = true; // Keep kinematic so it's not pushable
        rb.detectCollisions = true;
        col.enabled = true;

        if (pinchRotateInteractable != null)
            pinchRotateInteractable.enabled = true;

        if (grabInteractable != null)
            grabInteractable.interactionLayers = new InteractionLayerMask { value = -1 };
    }
}