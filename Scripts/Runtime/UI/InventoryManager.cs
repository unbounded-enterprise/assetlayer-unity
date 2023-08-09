using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; } // Singleton instance

    private Asset selectedAsset;

    public GameObject characterToDeleteOnChange;
    public GameObject parentOfCharacter;

    [SerializeField]
    private string selectedAssetId; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: if you want the manager to persist across scene changes
        }
        else
        {
            Destroy(gameObject); // Destroy any extra instances
        }
    }

    public static Asset GetSelectedCollection()
    {
        return Instance.selectedAsset;
    }

    public void OnAssetSelected(Asset asset)
    {
        Debug.Log("ManagerAssetChange");
        selectedAsset = asset;
        selectedAssetId = asset.assetId;
        string assetBundleUrl = UtilityFunctions.GetExpressionValue(asset.expressionValues, "AssetBundle");
        StartCoroutine(DownloadAndInstantiateAssetBundle(assetBundleUrl));

    }

    private IEnumerator DownloadAndInstantiateAssetBundle(string assetBundleUrl)
    {
        using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                var prefabName = bundle.GetAllAssetNames()[0];  // Assumes there's only one object in the bundle
                var prefab = bundle.LoadAsset<GameObject>(prefabName);

                GameObject instance = Instantiate(prefab, parentOfCharacter.transform);
                // Delete the characterToDeleteOnChange
                if (characterToDeleteOnChange != null)
                {
                    Destroy(characterToDeleteOnChange);
                }

            }
        }
    }
}
