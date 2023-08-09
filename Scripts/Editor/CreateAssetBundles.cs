using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;

public class CreateAssetBundlesFromSelection
{
    [MenuItem("Assets/Assetlayer/Create Assetlayer Collection")]

    static void ShowWindow()
    {
        // Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(AssetBundleCreatorWindow));
    }
}

public class AssetBundleCreatorWindow : EditorWindow
{
    string slotId = "";
    string collectionName = "MyPrefabCollection";
    int maxSupply = 100;
    Texture2D image;
    string successMessage = "";
    const string BUNDLEPATH = "AssetlayerUnitySDK/AssetBundles";
    float fieldOfView = 120f;
    float fieldOfViewPrefab = 30f;
    string[] slotIds;
    int slotIndex = 0;

    private bool isCreatingCollection = false;

    async void OnEnable()
    {
        SDKClass sdkInstance = new SDKClass();
        slotIds = await sdkInstance.GetAppSlots();
    }


    void OnGUI()
    {
        if (isCreatingCollection || !string.IsNullOrEmpty(successMessage))
        {
            if (!string.IsNullOrEmpty(successMessage))
            {
                GUILayout.Label(successMessage, EditorStyles.boldLabel);
                if (GUILayout.Button("Close"))
                {
                    this.Close();
                }
            }
        }
        else
        {
            GUILayout.Label("Create Asset Bundle", EditorStyles.boldLabel);

            if (slotIds != null)
            {
                slotIndex = EditorGUILayout.Popup("Slot ID", slotIndex, slotIds);
                slotId = slotIds[slotIndex];
            }

            collectionName = EditorGUILayout.TextField("Collection Name", collectionName);
            maxSupply = EditorGUILayout.IntField("Max Supply", maxSupply);
            image = (Texture2D)EditorGUILayout.ObjectField("Image", image, typeof(Texture2D), false);

            if (GUILayout.Button("Create Bundle"))
            {
                isCreatingCollection = true;
                CreateBundleFromSelection(slotId, maxSupply, collectionName);
            }
        }
    }
    async void CreateBundleFromSelection(string slotId, int maxSupply, string collectionName)
    {
        bool wasScene = Selection.activeObject is SceneAsset;
        bool wasPrefab = Selection.activeObject is GameObject;
        UnityEngine.Object selectedObject = Selection.activeObject;
        // Ensure the bundle save path exists.
        string fullPath = Path.Combine(Application.dataPath, BUNDLEPATH);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        // Retrieve the currently selected assets.
        var selectedAssets = Selection.objects;

        // Ensure that some assets are selected.
        if (selectedAssets.Length == 0)
        {
            UnityEngine.Debug.Log("No assets selected for bundling");
            return;
        }

        // Set the bundle name to the first selected object's name.
        string bundleName = selectedAssets[0].name.ToLower();

        // Assign each selected asset the same bundle name.
        foreach (var asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(bundleName, "");
        }

        // Refresh the AssetDatabase after setting the asset bundle names.
        AssetDatabase.Refresh();

        // Build all asset bundles.
        BuildPipeline.BuildAssetBundles(fullPath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        // Output log
        UnityEngine.Debug.Log("AssetBundle Created: " + bundleName);
        string imageUrl = "";

        // If no image is selected and the first selected asset is a scene or a prefab, capture a preview image
        if (image == null && (wasScene || wasPrefab))
        {
            string assetPath = AssetDatabase.GetAssetPath(selectedObject);
            string imagePath = "";

            if (wasScene)
            {
                imagePath = ScenePreviewCapturer.CaptureScenePreview(assetPath, fieldOfView);
            }
            else if (wasPrefab)
            {
                imagePath = await PrefabPreviewCapturer.CapturePrefabPreview(assetPath, fieldOfViewPrefab);
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                byte[] bytes = File.ReadAllBytes(imagePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    image = tex;
                }
                else
                {
                    Debug.LogError("Failed to load image data into texture");
                }
            }
        }
        // If an image is selected, convert it to a data URL and print it to the console.
        if (image != null)
        {
            Texture2D readableImage = MakeTextureReadable(image);
            imageUrl = ImageToDataUrlResized(readableImage);
            Debug.Log("Image Data URL: " + imageUrl);
        }

        SDKClass sdkInstance = new SDKClass();
        string collectionId = await sdkInstance.CreateCollection(slotId, collectionName, maxSupply, imageUrl);
        if (collectionId != null)
        {
            
            Debug.Log("Collection created successfully!");
            // First, try to get existing AssetBundle expression
            string expressionId = await sdkInstance.GetAssetExpression(slotId);
            if (string.IsNullOrEmpty(expressionId))
            {
                // If no existing expression found, create a new one
                string expressionTypeId = "64b1ce76716b83c3de7df84e";
                string expressionName = "AssetBundle";
                expressionId = await sdkInstance.CreateExpression(slotId, expressionTypeId, expressionName);

                if (string.IsNullOrEmpty(expressionId))
                {
                    Debug.Log("Failed to create expression.");
                    return;
                }
            }

            string bundlePath = MoveAssetBundles(bundleName);

            // Convert the AssetBundle to a Data URL.
            string dataUrl = BundleToDataUrl(bundlePath);

            Debug.Log("dataUrl: " + dataUrl.Length);

            string menuViewExpressionId = await sdkInstance.GetMenuViewExpression(slotId);

            bool menuViewSuccess = await sdkInstance.UploadBundleExpression(collectionId, imageUrl, "Image", "Menu View");

            // Upload the AssetBundle using the SDK.
            bool uploadSuccess = await sdkInstance.UploadBundleExpression(collectionId, dataUrl);

            if (uploadSuccess)
            {
                Debug.Log("AssetBundle uploaded successfully!");

                // Call MintNFT function after the upload.
                bool mintSuccess = await sdkInstance.Mint(collectionId, 10);  // Replace MintNFT with the actual function name from your SDK.

                if (mintSuccess)
                {
                    Debug.Log("NFT Minted successfully!");
                    successMessage = "Collection and AssetBundle created and uploaded successfully!";
                    Repaint();
                }
                else
                {
                    Debug.Log("Failed to mint NFT.");
                }
            }
            else
            {
                Debug.Log("Failed to upload AssetBundle.");
            }
        }
        else
        {
            Debug.Log("Failed to create collection.");
        }

        // Remove the AssetBundle association from each asset.
        foreach (var asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant("", "");
        }

        // Refresh the AssetDatabase after removing the asset bundle names.
        AssetDatabase.Refresh();
    }


    public string MoveAssetBundles(string bundleName)
    {
        // Convert the relative bundle path to an absolute path
        string absoluteBundlePath = Path.Combine(Application.dataPath, BUNDLEPATH);

        // Create a new directory for the bundle and its manifest.
        string bundleDirectoryPath = Path.Combine(absoluteBundlePath, bundleName + "_dir");

        // Check if directory exists, delete and recreate if it does.
        if (Directory.Exists(bundleDirectoryPath))
        {
            Directory.Delete(bundleDirectoryPath, true);
        }
        Directory.CreateDirectory(bundleDirectoryPath);

        string bundlePath = Path.Combine(absoluteBundlePath, bundleName);
        string bundleManifestPath = Path.Combine(absoluteBundlePath, bundleName + ".manifest");
        string targetBundlePath = Path.Combine(bundleDirectoryPath, bundleName + ".bundle");
        string targetBundleManifestPath = Path.Combine(bundleDirectoryPath, bundleName + ".bundle.manifest");

        // Check if bundle file exists, delete it if it does.
        if (File.Exists(targetBundlePath))
        {
            File.Delete(targetBundlePath);
        }

        // Check if bundle manifest file exists, delete it if it does.
        if (File.Exists(targetBundleManifestPath))
        {
            File.Delete(targetBundleManifestPath);
        }

        // Move the AssetBundle and its manifest file to the new directory with .bundle extension.
        File.Move(bundlePath, targetBundlePath);
        File.Move(bundleManifestPath, targetBundleManifestPath);

        return targetBundlePath;
    }



    Texture2D MakeTextureReadable(Texture2D originalTexture)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            originalTexture.width,
            originalTexture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(originalTexture, renderTexture);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D readableTexture = new Texture2D(originalTexture.width, originalTexture.height);
        readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        return readableTexture;
    }

