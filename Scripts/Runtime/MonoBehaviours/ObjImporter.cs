using UnityEngine.Networking;
using UnityEngine;
using System.Collections;
using Assetlayer.UnitySDK;

public class ObjImporter : MonoBehaviour
{
    private SDKClass sdk;
    private string nftId;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Material defaultMaterial;

    private void Start()
    {
        sdk = new SDKClass();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        defaultMaterial = meshRenderer.material;

        nftId = "<Your NFT ID>";

        StartCoroutine(sdk.GetExpression(nftId, "Obj", ApplyObj));

    }

    private void ApplyObj(string objUrl)
    {
        StartCoroutine(DownloadAndApplyObj(objUrl));
    }

    private IEnumerator DownloadAndApplyObj(string objUrl)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(objUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                // Assuming you have the OBJImporter class somewhere in your project
                Mesh newMesh = sdk.LoadOBJMesh(www.downloadHandler.text);

                meshFilter.mesh = newMesh;

                meshRenderer.material = defaultMaterial;
            }
        }
    }
}
