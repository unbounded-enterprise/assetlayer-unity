using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Assetlayer.UnitySDK;
using UnityEngine.UIElements;
using System;
using static Assetlayer.UnitySDK.SDKClass;
using PimDeWitte.UnityMainThreadDispatcher;
using static Assetlayer.UtilityFunctions.UtilityFunctions;

namespace Assetlayer.Inventory
{

    public class Inventory : MonoBehaviour
    {
        public string menuName;
        public bool selectFunctionality;
        public bool giftFunctionality;
        public bool list;

        public string detailExpressionId;

        private string currentSearchString = "";
        private Coroutine debounceCoroutine = null;
        private InventoryUIManager uiManager;

        private string selectedSlotId;
        private string selectedCollectionId;
        private string selectedAssetId;

        private Asset selectedAsset;
        private Collection selectedCollection;
        private Slot selectedSlot;

        private enum DisplayType { Slots, Collections, Assets }
        private DisplayType currentDisplayType;

        private IEnumerable<Slot> loadedSlots;
        private IEnumerable<Collection> loadedCollections;
        private IEnumerable<Asset> loadedAssets;

        private SDKClass sdk = new SDKClass();
        private AssetBundleDownloader bundleDownloader;

        [System.Serializable]
        public class AssetSelectedUnityEvent : UnityEvent<Asset>
        {
        }

        public AssetSelectedUnityEvent onAssetSelection;

        public string slotId;
        public List<string> slotIds;
        public string assetExpressionId;

        private void Awake()
        {
            bundleDownloader = GetComponent<AssetBundleDownloader>();
        }

        private void Start()
        {
            uiManager = GetComponent<InventoryUIManager>();

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

        private async Task DisplaySlots()
        {
            Debug.Log("DiplaySlotsstart");
            currentDisplayType = DisplayType.Slots;
            var slots = loadedSlots != null ? loadedSlots : await FetchSlots();
            loadedSlots = slots;
            Debug.Log("loaded slots: " + slots);

            if (slots == null)
            {
                Debug.Log("slots were not loaded");
                return;
            }

            List<UIAsset> convertedUISlots = new List<UIAsset>();
            Debug.Log("converted slots: " + convertedUISlots);
            // Only consider slots with slotId present in the slotIds list.
            IEnumerable<Slot> filteredLoadedSlots;

            if (slotIds.Any())
            {
                // If slotIds is not empty, filter the slots based on it.
                filteredLoadedSlots = slots.Where(slot => slotIds.Contains(slot.slotId));
            }
            else
            {
                // If slotIds is empty, use all the loadedSlots.
                filteredLoadedSlots = slots;
            }
            foreach (var slot in filteredLoadedSlots)
            {
                convertedUISlots.Add(UIAsset.ConvertToUIAsset(slot));
            }
            Debug.Log("converted end: " + convertedUISlots);

            IEnumerable<UIAsset> filteredSlots = FilterBySearch(convertedUISlots, currentSearchString);
            Debug.Log("ready to display ui slots: " + filteredSlots);

            // Ensure this is called on the main thread
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                uiManager.DisplayUIAssets(filteredSlots);
            });
        }


