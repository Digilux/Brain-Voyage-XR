using UnityEngine.UIElements;
using VideoPlayerAsset.Video;

namespace VideoPlayerAsset.UI
{
    public class VideoSliderManipulator : Manipulator
    {
        private bool pointerDown;
        private bool scrubbed;

        private VisualElement root
        {
            get
            {
                if (root_ == null)
                    root_ = uIDocument.rootVisualElement.Q<VisualElement>("VideoPlayer");
                return root_;
            }
        }
        private VisualElement root_;

        private UIDocument uIDocument;
        private VideoController videoController;

        public VideoSliderManipulator(UIDocument uIDocument, VideoController videoController)
        {
            this.uIDocument = uIDocument;
            this.videoController = videoController;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.Q("unity-drag-container").RegisterCallback<PointerUpEvent>(PointerUp);       // PointerUp is consumed by slider so it has to be registered here.
            target.RegisterCallback<ChangeEvent<float>>(SliderValueChanged);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.Q("unity-drag-container").UnregisterCallback<PointerUpEvent>(PointerUp);
            target.UnregisterCallback<ChangeEvent<float>>(SliderValueChanged);
        }

        // Called from parent class because Slider is consuming the event and not allowing it.
        public void PointerDown()
        {
            scrubbed = true;
            pointerDown = true;
        }

        private async void PointerUp(PointerUpEvent evt)
        {
            //Debug.Log("Pointer Up");
            scrubbed = false;
            await System.Threading.Tasks.Task.Delay(150);                   // Delay for smooth slider positioning.
            pointerDown = false;
        }

        // Scrub video.
        private void SliderValueChanged(ChangeEvent<float> evt)
        {
            if (scrubbed)
            {
                var scrubToTime = (double)evt.newValue * videoController.VideoPlayer.length;
                videoController.Scrub(scrubToTime);
            }
        }

        public void SliderProgress()
        {
            if (pointerDown)
                return;

            var time = videoController.VideoPlayer.time;
            root.Q<Slider>("Slider_Progress").value = (float)time / (float)videoController.VideoPlayer.length;
        }
    }
}