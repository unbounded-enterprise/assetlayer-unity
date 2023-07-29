using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SDKClass
{
    private const string API_URL = "https://api.assetlayer.com/api/v1/nft/info";
    // private const string APP_SECRET = "717b3ad702c58539f2e5e30f24cc0973";
    private const string HANDLE = "randomedge";

    public string APP_SECRET
    {
        get
        {
#if UNITY_EDITOR
            return SecretHolder.AppSecret;
#else
            return "not available"; 
#endif
        }
    }

    public IEnumerator GetExpression(string nftId, string expressionName, System.Action<string> callback)
    {
        string urlWithNftIdParameter = string.Format("{0}?nftId={1}", API_URL, nftId);
        UnityWebRequest request = UnityWebRequest.Get(urlWithNftIdParameter);
        request.SetRequestHeader("appsecret", APP_SECRET);
        // Specify the Content-Type
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            NFTInfoResponse response = JsonUtility.FromJson<NFTInfoResponse>(request.downloadHandler.text);

            if (response != null
                && response.body != null
                && response.body.nfts != null
                && response.body.nfts.Length > 0
                && response.body.nfts[0].expressionValues != null
                && response.body.nfts[0].expressionValues.Length > 0)
            {
                var expression = response.body.nfts[0].expressionValues.FirstOrDefault(e => e.expression.expressionName == expressionName);

                if (expression != null)
                {
                    callback?.Invoke(expression.value);
                }
                else
                {
                    callback?.Invoke(response.body.nfts[0].expressionValues[0].value);
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
    public class NFTInfoResponse
    {
        public int statusCode;
        public bool success;
        public Body body;

        [System.Serializable]
        public class Body
        {
            public NFT[] nfts;

            [System.Serializable]
            public class NFT
            {
                public ExpressionValue[] expressionValues;

                [System.Serializable]
                public class ExpressionValue
                {
                    public string value;
                    public Expression expression;

                    [System.Serializable]
                    public class Expression
                    {
                        public string expressionName;
                    }
                }
            }
        }
    }

    public class CollectionData
    {
        public string collectionName;
        public string collectionImage;
        public int nftMaximum;
        public List<string> tags;
        public Dictionary<string, string> properties;
        public string type;
        public string slotId;
        public string handle;
    }


    public async Task<string> CreateCollection(string slotId, string collectionName, int maxSupply, string dataUrl)
    {
        string url = "https://api.assetlayer.com/api/v1/collection/new";

        var newCollectionData = new CollectionData
        {
            collectionName = collectionName,
            collectionImage = dataUrl,
            type = "Identical",
            slotId = slotId,
            nftMaximum = maxSupply,
            tags = new List<string>(),
            properties = new Dictionary<string, string>(),
            handle = HANDLE
        };

        string jsonBody = JsonUtility.ToJson(newCollectionData);
        Debug.Log(jsonBody);

        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("appsecret", APP_SECRET);
        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Delay(100);
        }

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
            return null;
        }
        else
        {
            Debug.Log("CreateCollection response: " + request.downloadHandler.text);
            CollectionResponse response = JsonUtility.FromJson<CollectionResponse>(request.downloadHandler.text);
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
    public class CollectionResponse
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
        string url = "https://api.assetlayer.com/api/v1/expression/values/collection";

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
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("appsecret", APP_SECRET);
        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Delay(100);
        }

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

    public async Task<string> CreateExpression(string slotId, string expressionTypeId, string expressionName)
    {
        string url = "https://api.assetlayer.com/api/v1/expression/new";

        var expressionData = new ExpressionData
        {
            slotId = slotId,
            expressionTypeId = expressionTypeId,
            expressionName = expressionName
        };

        string jsonBody = JsonUtility.ToJson(expressionData);
        Debug.Log(jsonBody);

        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("appsecret", APP_SECRET);
        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Delay(100);
        }

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
        public Body body;

        [System.Serializable]
        public class Body
        {
            public string status;
        }
    }

    public class MintData
    {
        public string collectionId;
        public int number;
        public string handle;
    }

    public async Task<bool> Mint(string collectionId, int amount)
    {
        string url = "https://api.assetlayer.com/api/v1/nft/mint";

        var mintData = new MintData { collectionId = collectionId, number = amount, handle = HANDLE };

        string jsonBody = JsonUtility.ToJson(mintData);

        Debug.Log("Json body mint: " + jsonBody);

        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.uploadHandler.contentType = "application/json";
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("appsecret", APP_SECRET);
        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Delay(100);
        }

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
            return false;
        }
        else
        {
            Debug.Log("Mint response: " + request.downloadHandler.text);
            MintResponse response = JsonUtility.FromJson<MintResponse>(request.downloadHandler.text);
            if (response.success && response.body.status == "minting")
            {
                return true;
            }
            else
            {
                Debug.LogError("Failed to mint");
                return false;
            }
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
        string url = $"https://api.assetlayer.com/api/v1/expression/slot?slotId={slotId}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("appsecret", APP_SECRET);

        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Delay(100);
        }

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
                        Debug.Log("Exprrison found: " + expression.expressionId);
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
        string url = $"https://api.assetlayer.com/api/v1/expression/slot?slotId={slotId}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("appsecret", APP_SECRET);

        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Delay(100);
        }

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

    public async Task<List<Nft>> GetNftBalance(string slotId, string handle, bool idOnly, bool countsOnly)
    {
        string url = $"https://api.assetlayer.com/api/v1/nft/slot?slotId={slotId}&handle={handle}&idOnly={idOnly.ToString().ToLower()}&countsOnly={countsOnly.ToString().ToLower()}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("appsecret", APP_SECRET);

        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Delay(100);
        }

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
            return null;
        }
        else
        {
            Debug.Log("GetNftDetails response: " + request.downloadHandler.text);
            NftResponse response = JsonUtility.FromJson<NftResponse>(request.downloadHandler.text);
            Debug.Log("Getting NFT details res" + response.body.nfts);
            if (response.success && response.body.nfts != null)
            {
                return response.body.nfts;
            }

            Debug.LogError("No NFT details found");
            return null;
        }
    }
}

[Serializable]
public class ExpressionAttribute
{
    public string expressionAttributeName;
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
public class Nft
{
    public string nftId;
    public int serial;
    public List<ExpressionValue> expressionValues;
    public string collectionId;
}

[Serializable]
public class NftResponseBody
{
    public List<Nft> nfts;
}

[Serializable]
public class NftResponse
{
    public int statusCode;
    public bool success;
    public NftResponseBody body;
}