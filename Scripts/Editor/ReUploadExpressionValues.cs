using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using Assetlayer.UnitySDK;
using System.Threading.Tasks;

public class ReUploadExpressionValues : EditorWindow
{
    string collectionId = "";
    Texture2D image;
    string successMessage = "";
    const string BUNDLEPATH = "AssetlayerUnitySDK/AssetBundles";
    float fieldOfView = 120f;
    float fieldOfViewPrefab = 30f;

    private bool isUploadingExpression = false;

    [MenuItem("Assets/Assetlayer/Re-upload Expression Values")]
    static void ShowWindow()
    {
        // Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(ReUploadExpressionValues));
    }

    void OnGUI()
    {
        if (isUploadingExpression || !string.IsNullOrEmpty(successMessage))
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
            GUILayout.Label("Re-upload Expression Values", EditorStyles.boldLabel);
            collectionId = EditorGUILayout.TextField("Collection ID", collectionId);
            image = (Texture2D)EditorGUILayout.ObjectField("Image", image, typeof(Texture2D), false);

            if (GUILayout.Button("Re-upload"))
            {
                isUploadingExpression = true;
                CreateBundleAndUploadExpression(collectionId);
            }
        }
    }

    async Task CreateBundleAndUploadExpression(string collectionId)
    {

        bool wasScene = Selection.activeObject is SceneAsset;
        bool wasPrefab = Selection.activeObject is GameObject;
        UnityEngine.Object selectedObject = Selection.activeObject;

        
        // AssetBundling process
        var selectedAssets = Selection.objects;
        if (selectedAssets.Length == 0)
        {
            UnityEngine.Debug.Log("No assets selected for bundling");
            return;
        }
        string bundleName = selectedAssets[0].name.ToLower();
        foreach (var asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(bundleName, "");
        }
        // Ensure the bundle save path exists.
        string fullPath = Path.Combine(Application.dataPath, BUNDLEPATH);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
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
        if (image != null)
        {
            Texture2D readableImage = MakeTextureReadable(image);
            imageUrl = ImageToDataUrlResized(readableImage);
            UnityEngine.Debug.Log("Image Data URL: " + imageUrl);
        }
        Debug.Log("imageurls2: " + imageUrl);

        SDKClass sdkInstance = new SDKClass();
        bool uploadSuccess = false;
        try
        {
            BuildTarget[] platforms =
            {
                BuildTarget.iOS,
                BuildTarget.Android,
                BuildTarget.StandaloneWindows,
                BuildTarget.StandaloneOSX,
                BuildTarget.WebGL
            };
            
            foreach (BuildTarget platform in platforms)
            {
                // Build the asset bundles for the current platform
                BuildPipeline.BuildAssetBundles(fullPath, BuildAssetBundleOptions.ChunkBasedCompression, platform);

                // Move and get the dataUrl for the bundle
                string bundlePath = MoveAssetBundles(bundleName);
                string dataUrl = BundleToDataUrl(bundlePath);

                // Upload the AssetBundle for the current platform using the SDK.
                uploadSuccess = await sdkInstance.UploadBundleExpression(collectionId, dataUrl, "AssetBundle" + platform.ToString(), "AssetBundle");

                if (!uploadSuccess)
                {
                    Debug.LogError($"Failed to upload AssetBundle for platform {platform}.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception caught: " + e.ToString());
        }

        bool uploadSuccessMenuView = await sdkInstance.UploadBundleExpression(collectionId, imageUrl, "Image", "Menu View");

        if (uploadSuccess)
        {
            Debug.Log("AssetBundle uploaded successfully!");
            successMessage = "AssetBundle created and uploaded successfully!";
            Repaint();
        }
        else
        {
            Debug.Log("Failed to upload AssetBundle.");
        }

        foreach (var asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant("", "");
        }

        AssetDatabase.Refresh();
    }

    public string MoveAssetBundles(string bundleName)
    {
        // Convert the relative bundle path to an absolute path
        string absoluteBundlePath = Path.Combine(Application.dataPath, BUNDLEPATH);

        string bundleDirectoryPath = Path.Combine(absoluteBundlePath, bundleName + "_dir");

        if (Directory.Exists(bundleDirectoryPath))
        {
            Directory.Delete(bundleDirectoryPath, true);
        }
        Directory.CreateDirectory(bundleDirectoryPath);

        string bundlePath = Path.Combine(absoluteBundlePath, bundleName);
        string bundleManifestPath = Path.Combine(absoluteBundlePath, bundleName + ".manifest");
        string targetBundlePath = Path.Combine(bundleDirectoryPath, bundleName + ".bundle");
        string targetBundleManifestPath = Path.Combine(bundleDirectoryPath, bundleName + ".bundle.manifest");

        if (File.Exists(targetBundlePath))
        {
            File.Delete(targetBundlePath);
        }

        if (File.Exists(targetBundleManifestPath))
        {
            File.Delete(targetBundleManifestPath);
        }

        File.Move(bundlePath, targetBundlePath);
        File.Move(bundleManifestPath, targetBundleManifestPath);

        return targetBundlePath;
    }

    Texture2D MakeTextureReadable(Texture2D originalTexture)
    {
        if (originalTexture.isReadable)
        {
            return originalTexture;
        }
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
        Texture2D resizedImage = new Texture2D(500, 500);
        RenderTexture rt = RenderTexture.GetTemporary(500, 500);
        RenderTexture.active = rt;
        Graphics.Blit(image, rt);
        resizedImage.ReadPixels(new Rect(0, 0, 500, 500), 0, 0);
        resizedImage.Apply();
        RenderTexture.active = null;
        rt.Release();

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
