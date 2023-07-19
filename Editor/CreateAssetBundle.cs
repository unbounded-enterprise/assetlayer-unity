using UnityEditor;
using UnityEngine;
using System.IO;
using Unity.Android.Gradle;

public class CreateAssetBundles
{

    [MenuItem("Assets/Build AssetBundles")]
    static void BuildBundles()
    {

        // Retrieve the currently selected assets
        var selectedAssets = Selection.objects;

        // Early out if no assets selected
        if (selectedAssets.Length == 0)
        {
            Debug.Log("No assets selected for bundling");
            return;
        }

        // Get the first selected asset's name as the bundle name
        string bundleName = selectedAssets[0].name.ToLower();

        // Set all selected assets to this bundle name
        foreach (var asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(bundleName, "");
        }

        // Refresh database and build the asset bundles
        AssetDatabase.Refresh();
        BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        // Get bundle path
        string bundlePath = Path.Combine(Application.dataPath, "AssetBundles", bundleName);


        // Create bundle folder
        string bundleFolder = Path.Combine(Application.dataPath, "Bundles/" + bundleName);
        Directory.CreateDirectory(bundleFolder);

        // Move bundle to folder
        File.Move(bundlePath, Path.Combine(bundleFolder, bundleName));

        // Move manifest
        File.Move(bundlePath + ".manifest", Path.Combine(bundleFolder, bundleName + ".manifest"));

        // Log saved bundle
        Debug.Log("Saved bundle to: " + bundleFolder);

        // Log bundle created
        Debug.Log("Created AssetBundle: " + bundleName);

        // Remove bundle name association
        foreach (var asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant("", "");
        }

        AssetDatabase.Refresh();

    }

}