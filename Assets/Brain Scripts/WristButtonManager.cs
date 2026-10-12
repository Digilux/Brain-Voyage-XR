using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class WristButtonManager : MonoBehaviour
{
    [Header("References")]
    public ResetButton resetButtonScript;
    public GoalSequenceController goalSequence;
    public GameObject brainRoot;
    public GameObject menuCanvasRoot;
    public GameObject gameplayCanvasRoot;
    public GameObject coachingCardRoot;  // drag CoachingCardRoot here
    public GameObject shawnsVoyageUIAndControllers;
    public UI_Timer sessionTimer;

    [Header("Global Move Brain")]
    [Tooltip("Drag the blue GlobalMoveBrainObject here.")]
    public GameObject globalMoveBrainObject;

    [Tooltip("Drag the new YPivotHandle child object here.")]
    public GameObject yPivotHandleObject;

    [Tooltip("Drag the Move Object Description Canvas here.")]
    public GameObject moveDescriptionCanvas;

    [Tooltip("Drag the Pivot Object Description Canvas here.")]
    public GameObject pivotDescriptionCanvas;

    [Header("Diagram Toggle (B/Y buttons)")]
    [Tooltip("Diagram Toggle action from XRI Left Interaction.")]
    public InputActionReference leftDiagramToggleAction;
    [Tooltip("Diagram Toggle action from XRI Right Interaction.")]
    public InputActionReference rightDiagramToggleAction;
    [Tooltip("Drag L_DiagramCanvas here.")]
    public GameObject leftDiagramCanvas;
    [Tooltip("Drag R_DiagramCanvas here.")]
    public GameObject rightDiagramCanvas;

    private bool moveToolOn = true;   // Move tool starts active, matching old default
    private bool pivotToolOn = false;

    private void Awake()
    {
        ApplyBrainToolState();
    }

    private void OnEnable()
    {
        if (leftDiagramToggleAction != null)
        {
            leftDiagramToggleAction.action.Enable();
            leftDiagramToggleAction.action.performed += OnLeftDiagramTogglePressed;
        }
        if (rightDiagramToggleAction != null)
        {
            rightDiagramToggleAction.action.Enable();
            rightDiagramToggleAction.action.performed += OnRightDiagramTogglePressed;
        }
    }

    private void OnDisable()
    {
        if (leftDiagramToggleAction != null)
            leftDiagramToggleAction.action.performed -= OnLeftDiagramTogglePressed;
        if (rightDiagramToggleAction != null)
            rightDiagramToggleAction.action.performed -= OnRightDiagramTogglePressed;
    }

    private void OnLeftDiagramTogglePressed(InputAction.CallbackContext ctx)
    {
        if (leftDiagramCanvas)
            leftDiagramCanvas.SetActive(!leftDiagramCanvas.activeSelf);
    }

    private void OnRightDiagramTogglePressed(InputAction.CallbackContext ctx)
    {
        if (rightDiagramCanvas)
            rightDiagramCanvas.SetActive(!rightDiagramCanvas.activeSelf);
    }

    public void OnResetButtonPressed()
    {
        if (resetButtonScript != null)
            resetButtonScript.ResetAllFromUI();
        else
            Debug.LogWarning("ResetButtonScript is not assigned in WristButtonManager!");
    }

    public void OnMenuButtonPressed()
    {
        SceneManager.LoadScene("customInteraction");
    }

    public void OnSkipOrIntroPressed()
    {
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        StepController.GlobalDetectionPaused = true;

        if (menuCanvasRoot) menuCanvasRoot.SetActive(false);
        if (brainRoot) brainRoot.SetActive(true);
        if (gameplayCanvasRoot) gameplayCanvasRoot.SetActive(true);

        if (shawnsVoyageUIAndControllers)
            shawnsVoyageUIAndControllers.SetActive(true);

        if (sessionTimer)
        {
            sessionTimer.Stop();
            sessionTimer.ResetTimer();
        }

        yield return null;
        StepController.GlobalDetectionPaused = false;

        // Show coaching card so player picks difficulty
        if (coachingCardRoot) coachingCardRoot.SetActive(true);
    }

    public void OnDifficultySelected()
    {
        if (coachingCardRoot) coachingCardRoot.SetActive(false);
        // hide difficulty buttons explicitly if needed
    }

    private IEnumerator GoToMenuRoutine()
    {
        StepController.GlobalDetectionPaused = true;

        if (gameplayCanvasRoot) gameplayCanvasRoot.SetActive(false);
        if (brainRoot) brainRoot.SetActive(false);
        if (coachingCardRoot) coachingCardRoot.SetActive(false);

        if (resetButtonScript) resetButtonScript.ResetAllObjects();
        if (goalSequence) goalSequence.ResetSequence(activateFirstStepOnly: true);

        yield return null;
        yield return null;

        StepController.GlobalDetectionPaused = false;

        if (menuCanvasRoot) menuCanvasRoot.SetActive(true);
    }

    public void OnToggleMoveTool()
    {
        if (globalMoveBrainObject == null)
        {
            Debug.LogWarning("globalMoveBrainObject is not assigned in WristButtonManager!");
            return;
        }

        moveToolOn = !moveToolOn;
        if (moveToolOn) pivotToolOn = false; // only one tool active at a time
        ApplyBrainToolState();
    }

    public void OnTogglePivotTool()
    {
        if (yPivotHandleObject == null)
        {
            Debug.LogWarning("yPivotHandleObject is not assigned in WristButtonManager!");
            return;
        }

        pivotToolOn = !pivotToolOn;
        if (pivotToolOn) moveToolOn = false; // only one tool active at a time
        ApplyBrainToolState();
    }

    private void ApplyBrainToolState()
    {
        bool moveOn = moveToolOn;
        bool pivotOn = pivotToolOn;

        // --- Full move/rotate handle ---
        var pinch = globalMoveBrainObject.GetComponent<PinchRotateInteractable>();
        if (pinch) pinch.enabled = moveOn;

        var translate = globalMoveBrainObject.GetComponent<PinchTranslateInteractable>();
        if (translate) translate.enabled = moveOn;

        var moveCol = globalMoveBrainObject.GetComponent<Collider>();
        if (moveCol) moveCol.enabled = moveOn;

        var moveRend = globalMoveBrainObject.GetComponent<MeshRenderer>();
        if (moveRend) moveRend.enabled = moveOn;

        // --- Y-only pivot handle (now contains the brain — don't SetActive the whole thing!) ---
        if (yPivotHandleObject)
        {
            var pivotScript = yPivotHandleObject.GetComponent<PivotYRotateInteractable>();
            if (pivotScript) pivotScript.enabled = pivotOn;

            var pivotCol = yPivotHandleObject.GetComponent<Collider>();
            if (pivotCol) pivotCol.enabled = pivotOn;

            var pivotRend = yPivotHandleObject.GetComponent<MeshRenderer>();
            if (pivotRend) pivotRend.enabled = pivotOn; // only the handle knob's own renderer, not children
        }
        // --- Description canvases ---
        if (moveDescriptionCanvas) moveDescriptionCanvas.SetActive(moveOn);
        if (pivotDescriptionCanvas) pivotDescriptionCanvas.SetActive(pivotOn);

        //VRDebugText.Log($"BrainTool: Move={moveToolOn} Pivot={pivotToolOn}");
    }
    public void OnQuitButtonPressed()
    {
        Application.Quit();
    }


}