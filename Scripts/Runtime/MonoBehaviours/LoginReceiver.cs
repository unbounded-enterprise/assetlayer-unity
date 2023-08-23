using UnityEngine;
using Assetlayer.UtilityFunctions;
using UnityEngine.SceneManagement;
using System.Net;
using Assetlayer.Login;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System;
using PimDeWitte.UnityMainThreadDispatcher;
using Assetlayer.UnitySDK;
using Newtonsoft.Json.Linq;

public class LoginReceiver : MonoBehaviour
{
    public string SceneToLoadOnLogin;
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
    private HttpServer httpServer;

    void Start()
    {
        string savedDidToken = SecurePlayerPrefs.GetSecureString("didtoken");
        SDKClass sdk = new SDKClass();
        try
        {
            bool tokenIsValid = IsDIDTokenValid(savedDidToken);
            if (!tokenIsValid)
            {
                SecurePlayerPrefs.RemoveSecureString("didtoken");
                savedDidToken = null;
            }
        } catch(Exception ex)
        {
            Debug.Log("token no longer valid");
            SecurePlayerPrefs.RemoveSecureString("didtoken");
            savedDidToken = null;
        }
        if (string.IsNullOrEmpty(savedDidToken))
        {
            httpServer = new HttpServer();
            httpServer.onRequestReceived += HandleLoginReceived;
            httpServer.Start();
        } else
        {

            SetDIDToken(savedDidToken);
        }
        
    }


    public bool IsDIDTokenValid(string DIDToken)
    {
        try
        {
            // Decode Base64 string tuple to get 'proof' and 'claim'
            byte[] decodedBytes = Convert.FromBase64String(DIDToken);
            string decodedText = Encoding.UTF8.GetString(decodedBytes);
            JArray decodedJsonArray = JArray.Parse(decodedText);

            // Extract 'proof' and 'claim'
            string proof = decodedJsonArray[0].ToString();
            string claim = decodedJsonArray[1].ToString();

            // Parse 'claim' to JObject
            JObject claimObj = JObject.Parse(claim);

            // Check 'ext' (Expiration timestamp)
            long expirationTimestamp = claimObj["ext"].Value<long>();

            // Get current UTC time in seconds
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Check if token is expired
            if (currentTimestamp > expirationTimestamp)
            {
                Debug.Log("Token has expired.");
                return false;
            }


            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while validating the DID token: {ex.Message}");
            return false;
        }
    }


    void OnDestroy()
    {
        if (httpServer != null)
        {
            httpServer.Stop();
        }
    }
    public void HandleLoginReceived(HttpListenerContext context)
    {
        Debug.Log("Request received2");
        string encryptedToken = "";
        // Read query parameters
        try
        {
            encryptedToken = context.Request.QueryString["token"];
            Debug.Log("enrytpedToken" + encryptedToken);
        } catch(Exception ex)
        {
            Debug.Log(ex.ToString());
        }
        
    
        // Initialize decryption key and IV - make sure this matches what you used in JS
        string key = "1234567812345678";
        string iv = "1234567812345678";
        string decryptedToken;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedToken)))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        decryptedToken = srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        // Create the response string based on the decrypted token
        string responseText = "Hello from MonoBehaviour. Your decrypted token is: " + decryptedToken;
        Debug.Log("response decrypted: " + responseText);
        try
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseText);

            var response = context.Response;
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;
        
            if (decryptedToken != null) {
                UnityMainThreadDispatcher.Instance().Enqueue(() => SetDIDToken(decryptedToken));
            } else {
                Debug.Log("token is null");    
            }

            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);

            output.Close();

        }
        catch (Exception ex)
        {
            Debug.Log("error sending a response" + ex.ToString());
        }
    }


#endif

    public void SetDIDToken(string token)
    {
        SecurePlayerPrefs.SetSecureString("didtoken", token);
        Debug.Log("Did token recetive" + token.Substring(20));
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
        if (httpServer != null) { 
            httpServer.Stop();
        }
#endif
        // Load the desired scene after successful login
        SceneManager.LoadScene(SceneToLoadOnLogin);
    }

    
}