    string ImageToDataUrl(Texture2D image)
    {
        byte[] imageBytes = image.EncodeToPNG();
        string base64String = Convert.ToBase64String(imageBytes);
        return "data:image/png;base64," + base64String;
    }

    string ImageToDataUrlResized(Texture2D image)
    {
        // Create a new Texture2D of size 500x500
        Texture2D resizedImage = new Texture2D(500, 500);

        // Use RenderTexture to render the source image into the new size
        RenderTexture rt = RenderTexture.GetTemporary(500, 500);
        RenderTexture.active = rt;

        // Copy the source image into the RenderTexture
        Graphics.Blit(image, rt);

        // Read the RenderTexture into the new Texture2D
        resizedImage.ReadPixels(new Rect(0, 0, 500, 500), 0, 0);
        resizedImage.Apply();

        // Release the RenderTexture to free up memory
        RenderTexture.active = null;
        rt.Release();

        // Convert the resized image to a data URL
        byte[] imageBytes = resizedImage.EncodeToPNG();
        string base64String = Convert.ToBase64String(imageBytes);

        return "data:image/png;base64," + base64String;
    }


    string BundleToDataUrl(string bundleFilePath)
    {
        byte[] bundleBytes = File.ReadAllBytes(bundleFilePath);
        string base64String = Convert.ToBase64String(bundleBytes);
        return "data:application/octet-stream;base64," + base64String;
    }

}
