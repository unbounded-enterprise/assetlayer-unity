using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class UtilityFunctions
{
    public static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);

            if (result != null)
                return result;
        }

        return null;
    }

    // Method to load asset bundle
    public static async Task<AssetBundle> LoadAssetBundle(string bundleUrl)
    {
        UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl);
        await uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load AssetBundle: " + uwr.error);
            return null;
        }

        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
        return bundle;
    }
}
