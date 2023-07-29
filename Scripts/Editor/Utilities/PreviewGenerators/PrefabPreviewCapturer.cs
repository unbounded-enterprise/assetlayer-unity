#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public class PrefabPreviewCapturer
{
    public static async Task<string> CapturePrefabPreview(string prefabPath, float fieldOfView)
    {
        // Load the prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        // Create a new camera in the scene
        GameObject cameraObject = new GameObject("Preview Camera");
        Camera previewCamera = cameraObject.AddComponent<Camera>();

        // Calculate bounds
        Bounds bounds = new Bounds(gameObject.transform.position, Vector3.zero);
        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(renderer.bounds);
        }

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float distance = maxSize / (2.0f * Mathf.Tan(0.5f * fieldOfView * Mathf.Deg2Rad)); // changed to fieldOfView

        // Position the camera above and in front of the object and look down at 45 degrees
        cameraObject.transform.position = bounds.center + new Vector3(0, distance, -distance);
        cameraObject.transform.LookAt(bounds.center);

        // Set the field of view
        previewCamera.fieldOfView = fieldOfView;

        // Create a new RenderTexture
        RenderTexture renderTexture = new RenderTexture(500, 500, 24);
        // Assign the RenderTexture to the camera
        previewCamera.targetTexture = renderTexture;

        string outputPath = string.Empty;

        // Wait for a frame to ensure all textures are loaded
        await Task.Yield();

        // Render the camera's view to the RenderTexture
        previewCamera.Render();

        // Capture the RenderTexture to a Texture2D
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        // Write the Texture2D to a PNG file
        byte[] bytes = texture2D.EncodeToPNG();
        string directoryPath = Path.GetDirectoryName(prefabPath);
        string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
        outputPath = Path.Combine(directoryPath, prefabName + "_Preview.png");
        File.WriteAllBytes(outputPath, bytes);

        // Refresh the AssetDatabase
        AssetDatabase.Refresh();

        // Cleanup
        if (previewCamera != null)
        {
            UnityEngine.Object.DestroyImmediate(previewCamera.gameObject);
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        if (gameObject != null)
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        return outputPath;
    }
}
#endif