        private async Task DisplayCollectionsForSelectedSlot()
        {
            currentDisplayType = DisplayType.Collections;
            loadedCollections = await FetchCollections(selectedSlot.collections);
            Debug.Log("loaderCollections: " + loadedCollections?.ToString());
            List<UIAsset> convertedUICollections = new List<UIAsset>();
            foreach (var collection in loadedCollections)
            {
                convertedUICollections.Add(UIAsset.ConvertToUIAsset(collection, assetExpressionId));
            }

            IEnumerable<UIAsset> filteredCollections = FilterBySearch(convertedUICollections, currentSearchString);
            Debug.Log("filtered colelctions: " + filteredCollections);
            // Ensure this is called on the main thread
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
            Debug.Log("Display Collections: " + filteredCollections);
                uiManager.DisplayUIAssets(filteredCollections);
            });
        }


        private async Task DisplayAssetsForSelectedCollection()
        {
            currentDisplayType = DisplayType.Assets;
            loadedAssets = await FetchAssetsByCollectionId(selectedCollectionId);

            List<UIAsset> convertedUIAssets = new List<UIAsset>();
            foreach (var asset in loadedAssets)
            {
                AssetCacheManager.Instance.AddToCache(asset);
                convertedUIAssets.Add(UIAsset.ConvertToUIAsset(asset, assetExpressionId));
            }

            IEnumerable<UIAsset> filteredAssets = FilterBySearch(convertedUIAssets, currentSearchString);
            // Ensure this is called on the main thread
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                uiManager.DisplayUIAssets(filteredAssets);
            });
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
            Debug.Log("Asset selected: " + asset);
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
                        SelectAsset(asset.UIAssetId);
                        if (asset.LoadedAssetBundle == null)
                        {
                            string bundleUrl = string.IsNullOrEmpty(assetExpressionId)
                                ? GetExpressionValueAssetBundle(selectedAsset.expressionValues, "AssetBundle")
                                : GetExpressionValueByExpressionIdAssetBundle(selectedAsset.expressionValues, assetExpressionId);

                            bundleDownloader.DownloadAndLoadBundle(bundleUrl, loadedBundle =>
                            {
                                selectedAsset.loadedAssetBundle = loadedBundle;
                                Debug.Log("Loaded bundle before vent" + asset.LoadedAssetBundle);
                                if (loadedBundle != null)
                                {
                                    onAssetSelection.Invoke(selectedAsset);
                                }
                                else
                                {
                                    Debug.LogError("Failed to download or load the asset bundle.");
                                }
                            });
                        }
                        else
                        {
                            onAssetSelection.Invoke(selectedAsset);
                        }
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
                selectedSlot = null;
                DisplaySlots();
                uiManager.UpdateInventoryTitle(menuName);
            }
            else
            {
                if (loadedSlots != null)
                {
                    selectedSlot = loadedSlots.FirstOrDefault(a => a.slotId == selectedSlotId);
                    if (selectedSlot != null)
                    {
                        uiManager.UpdateInventoryTitle(selectedSlot.slotName);
                    }
                }
                
                DisplayCollectionsForSelectedSlot();
            }

        }

        public void SelectCollection(string collectionId)
        {
            selectedCollectionId = collectionId;
            if (string.IsNullOrEmpty(collectionId))
            {
                selectedCollection = null;
                SelectSlot(selectedSlotId);
            }
            else
            {
                
                if (loadedCollections != null)
                {
                    selectedCollection = loadedCollections.FirstOrDefault(a => a.collectionId == selectedCollectionId);
                    if (selectedCollection != null)
                    {
                        uiManager.UpdateInventoryTitle(selectedCollection.collectionName);
                    }

                }

                DisplayAssetsForSelectedCollection();


            }

        }

        public void SelectAsset(string assetId)
        {
            selectedAssetId = assetId;
            if (string.IsNullOrEmpty(assetId))
            {
                selectedAsset = null;
                SelectCollection(selectedCollectionId);
            }
            else
            {
                if (loadedAssets != null)
                {
                    selectedAsset = loadedAssets.FirstOrDefault(a => a.assetId == selectedAssetId);
                    uiManager.UpdateInventoryTitle(selectedAsset.collectionName + " #" + selectedAsset.serial);
                }
                
                // Fetch asset details or handle interactions specific to the selected asset.
            }

        }

        public async Task<IEnumerable<Collection>> FetchCollections(List<string> collectionIds)
        {
            try
            {
                IEnumerable<Collection> collectionInfos = await sdk.GetCollectionInfo(collectionIds);
                return collectionInfos;
            } catch(Exception e)
            {
                Debug.Log("Fetch Collections fail: " + e.Message);
                return null;
            }
        }


        public async Task<IEnumerable<Slot>> FetchSlots()
        {
            // Use the GetAppSlots method to retrieve all slot IDs
            string[] slotIds = await sdk.GetAppSlots();
            Debug.Log("slotIds: " + slotIds);

            if (slotIds == null || !slotIds.Any())
            {
                return Enumerable.Empty<Slot>();
            }

            // Fetch details for each slot using GetSlotInfo
            List<Slot> slots = new List<Slot>();

            foreach (var slotId in slotIds)
            {
                Debug.Log("in for each: " + slotId);
                SlotInfo slotInfo = await sdk.GetSlotInfo(slotId);
                Debug.Log("slotinfo: " + slotInfo);

                if (slotInfo != null)
                {
                    slots.Add(new Slot
                    {
                        slotId = slotInfo.slotId,
                        slotName = slotInfo.slotName,
                        slotImage = slotInfo.slotImage,
                        description = slotInfo.description,
                        appId = slotInfo.appId,
                        collections = slotInfo.collections,
                        expression = slotInfo.expressions,
                    });
                }
            }

            return slots;
        }

        public async Task<IEnumerable<Asset>> FetchAssetsByCollectionId(string collectionId)
        {
            IEnumerable<Asset> collectionAssets =  await sdk.GetBalanceOfCollection(collectionId);
            return collectionAssets;
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
                    GetExpressionValueAssetBundle(collection.exampleExpressionValues, "AssetBundle")
                :
                    GetExpressionValueByExpressionIdAssetBundle(collection.exampleExpressionValues, assetExpressionId),
                collection.minted,
                UIAssetType.Collection);
        }

        public static UIAsset ConvertToUIAsset(Asset asset, string assetExpressionId)
        {
            return new UIAsset(
                asset.assetId,
                asset.collectionName,
                GetExpressionValue(asset.expressionValues, "Menu View"),
                string.IsNullOrEmpty(assetExpressionId)
                ?
                    GetExpressionValueAssetBundle(asset.expressionValues, "AssetBundle")
                :
                    GetExpressionValueByExpressionIdAssetBundle(asset.expressionValues, assetExpressionId)
                ,
                asset.serial,
                UIAssetType.Asset);
        }
    }
}

