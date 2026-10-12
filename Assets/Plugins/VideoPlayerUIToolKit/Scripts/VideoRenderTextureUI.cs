using UnityEngine;
using UnityEngine.UIElements;

namespace VideoPlayerAsset.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class VideoRenderTextureUI : MonoBehaviour
    {
        private VisualElement videoVE
        {
            get
            {
                if (_videoVE == null)
                {
                    _videoVE = uIDocument.rootVisualElement.Q<VisualElement>("VideoPlayer").Q<VisualElement>("Video");
                }
                return _videoVE;
            }
        }
        private VisualElement _videoVE;

        private UIDocument uIDocument
        {
            get
            {
                if (uIDocumentCached == null)
                    uIDocumentCached = GetComponent<UIDocument>();
                
                return uIDocumentCached;
            }
        }
        private UIDocument uIDocumentCached;

        public void SetRenderTexture(RenderTexture renderTexture)
        {
            // The render texture has to be set in this format:
            var videoPlayerVE = videoVE.style.backgroundImage.value;
            videoPlayerVE.renderTexture = renderTexture;
            videoVE.style.backgroundImage = videoPlayerVE;
        }
    }
}