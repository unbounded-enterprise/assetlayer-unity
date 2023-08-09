using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Net;

namespace UnityEngine.UIElements
{
    public static class ImageLoader
    {
        public static async Task<Texture2D> LoadImageUnityWebRequest(string url) // not working for now decode errors
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                // Send request and await for response
                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Delay(100);
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(www.error);
                    return null;
                }
                else
                {
                    // Get downloaded texture once it's ready
                    var texture = DownloadHandlerTexture.GetContent(www);
                    return texture;
                }
            }
        }
        public static Texture2D LoadImage(string url) // has problems with plattform dependency, replace later
        {
            using (WebClient client = new WebClient())
            {
                byte[] imageBytes = client.DownloadData(url);

                // Create a texture
                Texture2D texture = new Texture2D(2, 2);
                // Load the image bytes into the texture
                if (texture.LoadImage(imageBytes))
                {
                    return texture;
                }
                else
                {
                    Debug.LogError("Failed to load image from " + url);
                    return null;
                }
            }
        }
    }
}
