using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;



public class NftInventory : MonoBehaviour
{
    public VisualTreeAsset nftCardTemplate;
    
    private string currentSearchString = "";
    private Coroutine debounceCoroutine = null;
    private InventoryUIManager uiManager;

    private string selectedSlotId;
    private string selectedCollectionId;
    private string selectedAssetId;

    private enum DisplayType { Slots, Collections, Assets }
    private DisplayType currentDisplayType;

    private IEnumerable<Slot> loadedSlots;
    private IEnumerable<Collection> loadedCollections;
    private IEnumerable<Asset> loadedAssets;

    [System.Serializable]
    public class AssetUnityEvent : UnityEvent<Asset>
    {
    }

    public AssetUnityEvent onAssetSelection;


    public string slotId;
    public string assetExpressionId;

    private async void Start()
    {
        uiManager = new InventoryUIManager(GetComponent<UIDocument>(), nftCardTemplate);

        uiManager.UISearchInitiated += OnSearchValueChanged;
        uiManager.UICloseInitiated += HideInventoryUI;
        uiManager.UIBackInitiated += BackClickedHandler;
        uiManager.UIAssetSelected += UIAssetSelectedHandler;

        if (string.IsNullOrEmpty(slotId))
        {
            DisplaySlots();
        }
        else
        {
            selectedSlotId = slotId;
            DisplayCollectionsForSelectedSlot();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            uiManager.ToggleInventoryUI();
        }
    }

    private void DisplaySlots()
    {
        currentDisplayType = DisplayType.Slots;
        loadedSlots = FetchSlots(""); // pass appropriate appId if needed

        List<UIAsset> convertedUISlots = new List<UIAsset>();
        foreach (var slot in loadedSlots)
        {
            convertedUISlots.Add(UIAsset.ConvertToUIAsset(slot));
        }

        IEnumerable<UIAsset> filteredSlots = FilterBySearch(convertedUISlots, currentSearchString);
        uiManager.DisplayUIAssets(filteredSlots);
    }

    private void DisplayCollectionsForSelectedSlot()
    {
        currentDisplayType = DisplayType.Collections;
        loadedCollections = FetchCollectionsBySlotId(selectedSlotId);
        Debug.Log("loaderCollections: " + loadedCollections?.ToString());
        List<UIAsset> convertedUICollections = new List<UIAsset>();
        foreach (var collection in loadedCollections)
        {
            convertedUICollections.Add(UIAsset.ConvertToUIAsset(collection, assetExpressionId));
        }

        IEnumerable<UIAsset> filteredCollections = FilterBySearch(convertedUICollections, currentSearchString);
        uiManager.DisplayUIAssets(filteredCollections);
    }


    private void DisplayAssetsForSelectedCollection()
    {
        currentDisplayType = DisplayType.Assets;
        loadedAssets = FetchAssetsByCollectionId(selectedCollectionId);

        List<UIAsset> convertedUIAssets = new List<UIAsset>();
        foreach (var asset in loadedAssets)
        {
            convertedUIAssets.Add(UIAsset.ConvertToUIAsset(asset, assetExpressionId));
        }

        IEnumerable<UIAsset> filteredAssets = FilterBySearch(convertedUIAssets, currentSearchString);
        uiManager.DisplayUIAssets(filteredAssets);
    }

    public IEnumerable<UIAsset> FilterBySearch(IEnumerable<UIAsset> items, string searchString)
    {
        var loweredSearchString = searchString.ToLower();
        var results = new List<UIAsset>();

        foreach (var item in items)
        {
            if (item.Name.ToLower().Contains(loweredSearchString))
            {
                results.Add(item);
            }
        }
        return results;
    }


    private IEnumerator DebounceSearch()
    {
        yield return new WaitForSeconds(0.3f);
        switch (currentDisplayType)
        {
            case DisplayType.Slots:
                DisplaySlots();
                break;
            case DisplayType.Collections:
                DisplayCollectionsForSelectedSlot();
                break;
            case DisplayType.Assets:
                DisplayAssetsForSelectedCollection();
                break;
        }
        debounceCoroutine = null;
    }


    private void OnSearchValueChanged(string newValue)
    {
        currentSearchString = newValue;

        if (debounceCoroutine != null)
        {
            StopCoroutine(debounceCoroutine);
        }

        debounceCoroutine = StartCoroutine(DebounceSearch());
    }

