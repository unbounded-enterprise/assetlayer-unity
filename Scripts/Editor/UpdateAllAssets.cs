using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace AssetLayer.Unity
{
    public class UpdateAllAssets : EditorWindow
    {
        private AssetBundleDatabase assetBundleDatabase;
        private const string BUNDLEPATH = "AssetlayerUnitySDK/AssetBundles";
        private string successMessage = "";
        private bool isProcessing = false; // Variable to track the process state


        [MenuItem("Assets/AssetLayer/UpdateAllAssets")]
        static void ShowWindow()
        {
            // Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(UpdateAllAssets));
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            assetBundleDatabase = AssetDatabase.LoadAssetAtPath<AssetBundleDatabase>("Assets/AssetlayerUnitySDK/ScriptableObjects/AssetBundleDatabase.asset");
#endif
            if (assetBundleDatabase == null)
            {
                Debug.LogError("AssetBundleDatabase not found. Please make sure it is located in a Resources folder and named 'AssetBundleDatabase'");
            }
        }

        void OnGUI()
        {
            GUILayout.Label("Upload Asset Bundles", EditorStyles.boldLabel);

            // Use the `isProcessing` flag to enable/disable the button
            EditorGUI.BeginDisabledGroup(isProcessing);

            if (GUILayout.Button("Upload All Bundles"))
            {
                // Set `isProcessing` to true to disable the button
                isProcessing = true;
                // Use the `isProcessing` flag to enable/disable the button
                EditorGUI.BeginDisabledGroup(isProcessing);
                if (assetBundleDatabase != null)
                {
                    UploadAllBundlesSequentially();
                } else
                {
                    Debug.Log("assetbundle database not setup yet, reupload prefabs to add");
                }
            }

            // Display success message if any
            if (!string.IsNullOrEmpty(successMessage))
            {
                EditorGUILayout.HelpBox(successMessage, MessageType.Info);
            }
        }
        private async void UploadAllBundlesSequentially()
        {
            try
            {
                foreach (var bundleData in assetBundleDatabase.bundles)
                {
                    await CreateBundleAndUpload(bundleData);
                }
            }
            finally
            {
                isProcessing = false; // Ensure processing flag is reset even if an exception occurs
                Repaint(); // Repaint the editor window to update the UI
            }
        }

        private async Task CreateBundleAndUpload(AssetBundleData bundleData)
        {
            try
            {
                // Mock function calls since actual implementations are not provided
                // You'd include here the actual logic for asset bundle creation and upload
                bool uploadSuccess = await UploadBundle(bundleData);

                if (uploadSuccess)
                {
                    successMessage = "AssetBundle for slot " + bundleData.slotId + " uploaded successfully!";
                }
                else
                {
                    successMessage = "Failed to upload AssetBundle for slot " + bundleData.slotId + ".";
                }

                Repaint(); // Refresh the editor window to show the success message
            }
            catch (Exception e)
            {
                Debug.LogError("Exception caught: " + e.ToString());
            }
        }

        // This is a placeholder for the actual upload logic
        private async Task<bool> UploadBundle(AssetBundleData bundleData)
        {
            await CreateBundleAndUploadExpression(bundleData.collectionId, bundleData.prefabPath);
            // Implement actual bundle creation and uploading logic here
            Debug.Log($"Uploading bundle for slotId {bundleData.slotId} and collectionName {bundleData.collectionName}");
            return true; // mock return to indicate success
        }

        public async Task CreateBundleAndUploadExpression(string collectionId, string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("Prefab path is null or empty.");
                return;
            }

            if (!File.Exists(prefabPath))
            {
                Debug.LogError("Prefab file does not exist at the given path.");
                return;
            }



            // Ensuring only a prefab will be processed
            string bundleName = Path.GetFileNameWithoutExtension(prefabPath).ToLower();
            AssetImporter.GetAtPath(prefabPath).SetAssetBundleNameAndVariant(bundleName, "");

            // Ensure the bundle save path exists
            string fullPath = Path.Combine(Application.dataPath, BUNDLEPATH);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Load the prefab asset to calculate the hash
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            string newPrefabHash = UtilityFunctions.CalculatePrefabHash(prefabAsset);

            // Find the corresponding AssetBundleData entry in the database
            AssetBundleData bundleData = assetBundleDatabase.bundles.FirstOrDefault(b => b.prefabPath == prefabPath);

            if (bundleData != null && bundleData.hash == newPrefabHash)
            {
                Debug.Log("Prefab has not changed, no need to upload.");
                return;
            }


            // Build the asset bundle for the prefab
            BuildTarget[] platforms = {
            BuildTarget.iOS,
            BuildTarget.Android,
            BuildTarget.StandaloneWindows,
            BuildTarget.StandaloneOSX,
            BuildTarget.WebGL
        };
            ApiManager manager = new ApiManager();
            foreach (BuildTarget platform in platforms)
            {
                // Build the asset bundles for the current platform
                BuildPipeline.BuildAssetBundles(fullPath, BuildAssetBundleOptions.ChunkBasedCompression, platform);

                // Move and get the dataUrl for the bundle
                string bundlePath = MoveAssetBundles(bundleName);
                string dataUrl = BundleToDataUrl(bundlePath);

                // Upload the AssetBundle for the current platform using the SDK
                bool uploadSuccess = await manager.UploadBundleExpression(collectionId, dataUrl, "AssetBundle" + platform.ToString(), "AssetBundle");
                if (!uploadSuccess)
                {
                    Debug.LogError($"Failed to upload AssetBundle for platform {platform}.");
                }
            }

            // Update the hash in the AssetBundleData entry
            if (bundleData != null)
            {
                bundleData.hash = newPrefabHash;
                EditorUtility.SetDirty(assetBundleDatabase); // Mark the database as dirty to ensure it gets saved
                AssetDatabase.SaveAssets(); // Save the updated database asset
            }

            // Reset the asset bundle name to cleanup
            AssetImporter.GetAtPath(prefabPath).SetAssetBundleNameAndVariant("", "");

            AssetDatabase.Refresh();

            Debug.Log("Prefab AssetBundle process completed.");
        }

        string BundleToDataUrl(string bundleFilePath)
        {
            byte[] bundleBytes = File.ReadAllBytes(bundleFilePath);
            string base64String = Convert.ToBase64String(bundleBytes);
            return "data:application/octet-stream;base64," + base64String;
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
    }


}
