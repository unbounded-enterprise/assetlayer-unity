using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using AssetLayer.Unity;
using static AssetLayer.Unity.UtilityFunctions;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; } // Singleton instance

    private Asset selectedAsset;

    public GameObject AssetlayerGameObjectToSwitchOnChange;

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
        string assetBundleUrl = GetExpressionValueAssetBundle(asset.expressionValues, "AssetBundle");
        // StartCoroutine(DownloadAndInstantiateAssetBundle(assetBundleUrl));
        if (AssetlayerGameObjectToSwitchOnChange != null)
        {
            AssetlayerGameObjectToSwitchOnChange.GetComponent<AssetBundleImporter>()?.SetNftId(asset.assetId);
        }

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

                AssetBundleImporter.SwitchOutGameObject(AssetlayerGameObjectToSwitchOnChange, prefab);
                

            }
        }
    }
}
