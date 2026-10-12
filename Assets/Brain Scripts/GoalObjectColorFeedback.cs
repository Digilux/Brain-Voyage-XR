using System.Collections;
using UnityEngine;

public class GoalObjectColorFeedback : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color correctColor = Color.blue;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private float displayDuration = 3f;

    public bool IsShowingFeedback { get; private set; }

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool[] originalEnabledStates;   // NEW
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        originalEnabledStates = new bool[renderers.Length];   // NEW

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
            originalEnabledStates[i] = renderers[i].enabled;  // NEW: snapshot enabled state
        }
    }

    public void ShowCorrect() => ShowFeedback(correctColor);
    public void ShowIncorrect() => ShowFeedback(incorrectColor);

    private void ShowFeedback(Color color)
    {
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(FeedbackRoutine(color));
    }

    private IEnumerator FeedbackRoutine(Color color)
    {
        IsShowingFeedback = true;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = true;                // force visible
            renderers[i].material.color = color;
        }

        yield return new WaitForSecondsRealtime(displayDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
            renderers[i].enabled = originalEnabledStates[i];  // restore original state
        }

        IsShowingFeedback = false;
        feedbackRoutine = null;
    }

    public void ResetColor()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        IsShowingFeedback = false;

        // ADD null check before iterating
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i])
            {
                renderers[i].material.color = originalColors[i];
                renderers[i].enabled = originalEnabledStates[i];
            }
        }
    }

    
}