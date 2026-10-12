using UnityEngine;
using VideoPlayerAsset.Video;
//using System.IO;

// This class uses the open source plugin Native Gallery for Unity.
// https://github.com/yasirkula/UnityNativeGallery

namespace VideoPlayerAsset.Plugin
{
    // Require VideoController
    [RequireComponent(typeof(VideoController))]
    public class VideoGallery : MonoBehaviour
    {
        VideoController videoController
        {
            get
            {
                if(videoControllerCached == null)
                    videoControllerCached = GetComponent<VideoController>();
                return videoControllerCached;
            }
        }
        VideoController videoControllerCached;

        public void Pick()
        {
            NativeGallery.Permission permission = NativeGallery.GetVideoFromGallery((_videoPath) =>
            {
                if (_videoPath != null)
                {
                    Debug.Log("Video path: " + _videoPath);
                    videoController.PrepareVideo(_videoPath);

                    // Optionally save the video to the app.
                    // Comment out videoController.PrepareVideo(_videoPath);.
                    // Uncomment the code in thie class.
                    //var newPath = SaveToApp(_videoPath);
                    //if (newPath != null)
                    //    videoController.PrepareVideo(newPath);
                }
                else
                {
                    Debug.Log("Cancelled picking.");

                }
            }, "Select a video", "video/*");
        }

        // Optional video save to app.
        //private string videosDirectory = Application.persistentDataPath + "/Videos/";             // App folder to save videos to.
        //private string SaveToApp(string originalPath)
        //{
        //    if (string.IsNullOrEmpty(originalPath))
        //    {
        //        //Debug.LogError("No video selected.");
        //        return null;
        //    }

        //    // Check if directory exists, if not, create it
        //    if (!Directory.Exists(videosDirectory))
        //        Directory.CreateDirectory(videosDirectory);

        //    // Construct the new _imagePath
        //    string fileName = Path.GetFileName(originalPath);
        //    string newPath = Path.Combine(videosDirectory, fileName);

        //    // Copy the video to new location
        //    if (File.Exists(newPath))
        //        File.Delete(newPath);
        //    File.Copy(originalPath, newPath);

        //    Debug.Log($"Video copied to: {newPath}");
        //    return newPath;
        //}
    }
}