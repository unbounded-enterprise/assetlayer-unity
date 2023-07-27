using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class SceneImporter : MonoBehaviour
{
    public static SceneImporter Instance { get; private set; } // Singleton instance

    private SDKClass sdk;
    public string NftId { get; private set; }
    public string defaultNftId;
    private string bundleDirectory;
    public string loadingSceneName = "LoadingScene"; // the name of your loading scene

    // Initialize the Singleton in the Awake function
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Ensures that the instance isn't destroyed between scenes
        }
        else
        {
            Destroy(gameObject);
        }
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
        if (defaultNftId != "")
        {
            SetNftId(defaultNftId);
        }
    }

    public void SetNftId(string newNftId)
    {
        this.NftId = newNftId;
        Initialize();
    }

    public void LoadScene()
    {
        if (NftId == null || NftId == "")
        {
            Debug.LogError("No NftId has been set. Can't load the scene.");
            return;
        }
        LoadLoadingScene();
        StartProcess();
    }

    private void StartProcess()
    {
        StartCoroutine(sdk.GetExpression(NftId, "AssetBundle", ApplyScene));
    }

    private void ApplyScene(string bundleUrl)
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

                    // Load the scene from the bundle
                    var scenePath = bundle.GetAllScenePaths()[0];  // get the path of the first scene in the bundle
                    yield return StartCoroutine(LoadSceneFromBundle(scenePath));

                    bundle.Unload(false);
                }
                else
                {
                    Debug.LogError($"Asset bundle not found at {bundleUrl}");
                }
            }
            UnloadLoadingScene();
        }
    }

    private IEnumerator LoadSceneFromBundle(string scenePath)
    {
        var asyncLoad = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private void LoadLoadingScene()
    {
        SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Additive);
    }

    private void UnloadLoadingScene()
    {
        SceneManager.UnloadSceneAsync(loadingSceneName);
    }
}