    private void BackClickedHandler()
    {
        switch (currentDisplayType)
        {
            case DisplayType.Assets:
                selectedCollectionId = "";
                SelectCollection(selectedCollectionId);
                break;

            case DisplayType.Collections:
                selectedSlotId = "";
                SelectSlot(selectedSlotId);
                break;

            case DisplayType.Slots:
                selectedSlotId = "";
                HideInventoryUI();
                break;

            default:
                HideInventoryUI();
                break;
        }
    }

    private void UIAssetSelectedHandler(UIAsset asset)
    {
        switch (asset.AssetType)
        {
            case UIAssetType.Slot:
                SelectSlot(asset.UIAssetId);
                break;

            case UIAssetType.Collection:
                SelectCollection(asset.UIAssetId);
                break;

            case UIAssetType.Asset:
                Debug.Log("selectedAsset: " + asset.ToString());
                Asset selectedAsset = loadedAssets.FirstOrDefault(a => a.assetId == asset.UIAssetId);
                if (selectedAsset != null)
                {
                    Debug.Log("selectedAsset found: " + selectedAsset.ToString());
                    selectedAssetId = asset.UIAssetId;
                    onAssetSelection.Invoke(selectedAsset);
                    SelectAsset(asset.UIAssetId);
                }
                break;

            default:
                break;
        }
    }


    private void HideInventoryUI()
    {
        uiManager.HideInventoryUI();
    }

    public void SelectSlot(string slotId)
    {
        selectedSlotId = slotId;
        if (string.IsNullOrEmpty(slotId))
        {
            DisplaySlots();
            uiManager.UpdateInventoryTitle("Inventory");
        } else
        {
            Slot selectedSlot = loadedSlots.FirstOrDefault(a => a.slotId == selectedSlotId);
            uiManager.UpdateInventoryTitle(selectedSlot.slotName);
            DisplayCollectionsForSelectedSlot();
        }
        
    }

    public void SelectCollection(string collectionId)
    {
        selectedCollectionId = collectionId;
        if (string.IsNullOrEmpty(collectionId))
        {
            SelectSlot(selectedSlotId);
        } else
        {
            Collection selectedCollection = loadedCollections.FirstOrDefault(a => a.collectionId == selectedCollectionId);
            uiManager.UpdateInventoryTitle(selectedCollection.collectionName);
            DisplayAssetsForSelectedCollection(); 
        }
        
    }

    public void SelectAsset(string assetId)
    {
        selectedAssetId = assetId;
        if (string.IsNullOrEmpty(assetId))
        {
            SelectCollection(selectedCollectionId);
        } else
        {
            Asset selectedAsset = loadedAssets.FirstOrDefault(a => a.assetId == selectedAssetId);
            uiManager.UpdateInventoryTitle(selectedAsset.collectionName + " #" + selectedAsset.serial);
            // Fetch asset details or handle interactions specific to the selected asset.
        }

    }

