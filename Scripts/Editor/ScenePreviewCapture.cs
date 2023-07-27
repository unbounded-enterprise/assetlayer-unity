#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ScenePreviewCapture : EditorWindow
{
    string scenePath = "";
    string status = "";
    float fieldOfView = 120f;

    [MenuItem("Assets/Assetlayer/Scene Preview Capture")]
    public static void ShowWindow()
    {
        ScenePreviewCapture window = GetWindow<ScenePreviewCapture>();
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (assetPath.EndsWith(".unity"))
        {
            window.scenePath = assetPath;
            window.LoadAndCaptureScenePreview(assetPath);
        }
        else
        {
            window.status = "Error: Selected asset is not a scene!";
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Capture Scene Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Scene Path", scenePath);
        EditorGUILayout.LabelField("Status", status);
    }

    void LoadAndCaptureScenePreview(string scenePath)
    {
        status = "In Progress...";
        ScenePreviewCapturer.CaptureScenePreview(scenePath, fieldOfView);
        status = "Capture completed.";
    }
}
#endif
