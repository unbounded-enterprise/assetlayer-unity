  using System.IO;
using UnityEngine;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Assetlayer.UnitySDK;
public class SceneImporter : MonoBehaviour
{

    private SDKClass sdk;
    public string AssetId { get; private set; }
    public string defaultAssetId;
    private Asset loadedSceneAsset;
    private string bundleDirectory;
    public string loadingSceneName = "LoadingScene"; // the name of your loading scene

    private AssetBundleDownloader bundleDownloader;

    // Initialize the Singleton in the Awake function
    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // Ensures that the instance isn't destroyed between scenes
        bundleDownloader = GetComponent<AssetBundleDownloader>();
    }

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
        if (defaultAssetId != "")
        {
            SetAssetId(defaultAssetId);
        }

        
    }

    public void SetAssetId(string newAssetId)
    {
        
        this.AssetId = newAssetId;
        if (this.loadedSceneAsset?.assetId != newAssetId)
        {
            this.loadedSceneAsset = null;
        }
        Initialize();
        
    }


    public void SetSceneAsset(Asset asset)
    {
        Debug.Log("Loaded bundle before vent" + asset.loadedAssetBundle);
        if (asset == null || asset.loadedAssetBundle == null)
        {
            return;
        }
        if (asset.loadedAssetBundle.isStreamedSceneAssetBundle)
        {
            Debug.Log("New Scene Selected");
            this.loadedSceneAsset = asset;
            this.AssetId = asset.assetId;
        }
    }

    public void LoadScene()
    {
        if (string.IsNullOrEmpty(this.AssetId) && (this.loadedSceneAsset == null || string.IsNullOrEmpty(this.loadedSceneAsset.assetId)))
        {
            return;
        }
        bool alreadyLoaded = this.loadedSceneAsset?.assetId == this.AssetId;

        if (alreadyLoaded)
        {
            Debug.Log("Asset already loaded");
            StartCoroutine(LoadLoadingScene());
            HandleLoadedBundle(this.loadedSceneAsset.loadedAssetBundle);
            return;
        }
        if (AssetId == null || AssetId == "")
        {
            Debug.LogError("No NftId has been set. Can't load the scene.");
            return;
        }
        StartCoroutine(LoadLoadingScene());
        StartProcess();
    }

    private void StartProcess()
    {
        StartCoroutine(sdk.GetExpression(AssetId, "AssetBundle", ApplyScene));
    }

    private void ApplyScene(string bundleUrl)
    {
        Debug.Log("downloadlink: " + bundleUrl);
        bundleDownloader.DownloadAndLoadBundle(bundleUrl, HandleLoadedBundle);
    }

    private void HandleLoadedBundle(AssetBundle bundle)
    {         
                var scenePath = bundle.GetAllScenePaths()[0];  // get the path of the first scene in the bundle
                StartCoroutine(LoadSceneFromBundle(scenePath));

                bundle.Unload(false);  
    }

    private IEnumerator LoadSceneFromBundle(string scenePath)
    {
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        var asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        EnsureSingleEventSystem();
    }

    private IEnumerator LoadLoadingScene()
    {
        var asyncLoad = SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        EnsureSingleEventSystem();
    }

    private void UnloadLoadingScene()
    {
        SceneManager.UnloadSceneAsync(loadingSceneName);
    }

    private void EnsureSingleEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();

        if (eventSystems.Length > 1)
        {
            // Destroy all but the first EventSystem.
            for (int i = 1; i < eventSystems.Length; i++)
            {
                Destroy(eventSystems[i].gameObject);
            }
        }
    }
}
