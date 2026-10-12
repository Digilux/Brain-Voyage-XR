using UnityEngine;

public class DynamicPivotManager : MonoBehaviour
{
    public GameObject pivotObject; // Empty GameObject acting as the dynamic pivot
    public Transform[] pages;  // Array of pages to manage

    public float rotationSpeed = 30f; // Speed at which pivot rotates
    public float rotationX = 0f; // Rotation amount around X axis
    public float rotationY = 0f; // Rotation amount around Y axis
    public float rotationZ = 0f; // Rotation amount around Z axis

    void Start()
    {
        // Ensure pivotObject and pages are assigned in the Inspector
        if (pivotObject == null || pages == null || pages.Length == 0)
        {
            Debug.LogError("Missing pivotObject or pages array in DynamicPivotManager.");
            return;
        }

        // Initially set all pages as children of the pivotObject
        foreach (Transform page in pages)
        {
            page.SetParent(pivotObject.transform);
        }
    }

    void Update()
    {
        // Rotate the pivotObject based on user input or some other condition
        // Use numerical values set in Inspector for rotation
        pivotObject.transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
    }
}