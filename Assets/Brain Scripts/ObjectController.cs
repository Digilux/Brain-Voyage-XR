using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 100.0f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 10.0f;
    public float minZoomFOV = 20.0f;
    public float maxZoomFOV = 60.0f;

    [Header("Mouse Look Settings")]
    public float mouseLookSpeed = 2.0f;
    public bool invertY = false;
    public bool invertX = false;

    [Header("Input Actions")]
    public InputAction rotateInput;     // expects Vector2
    public InputAction zoomInput;       // expects float (scroll)
    public InputAction mouseLookInput;  // expects Vector2

    private Transform objectTransform;
    private Camera mainCamera;

    private void OnEnable()
    {
        rotateInput.Enable();
        zoomInput.Enable();
        mouseLookInput.Enable();
    }

    private void OnDisable()
    {
        rotateInput.Disable();
        zoomInput.Disable();
        mouseLookInput.Disable();
    }

    private void Awake()
    {
        objectTransform = transform;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleRotation();
        HandleZoom();
        HandleMouseLook();
    }

    private void HandleRotation()
    {
        Vector2 input = rotateInput.ReadValue<Vector2>();
        float yaw = input.x * rotationSpeed * Time.deltaTime;
        float pitch = -input.y * rotationSpeed * Time.deltaTime;

        objectTransform.Rotate(Vector3.up, yaw, Space.World);
        objectTransform.Rotate(Vector3.right, pitch, Space.Self);
    }

    private void HandleZoom()
    {
        float scroll = zoomInput.ReadValue<float>();
        float newFOV = mainCamera.fieldOfView - scroll * zoomSpeed;
        mainCamera.fieldOfView = Mathf.Clamp(newFOV, minZoomFOV, maxZoomFOV);
    }

    private void HandleMouseLook()
    {
        Vector2 delta = mouseLookInput.ReadValue<Vector2>();
        float mouseX = delta.x * mouseLookSpeed * (invertX ? -1 : 1);
        float mouseY = delta.y * mouseLookSpeed * (invertY ? 1 : -1);

        objectTransform.Rotate(Vector3.up, mouseX, Space.World);
        objectTransform.Rotate(Vector3.right, mouseY, Space.Self);
    }
}