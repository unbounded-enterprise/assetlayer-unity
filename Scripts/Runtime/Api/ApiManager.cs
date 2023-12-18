using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using AssetLayer.SDK;
using AssetLayer.SDK.Assets;
using AssetLayer.SDK.Collections;
using AssetLayer.SDK.Currencies;
using AssetLayer.SDK.Expressions;
using AssetLayer.SDK.Slots;
using AssetLayer.SDK.Users;
using AssetLayer.SDK.Utils;
using AssetLayer.SDK.Apps;
using Unity.VisualScripting;
using Newtonsoft.Json;

namespace AssetLayer.Unity
{


    public class ApiManager
    {
#if UNITY_EDITOR
        private string HANDLE = SecretHolder.AssetlayerHandle;
#else
        private const string HANDLE = "Your Assetlayer Handle, not needed if not in editor though";
#endif

        public string apiBase
        {
            get
            {
#if UNITY_EDITOR
                return AssetLayerSDK.APIURL;
#else
            return "https://yourgameserver.com/api";
#endif
            }
        }

        public string gameServerUrl
        {
            get
            {
                return "https://yourgameserver.com/api";
            }
        }

        public string APP_SECRET
        {
            get
            {
#if UNITY_EDITOR
                return SecretHolder.AppSecret;
#else
            return "hardcoded appsecret"; 
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
            return "hardcoded appid";
#endif
            }
        }

        public ApiManager()
        {
            InitSDKCheck();
        }

        private void InitSDKCheck()
        {
            if (!AssetLayerSDK.Initialized)
            {

#if UNITY_EDITOR
                AssetLayerSDK.Initialize(new AssetLayerConfig { baseUrl = apiBase, appSecret = string.IsNullOrEmpty(APP_SECRET) ? null : APP_SECRET, didToken = DID_TOKEN });
#else
                AssetLayerSDK.Initialize(new AssetLayerConfig { baseUrl = apiBase, didToken = DID_TOKEN });
#endif
            }
        }

        public IEnumerator GetExpression(string assetId, string expressionName, System.Action<string> callback)
        {
            InitSDKCheck();

            GetAssetProps props = new GetAssetProps
            {
                assetId = assetId
            };

            Task<SDK.Assets.Asset> getAssetTask = AssetLayerSDK.Assets.GetAsset(props);

            yield return new WaitUntil(() => getAssetTask.IsCompleted);

            SDK.Assets.Asset assetInfo = getAssetTask.Result;

            if (assetInfo == null || assetInfo.expressionValues == null || assetInfo.expressionValues.Count == 0)
            {
                Debug.LogError("Failed to load expression - response structure does not match expected format");
                callback?.Invoke(null);
                yield break;
            }

            // Find the appropriate expression value
            string currentPlatformAttributeName = UtilityFunctions.GetCurrentPlatformExpressionAttribute();

            var expression = assetInfo.expressionValues.FirstOrDefault(
                e => e.expression.expressionName == expressionName &&
                e.expressionAttribute.expressionAttributeName == currentPlatformAttributeName);

            if (expression != null)
            {
                callback?.Invoke(expression.value);
            }
            else
            {
                callback?.Invoke(assetInfo.expressionValues[0].value);
            }
        }

