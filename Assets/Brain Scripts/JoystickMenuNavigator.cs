using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class JoystickMenuNavigator : MonoBehaviour
{
    [Header("Debug")]
    public bool debugLog = false;

    [Header("Buttons to navigate (in order, top to bottom)")]
    public List<Button> menuButtons;

    [Header("Input Actions")]
    public InputActionReference joystickAction;   // thumbstick Y axis
    public InputActionReference confirmAction;     // joystick press OR A/X button

    [Header("Locomotion to disable while menu is open")]
    public MonoBehaviour locomotionProvider; // drag your ContinuousMoveProvider here


    [Header("Visual")]
    public Color highlightColor = Color.blue;
    public Color normalColor = Color.white;

    [Header("Navigation Settings")]
    [Tooltip("How far the joystick must be pushed to navigate (0-1)")]
    public float deadzone = 0.5f;
    [Tooltip("Seconds between repeated navigation while joystick is held")]
    public float repeatDelay = 0.3f;

    private int _selectedIndex = 0;
    private float _lastMoveTime;
    private bool _joystickWasNeutral = true;

    void OnEnable()
    {
        if (locomotionProvider) locomotionProvider.enabled = false;

        if (joystickAction != null) joystickAction.action.Enable();
        if (confirmAction != null) confirmAction.action.Enable();

        if (confirmAction != null)
            confirmAction.action.performed += OnConfirm;

        // Highlight first button on show
        RefreshHighlight();
    }

    void OnDisable()
    {
        if (locomotionProvider) locomotionProvider.enabled = true;

        if (confirmAction != null)
            confirmAction.action.performed -= OnConfirm;
    }

    void Update()
    {
        if (menuButtons == null || menuButtons.Count == 0) return;
        if (joystickAction == null) return;

        float y = joystickAction.action.ReadValue<Vector2>().y;

        if (debugLog)
        {
            float debugY = joystickAction != null ? joystickAction.action.ReadValue<Vector2>().y : 0f;
            //VRDebugText.Log($"joystick y={debugY:0.00} idx={_selectedIndex}");
        }

        bool pushedUp = y > deadzone;
        bool pushedDown = y < -deadzone;
        bool isNeutral = !pushedUp && !pushedDown;

        if (isNeutral)
        {
            _joystickWasNeutral = true;
            return;
        }

        // Require neutral before re-triggering, OR enough time has passed for repeat
        if (!_joystickWasNeutral && Time.unscaledTime - _lastMoveTime < repeatDelay)
            return;

        _joystickWasNeutral = false;
        _lastMoveTime = Time.unscaledTime;

        if (pushedUp)
            _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
        else if (pushedDown)
            _selectedIndex = Mathf.Min(menuButtons.Count - 1, _selectedIndex + 1);

        RefreshHighlight();
    }

    private void OnConfirm(InputAction.CallbackContext ctx)
    {
        if (menuButtons == null || _selectedIndex >= menuButtons.Count) return;
        menuButtons[_selectedIndex]?.onClick.Invoke();
    }

    private void RefreshHighlight()
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            if (!menuButtons[i]) continue;
            var colors = menuButtons[i].colors;
            colors.normalColor = (i == _selectedIndex) ? highlightColor : normalColor;
            menuButtons[i].colors = colors;
        }
    }
}