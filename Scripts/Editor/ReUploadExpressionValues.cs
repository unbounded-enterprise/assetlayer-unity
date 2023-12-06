using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Collections.Generic;
using AssetLayer.SDK.Collections;

namespace AssetLayer.Unity
{

    public class ReUploadExpressionValues : EditorWindow
    {
        string collectionId = "";
        Texture2D image;
        string successMessage = "";
        const string BUNDLEPATH = "AssetlayerUnitySDK/AssetBundles";
        float fieldOfView = 120f;
        float fieldOfViewPrefab = 30f;
        private const string AppAssetsPath = "Assets/AssetlayerUnitySDK/AppAssets";

        private bool isUploadingExpression = false;

        [MenuItem("Assets/AssetLayer/Re-upload Expression Values")]
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
            // get collection info
            ApiManager manager = new ApiManager();
            List<string> collectionIds = new List<string>();
            collectionIds.Add(collectionId);
            List<Collection> collectionInfo = await manager.GetCollectionInfo(collectionIds);
            string collectionName = collectionInfo[0].collectionName;
            string slotId = collectionInfo[0].slotId;

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
                    uploadSuccess = await manager.UploadBundleExpression(collectionId, dataUrl, "AssetBundle" + platform.ToString(), "AssetBundle");

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

            bool uploadSuccessMenuView = await manager.UploadBundleExpression(collectionId, imageUrl, "Image", "Menu View");

            if (uploadSuccess && selectedObject is GameObject)
            {
                SavePrefab((GameObject)selectedObject, slotId, collectionId, collectionName);
            }

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

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void SavePrefab(GameObject selectedGameObject, string slotId, string collectionId, string collectionName)
        {
            try
            {
                string slotFolderPath = Path.Combine(AppAssetsPath, slotId);
                string collectionFolderPath = Path.Combine(slotFolderPath, collectionId);
                string gameObjectPath = Path.Combine(slotFolderPath, collectionName + collectionId + ".prefab");

                // Create directories if they do not exist
                EnsureDirectoryExists(AppAssetsPath);
                EnsureDirectoryExists(slotFolderPath);
                EnsureDirectoryExists(collectionFolderPath);

                // Check if a prefab with the same name already exists
                if (File.Exists(gameObjectPath))
                {
                    // Delete the existing prefab
                    AssetDatabase.DeleteAsset(gameObjectPath);
                }

                // Save the prefab to disk
                GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(selectedGameObject, gameObjectPath);

                if (prefabAsset != null)
                {
                    // Calculate hash
                    string prefabHash = UtilityFunctions.CalculatePrefabHash(prefabAsset);
                    Debug.Log("Prefab Hash: " + prefabHash);

                    // Load the AssetBundleDatabase ScriptableObject
                    string databasePath = "Assets/AssetlayerUnitySDK/ScriptableObjects/AssetBundleDatabase.asset";
                    AssetBundleDatabase assetBundleDatabase = AssetDatabase.LoadAssetAtPath<AssetBundleDatabase>(databasePath);
                    if (assetBundleDatabase == null)
                    {
                        Debug.LogError("AssetBundleDatabase not found at path: " + databasePath);
                        return;
                    }
                    else
                    {
                        Debug.Log("scriptable object found");
                    }

                    // Create a new AssetBundleData entry
                    AssetBundleData newAssetBundleData = new AssetBundleData
                    {
                        prefabPath = gameObjectPath,
                        hash = prefabHash,
                        collectionId = collectionId,
                        collectionName = collectionName,
                        slotId = slotId,
                        version = "1.0" // Set the version number as required
                    };

                    // Add the new entry to the AssetBundleDatabase
                    assetBundleDatabase.bundles.Add(newAssetBundleData);

                    // Save changes to the AssetBundleDatabase
                    EditorUtility.SetDirty(assetBundleDatabase);
                    AssetDatabase.SaveAssets();

                    // Refresh the AssetDatabase
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogError("Failed to save prefab: " + selectedGameObject.name);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("save prefab exception: " + ex.Message);
            }

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

}
