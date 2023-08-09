using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class NftViewer : MonoBehaviour
{
    private const string MenuViewAttributeName = "Menu View";
    private const string AssetBundleAttributeId = "64b1ce94716b83c3de7df852";


    // Set these in inspector or in code
    public string SlotId;
    public string Handle;
    public Material plattformMaterial;


    private List<Nft> validNfts;  //  We store the list of valid NFTs.
    private int selectedNftIndex = 0; //  The index of the currently selected NFT.
    private Dictionary<int, GameObject> loadedNfts = new Dictionary<int, GameObject>();




    private float zoomSpeed = 0.02f;  // Speed of zooming in and out
    private float maxZoom = 0.2f;  // Maximum zoom distance
    private float rotationSpeed = 0.5f; // Speed of rotation
    private float rotationRadius = 0.3f; // Radius of rotation

    private float itemSize = 0.38f;

    private List<Light> lights = new List<Light>();
    private List<GameObject> lightGameObjects = new List<GameObject>();
    private List<GameObject> imageCubeRefs = new List<GameObject>();
    private GameObject platformCubeRef;
    private GameObject highlightedObjectRef;

    private Coroutine cameraAnimationCoroutine = null;
    private Coroutine lightAnimation = null;



    private void Start()
    {
        platformCubeRef = CreatePlatformCube(); // Create platform first
        StartCoroutine(FetchAndDisplayNft());
    }

    private void Update()
    {
        if (validNfts == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow)) // Check if the right arrow key was pressed.
        {
            SelectNextNft();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) // Check if the left arrow key was pressed.
        {
            SelectPreviousNft();
        }
    }

    private void SelectNextNft()
    {
        if (lightAnimation != null)
        {
            StopCoroutine(lightAnimation);
        }
        if (cameraAnimationCoroutine != null)
        {
            StopCoroutine(cameraAnimationCoroutine);
        }
        DestroyLights();
        Debug.Log("nfts count: " + validNfts.Count + validNfts);
        selectedNftIndex = (selectedNftIndex + 1) % validNfts.Count;
        DisplayNft(validNfts[selectedNftIndex]);
    }

    private void SelectPreviousNft() 
    {
        if (lightAnimation != null)
        {
            StopCoroutine(lightAnimation);
        }
        if (cameraAnimationCoroutine != null)
        {
            StopCoroutine(cameraAnimationCoroutine);
        }
        DestroyLights();
        selectedNftIndex--;
        if (selectedNftIndex < 0)
        {
            selectedNftIndex = validNfts.Count - 1;
        }
        DisplayNft(validNfts[selectedNftIndex]);
    }

    private GameObject CreatePlatformCube()
    {
        GameObject platformCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platformCube.transform.localScale = new Vector3(100f, 10f, 10f);
        platformCube.GetComponent<MeshRenderer>().material = plattformMaterial ?? CreateDefaultMaterial(Color.blue);
        return platformCube;
    }

    private IEnumerator FetchAndDisplayNft()
    {
        var task = FetchNfts();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result != null && task.Result.Count > 0)
        {
            DisplayFirstValidNft(task.Result);
        }
        else
        {
            Debug.LogError("No NFTs found");
        }
    }


    private async Task<List<Nft>> FetchNfts()
    {
        SDKClass sdk = new SDKClass();
        return await sdk.GetNftBalance(SlotId, false, false);
    }

    private void DisplayFirstValidNft(List<Nft> nfts)
    {
        validNfts = FilterValidNfts(nfts);  // We store the list of valid NFTs.

        if (validNfts.Count > 0)
        {
            DisplayNft(validNfts[0]); 
        }
        else
        {
            Debug.LogError("No NFTs found with a 'Menu View' expression value");
        }
    }

    private void DisplayNft(Nft nft)
    {
        Debug.Log("Display NFT: " + nft + nft.nftId);

        // If the NFT has already been loaded, move the camera to it instead of loading it again
        if (loadedNfts.ContainsKey(selectedNftIndex))
        {
            highlightedObjectRef = loadedNfts[selectedNftIndex];
            PositionCamera();
            CreateSpotlights();
            CreateSpotlightForPrefab(highlightedObjectRef);
            return;
        }

        string imageUrl = GetExpressionValue(nft, MenuViewAttributeName);

        string assetBundleUrl = GetExpressionValueByAttributeId(nft, AssetBundleAttributeId);
        if (assetBundleUrl != null)
        {
            StartCoroutine(DownloadDisplayImageThenInstantiateAssetBundle(imageUrl, assetBundleUrl));
        }
    }

    private IEnumerator DownloadDisplayImageThenInstantiateAssetBundle(string imageUrl, string assetBundleUrl)
    {
        yield return StartCoroutine(DownloadAndDisplayImage(imageUrl));
        // Check if imageCubeRef is set. If not, you can choose to wait, or return an error

        // Wait for a short period (e.g., 1 second) before starting the next Coroutine
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(DownloadAndInstantiateAssetBundle(assetBundleUrl));
    }


    private IEnumerator DownloadAndInstantiateAssetBundle(string assetBundleUrl)
    {
        using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                var prefabName = bundle.GetAllAssetNames()[0];  // Assumes there's only one object in the bundle
                var prefab = bundle.LoadAsset<GameObject>(prefabName);

                GameObject instance = Instantiate(prefab);
                highlightedObjectRef = instance;

                // Add the instance to the dictionary
                loadedNfts[selectedNftIndex] = instance;
                // Get the scale factor
                float scaleFactor = imageCubeRefs[selectedNftIndex].transform.localScale.y * itemSize / instance.transform.localScale.y;
                // Scale the object while maintaining the aspect ratio
                instance.transform.localScale *= scaleFactor;

                // Position the object in front of the image (you might need to adjust this based on your needs)
                instance.transform.position = imageCubeRefs[selectedNftIndex].transform.position + new Vector3(0, -imageCubeRefs[selectedNftIndex].transform.localScale.y * 0.5f + instance.transform.localScale.y * 0.5f, -platformCubeRef.transform.localScale.z * 0.25f);

                CreateSpotlightForPrefab(instance); // Create spotlights for the prefab instance
            }
        }
    }

    private void CreateSpotlightForPrefab(GameObject prefab)
    {
        float lightRadius = prefab.transform.localScale.y * 3f;
        for (int i = 0; i < 2; i++)
        {
            CreateSpotlightForPrefab(i, lightRadius, prefab);
        }
    }

    private void CreateSpotlightForPrefab(int index, float lightRadius, GameObject prefab)
    {
        GameObject lightGameObject = new GameObject("PrefabLight" + index);
        Light lightComp = lightGameObject.AddComponent<Light>();
        lightComp.type = LightType.Spot;
        lightComp.color = Color.white;
        lightComp.intensity = 1.2f;
        lightComp.spotAngle = 45.0f;
        lightComp.range = lightRadius * 4;

        Vector3 lightPosition;
        if (index == 0)
        {
            // Position the first light to the left of the prefab.
            lightPosition = new Vector3(
                prefab.transform.position.x - prefab.transform.localScale.x / 2 - lightRadius,
                prefab.transform.position.y,
                prefab.transform.position.z
            );
        }
        else
        {
            // Position the second light to the right of the prefab.
            lightPosition = new Vector3(
                prefab.transform.position.x + prefab.transform.localScale.x / 2 + lightRadius,
                prefab.transform.position.y,
                prefab.transform.position.z
            );
        }

        lightGameObject.transform.position = lightPosition;
        lightGameObject.transform.LookAt(prefab.transform.position);

        lights.Add(lightComp); // Add the light to the list
        lightGameObjects.Add(lightGameObject);
    }






    private List<Nft> FilterValidNfts(List<Nft> nfts)
    {
        return nfts
            .GroupBy(nft => GetExpressionValue(nft, MenuViewAttributeName))
            .Where(g => IsValidImageExtension(g.Key))
            .Select(g => g.First())
            .ToList();
    }


    private bool IsValidImageExtension(string expressionValue)
    {
        return expressionValue != null &&
               (expressionValue.EndsWith(".png") || expressionValue.EndsWith(".jpg"));
    }

    private string GetExpressionValue(Nft nft, string attributeName)
    {
        var expressionValue = nft.expressionValues
                             .FirstOrDefault(ev => ev.expression.expressionName == attributeName);
        return expressionValue?.value;
    }

    private string GetExpressionValueByAttributeId(Nft nft, string attributeId)
    {
        var expressionValue = nft.expressionValues
                             .FirstOrDefault(ev => ev.expressionAttribute.expressionAttributeId == attributeId);
        return expressionValue?.value;
    }

    private Material CreateDefaultMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        return material;
    }

    private IEnumerator DownloadAndDisplayImage(string imageUrl)
    {
        using (System.Net.WebClient client = new System.Net.WebClient())
        {
            client.DownloadDataCompleted += (sender, e) =>
            {
                if (e.Error != null)
                {
                    Debug.LogError("Failed to download image. Error: " + e.Error);
                    return;
                }

                SetupNftScene(e.Result);
            };
            client.DownloadDataAsync(new System.Uri(imageUrl));
        }

        yield return null;
    }

    private void SetupNftScene(byte[] imageData)
    {
        Material material = LoadTexture(imageData);

        CreateImageCube(platformCubeRef, material);

        PositionCamera();
        

        DimAmbientLight();

        CreateSpotlights();
    }

    private Material LoadTexture(byte[] imageData)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);
        Material PictureMaterial = CreateDefaultMaterial(Color.white);
        PictureMaterial.mainTexture = texture;
        return PictureMaterial;
    }

    private GameObject CreateImageCube(GameObject platformCube, Material material)
    {
        GameObject imageCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        imageCube.transform.localScale = new Vector3(material.GetTexture("_MainTex").width * 0.01f, material.GetTexture("_MainTex").height * 0.01f, 0.1f);
        imageCube.transform.position = platformCube.transform.position
            + new Vector3(imageCube.transform.localScale.x * selectedNftIndex * 2, platformCube.transform.localScale.y / 2f + imageCube.transform.localScale.y / 2f, 0);
        imageCube.GetComponent<MeshRenderer>().material = material;
        imageCubeRefs.Add(imageCube);
        return imageCube;
    }

    private void PositionCamera()
    {
        Camera.main.transform.position = imageCubeRefs[selectedNftIndex].transform.position
            + new Vector3(0, imageCubeRefs[selectedNftIndex].transform.localScale.y * 0.35f, -imageCubeRefs[selectedNftIndex].transform.localScale.y * 2.5f);
        Camera.main.transform.LookAt(imageCubeRefs[selectedNftIndex].transform.position
            + new Vector3(0, imageCubeRefs[selectedNftIndex].transform.localScale.y * 0.1f, 0));

        // StartCameraAnimation();
    }

    private void StartCameraAnimation()
    {
        // If an animation is already running, stop it
        if (cameraAnimationCoroutine != null)
        {
            StopCoroutine(cameraAnimationCoroutine);
        }
        cameraAnimationCoroutine = StartCoroutine(AnimateCamera());
    }

    private IEnumerator AnimateCamera()
    {
        
        GameObject target = imageCubeRefs[selectedNftIndex];
        Vector3 originalPos = Camera.main.transform.position;
        Vector3 zoomPos = originalPos + Camera.main.transform.forward * maxZoom; // Calculate the zoomed position
        Vector3 originalOffset = originalPos - target.transform.position; // Offset of the camera from the target at the beginning

        float rotationAngle = 0f;

        // Step 1: Zoom in and rotate
        while (Vector3.Distance(Camera.main.transform.position, zoomPos) > 0.01f)
        {
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, zoomPos, zoomSpeed);

            rotationAngle += rotationSpeed * Time.deltaTime; // Accumulate rotation angle
            float x = originalOffset.x + rotationRadius * Mathf.Cos(rotationAngle);
            float z = originalOffset.z + rotationRadius * Mathf.Sin(rotationAngle);
            Camera.main.transform.position = new Vector3(x, Camera.main.transform.position.y, z);

            Camera.main.transform.LookAt(target.transform.position);
            yield return null;
        }

        // Step 2: Zoom out and rotate
        while (Vector3.Distance(Camera.main.transform.position, originalPos) > 0.01f)
        {
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, originalPos, zoomSpeed);

            rotationAngle += rotationSpeed * Time.deltaTime; // Accumulate rotation angle
            float x = originalOffset.x + rotationRadius * Mathf.Cos(rotationAngle);
            float z = originalOffset.z + rotationRadius * Mathf.Sin(rotationAngle);
            Camera.main.transform.position = new Vector3(x, Camera.main.transform.position.y, z);

            Camera.main.transform.LookAt(target.transform.position);
            yield return null;
        }
    }

    private void DimAmbientLight()
    {
        RenderSettings.ambientIntensity = 0.2f;
    }

    private void CreateSpotlights()
    {
        float lightRadius = imageCubeRefs[selectedNftIndex].transform.localScale.y * 3f;
        for (int i = 0; i < 4; i++)
        {
            CreateSpotlight(i, lightRadius, imageCubeRefs[selectedNftIndex]);
        }

        if (lightAnimation != null)
        {
            StopCoroutine(lightAnimation);
        }
        lightAnimation = StartCoroutine(AnimateLights());
    }

    private void CreateSpotlight(int index, float lightRadius, GameObject imageCube)
    {
        GameObject lightGameObject = new GameObject("NftLight" + index);
        Light lightComp = lightGameObject.AddComponent<Light>();
        lightComp.type = LightType.Spot;
        lightComp.color = Color.white;
        lightComp.intensity = 1.2f;
        lightComp.spotAngle = 45.0f;
        lightComp.range = lightRadius * 4;

        float angle = index * Mathf.PI / 3;
        Vector3 lightPosition = new Vector3(
            imageCube.transform.position.x + lightRadius * Mathf.Sin(angle),
            imageCube.transform.position.y,
            imageCube.transform.position.z + lightRadius * Mathf.Cos(angle)
        );

        lightGameObject.transform.position = lightPosition;
        lightGameObject.transform.LookAt(imageCube.transform.position);
        lightGameObjects.Add(lightGameObject);
    }

    private void DestroyLights()
    {
        foreach (var lightGO in lightGameObjects)
        {
            Destroy(lightGO);
        }
        lightGameObjects.Clear();
        lights.Clear();
    }

    private IEnumerator AnimateLights()
    {
        float lightChangeSpeed = 0.1f;
        while (true)
        {
            foreach (var light in lights)
            {
                float targetIntensity = 1.2f + Mathf.PingPong(Time.time, 0.4f);
                light.intensity = Mathf.Lerp(light.intensity, targetIntensity, Time.deltaTime * lightChangeSpeed);

                // If it's not a PrefabLight, then rotate around the image cube.
                if (!light.gameObject.name.Contains("PrefabLight"))
                {
                    // Slightly rotate the light around the Y-axis of the image cube.
                    light.transform.RotateAround(imageCubeRefs[selectedNftIndex].transform.position, Vector3.up, rotationSpeed * Time.deltaTime);
                    light.transform.LookAt(imageCubeRefs[selectedNftIndex].transform.position);
                }
            }

            yield return null;
        }
    }

    private void PositionCameraToHighlightedObject()
    {
        // Position the camera based on the highlighted object. 
        // Modify this based on how you want the camera to focus on the object.
        Camera.main.transform.position = highlightedObjectRef.transform.position
                + new Vector3(0, highlightedObjectRef.transform.localScale.y * 1.25f, -highlightedObjectRef.transform.localScale.y * 3.5f);
        Camera.main.transform.LookAt(highlightedObjectRef.transform.position
                + new Vector3(0, highlightedObjectRef.transform.localScale.y * 0.4f, 0));

        // StartCameraAnimation();
    }




}
