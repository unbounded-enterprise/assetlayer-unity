using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Net;
using System;

namespace UnityEngine.UIElements
{
    public static class ImageLoader
    {
        public static async Task<Texture2D> LoadImageUnityWebRequest(string url)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                // Add headers to accept encoding types supported by libcurl
                www.SetRequestHeader("Accept-Encoding", "gzip, deflate");

                // Send request and await response
                await www.SendWebRequest();

                // Log detailed info
                Debug.Log($"[UnityWebRequest] URL: {url}");
                Debug.Log($"[UnityWebRequest] Response Code: {www.responseCode}");
                Debug.Log($"[UnityWebRequest] Error: {www.error}");
                foreach (var header in www.GetResponseHeaders())
                {
                    Debug.Log($"{header.Key}: {header.Value}");
                }

                if (www.result == UnityWebRequest.Result.ConnectionError ||
                    www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"[UnityWebRequest] Error downloading image from {url}. Error: {www.error}");
                    return null;
                }

                try
                {
                    return DownloadHandlerTexture.GetContent(www);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UnityWebRequest] Failed to decode image from {url}. Exception: {e}");
                    return null;
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
