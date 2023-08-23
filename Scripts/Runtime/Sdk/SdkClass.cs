using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Assetlayer.UtilityFunctions;

namespace Assetlayer.UnitySDK
{


    public class SDKClass
    {
        private const string HANDLE = "Incredible-jobling-73";
        private const string apiBase = "https://api-v2.assetlayer.com";

        public string APP_SECRET
        {
            get
            {
#if UNITY_EDITOR
                return SecretHolder.AppSecret;
#else
            return "d93d20ab48db93e9d010985a7bd74177"; 
#endif
            }
        }

        public string DID_TOKEN
        {
            get
            {
#if UNITY_EDITOR
                return SecretHolder.DidToken;
#else
                return SecurePlayerPrefs.GetSecureString("didtoken");
#endif
            }
        }

        public string APP_ID
        {
            get
            {
#if UNITY_EDITOR
                return SecretHolder.AssetlayerAppId;
#else
            return "64dc10469f07eb4ceb26ef14";
#endif
            }
        }




        public IEnumerator GetExpression(string assetId, string expressionName, System.Action<string> callback)
        {
            string urlWithAssetIdParameter = string.Format("{0}/api/v1/asset/info?assetId={1}", apiBase, assetId);
            UnityWebRequest request = UnityWebRequest.Get(urlWithAssetIdParameter);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                AssetInfoResponse response = JsonUtility.FromJson<AssetInfoResponse>(request.downloadHandler.text);

                if (response != null
                    && response.body != null
                    && response.body.assets != null
                    && response.body.assets.Length > 0
                    && response.body.assets[0].expressionValues != null
                    && response.body.assets[0].expressionValues.Count > 0)
                {
                    string currentPlatformAttributeName = UtilityFunctions.UtilityFunctions.GetCurrentPlatformExpressionAttribute();
                    Debug.Log("Current Plattform: " + currentPlatformAttributeName);
                    var expression = response.body.assets[0].expressionValues.FirstOrDefault(e => e.expression.expressionName == expressionName && e.expressionAttribute.expressionAttributeName == currentPlatformAttributeName);


                    if (expression != null)
                    {
                        callback?.Invoke(expression.value);
                    }
                    else
                    {
                        callback?.Invoke(response.body.assets[0].expressionValues[0].value);
                    }
                }
                else
                {
                    Debug.LogError("Failed to load expression - response structure does not match expected format");
                }
            }
        }


