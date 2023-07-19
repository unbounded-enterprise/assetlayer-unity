using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Threading.Tasks;

public class ReUploadExpressionValues : EditorWindow
{
    string collectionId = "";
    Texture2D image;
    string successMessage = "";

    private bool isUploadingExpression = false;

    [MenuItem("Assets/Re-upload Expression Values")]
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

    async void CreateBundleAndUploadExpression(string collectionId)
    {
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
        AssetDatabase.Refresh();
        BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        UnityEngine.Debug.Log("AssetBundle Created: " + bundleName);

        string imageUrl = "";
        if (image != null)
        {
            Texture2D readableImage = MakeTextureReadable(image);
            imageUrl = ImageToDataUrlResized(readableImage);
            UnityEngine.Debug.Log("Image Data URL: " + imageUrl);
        }

        SDKClass sdkInstance = new SDKClass();

        string bundlePath = MoveAssetBundles(bundleName);
        string dataUrl = BundleToDataUrl(bundlePath);
        Debug.Log("dataUrl: " + dataUrl.Length);
        bool uploadSuccess = await sdkInstance.UploadBundleExpression(collectionId, dataUrl, "AssetBundle", "AssetBundle");
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
        string bundleDirectoryPath = Path.Combine("Assets/AssetBundles", bundleName + "_dir");

        if (Directory.Exists(bundleDirectoryPath))
        {
            Directory.Delete(bundleDirectoryPath, true);
        }
        Directory.CreateDirectory(bundleDirectoryPath);

        string bundlePath = Path.Combine("Assets/AssetBundles", bundleName);
        string bundleManifestPath = Path.Combine("Assets/AssetBundles", bundleName + ".manifest");
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
