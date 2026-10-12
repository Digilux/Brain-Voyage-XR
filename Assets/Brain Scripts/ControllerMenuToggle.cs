using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerMenuToggle : MonoBehaviour
{
    public InputActionReference menuToggleAction;
    public GameObject menuRoot;
    public CanvasGroup menuCanvasGroup;

    private bool isShowing = false;

    void Start()
    {
        // Start hidden
        isShowing = false;
        SetMenuVisible(true);
    }

    void OnEnable()
    {
        if (menuToggleAction != null)
        {
            menuToggleAction.action.Enable();
            menuToggleAction.action.performed += OnToggle;
        }
    }

    void OnDisable()
    {
        if (menuToggleAction != null)
        {
            menuToggleAction.action.performed -= OnToggle;
            menuToggleAction.action.Disable();
        }
    }

    private void OnToggle(InputAction.CallbackContext ctx)
    {
        isShowing = !isShowing;
        SetMenuVisible(isShowing);
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuRoot) menuRoot.SetActive(visible);

        if (menuCanvasGroup)
        {
            menuCanvasGroup.alpha = visible ? 1f : 0f;
            menuCanvasGroup.interactable = visible;
            menuCanvasGroup.blocksRaycasts = visible;
        }
    }
}