#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// This class is used to hold sensitive data for your project when it's open in Unity Editor. 
// It should not be used to hold sensitive data for your built game.
// the #if UNITY_EDITOR ensures this is not included in your build.
// Instead, secrets should only be handled on the server side in production.
[InitializeOnLoad]
public static class SecretHolder
{
    // These variables hold the secret app key and asset layer app ID
    public static string AppSecret;
    public static string AssetlayerAppId;
    public static string DidToken;
    public static string UserDidToken;
    public static string AssetlayerHandle;

    // This static constructor is called once when the scripts are loaded or the game starts in the editor.
    static SecretHolder()
    {
        try
        {
            // Get the path to the user's home directory
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            // Define the path to the .env file
            string envPath = Path.Combine(homePath, ".env");

            // If the .env file exists at the defined path
            if (File.Exists(envPath))
            {
                // Read all lines in the .env file
                string[] lines = File.ReadAllLines(envPath);
                // Loop through each line
                foreach (string line in lines)
                {
                    // Split the line into key and value at the equals sign
                    string[] parts = line.Split(new char[] { '=' }, 2); // values might contain = as well

                    // If the line correctly splits into a key and value
                    Debug.Log("line: " + line + "parts: " + parts[0] + ":" + parts[1]);
                    if (parts.Length == 2)
                    {

                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        Debug.Log("key" + key);

                        // If the key matches "APP_SECRET", store the value in the AppSecret variable
                        if (key == "ASSETLAYER_APP_SECRET")
                        {
                            AppSecret = value;
                        }
                        // If the key matches "ASSETLAYER_APP_ID", store the value in the AssetlayerAppId variable
                        else if (key == "ASSETLAYER_APP_ID")
                        {
                            AssetlayerAppId = value;
                        }
                        else if (key == "ASSETLAYER_DID_TOKEN")
                        {
                            DidToken = value;
                            Debug.Log("didtoken read" + value);
                        }
                        else if (key == "ASSETLAYER_HANDLE")
                        {
                            AssetlayerHandle = value;
                            Debug.Log("handle read" + value);
                        }
                    }
                }
            }
            else
            {
                // If the .env file doesn't exist at the defined path, log an error to the console
                UnityEngine.Debug.LogError("Could not find .env file at: " + envPath);
            }
        }
        catch (Exception ex)
        {
            // If there's any error while reading the .env file or storing the secrets, log it to the console
            UnityEngine.Debug.LogError("Error while loading secrets: " + ex);
        }
    }
}
#endif