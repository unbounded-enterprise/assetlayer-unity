using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

[InitializeOnLoad]
public class ScriptLoader
{
    public class ScriptObject
    {
        public string scriptId;
        public string scriptFilename;
        public string slotId;
        public string value; // This is the URL to the script
    }

    private const string warningShownKey = "ScriptLoaderWarningShown";


    static ScriptLoader()
    {
        LoadAllScripts();
    }

    [MenuItem("Assetlayer/Download Required Slot Scripts")]
    public static void LoadAllScripts()
    {
        List<ScriptObject> scriptObjects = FetchScriptPaths();
        List<string> conflictFiles = DetectNamingConflicts(scriptObjects);

        if (conflictFiles.Count > 0 && !EditorPrefs.GetBool(warningShownKey, false))
        {
            string message = "Following scripts have naming conflicts elsewhere in the project or already exist in the target directory:\n\n" + string.Join("\n", conflictFiles);
            EditorUtility.DisplayDialog("Script Naming Conflicts Detected", message, "OK");

            // Mark the warning as shown
            EditorPrefs.SetBool(warningShownKey, true);

            // Optionally, return here to prevent further processing. Comment this if you want to continue downloading non-conflicting files.
            return;
        }

        foreach (var script in scriptObjects)
        {
            if (!conflictFiles.Contains(script.scriptFilename)) // Only download non-conflicting files
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(DownloadCoroutine(script));
            }
        }
    }

    private static List<ScriptObject> FetchScriptPaths()
    {
        // Here, you can provide a list of script objects
        // Replace with your logic if the list is fetched dynamically
        return new List<ScriptObject>
        {
            new ScriptObject
            {
                scriptId = "someUUID",
                scriptFilename = "TestScript.cs",
                slotId = "someSlotId",
                value = "https://asset-api-files-bucket.s3.amazonaws.com/dfed0f1c-ef79-4f0b-a240-c1e7489ec6cd.cs"
            },
            //... add more script objects as needed
        };
    }

    private static System.Collections.IEnumerator DownloadCoroutine(ScriptObject script)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(script.value))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download {script.value}: {www.error}");
            }
            else
            {
                SaveScript(www.downloadHandler.text, script.scriptFilename, script.slotId);
            }
        }
    }

    private static void SaveScript(string scriptContent, string filename, string slotId)
    {
        string directoryPath = Path.Combine("Assets", "AssetlayerUnitySDK", "Scripts", "RequiredSlotScripts", slotId);

        // Ensure the directory exists
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(directoryPath, filename);

        // Check if the file already exists
        if (File.Exists(filePath))
        {
            Debug.LogWarning($"{filename} already exists in slot {slotId}. Skipping download.");
            return;
        }

        File.WriteAllText(filePath, scriptContent);
        AssetDatabase.Refresh();
    }
    private static List<string> DetectNamingConflicts(List<ScriptObject> scriptObjects)
    {
        string[] allScriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        HashSet<string> projectScriptNames = new HashSet<string>();

        foreach (var scriptFile in allScriptFiles)
        {
            // Exclude scripts in the RequiredSlotScripts folder
            if (!scriptFile.Contains(Path.Combine("AssetlayerUnitySDK", "Scripts", "RequiredSlotScripts")))
            {
                projectScriptNames.Add(Path.GetFileName(scriptFile));
            }
        }

        List<string> conflicts = new List<string>();
        foreach (var scriptObj in scriptObjects)
        {
            if (projectScriptNames.Contains(scriptObj.scriptFilename))
            {
                conflicts.Add(scriptObj.scriptFilename);
            }
        }

        return conflicts;
    }



}