        public Mesh LoadOBJMesh(string objText)
        {
            var lines = objText.Split('\n');

            var verticesList = new List<Vector3>();
            var trianglesList = new List<int>();

            foreach (var line in lines)
            {
                if (line.StartsWith("v "))
                {
                    var parts = line.Split(' ');
                    var vertex = new Vector3(
                        float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3])
                    );
                    verticesList.Add(vertex);
                }
                else if (line.StartsWith("f "))
                {
                    var parts = line.Split(' ');
                    trianglesList.Add(int.Parse(parts[1]) - 1);
                    trianglesList.Add(int.Parse(parts[2]) - 1);
                    trianglesList.Add(int.Parse(parts[3]) - 1);
                }
            }

            var mesh = new Mesh();
            mesh.vertices = verticesList.ToArray();
            mesh.triangles = trianglesList.ToArray();
            mesh.RecalculateNormals();

            return mesh;
        }



        [System.Serializable]
        public class AssetInfoResponse
        {
            public int statusCode;
            public bool success;
            public Body body;

            [System.Serializable]
            public class Body
            {
                public Asset[] assets;
            }
        }

        public class CollectionData
        {
            public string collectionName;
            public string collectionImage;
            public string collectionBanner;
            public int maximum;
            public List<string> tags;
            public Dictionary<string, string> properties;
            public string type;
            public string slotId;
            public string royaltyRecipient;
            public string description;
        }


        public async Task<string> CreateCollection(string slotId, string collectionName, int maxSupply, string dataUrl)
        {
            string url = apiBase + "/api/v1/collection/new";

            var newCollectionData = new CollectionData
            {
                collectionName = collectionName,
                collectionImage = dataUrl,
                description = "",
                type = "Identical",
                slotId = slotId,
                maximum = maxSupply,
                tags = new List<string>(),
                properties = new Dictionary<string, string>(),
                collectionBanner = dataUrl,
                royaltyRecipient = HANDLE
            };

            string jsonBody = JsonUtility.ToJson(newCollectionData);
            Debug.Log(jsonBody);

            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);
            request.SetRequestHeader("didtoken", DID_TOKEN);
            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                Debug.Log("CreateCollection response: " + request.downloadHandler.text);
                CollectionCreationResponse response = JsonUtility.FromJson<CollectionCreationResponse>(request.downloadHandler.text);
                if (response.success)
                {
                    return response.body.collectionId;
                }
                else
                {
                    Debug.LogError("Failed to get Collection ID");
                    return null;
                }
            }
        }



        [System.Serializable]
        public class CollectionCreationResponse
        {
            public int statusCode;
            public bool success;
            public Body body;

            [System.Serializable]
            public class Body
            {
                public string collectionId;
            }
        }

        public class ExpressionValueData
        {
            public string expressionAttributeName;
            public string expressionName;
            public string collectionId;
            public string value;
        }

        [System.Serializable]
        public class ExpressionValueResponse
        {
            public int statusCode;
            public bool success;
        }

        public async Task<bool> UploadBundleExpression(string collectionId, string dataUrl, string expressionAttributeName = "AssetBundle", string expressionName = "AssetBundle")
        {
            string url = apiBase + "/api/v1/asset/expressionValues";

            var expressionValueData = new ExpressionValueData
            {
                expressionAttributeName = expressionAttributeName,
                expressionName = expressionName,
                collectionId = collectionId,
                value = dataUrl
            };

            string jsonBody = JsonUtility.ToJson(expressionValueData);
            Debug.Log("jsonbody: " + jsonBody);

            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return false;
            }
            else
            {
                Debug.Log("UploadBundleExpression response: " + request.downloadHandler.text);
                ExpressionValueResponse response = JsonUtility.FromJson<ExpressionValueResponse>(request.downloadHandler.text);
                if (response.success)
                {
                    return true;
                }
                else
                {
                    Debug.LogError("Failed to upload bundle expression");
                    return false;
                }
            }
        }


        public class ExpressionData
        {
            public string slotId;
            public string expressionTypeId;
            public string expressionName;
            public string description;
        }

        [System.Serializable]
        public class ExpressionResponse
        {
            public int statusCode;
            public bool success;
            public Body body;

            [System.Serializable]
            public class Body
            {
                public string expressionId;
            }
        }

        public async Task<string> CreateExpression(string slotId, string expressionTypeId, string expressionName, string description)
        {
            string url = apiBase + "/api/v1/slot/expressions/new";

            var expressionData = new ExpressionData
            {
                slotId = slotId,
                expressionTypeId = expressionTypeId,
                expressionName = expressionName,
                description = description
            };

            string jsonBody = JsonUtility.ToJson(expressionData);
            Debug.Log(jsonBody);

            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);
            request.SetRequestHeader("didtoken", DID_TOKEN);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                Debug.Log("CreateExpression response: " + request.downloadHandler.text);
                ExpressionResponse response = JsonUtility.FromJson<ExpressionResponse>(request.downloadHandler.text);
                if (response.success)
                {
                    return response.body.expressionId;
                }
                else
                {
                    Debug.LogError("Failed to create expression");
                    return null;
                }
            }
        }


        public class MintResponse
        {
            public int statusCode;
            public bool success;
        }

        public class MintData
        {
            public string collectionId;
            public int number;
        }

        public async Task<bool> Mint(string collectionId, int amount)
        {
            string url = apiBase + "/api/v1/asset/mint";

            var mintData = new MintData { collectionId = collectionId, number = amount };

            string jsonBody = JsonUtility.ToJson(mintData);

            Debug.Log("Json body mint: " + jsonBody);

            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.uploadHandler.contentType = "application/json";
            request.downloadHandler = new DownloadHandlerBuffer();

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);
            request.SetRequestHeader("didtoken", DID_TOKEN);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return false;
            }
            else
            {
                Debug.Log("Mint response: " + request.downloadHandler.text);
                MintResponse response = JsonUtility.FromJson<MintResponse>(request.downloadHandler.text);
                return response.success;
            }
        }



        public class SlotExpressionResponse
        {
            public int statusCode;
            public bool success;
            public Body body;

            [System.Serializable]
            public class Body
            {
                public Expression[] expressions;

                [System.Serializable]
                public class Expression
                {
                    public string expressionId;
                    public ExpressionType expressionType;
                    public string expressionName;
                    public string slotId;

                    [System.Serializable]
                    public class ExpressionType
                    {
                        public ExpressionAttribute[] expressionAttributes;
                        public string expressionTypeName;
                        public string expressionTypeId;

                        [System.Serializable]
                        public class ExpressionAttribute
                        {
                            public string expressionAttributeName;
                            public string expressionAttributeId;
                        }
                    }
                }
            }
        }

        public async Task<string> GetAssetExpression(string slotId)
        {
            string url = $"{apiBase}/api/v1/slot/expressions?slotId={slotId}";

            UnityWebRequest request = UnityWebRequest.Get(url);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);

            await request.SendWebRequest();


            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                Debug.Log("GetAssetExpression response: " + request.downloadHandler.text);
                SlotExpressionResponse response = JsonUtility.FromJson<SlotExpressionResponse>(request.downloadHandler.text);
                Debug.Log("getting expression res" + response.body.expressions);
                if (response.success && response.body.expressions != null)
                {
                    foreach (var expression in response.body.expressions)
                    {
                        if (expression.expressionType.expressionTypeName == "AssetBundle")
                        {
                            Debug.Log("Expression found: " + expression.expressionId);
                            return expression.expressionId;
                        }
                    }
                }

                Debug.LogError("No AssetBundle expression found");
                return null;
            }
        }



        public async Task<string> GetMenuViewExpression(string slotId)
        {
            string url = $"{apiBase}/api/v1/slot/expressions?slotId={slotId}";

            UnityWebRequest request = UnityWebRequest.Get(url);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                Debug.Log("GetSlotExpression response: " + request.downloadHandler.text);
                SlotExpressionResponse response = JsonUtility.FromJson<SlotExpressionResponse>(request.downloadHandler.text);
                Debug.Log("getting expression res" + response.body.expressions);
                if (response.success && response.body.expressions != null)
                {
                    foreach (var expression in response.body.expressions)
                    {
                        if (expression.expressionName == "Menu View")
                        {
                            Debug.Log("Menu View found: " + expression.expressionId);
                            return expression.expressionId;
                        }
                    }
                }

                Debug.LogError("No Menu View expression found");
                return null;
            }
        }

        


        public async Task<List<Asset>> GetAssetBalance(string slotId, bool idOnly, bool countsOnly)
        {
            string url = $"{apiBase}/api/v1/asset/slots?slotIds=[{slotId}]&idOnly={idOnly.ToString().ToLower()}&countsOnly={countsOnly.ToString().ToLower()}";

            UnityWebRequest request = UnityWebRequest.Get(url);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);
            request.SetRequestHeader("didtoken", DID_TOKEN);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                Debug.Log("GetAssetDetails response: " + request.downloadHandler.text);
                AssetResponse response = JsonUtility.FromJson<AssetResponse>(request.downloadHandler.text);
                Debug.Log("Getting Asset details res" + response.body.assets);
                if (response.success && response.body.assets != null)
                {
                    return response.body.assets;
                }

                Debug.LogError("No Asset details found");
                return null;
            }
        }

        public async Task<List<Asset>> GetBalanceOfCollection(string collectionId, bool idOnly = false, bool countsOnly = false, string rangeFrom = null, string rangeTo = null, string serials = null)
        {
            string rangeString = "";
            if (rangeFrom != null && rangeTo != null)
            {
                rangeString = $"&range={rangeFrom}-{rangeTo}";
            }
            else if (serials != null)
            {
                rangeString = $"&serials={serials}";
            }

            string url = $"{apiBase}/api/v1/asset/collection?collectionId={collectionId}&idOnly={idOnly.ToString().ToLower()}&countsOnly={countsOnly.ToString().ToLower()}{rangeString}";

            UnityWebRequest request = UnityWebRequest.Get(url);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);
            request.SetRequestHeader("didtoken", DID_TOKEN);
            Debug.Log("set header to didtoken: " + DID_TOKEN);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                Debug.Log("GetAssetCollection response: " + request.downloadHandler.text);
                AssetResponse response = JsonUtility.FromJson<AssetResponse>(request.downloadHandler.text);
                Debug.Log("Getting Asset collection res" + response.body.assets);
                if (response.success && response.body.assets != null)
                {
                    return response.body.assets;
                }

                Debug.LogError("No Asset collection found");
                return null;
            }
        }


        public async Task<string[]> GetAppSlots()
        {
            string url = $"{apiBase}/api/v1/app/info?appId={APP_ID}";
            Debug.Log("DID_TOKEN APP SLOTS: " + DID_TOKEN);
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                Debug.Log("GetAppSlots response: " + request.downloadHandler.text);
                AppInfoResponse response = JsonUtility.FromJson<AppInfoResponse>(request.downloadHandler.text);
                if (response.success && response.body.app != null)
                {
                    Debug.Log($"Number of slotIds: {response.body.app.slots.Length}");
                    Debug.Log($"Slot Ids: {string.Join(", ", response.body.app.slots)}");


                    return response.body.app.slots;
                }

                Debug.LogError("No slots found");
                return null;
            }
        }

        public async Task<SlotInfo> GetSlotInfo(string slotId)
        {
            string url = $"{apiBase}/api/v1/slot/info?slotId={slotId}";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                Debug.Log("GetSlotInfo response: " + request.downloadHandler.text);
                SlotInfoResponse response = JsonUtility.FromJson<SlotInfoResponse>(request.downloadHandler.text);
                Debug.Log("seriazed slot info: " + response);
                if (response.success && response.body.slot != null)
                {
                    response.body.slot.isForeignSlot = response.body.slot.appId != APP_ID;
                    return response.body.slot;
                }

                Debug.LogError("Failed to fetch slot info");
                return null;
            }
        }

        public async Task<List<Collection>> GetCollectionInfo(List<string> collectionIds)
        {
            try
            {
                Debug.Log("GetCollectionInfo, for ids: " + collectionIds);
                // Constructing the request URL
                string collectionIdsQueryParam = string.Join("&", collectionIds.Select(id => $"collectionIds={id}").ToArray());
                string url = $"{apiBase}/api/v1/collection/info?{collectionIdsQueryParam}";

                UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("appsecret", APP_SECRET);

                await request.SendWebRequest();

                Debug.Log("GetCollectionInfo response: " + request.downloadHandler.text);
                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError(request.error);
                    return null;
                }
                else
                {

                    CollectionResponse response = JsonUtility.FromJson<CollectionResponse>(request.downloadHandler.text);
                    Debug.Log("Jsonrespons: " + response);
                    if (response.success && response.body.collections != null)
                    {
                        return response.body.collections;
                    }

                    Debug.LogError("Failed to fetch collection info");
                    return null;
                }
            } catch(Exception e)
            {
                Debug.LogError("error fetching collection info: " + e.Message);
                return null;
            }
        }

        [Serializable] 
        class UserRegisterData
        {
            public string otp;
        }

        [Serializable]
        class UserRegisterResponse
        {
            public int statusCode;
            public bool success;
            [Serializable]
            public class UserRegisterBody
            {
                public string otp;
            }
            public UserRegisterBody body;
        }

        [Serializable]
        public class UserRegisterBody2
        {
            public string _id;
            public string email;
            public string handle;
        }

        class UserRegisterResponse2
        {
            public int statusCode;
            public bool success;
            
            public UserRegisterBody2 body;
        }
        public async Task<string> GetOTP(string token) 
        {
            string url = apiBase + "/api/v1/user/register";

            var registerData = new UserRegisterData();

            string jsonBody = JsonUtility.ToJson(registerData);

            Debug.Log("Json body mint: " + jsonBody);

            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.uploadHandler.contentType = "application/json";
            request.downloadHandler = new DownloadHandlerBuffer();

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);
            request.SetRequestHeader("didtoken", token);

            await request.SendWebRequest();

            UserRegisterResponse response = JsonUtility.FromJson<UserRegisterResponse>(request.downloadHandler.text);
            return response.body.otp;

        }

        public async Task<UserRegisterBody2> RegisterUser(string otp)
        {
            string url = apiBase + "/api/v1/user/register";

            var registerData = new UserRegisterData();
            registerData.otp = otp;

            string jsonBody = JsonUtility.ToJson(registerData);

            Debug.Log("Json body mint: " + jsonBody);

            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.uploadHandler.contentType = "application/json";
            request.downloadHandler = new DownloadHandlerBuffer();

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("appsecret", APP_SECRET);
            request.SetRequestHeader("didtoken", DID_TOKEN);

            await request.SendWebRequest();

            UserRegisterResponse2 response = JsonUtility.FromJson<UserRegisterResponse2>(request.downloadHandler.text);
            return response.body;

        }

        [Serializable]
        public class CollectionResponse
        {
            public int statusCode;
            public bool success;
            public CollectionResponseBody body;
        }


        [Serializable]
        public class CollectionResponseBody
        {
            public List<Collection> collections;
        }


        [Serializable]
        public class SlotInfo
        {
            public string slotId;
            public string slotName;
            public string slotImage;
            public string description;
            public string appId;
            public bool acceptingCollections;
            public bool isPublic;
            public string collectionTypes;
            public long createdAt;
            public long updatedAt;
            public List<string> collections;
            public List<string> expressions;
            public bool isForeignSlot;
        }

        [Serializable]
        public class SlotInfoResponseBody
        {
            public SlotInfo slot;
        }

        [Serializable]
        public class SlotInfoResponse
        {
            public int statusCode;
            public bool success;
            public SlotInfoResponseBody body;
        }

        public enum BuildPlatform
        {
            iOS,
            Android,
            StandaloneWindows,
            StandaloneOSX,
            WebGL
        }




        [Serializable]
        public class AppInfoResponse
        {
            public int statusCode;
            public bool success;
            public Body body;

            [System.Serializable]
            public class Body
            {
                public App app;

                [System.Serializable]
                public class App
                {
                    public string[] slots;
                }
            }
        }
    }

    [Serializable]
    public class ExpressionAttribute
    {
        public string expressionAttributeName;
        public string expressionAttributeId;
    }

    [Serializable]
    public class Expression
    {
        public string expressionName;
        public string expressionId;
    }

    [Serializable]
    public class ExpressionValue
    {
        public ExpressionAttribute expressionAttribute;
        public Expression expression;
        public string value;
    }

    [Serializable]
    public class User
    {
        string userId;
        string handle;
    }

    [Serializable]
    public class Asset
    {
        public string assetId;
        public int serial;
        public string collectionId;
        public string collectionName;
        public User user;
        public List<ExpressionValue> expressionValues;
        public string properties;
        public AssetBundle loadedAssetBundle;
    }

    [Serializable]
    public class AssetResponseBody
    {
        public List<Asset> assets;
    }

    [Serializable]
    public class AssetResponse
    {
        public int statusCode;
        public bool success;
        public AssetResponseBody body;
    }

    [Serializable]
    public class Slot
    {
        public string slotId;
        public string slotName;
        public string slotImage;
        public string description;
        public string appId;
        public List<string> collections;
        public List<string> expression;
        public string collectionName;
    }

    [Serializable]
    public class Collection
    {
        public string collectionId;
        public string collectionName;
        public string collectionImage;
        public string collectionBanner;
        public string description;
        public Creator creator;
        public string slotId;
        public int maximum;
        public int minted;
        public List<string> tags;
        public RoyaltyRecipient royaltyRecipient;
        public string type;
        public Dictionary<string, object> properties;
        public string status;
        public long createdAt;
        public long updatedAt;
        public List<ExpressionValue> exampleExpressionValues;
    }

    [Serializable]
    public class Creator
    {
        public string userId;
        public string handle;
    }

    [Serializable]
    public class RoyaltyRecipient
    {
        public string userId;
        public string handle;
    }

}