    public IEnumerable<Collection> FetchCollectionsBySlotId(string slotId)
    {
        // Replace with an actual API call to get collections for a given slot
        // Here are some example values based on the given types and made up values:
        return new List<Collection>
    {
        new Collection
        {
            collectionId = "idtest1",
            collectionName = "test1",
            collectionImage = "https://asset-api-files-bucket.s3.amazonaws.com/335c6083-189e-4b4f-bd10-dc05192f98d0.png",
            description = "A test collection 1",
            slotId = slotId,
            maximum = 10,
            minted = 2,
            tags = new List<string> { "tag1", "tag2" },
            type = "someType",
            properties = "someProperties",
            exampleExpressionValues = new List<ExpressionValue>
            {
                new ExpressionValue
                {
                    value = "https://asset-api-files-bucket.s3.amazonaws.com/a0d5e660-d1dc-4782-8822-fbf25a31b729.png",
                    expressionAttribute = new ExpressionAttribute
                    {
                        expressionAttributeName = "Image",
                        expressionAttributeId = "62f83b2482081d6f89953fa7"
                    },
                    expression = new Expression
                    {
                        expressionName = "Menu View",
                        expressionId = "64c85ab9441a8add3692de8b"
                    }
                },
                new ExpressionValue
                {
                    value = "https://asset-api-files-bucket.s3.amazonaws.com/33a5e8f5-0cd7-4a07-ac60-e19d4ea7fcd9.bundle",
                    expressionAttribute = new ExpressionAttribute
                    {
                        expressionAttributeName = "AssetBundle",
                        expressionAttributeId = "64b1ce94716b83c3de7df852"
                    },
                    expression = new Expression
                    {
                        expressionName = "AssetBundle",
                        expressionId = "64c85af0441a8add3692dfe7"
                    }
                }
            }
        },
        new Collection
        {
            collectionId = "idtest3",
            collectionName = "test3",
            collectionImage = "https://asset-api-files-bucket.s3.amazonaws.com/b836d66c-2c18-46e7-bcb5-e7059431ee13.png",
            description = "A test collection 3",
            slotId = slotId,
            maximum = 15,
            minted = 8,
            tags = new List<string> { "tag3", "tag4" },
            type = "anotherType",
            properties = "moreProperties",
            exampleExpressionValues = new List<ExpressionValue>
            {
                new ExpressionValue
                {
                    value = "https://asset-api-files-bucket.s3.amazonaws.com/a0d5e660-d1dc-4782-8822-fbf25a31b729.png",
                    expressionAttribute = new ExpressionAttribute
                    {
                        expressionAttributeName = "Image",
                        expressionAttributeId = "62f83b2482081d6f89953fa7"
                    },
                    expression = new Expression
                    {
                        expressionName = "Menu View",
                        expressionId = "64c85ab9441a8add3692de8b"
                    }
                },
                new ExpressionValue
                {
                    value = "https://asset-api-files-bucket.s3.amazonaws.com/33a5e8f5-0cd7-4a07-ac60-e19d4ea7fcd9.bundle",
                    expressionAttribute = new ExpressionAttribute
                    {
                        expressionAttributeName = "AssetBundle",
                        expressionAttributeId = "64b1ce94716b83c3de7df852"
                    },
                    expression = new Expression
                    {
                        expressionName = "AssetBundle",
                        expressionId = "64c85af0441a8add3692dfe7"
                    }
                }
            }
        }
    };
    }


    public IEnumerable<Slot> FetchSlots(string appId)
    {
        // Replace with an actual API call to get slots for a given appId
        // Here are some example values based on the given Slot class:
        return new List<Slot>
    {
        new Slot
        {
            slotId = "64c85ab9441a8add3692de88",
            slotName = "Characters",
            slotImage = "https://asset-api-files-bucket.s3.amazonaws.com/8d877164-d8ff-4662-8dad-0f30da0d79a6.png?",
            description = "Description for Slot One",
            appId = appId,
            collections = new List<string> { "idtest1", "idtest3" },
            expression = new List<string> { "expression1", "expression2" },
            collectionName = "Collection Name 1"
        },
        new Slot
        {
            slotId = "64bfc3f51c60d5f2bee83210",
            slotName = "Scenes",
            slotImage = "https://asset-api-files-bucket.s3.amazonaws.com/7893f4c7-c38f-4ddb-99cf-e480f30157ce.png?",
            description = "Description for Slot Two",
            appId = appId,
            collections = new List<string> { "idtest3" },
            expression = new List<string> { "expression3", "expression4" },
            collectionName = "Collection Name 2"
        }
    };
    }

