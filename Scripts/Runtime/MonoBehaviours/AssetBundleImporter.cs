using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using UnityEditor;

namespace AssetLayer.Unity
{

    public class AssetBundleImporter : MonoBehaviour
    {
        private ApiManager manager;
        public string AssetId { get; private set; }
        public string defaultAssetId;
        public string bundleExpressionId;

        private AssetBundleDownloader bundleDownloader;

        private void Awake()
        {
            bundleDownloader = GetComponent<AssetBundleDownloader>();
        }

        private void Initialize()
        {
            manager = new ApiManager();
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(defaultAssetId))
            {
                SetNftId(defaultAssetId);
            }
        }

        public void SetNftId(string newNftId)
        {
            this.AssetId = newNftId;
            if (!string.IsNullOrEmpty(newNftId))
            {
                Initialize();
                StartProcess();
            }
        }

        public void SetNewAsset(Asset asset)
        {
            Debug.Log("NewAsset");
            this.AssetId = asset.assetId;
            if (!string.IsNullOrEmpty(this.AssetId))
            {
                PlayerPrefs.SetString("AssetLayerSelectedAssetId", asset.assetId);
                PlayerPrefs.Save();
                Initialize();
                StartProcess();
            }
        }

        private void StartProcess()
        {
            if (AssetCacheManager.Instance.IsInCache(AssetId))
            {
                Asset cachedAsset = AssetCacheManager.Instance.GetFromCache(AssetId);
                string bundleUrl = string.IsNullOrEmpty(bundleExpressionId) ?
                    UtilityFunctions.GetExpressionValueAssetBundle(cachedAsset.expressionValues, "AssetBundle")
                    :
                    UtilityFunctions.GetExpressionValueByExpressionIdAssetBundle(cachedAsset.expressionValues, bundleExpressionId);
                ApplyObj(bundleUrl);
            }
            else
            {
                StartCoroutine(manager.GetExpression(AssetId, "AssetBundle", ApplyObj)); // replace this later with sdk method to get an Asset, cache that, then load 
            }

        }

        private async void ApplyObj(string bundleUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(bundleUrl))
                {
                    Debug.Log("Empty Bundle URL");
                    ClearCacheAndRestartProcess();
                    return;
                }

                Debug.Log("Download link: " + bundleUrl);

                Debug.Log("downloadlink: " + bundleUrl);
                // Check if the bundle is already cached
                if (AssetBundleCacheManager.Instance.CachedBundles.ContainsKey(bundleUrl) && AssetBundleCacheManager.Instance.CachedBundles[bundleUrl] != null)
                {
                    Debug.Log("Bundle already cached, no need to download.");
                    HandleLoadedBundle(AssetBundleCacheManager.Instance.CachedBundles[bundleUrl]);
                }
                else
                {
                    bundleDownloader.DownloadAndLoadBundle(bundleUrl, HandleLoadedBundle);
                    // StartCoroutine(DownloadAndLoadBundleCoroutine(bundleUrl));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error encountered: {ex.Message}");
                ClearCacheAndRestartProcess();
            }


        }

        private void ClearCacheAndRestartProcess()
        {
            ClearCacheEntry(AssetId); // Clear the problematic cache entry
            StartProcess(); // Restart the process
        }

        private void ClearCacheEntry(string assetId)
        {
            // Use the existing method to remove the asset from the cache
            AssetCacheManager.Instance.RemoveFromCache(assetId);
        }


        IEnumerator DownloadAndLoadBundleCoroutine(string bundleUrl)
        {
            Debug.Log($"Starting to download AssetBundle from: {bundleUrl}");

            using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl))
            {
                // Send request
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to download AssetBundle: " + request.error);
                    yield break;
                }

                // Load downloaded asset bundle
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
                if (bundle == null)
                {
                    Debug.LogError("Failed to load downloaded AssetBundle");
                    yield break;
                }

                Debug.Log("Successfully downloaded and loaded AssetBundle");

                // Cache bundle
                AssetBundleCacheManager.Instance.CachedBundles[bundleUrl] = bundle;

                HandleLoadedBundle(bundle);
                yield break;
            }
        }
        private async void HandleLoadedBundle(AssetBundle bundle)
        {
            if (bundle == null)
            {
                return;
            }
            Debug.Log("HandleLoadedBundel" + bundle);
            if (bundle.isStreamedSceneAssetBundle)
            {
                Debug.Log("loaded bundle is a scene");
                return;
            }

            // Load assets asynchronously
            AssetBundleRequest request = bundle.LoadAllAssetsAsync();
            await request; // Wait until assets are loaded

            UnityEngine.Object[] allAssets = request.allAssets;
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
                        // Instantiate a new gameobject from the imported prefab as a child of the gameobject, deletes the first child (previous character loaded)
                        SwitchOutGameObject(this.gameObject, prefab);
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
                Debug.LogError($"Failed to load assets from asset bundle");
            }
        }

        public static void SwitchOutGameObject(GameObject AssetlayerGameObjectToSwitchOnChange, GameObject prefab)
        {
            Debug.Log("switching out Asset Layer Asset");
            // Destroy the first child if it exists
            if (AssetlayerGameObjectToSwitchOnChange.transform.childCount > 0)
            {
                Transform firstChild = AssetlayerGameObjectToSwitchOnChange.transform.GetChild(0);
                Destroy(firstChild.gameObject);
            }
            // Instantiate the new prefab as a child
            GameObject newObject = Instantiate(prefab, AssetlayerGameObjectToSwitchOnChange.transform.position, AssetlayerGameObjectToSwitchOnChange.transform.rotation, AssetlayerGameObjectToSwitchOnChange.transform);
            newObject.transform.localRotation = prefab.transform.rotation;
        }
    }
}