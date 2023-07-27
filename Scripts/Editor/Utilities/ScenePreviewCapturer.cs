#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public class ScenePreviewCapturer
{

    public static string CaptureScenePreview(string scenePath, float fieldOfView)
    {
        // Load the scene
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Create a new RenderTexture
        RenderTexture renderTexture = new RenderTexture(500, 500, 24);

        // Find the camera in the scene
        Camera previewCamera = GameObject.FindObjectOfType<Camera>();
        string outputPath = string.Empty;

        if (previewCamera != null)
        {
            // Set the field of view
            previewCamera.fieldOfView = fieldOfView;

            // Assign the RenderTexture to the camera
            previewCamera.targetTexture = renderTexture;

            // Render the camera's view to the RenderTexture
            previewCamera.Render();

            // Capture the RenderTexture to a Texture2D
            Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();

            // Write the Texture2D to a PNG file
            byte[] bytes = texture2D.EncodeToPNG();
            string directoryPath = Path.GetDirectoryName(scenePath);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            outputPath = Path.Combine(directoryPath, sceneName + "_Preview.png");
            File.WriteAllBytes(outputPath, bytes);

            // Refresh the AssetDatabase
            AssetDatabase.Refresh();
        }

        // Cleanup
        if (previewCamera != null)
        {
            previewCamera.targetTexture = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        return outputPath;
    }

}
#endif
