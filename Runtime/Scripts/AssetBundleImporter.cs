using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class AssetBundleImporter : MonoBehaviour
{
    private SDKClass sdk;
    public string NftId { get; private set; }
    public string defaultNftId;
    private string bundleDirectory;

    private void Initialize()
    {
        Debug.Log("GameObject calling: " + gameObject.name);
        sdk = new SDKClass();
        bundleDirectory = Path.Combine(Application.persistentDataPath, "AssetBundles");
        string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Debug.Log(userProfilePath);
    }

    private void Start()
    {
        if (defaultNftId != "")
        {
            SetNftId(defaultNftId);
        }
    }

    public void SetNftId(string newNftId)
    {
        this.NftId = newNftId;
        Initialize();
        StartProcess();
    }

    private void StartProcess()
    {
        StartCoroutine(sdk.GetExpression(NftId, "AssetBundle", ApplyObj));
    }

    private void ApplyObj(string bundleUrl)
    {
        Debug.Log("downloadlink: " + bundleUrl);
        StartCoroutine(DownloadAndLoadBundle(bundleUrl, bundleDirectory));
    }

    private IEnumerator DownloadAndLoadBundle(string bundleUrl, string directoryPath)
    {
        Debug.Log($"Starting to download AssetBundle from: {bundleUrl}");

        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download AssetBundle: {uwr.error}");
            }
            else
            {
                Debug.Log("Successfully downloaded AssetBundle");

                // Get downloaded asset bundle
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                if (bundle != null)
                {
                    Debug.Log("Successfully loaded AssetBundle");

                    // Load all assets
                    UnityEngine.Object[] allAssets = bundle.LoadAllAssets();
                    if (allAssets != null && allAssets.Length > 0)
                    {
                        Debug.Log($"Successfully loaded all assets. Count: {allAssets.Length}");
                        foreach (UnityEngine.Object asset in allAssets)
                        {
                            // Handle each asset
                            if (asset is GameObject)
                            {
                                Debug.Log($"Processing GameObject asset: {asset.name}");

                                GameObject prefab = asset as GameObject;
                                // Instantiate a new gameobject from the imported prefab at the same position and rotation as the parent
                                GameObject importedObj = Instantiate(prefab, transform.position, transform.rotation, transform.parent);
                                importedObj.transform.rotation = Quaternion.identity; // remove this later

                                // Destroy the parent game object
                                Destroy(gameObject);
                            }
                            else
                            {
                                Debug.LogWarning($"Asset {asset.name} is not a GameObject");
                            }
                        }
                        bundle.Unload(false);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load assets from asset bundle at {bundleUrl}");
                    }
                }
                else
                {
                    Debug.LogError($"Asset bundle not found at {bundleUrl}");
                }
            }
        }
    }
}
