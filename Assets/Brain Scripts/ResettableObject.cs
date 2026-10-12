using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// This was the older resettable object script. Don't use this anymore.
public class ResettableObject : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;
    private Collider col;
    private PinchRotateInteractable pinchRotateInteractable;

    void Start()
    {
        //// Store initial position and rotation based on whether the object has a parent or not
        //if (transform.parent != null)
        //{
        //    initialPosition = transform.localPosition;
        //    initialRotation = transform.localRotation;
        //}
        //else
        //{
        //    initialPosition = transform.position;
        //    initialRotation = transform.rotation;
        //}

        //rb = GetComponent<Rigidbody>();
        //col = GetComponent<Collider>();
        //pinchRotateInteractable = GetComponent<PinchRotateInteractable>();


        // Store initial position and rotation
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

        // Make sure objects are NOT kinematic at the start
        rb.isKinematic = false;
  
    }

    public void ResetObject()
    {
        // ALSO CAUSES RESET OBJECT TO NOT WORK!
        if (rb == null || pinchRotateInteractable == null || col == null) return;

        // Temporarily disable interactions. CAUSES RESET OBJECT TO NOT WORK!
        //pinchRotateInteractable.enabled = false;

        // Make object kinematic to prevent unintended movement
        rb.isKinematic = true;

        // Disable collider to prevent accidental collisions
        col.enabled = false;
        rb.detectCollisions = false;

        // Stop all physics interactions
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();

        // Reset position and rotation
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

        // Ensure physics recognizes the new position
        rb.MovePosition(initialPosition);
        rb.MoveRotation(initialRotation);

        // Re-enable collider and reset kinematic after a short delay
        StartCoroutine(ReenablePhysics());
    }

    private IEnumerator ReenablePhysics()
    {
        yield return new WaitForSeconds(1f); // Small delay for stability

        // Re-enable physics interactions
        rb.isKinematic = false; // Allow movement again
        rb.detectCollisions = true;
        col.enabled = true;

        // Re-enable interaction
        pinchRotateInteractable.enabled = true;
    }
}

//  private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

// grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();



