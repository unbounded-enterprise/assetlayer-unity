#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AssetPreviewCapture : EditorWindow
{
    string assetPath = "";
    string status = "";
    float fieldOfView = 120f;
    float fieldOfViewPrefab = 30f;

    [MenuItem("Assets/Assetlayer/Asset Preview Capture")]
    public static void ShowWindow()
    {
        AssetPreviewCapture window = GetWindow<AssetPreviewCapture>();
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (assetPath.EndsWith(".unity"))
        {
            window.assetPath = assetPath;
            window.status = "In Progress...";
            window.LoadAndCaptureScenePreview(assetPath);
            window.status = "Scene Capture completed.";
        }
        else if (assetPath.EndsWith(".prefab"))
        {
            window.assetPath = assetPath;
            window.status = "In Progress...";
            window.LoadAndCapturePrefabPreview(assetPath);
            window.status = "Prefab Capture completed.";
        }
        else
        {
            window.status = "Error: Selected asset is neither a scene nor a prefab!";
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Capture Asset Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Asset Path", assetPath);
        EditorGUILayout.LabelField("Status", status);
    }

    void LoadAndCaptureScenePreview(string scenePath)
    {
        ScenePreviewCapturer.CaptureScenePreview(scenePath, fieldOfView);
    }

    void LoadAndCapturePrefabPreview(string prefabPath)
    {
        PrefabPreviewCapturer.CapturePrefabPreview(prefabPath, fieldOfViewPrefab);
    }
}
#endif
