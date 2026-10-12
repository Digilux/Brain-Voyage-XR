using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using TMPro;
using LazyFollow = UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow;
using UnityEngine.Video;

/* This is the core script driving the menu interactions as well as
   the events within the scene. 
*/

    public class EventSystem : MonoBehaviour
    {
        [SerializeField]
        public GameObject creditsCanvas;
        [SerializeField] 
        private GameObject mainMenuRoot;

        [Header("Video-End Game Start (mirrors Skip button)")]
        [SerializeField]
        private WristButtonManager wristButtonManager;

        [SerializeField]
        private GameObject uiAndAudioAndGoalStepControllers;

        [SerializeField]
        UnityEngine.XR.Templates.MR.FadeMaterial m_FadeMaterial;

        [SerializeField]
        Toggle m_PassthroughToggle;

        [SerializeField]
        public GameObject m_IntroButton;

        [SerializeField]
        public GameObject IntroCard;

        [SerializeField]
        public GameObject m_SkipButton;

        [SerializeField]
        public GameObject SkipCard;

        [SerializeField]
        public GameObject blueIntroButton;

        [SerializeField]
        public GameObject blueSkipButton;

        [SerializeField]
        private GameObject videoGameObject;

        [SerializeField]
        private VideoPlayer videoPlayer;

        [SerializeField]
        public GameObject Brain;

        public LazyFollow lazyFollow;

        // Brain was too close to the user after intro animation so I made a dialogue box to offset the instantiation so it can be further or closer to the user
        public Vector3 BrainOffset = new Vector3(0f, 0f, 0.5f); // Default Brain offset (adjust in Inspector)

        // Same for the trigger as well as the brain offset
        //public Vector3 ResetTriggerOffset = new Vector3(0f, -1f, 0.5f); // Default Reset Trigger offset (adjust in Inspector)

        [SerializeField]
        public GameObject blueChallengeModeButton;

        [SerializeField]
        public GameObject blueCreditsButton;


        // Start is called before the first frame update
        void Start()
        {
            // Starts with video player off
            m_IntroButton.SetActive(false);

            // Starts without Brain
            Brain.SetActive(false);
            //ResetBrainTrigger.SetActive(false);

            // Setup for mixed reality mode ensuring no material is in the environment
            if (m_FadeMaterial != null)
            {
                m_FadeMaterial.FadeSkybox(true);

                if (m_PassthroughToggle != null)
                    m_PassthroughToggle.isOn = false;
            }
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached += OnVideoEnd;
            }
        }

        // Button action to assign in the inspector
        public void ForceCompleteIntro()
        {
            CompleteIntro();
        }

        // Button action to assign in the inspector
        public void ForceCompleteSkip()
        {
            CompleteSkip();
        }
        // This action is for when the user hits the "Intro" button
        void CompleteIntro()
        {
            m_IntroButton.SetActive(true);
            IntroCard.SetActive(false);

            blueIntroButton.SetActive(false);
            blueSkipButton.SetActive(false);

            blueChallengeModeButton.SetActive(false);
            blueCreditsButton.SetActive(false);
        }

        // When user hit the "Skip" button it instantiates the brain automatically
        void CompleteSkip()
        {
            m_SkipButton.SetActive(false);
            SkipCard.SetActive(false);

            blueIntroButton.SetActive(false);
            blueSkipButton.SetActive(false);

            blueChallengeModeButton.SetActive(false);
            blueCreditsButton.SetActive(false);

            if (lazyFollow != null && Brain != null)
            {
                // Offset Brain in the player's gaze direction
                Vector3 newPosition = lazyFollow.transform.position + (lazyFollow.transform.forward * BrainOffset.z) +
                                      (lazyFollow.transform.right * BrainOffset.x) +
                                      (Vector3.up * BrainOffset.y);

                // Offset Brain Trigger in the player's gaze direction
                // Dialogue box to offset the trigger as well
                //Vector3 newTriggerPosition = lazyFollow.transform.position + (lazyFollow.transform.forward * ResetTriggerOffset.z) +
                //                             (lazyFollow.transform.right * ResetTriggerOffset.x) +
                //                             (Vector3.up * ResetTriggerOffset.y);

                Brain.transform.position = newPosition;
                //ResetBrainTrigger.transform.position = newTriggerPosition;

                // Activate the object if it is not already active
                if (!Brain.activeSelf)
                {
                    Brain.SetActive(true);
                    Debug.Log($"Activated object at position: {newPosition}");


                }
            }
        }

        // For when the video stops playing it then instantiates the brain object
        private void OnVideoEnd(VideoPlayer vp)
        {
                     
            VRDebugText.Log("Video finished playing.");
            if (videoGameObject != null)
            {
                videoGameObject.SetActive(false);

                // Mirror exactly what the Skip button does, so the
                // difficulty picker (coachingCardRoot) shows up instead
                // of jumping straight into gameplay.
                if (wristButtonManager != null)
                    wristButtonManager.OnSkipOrIntroPressed();

                if (uiAndAudioAndGoalStepControllers != null)
                    uiAndAudioAndGoalStepControllers.SetActive(true);
            }
        
        }

    public void ResetCoaching()
    {
        // Reset visibility of core UI buttons
        m_IntroButton.SetActive(false);
        m_SkipButton.SetActive(true);
        IntroCard.SetActive(true);
        SkipCard.SetActive(true);

        // Reset auxiliary buttons to visible
        blueIntroButton.SetActive(true);
        blueSkipButton.SetActive(true);
        blueChallengeModeButton.SetActive(true);
        blueCreditsButton.SetActive(true);

        // Reset the Brain object
        if (Brain != null)
        {
            Brain.SetActive(false); // Hide it first
            Brain.transform.position = Vector3.zero; // Reset position or optionally move it offscreen
        }

        // Reset AR material and passthrough settings
        if (m_FadeMaterial != null)
        {
            m_FadeMaterial.FadeSkybox(true);
        }

        if (m_PassthroughToggle != null)
        {
            m_PassthroughToggle.isOn = false;
        }

        //Debug.Log("Coaching UI has been reset.");

    }

    // Button action to assign in the inspector on blueCreditsButton
    public void OnCreditsButtonPressed()
    {
        mainMenuRoot.SetActive(false);
        creditsCanvas.SetActive(true);
    }

    public void OnCreditsToMenuPressed()
    {
        creditsCanvas.SetActive(false);
        mainMenuRoot.SetActive(true);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
