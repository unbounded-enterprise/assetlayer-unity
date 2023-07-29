using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class NftViewer : MonoBehaviour
{
    private const string MenuViewAttributeName = "Menu View";

    // Set these in inspector or in code
    public string SlotId;
    public string Handle;

    private Material PictureMaterial;

    private void Start()
    {
        PictureMaterial = new Material(Shader.Find("Standard")); // Finds and applies the standard shader
        StartCoroutine(FetchAndDisplayNft());
    }

    private IEnumerator FetchAndDisplayNft()
    {
        SDKClass sdk = new SDKClass();
        Task<List<Nft>> task = sdk.GetNftBalance(SlotId, Handle, false, false);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result != null && task.Result.Count > 0)
        {
            // Filter out NFTs that do not have a 'Menu View' expression value
            List<Nft> nfts = task.Result.Where(nft =>
            {
                string expressionValue = GetExpressionValue(nft, MenuViewAttributeName);
                return expressionValue != null && (expressionValue.EndsWith(".png") || expressionValue.EndsWith(".jpg"));
            }).ToList();


            if (nfts.Count > 0)
            {
                Nft firstNft = nfts[0];
                string imageUrl = GetExpressionValue(firstNft, MenuViewAttributeName);
                StartCoroutine(DownloadAndDisplayImage(imageUrl));
            }
            else
            {
                Debug.LogError("No NFTs found with a 'Menu View' expression value");
            }
        }
        else
        {
            Debug.LogError("No NFTs found");
        }
    }

    private string GetExpressionValue(Nft nft, string attributeName)
    {
        foreach (ExpressionValue expressionValue in nft.expressionValues)
        {
            if (expressionValue.expression.expressionName == attributeName)
            {
                Debug.Log("value: " + expressionValue.value);
                return expressionValue.value;
            }
        }

        return null;
    }
private IEnumerator DownloadAndDisplayImage(string imageUrl)
{
    using (System.Net.WebClient client = new System.Net.WebClient())
    {
        client.DownloadDataCompleted += (sender, e) =>
        {
            if (e.Error != null)
            {
                Debug.LogError("Failed to download image. Error: " + e.Error);
                return;
            }

            byte[] data = e.Result;
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(data); //this will auto-resize the texture dimensions.

            PictureMaterial.mainTexture = texture;

            //Create thin cube and position it such that its bottom aligns with the top of the original cube
            GameObject pictureObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            float scaledHeight = texture.height / 300.0f; // Made the cube 3x smaller
            pictureObject.transform.position = transform.position + new Vector3(transform.localScale.x / 2, transform.localScale.y + scaledHeight, transform.localScale.z);
            pictureObject.transform.localScale = new Vector3(texture.width / 300.0f, scaledHeight, 0.01f);
            pictureObject.GetComponent<MeshRenderer>().material = PictureMaterial;

            //Position the camera
            Camera.main.transform.position = pictureObject.transform.position - new Vector3(0, 0, 7); // Moved the camera a bit further back
            Camera.main.transform.LookAt(pictureObject.transform.position);

            // Dim the main camera lighting
            RenderSettings.ambientIntensity = 0.2f; // Reduced ambient light to 20% (dimmed by 80%)

            // Create 4 spotlight sources
            for (int i = 0; i < 4; i++)
            {
                GameObject lightGameObject = new GameObject("NftLight" + i);
                Light lightComp = lightGameObject.AddComponent<Light>();
                lightComp.type = LightType.Spot; // Set light type to Spot
                lightComp.color = Color.white;
                lightComp.intensity = 0.5f; // Adjust intensity as per your need
                lightComp.spotAngle = 45.0f; // Adjust this as per your need
                lightComp.range = 10.0f; // Adjust this as per your need
                lightGameObject.transform.position = pictureObject.transform.position + new Vector3((i - 1.5f) * 2, 0, -2); // Adjust position as per your need
                lightGameObject.transform.LookAt(pictureObject.transform.position); // Orient light to shine at NFT
            }
        };
        client.DownloadDataAsync(new System.Uri(imageUrl));
    }
    yield return null;
}








}
