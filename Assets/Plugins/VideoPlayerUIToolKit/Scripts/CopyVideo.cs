using System.IO;
using UnityEngine;

namespace VideoPlayerAsset
{
    public class CopyVideo
    {
        string videosDirectory = Application.persistentDataPath + "/Videos/";

        public string SaveToApp(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
            {
                //Debug.LogError("No video selected.");
                return null;
            }

            // Check if directory exists, if not, create it
            if (!Directory.Exists(videosDirectory))
                Directory.CreateDirectory(videosDirectory);

            // Construct the new _imagePath
            string fileName = Path.GetFileName(originalPath);
            string newPath = Path.Combine(videosDirectory, fileName);

            // Copy the video to new location
            if (File.Exists(newPath))
                File.Delete(newPath);
            File.Copy(originalPath, newPath);

            Debug.Log($"Video copied to: {newPath}");
            return newPath;
        }
    }
}