        public IEnumerator GetExpressionOld(string assetId, string expressionName, System.Action<string> callback)
        {
            string urlWithAssetIdParameter = string.Format("{0}/asset/info?assetId={1}", apiBase, assetId);
            UnityWebRequest request = UnityWebRequest.Get(urlWithAssetIdParameter);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET))
            {
                request.SetRequestHeader("appsecret", APP_SECRET);
            }

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
                    string currentPlatformAttributeName = UtilityFunctions.GetCurrentPlatformExpressionAttribute();
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
            string url = apiBase + "/collection/new";

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
                collectionBanner = "",
                royaltyRecipient = HANDLE
            };

            string jsonBody = JsonUtility.ToJson(newCollectionData);
            Debug.Log(jsonBody);

            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }
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
            string url = apiBase + "/asset/expressionValues";

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
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }

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
            string url = apiBase + "/slot/expressions/new";

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
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }
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
            string url = apiBase + "/asset/mint";

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
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }
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

        public async Task<string> GetAssetExpressionOld(string slotId)
        {
            string url = $"{apiBase}/slot/expressions?slotId={slotId}";

            UnityWebRequest request = UnityWebRequest.Get(url);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }

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

        public async Task<string> GetAssetExpression(string slotId)
        {
            InitSDKCheck();

            GetSlotExpressionsProps props = new GetSlotExpressionsProps
            {
                slotId = slotId
            };

            Task<List<SDK.Expressions.Expression>> getSlotExpressionTask = AssetLayerSDK.Slots.GetSlotExpressions(props);
            await getSlotExpressionTask;

            // Extract the result.
            List<SDK.Expressions.Expression> expressions = getSlotExpressionTask.Result;


            foreach (var expression in expressions)
            {
                if (expression.expressionType.expressionTypeName == "AssetBundle")
                {
                    Debug.Log("Expression found: " + expression.expressionId);
                    return expression.expressionId;
                }
            }

            Debug.LogError("No AssetBundle expression found");
            return null;
        }




        public async Task<string> GetMenuViewExpressionOld(string slotId)
        {
            string url = $"{apiBase}/slot/expressions?slotId={slotId}";

            UnityWebRequest request = UnityWebRequest.Get(url);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }

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

        public async Task<string> GetMenuViewExpression(string slotId)
        {
            // Initialize the SDK if it hasn't been initialized.
            InitSDKCheck();
            Debug.Log("Init done");

            // Create the properties object for the SDK call.
            GetSlotExpressionsProps props = new GetSlotExpressionsProps
            {
                slotId = slotId
            };

            // Make the SDK call and await its completion.
            Task<List<SDK.Expressions.Expression>> getSlotExpressionTask = AssetLayerSDK.Slots.GetSlotExpressions(props);
            await getSlotExpressionTask;

            // Extract the result.
            List<SDK.Expressions.Expression> expressions = getSlotExpressionTask.Result;

            // Search for the Menu View expression.
            foreach (var expression in expressions)
            {
                if (expression.expressionName == "Menu View")
                {
                    Debug.Log("Menu View found: " + expression.expressionId);
                    return expression.expressionId;
                }
            }

            Debug.LogError("No Menu View expression found");
            return null;
        }


        public async Task<object> GetAssetBalance(string slotId, bool idOnly, bool countsOnly)
        {
            InitSDKCheck();

            GetUserSlotAssetsProps props = new GetUserSlotAssetsProps
            {
                slotId = slotId,
                idOnly = idOnly,
                countsOnly = countsOnly,
            };

            // var tuple = await AssetLayerSDK.Assets.GetUserSlotAssets(props);
            Debug.Log("shortly before getting the request for slot balance");
            try
            {
                var (assetList, assetIdList, assetCounts) = await AssetLayerSDK.Assets.GetUserSlotAssets(props);

                if (countsOnly)
                {
                    if (assetCounts != null)
                    {
                        Dictionary<string, long> counts = assetCounts;

                        return counts;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (idOnly)
                {
                    if (assetIdList != null)
                    {
                        return assetIdList;
                    }
                }
                else
                {
                    if (assetList != null)
                    {
                        return assetList;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("balance request error: " + e.Message);
            }
            Debug.LogError("No Asset details found");
            return null;
        }



        public async Task<List<Asset>> GetAssetBalanceOld(string slotId, bool idOnly, bool countsOnly)
        {
            string url = $"{apiBase}/asset/slots?slotIds=[{slotId}]&idOnly={idOnly}&countsOnly={countsOnly}";

            UnityWebRequest request = UnityWebRequest.Get(url);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }
            request.SetRequestHeader("didtoken", DID_TOKEN);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                AssetResponse response = JsonUtility.FromJson<AssetResponse>(request.downloadHandler.text);
                if (response.success && response.body.assets != null)
                {
                    return response.body.assets;
                }

                Debug.LogError("No Asset details found");
                return null;
            }
        }

        public async Task<List<Asset>> GetBalanceOfCollectionOld(string collectionId, bool idOnly = false, bool countsOnly = false, string rangeFrom = null, string rangeTo = null, string serials = null)
        {
            string rangeString = "";
            if (!string.IsNullOrEmpty(rangeFrom) && !string.IsNullOrEmpty(rangeTo))
            {
                rangeString = $"&range={rangeFrom}-{rangeTo}";
            }
            else if (!string.IsNullOrEmpty(serials))
            {
                rangeString = $"&serials={serials}";
            }

            string url = $"{apiBase}/asset/collection?collectionId={collectionId}&idOnly={idOnly}&countsOnly={countsOnly}{rangeString}";

            UnityWebRequest request = UnityWebRequest.Get(url);

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }
            request.SetRequestHeader("didtoken", DID_TOKEN);

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                AssetResponse response = JsonUtility.FromJson<AssetResponse>(request.downloadHandler.text);
                if (response.success && response.body.assets != null)
                {
                    return response.body.assets;
                }

                Debug.LogError("No Asset collection found");
                return null;
            }
        }

        public async Task<List<Asset>> GetBalanceOfCollection(string collectionId, bool idOnly = false, bool countsOnly = false, string rangeFrom = null, string rangeTo = null, string serials = null)
        {
            // Initialize the SDK if it hasn't been initialized.
            InitSDKCheck();

            // Create the properties object for the SDK call.
            GetUserCollectionAssetsProps props = new GetUserCollectionAssetsProps
            {
                collectionId = collectionId,
                idOnly = idOnly,
                countsOnly = countsOnly,
            };

            // Only add range if rangeFrom and rangeTo are not null or empty.
            if (!string.IsNullOrEmpty(rangeFrom) || !string.IsNullOrEmpty(rangeTo))
            {
                props.range = (rangeFrom ?? "") + "-" + (rangeTo ?? "");
            }

            // Only add serials if it is not null or empty.
            if (!string.IsNullOrEmpty(serials))
            {
                props.serials = serials;
            }

            // Make the SDK call and await its completion.
            Task<(List<AssetLayer.SDK.Assets.Asset>, List<AssetLayer.SDK.Assets.AssetIdOnly>, Dictionary<string, long>)> getBalanceOfCollectionTask = AssetLayerSDK.Assets.GetUserCollectionAssets(props);
            await getBalanceOfCollectionTask;

            // Extract the result.
            var (assetList, assetIdList, assetCounts) = getBalanceOfCollectionTask.Result;
            if (countsOnly)
            {
                return new List<Asset>();
            }
            else if (idOnly)
            {
                return new List<Asset>();
            }
            else
            {
                if (assetList != null)
                {
                    // Cast the list to List<Asset> if Asset is derived from AssetLayer.SDK.Assets.Asset
                    return assetList.Select(asset => new Asset(asset)).ToList();
                }
            }

            Debug.LogError("No Asset details found");
            return null;
        }

        public async Task<List<Asset>> GetBalanceOfSlot(string slotId)
        {
            // Initialize the SDK if it hasn't been initialized.
            InitSDKCheck();

            // Create the properties object for the SDK call.
            GetUserSlotAssetsProps props = new GetUserSlotAssetsProps
            {
                slotId = slotId,
                idOnly = false,
                countsOnly = false,
            };


            // Make the SDK call and await its completion.
            Task<(List<AssetLayer.SDK.Assets.Asset>, List<AssetLayer.SDK.Assets.AssetIdOnly>, Dictionary<string, long>)> getBalanceOfSlotTask = AssetLayerSDK.Assets.GetUserSlotAssets(props);
            await getBalanceOfSlotTask;

            // Extract the result.
            var (assetList, assetIdList, assetCounts) = getBalanceOfSlotTask.Result;

            if (assetList != null)
            {
                Debug.Log("Getting Slot Asset details: " + assetList);
                // Cast the list to List<Asset> if Asset is derived from AssetLayer.SDK.Assets.Asset
                return assetList.Select(asset => new Asset(asset)).ToList();
            }


            Debug.LogError("No Asset details found");
            return null;
        }

        public async Task<List<Collection>> GetAllAssetsOfSlot(string slotId)
        {
            // Initialize the SDK if it hasn't been initialized.
            InitSDKCheck();

            GetSlotCollectionsProps props = new GetSlotCollectionsProps
            {
                slotId = slotId
            };

            List<string> collectionIds = await AssetLayerSDK.Slots.GetSlotCollectionIds(props);

            GetCollectionsProps collectionProps = new GetCollectionsProps
            {
                collectionIds = collectionIds
            };



            // Make the SDK call and await its completion.
            Task<List<AssetLayer.SDK.Collections.Collection>> getBalanceOfSlotTask = AssetLayerSDK.Collections.GetCollections(collectionProps);
            await getBalanceOfSlotTask;

            // Extract the result.
            var assetList = getBalanceOfSlotTask.Result;

            if (assetList != null)
            {
                Debug.Log("Getting Slot Asset details: " + assetList);
                // Cast the list to List<Asset> if Asset is derived from AssetLayer.SDK.Assets.Asset
                return assetList;
            }


            Debug.LogError("No Asset details found");
            return null;
        }


        public async Task<string[]> GetAppSlotsOld()
        {
            string url = $"{apiBase}/app/info?appId={APP_ID}";
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }

            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }
            else
            {
                AppInfoResponse response = JsonUtility.FromJson<AppInfoResponse>(request.downloadHandler.text);
                if (response.success && response.body.app != null)
                {
                    return response.body.app.slots;
                }

                Debug.LogError("No slots found");
                return null;
            }
        }

        public async Task<string[]> GetAppSlots()
        {
            InitSDKCheck();

            AppInfoProps props = new AppInfoProps
            {
                appId = APP_ID
            };
            var dummy = props.appId;
            App appInfo = (await AssetLayerSDK.Apps.Info(props)).Item1;
            return appInfo.slots.ToArray();
        }




        public async Task<SlotInfo> GetSlotInfoOld(string slotId)
        {
            Debug.Log("get slot info");
            string url = $"{apiBase}/slot/info?slotId={slotId}";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }

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

        public async Task<SlotInfo> GetSlotInfo(string slotId)
        {
            InitSDKCheck();  // Initialize SDK

            GetSlotProps props = new GetSlotProps
            {
                slotId = slotId
            };

            SDK.Slots.Slot slotInfoBase = await AssetLayerSDK.Slots.GetSlot(props);
            SlotInfo slot = new SlotInfo(slotInfoBase);

            slot.isForeignSlot = slot.appId != APP_ID;
            return slot;


        }

        public async Task<List<SDK.Collections.Collection>> GetSlotCollections(string slotId)
        {
            InitSDKCheck();

            GetSlotCollectionsProps props = new GetSlotCollectionsProps
            {
                slotId = slotId
            };
            List<SDK.Collections.Collection> collections = await AssetLayerSDK.Slots.GetSlotCollections(props);
            Debug.Log("collections response1: " + collections);

            return collections;

        }




        public async Task<List<Collection>> GetCollectionInfoOld(List<string> collectionIds)
        {
            try
            {
                Debug.Log("GetCollectionInfo, for ids: " + collectionIds);
                // Constructing the request URL
                string collectionIdsQueryParam = string.Join("&", collectionIds.Select(id => $"collectionIds={id}").ToArray());
                string url = $"{apiBase}/collection/info?{collectionIdsQueryParam}";

                UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }

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
            }
            catch (Exception e)
            {
                Debug.LogError("error fetching collection info: " + e.Message);
                return null;
            }
        }

        public async Task<List<Collection>> GetCollectionInfo(List<string> collectionIds)
        {
            InitSDKCheck();
            GetCollectionsProps props = new GetCollectionsProps
            {
                collectionIds = collectionIds
            };

            List<SDK.Collections.Collection> collections = await AssetLayerSDK.Collections.GetCollections(props);
            return collections;
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
            string url = apiBase + "/user/register";

            var registerData = new UserRegisterData();

            string jsonBody = JsonUtility.ToJson(registerData);


            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.uploadHandler.contentType = "application/json";
            request.downloadHandler = new DownloadHandlerBuffer();

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }
            request.SetRequestHeader("didtoken", token);

            await request.SendWebRequest();

            UserRegisterResponse response = JsonUtility.FromJson<UserRegisterResponse>(request.downloadHandler.text);
            return response.body.otp;

        }

        public async Task<UserRegisterBody2> RegisterUser(string otp)
        {
            string url = apiBase + "/user/register";

            var registerData = new UserRegisterData();
            registerData.otp = otp;

            string jsonBody = JsonUtility.ToJson(registerData);


            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.uploadHandler.contentType = "application/json";
            request.downloadHandler = new DownloadHandlerBuffer();

            // Specify headers
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(APP_SECRET)) { request.SetRequestHeader("appsecret", APP_SECRET); }
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
        public class SlotInfo : SDK.Slots.Slot
        {
            public bool isForeignSlot;

            public SlotInfo(SDK.Slots.Slot slot)
            {
                UtilityFunctions.CopyProperties(slot, this);
            }
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
    public class User
    {
        string userId;
        string handle;
    }

    [Serializable]
    public class Asset : SDK.Assets.Asset
    {
        public AssetBundle loadedAssetBundle;
        public Asset(SDK.Assets.Asset assetBase)
        {
            UtilityFunctions.CopyProperties(assetBase, this);
        }
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
    public class Slot : SDK.Slots.Slot
    {
        public List<string> expression;
        public string collectionName;
        public Dictionary<string, long> balanceCounts;
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

    public enum BuildPlatform
    {
        iOS,
        Android,
        StandaloneWindows,
        StandaloneOSX,
        WebGL
    }

}