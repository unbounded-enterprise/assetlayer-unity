using PimDeWitte.UnityMainThreadDispatcher;
using System;
using UnityEngine.Networking;
using UnityEngine;
using System.Collections;

public class ImageDownloaderManager : MonoBehaviour
{
    public delegate void ImageDownloadedCallback(Texture2D texture);

    public void LoadImage(string url, ImageDownloadedCallback callback)
    {
        // Ensure that LoadImageCoroutine is invoked on the main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            StartCoroutine(LoadImageCoroutine(url, callback));
        });
    }

    private IEnumerator LoadImageCoroutine(string url, ImageDownloadedCallback callback)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            // Add headers to accept encoding types supported by libcurl
            www.SetRequestHeader("Accept-Encoding", "gzip, deflate");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[UnityWebRequest] Error downloading image from {url}. Error: {www.error}");
                callback?.Invoke(null);
            }
            else
            {
                try
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    callback?.Invoke(texture);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UnityWebRequest] Failed to decode image from {url}. Exception: {e}");
                    callback?.Invoke(null);
                }
            }
        }
    }
}