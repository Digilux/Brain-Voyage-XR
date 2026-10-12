using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SequenceClip
{
    public Sprite[] frames;
    public AudioClip audio;
}

public class GoalSequencePlayer : MonoBehaviour
{
    [Header("Sequences")]
    [SerializeField] private SequenceClip[] sequences;
    [SerializeField] private Image displayImage;
    private const float FrameRate = 24f;

    private bool hasPlayed = false;
    private Coroutine playRoutine;

    public void Play(AudioSource source, Action onComplete = null)
    {
        if (hasPlayed) return;
        if (sequences == null || sequences.Length == 0) return;
        hasPlayed = true;
        int index = UnityEngine.Random.Range(0, sequences.Length);
        Debug.Log($"GoalSequencePlayer: chose sequence {index}");
        SequenceClip chosen = sequences[index];
        playRoutine = StartCoroutine(RunSequence(chosen.frames, chosen.audio, source, onComplete));
    }

    private IEnumerator RunSequence(Sprite[] frames, AudioClip clip, AudioSource source, Action onComplete)
    {
        if (displayImage && frames.Length > 0)
            displayImage.sprite = frames[0];

        if (source && clip)
            source.PlayOneShot(clip);

        float clipLength = clip ? clip.length : 0f;
        float elapsed = 0f;

        while (elapsed < clipLength)
        {
            elapsed += Time.unscaledDeltaTime;

            if (displayImage && frames.Length > 0)
            {
                int frame = Mathf.Clamp(
                    Mathf.FloorToInt(elapsed * FrameRate),
                    0, frames.Length - 1);
                displayImage.sprite = frames[frame];
            }
            yield return null;
        }

        if (displayImage && frames.Length > 0)
            displayImage.sprite = frames[frames.Length - 1];

        onComplete?.Invoke();
    }

    public void ResetPlayer()
    {
        if (playRoutine != null) { StopCoroutine(playRoutine); playRoutine = null; }
        hasPlayed = false;
        if (displayImage) displayImage.sprite = null;
        // No SetActive � StepController handles canvas lifecycle
    }
}
