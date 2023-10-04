using UnityEngine.Networking;
using UnityEngine;
using System.Collections;

namespace AssetLayer.Unity
{

    public class ChangeMaterial : MonoBehaviour
    {
        public string assetId;
        public Material defaultMat;
        public string shader = "OFGames/Self Illumin Diffuse";
        private ApiManager manager;
        private Renderer renderer;

        void Start()
        {
            manager = new ApiManager();
            Debug.Log(manager);
            renderer = GetComponent<Renderer>();

            if (string.IsNullOrEmpty(assetId))
            {
                renderer.material = defaultMat;
            }
            else
            {
                _ = StartCoroutine(manager.GetExpression(assetId, "Menu View", (materialPath) =>
                {
                    Debug.Log("Request Done");
                    StartCoroutine(LoadMaterial(materialPath));
                }));
            }

        }

        IEnumerator LoadMaterial(string materialPath)
        {
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                wc.Headers[System.Net.HttpRequestHeader.AcceptEncoding] = "gzip";
                wc.DownloadDataCompleted += (s, e) =>
                {
                    byte[] imageBytes = e.Result;

                    Texture2D myTexture = new Texture2D(2, 2);
                    if (myTexture.LoadImage(imageBytes))
                    {
                        Material newMat = new Material(Shader.Find(shader));
                        newMat.mainTexture = myTexture;
                        renderer.material = newMat;
                    }
                    else
                    {
                        Debug.LogError("Failed to load image from bytes");
                    }
                };

                wc.DownloadDataAsync(new System.Uri(materialPath));
            }

            yield return null;
        }
    }
}