    public IEnumerable<Asset> FetchAssetsByCollectionId(string collectionId)
    {
        // Here are some example values for Asset:
        return new List<Asset>
    {
        new Asset
        {
            assetId = "asset1",
            serial = 101,
            collectionId = collectionId,
            collectionName = "test1",
            expressionValues = new List<ExpressionValue>
        {
            new ExpressionValue
            {
                value = "https://asset-api-files-bucket.s3.amazonaws.com/a0d5e660-d1dc-4782-8822-fbf25a31b729.png",
                expressionAttribute = new ExpressionAttribute
                {
                    expressionAttributeName = "Image",
                    expressionAttributeId = "62f83b2482081d6f89953fa7"
                },
                expression = new Expression
                {
                    expressionName = "Menu View",
                    expressionId = "64c85ab9441a8add3692de8b"
                }
            },
            new ExpressionValue
            {
                value = "https://asset-api-files-bucket.s3.amazonaws.com/33a5e8f5-0cd7-4a07-ac60-e19d4ea7fcd9.bundle",
                expressionAttribute = new ExpressionAttribute
                {
                    expressionAttributeName = "AssetBundle",
                    expressionAttributeId = "64b1ce94716b83c3de7df852"
                },
                expression = new Expression
                {
                    expressionName = "AssetBundle",
                    expressionId = "64c85af0441a8add3692dfe7"
                }
            }
        },
            properties = "somePropertiesForAsset1"
        },
        new Asset
        {
            assetId = "asset2",
            serial = 102,
            collectionId = collectionId,
            collectionName = "test1",
            expressionValues = new List<ExpressionValue>
        {
            new ExpressionValue
            {
                value = "https://asset-api-files-bucket.s3.amazonaws.com/a0d5e660-d1dc-4782-8822-fbf25a31b729.png",
                expressionAttribute = new ExpressionAttribute
                {
                    expressionAttributeName = "Image",
                    expressionAttributeId = "62f83b2482081d6f89953fa7"
                },
                expression = new Expression
                {
                    expressionName = "Menu View",
                    expressionId = "64c85ab9441a8add3692de8b"
                }
            },
            new ExpressionValue
            {
                value = "https://asset-api-files-bucket.s3.amazonaws.com/f3ea3027-55f5-4e97-b4be-d44b23ddd0b1.bundle",
                expressionAttribute = new ExpressionAttribute
                {
                    expressionAttributeName = "AssetBundle",
                    expressionAttributeId = "64b1ce94716b83c3de7df852"
                },
                expression = new Expression
                {
                    expressionName = "AssetBundle",
                    expressionId = "64c85af0441a8add3692dfe7"
                }
            }
        },
            properties = "somePropertiesForAsset2"
        }
    };
    }
}

public enum UIAssetType
{
    Slot,
    Collection,
    Asset
}

public class UIAsset
{
    public string UIAssetId { get; set; }
    public string Name { get; set; }
    public string ImageURL { get; set; }

    public string AssetBundleURL { get; set; }
    public int CountOrSerial { get; set; }
    public Texture2D LoadedTexture { get; set; }
    public AssetBundle LoadedAssetBundle { get; set; }
    public UIAssetType AssetType { get; set; }

    public UIAsset(string id, string name, string url, string assetBundleUrl, int countOrSerial, UIAssetType type)
    {
        UIAssetId = id;
        Name = name;
        ImageURL = url;
        CountOrSerial = countOrSerial;
        AssetType = type;
        AssetBundleURL = assetBundleUrl;
    }

    public static UIAsset ConvertToUIAsset(Slot slot)
    {
        return new UIAsset(slot.slotId, slot.slotName, slot.slotImage, "", slot.collections.Count, UIAssetType.Slot);
    }

    public static UIAsset ConvertToUIAsset(Collection collection, string assetExpressionId)
    {
        return new UIAsset(
            collection.collectionId,
            collection.collectionName,
            collection.collectionImage,
            string.IsNullOrEmpty(assetExpressionId)
            ?
                UtilityFunctions.GetExpressionValue(collection.exampleExpressionValues, "AssetBundle")
            :
                UtilityFunctions.GetExpressionValueByExpressionId(collection.exampleExpressionValues, assetExpressionId),
            collection.minted, 
            UIAssetType.Collection);
    }

    public static UIAsset ConvertToUIAsset(Asset asset, string assetExpressionId)
    {
        return new UIAsset(
            asset.assetId, 
            asset.collectionName, 
            UtilityFunctions.GetExpressionValue(asset.expressionValues, "Menu View"),
            string.IsNullOrEmpty(assetExpressionId)
            ?
                UtilityFunctions.GetExpressionValue(asset.expressionValues, "AssetBundle")
            :
                UtilityFunctions.GetExpressionValueByExpressionId(asset.expressionValues, assetExpressionId)
            ,
            asset.serial, 
            UIAssetType.Asset);
    }
}

public class Asset
{
    public string assetId;
    public int serial;
    public string collectionId;
    public string collectionName;
    public List<ExpressionValue> expressionValues;
    public string properties;

}

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

public class Collection
{
    public string collectionId;
    public string collectionName;
    public string collectionImage;
    public string description;
    public string slotId;
    public int maximum;
    public int minted;
    public List<string> tags;
    public string type;
    public string properties;
    public List<ExpressionValue> exampleExpressionValues;

}

