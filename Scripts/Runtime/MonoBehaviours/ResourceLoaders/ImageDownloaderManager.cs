using PimDeWitte.UnityMainThreadDispatcher;
using System;
using UnityEngine.Networking;
using UnityEngine;
using System.Collections;

namespace AssetLayer.Unity
{

    public class ImageDownloaderManager : MonoBehaviour
    {
        public delegate void ImageDownloadedCallback(Texture2D texture, bool success = true);

        public void LoadImage(string url, ImageDownloadedCallback callback)
        {
            // Ensure that LoadImageCoroutine is invoked on the main thread
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                StartCoroutine(LoadImageCoroutine(url, callback));
            });
        }

        private IEnumerator DownloadImage(string url, ImageDownloadedCallback callback)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError ||
                    www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error downloading image from {url}. Error: {www.error}");
                    callback?.Invoke(null, false);
                }
                else
                {
                    try
                    {
                        Texture2D texture = DownloadHandlerTexture.GetContent(www);
                        callback?.Invoke(texture, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to decode image from {url}. Exception: {e}");
                        callback?.Invoke(null, false);
                    }
                }
            }
        }


        private IEnumerator LoadImageCoroutine(string url, ImageDownloadedCallback finalCallback)
        {
            if (String.IsNullOrEmpty(url) || !url.StartsWith("http"))
            {
                yield return null;
            }
            bool retry = false;

            ImageDownloadedCallback intermediateCallback = (texture, success) =>
            {
                if (!success && !retry)
                {
                    retry = true;
                    string uniqueUrl = url + "?nocache=" + DateTime.Now.Ticks.ToString();
                    StartCoroutine(DownloadImage(uniqueUrl, finalCallback));
                }
                else
                {
                    finalCallback?.Invoke(texture, success);
                }
            };

            yield return StartCoroutine(DownloadImage(url, intermediateCallback));
        }


    }
}