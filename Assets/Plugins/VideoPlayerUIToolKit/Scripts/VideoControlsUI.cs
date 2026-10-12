using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using VideoPlayerAsset.Plugin;
using VideoPlayerAsset.Video;

namespace VideoPlayerAsset.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class VideoControlsUI : MonoBehaviour
    {
        [SerializeField] private VideoController videoController;
        [SerializeField] private VideoGallery videoGallery;

        private UIDocument uIDocument
        {
            get => uIDocumentCached ??= GetComponent<UIDocument>();     // If not cached, cache it.
        }
        private UIDocument uIDocumentCached;

        private VisualElement root
        {
            get => rootCached ??= uIDocument.rootVisualElement;
        }
        private VisualElement rootCached;

        private VideoSliderManipulator videoSliderManipulator
        {
            get => videoSliderManipulatorCached ??= new VideoSliderManipulator(uIDocument, videoController);
        }
        private VideoSliderManipulator videoSliderManipulatorCached;

        private void OnEnable()
        {
            RegisterCallbacks();
        }
        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void Update()
        {
            if (videoController.VideoPlayer.isPlaying)
                videoSliderManipulator.SliderProgress();
        }

        void RegisterCallbacks()
        {
            root.Q<Button>("Button_Play").clicked += ButtonPlay;
            root.Q<Button>("Button_Pause").clicked += ButtonPause;
            root.Q<Button>("Button_Mute").clicked += ButtonMute;
            root.Q<Button>("Button_Sound").clicked += ButtonSound;
            root.Q<Button>("Button_Loop").clicked += ButtonLoop;
            root.Q<Button>("Button_NoLoop").clicked += ButtonNoLoop;
            root.Q<Button>("Button_Expand").clicked += ButtonExpandFullScreen;
            root.Q<Button>("Button_Minimize").clicked += ButtonnMinimizeFullScreen;
            root.Q<Button>("Button_Gallery").clicked += ButtonGallery;

            videoController.VideoPlayer.loopPointReached += VideoLoopPointReached;

            root.Q<VisualElement>("SliderArea").RegisterCallback<PointerDownEvent>(SliderPointerDown, TrickleDown.TrickleDown);       // Capture the pointer down event here because otherwise it is consumed by the slider.
            root.Q<Slider>("Slider_Progress").AddManipulator(videoSliderManipulator);
        }

        void UnregisterCallbacks()
        {
            root.Q<Button>("Button_Play").clicked -= ButtonPlay;
            root.Q<Button>("Button_Pause").clicked -= ButtonPause;
            root.Q<Button>("Button_Mute").clicked -= ButtonMute;
            root.Q<Button>("Button_Sound").clicked -= ButtonSound;
            root.Q<Button>("Button_Loop").clicked -= ButtonLoop;
            root.Q<Button>("Button_NoLoop").clicked -= ButtonNoLoop;
            root.Q<Button>("Button_Expand").clicked -= ButtonExpandFullScreen;
            root.Q<Button>("Button_Minimize").clicked -= ButtonnMinimizeFullScreen;
            root.Q<Button>("Button_Gallery").clicked -= ButtonGallery;

            videoController.VideoPlayer.loopPointReached -= VideoLoopPointReached;

            root.Q<VisualElement>("SliderArea").UnregisterCallback<PointerDownEvent>(SliderPointerDown, TrickleDown.TrickleDown);
            root.Q<Slider>("Slider_Progress").RemoveManipulator(videoSliderManipulator);
        }

        private void ButtonPlay()
        {
            root.Q<Button>("Button_Play").style.display = DisplayStyle.None;
            root.Q<Button>("Button_Pause").style.display = DisplayStyle.Flex;

            videoController.PlayVideo();
        }

        private void ButtonPause()
        {
            root.Q<Button>("Button_Play").style.display = DisplayStyle.Flex;
            root.Q<Button>("Button_Pause").style.display = DisplayStyle.None;

            videoController.PauseVideo();
        }

        private void ButtonMute()
        {
            root.Q<Button>("Button_Sound").style.display = DisplayStyle.Flex;
            root.Q<Button>("Button_Mute").style.display = DisplayStyle.None;

            videoController.Mute(true);
        }

        private void ButtonSound()
        {
            root.Q<Button>("Button_Mute").style.display = DisplayStyle.Flex;
            root.Q<Button>("Button_Sound").style.display = DisplayStyle.None;

            videoController.Mute(false);
        }

        private void ButtonLoop()
        {
            root.Q<Button>("Button_Loop").style.display = DisplayStyle.None;
            root.Q<Button>("Button_NoLoop").style.display = DisplayStyle.Flex;

            videoController.LoopVideo(false);
        }

        // Play video once.
        private void ButtonNoLoop()
        {
            root.Q<Button>("Button_Loop").style.display = DisplayStyle.Flex;
            root.Q<Button>("Button_NoLoop").style.display = DisplayStyle.None;

            videoController.LoopVideo(true);
        }
        private void ButtonExpandFullScreen()
        {
            root.Q<Button>("Button_Expand").style.display = DisplayStyle.None;
            root.Q<Button>("Button_Minimize").style.display = DisplayStyle.Flex;

            root.Q<VisualElement>("Video").AddToClassList("button-control-video-expand");
        }

        private void ButtonnMinimizeFullScreen()
        {
            root.Q<Button>("Button_Expand").style.display = DisplayStyle.Flex;
            root.Q<Button>("Button_Minimize").style.display = DisplayStyle.None;

            root.Q<VisualElement>("Video").RemoveFromClassList("button-control-video-expand");
        }

        private void ButtonGallery()
        {
            videoGallery.Pick();
        }

        private void VideoLoopPointReached(VideoPlayer source)
        {
            // If video is not looping, reset the video to the beginning. Change pause button to play button.
            if (!videoController.VideoPlayer.isLooping)
            {
                videoController.VideoPlayer.Pause();

                root.Q<Button>("Button_Play").style.display = DisplayStyle.Flex;
                root.Q<Button>("Button_Pause").style.display = DisplayStyle.None;
            }
        }

        private void SliderPointerDown(PointerDownEvent evt)
        {
            videoSliderManipulator.PointerDown();
        }
    }
}