using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using System;
using static AssetLayer.Unity.ApiManager;
using PimDeWitte.UnityMainThreadDispatcher;
using static AssetLayer.Unity.UtilityFunctions;
using AssetLayer.SDK.Collections;
using Newtonsoft.Json;
using AssetLayer.SDK.Assets;

namespace AssetLayer.Unity
{

    public class Inventory : MonoBehaviour
    {
        public string menuName;
        public bool selectFunctionality;
        public bool giftFunctionality;
        public bool list;
        public bool closeOnSelection = true;

        public string detailExpressionId;

        private string currentSearchString = "";
        private Coroutine debounceCoroutine = null;
        private InventoryUIManagerUnityUI uiManager;

        private string selectedSlotId;
        private string selectedCollectionId;
        private string selectedAssetId;

        private bool initialSetupDone = false;
        private Asset selectedAsset;
        private Collection selectedCollection;
        private Slot selectedSlot;

        private enum DisplayType { Slots, Collections, Assets }
        private DisplayType currentDisplayType;

        private IEnumerable<Slot> loadedSlots;
        private IEnumerable<Collection> loadedCollections;
        private IEnumerable<Asset> loadedAssets;


        private ApiManager manager = new ApiManager();
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

        private IEnumerator Start()
        {
            Debug.Log("Starting inventory");
            uiManager = GetComponent<InventoryUIManagerUnityUI>();

            uiManager.UISearchInitiated += OnSearchValueChanged;
            uiManager.UICloseInitiated += HideInventoryUI;
            InventoryUIManagerUnityUI.OnInventoryToggled += ToggleInventoryUI;
            uiManager.UIBackInitiated += BackClickedHandler;
            uiManager.UIAssetSelected += UIAssetSelectedHandler;
            yield return StartCoroutine(InitialLoading());
        }

        private IEnumerator InitialLoading()
        {
            if (!initialSetupDone)
            {


                if (string.IsNullOrEmpty(slotId))
                {
                    StartCoroutine(DisplaySlots());
                }
                else
                {

                    if (loadedSlots == null)
                    {
                        Task<IEnumerable<Slot>> fetchTask = FetchSlots();
                        yield return WaitForTask(fetchTask);
                        loadedSlots = fetchTask.Result;

                    }
                    SelectSlot(slotId);
                    // StartCoroutine(DisplayCollectionsForSelectedSlot());
                }
                uiManager.UpdateInventoryTitle(menuName);
                initialSetupDone = true;
            }
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                uiManager.ToggleInventoryUI();
            }
        }

        private IEnumerator DisplaySlots()
        {
            Debug.Log("DisplaySlots start, display type slots");
            currentDisplayType = DisplayType.Slots;

            if (loadedSlots == null)
            {
                Task<IEnumerable<Slot>> fetchTask = FetchSlots();
                yield return WaitForTask(fetchTask);
                loadedSlots = fetchTask.Result;
            }

            var slots = loadedSlots;
            Debug.Log("loaded slots: " + slots);

            if (slots == null)
            {
                Debug.Log("slots were not loaded");
                yield break; // Exit the coroutine early
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

            uiManager.DisplayUIAssets(filteredSlots);
        }

        private IEnumerator DisplayCollectionsForSelectedSlot()
        {
            Debug.Log("DisplayCollectionsForSelectedSlot");
            currentDisplayType = DisplayType.Collections;
            Task<IEnumerable<Collection>> fetchTask = FetchCollections(selectedSlot.collections);
            yield return WaitForTask(fetchTask);
            loadedCollections = fetchTask.Result;

            Debug.Log("loadedCollections: " + loadedCollections?.ToString());

            List<UIAsset> convertedUICollections = new List<UIAsset>();

            foreach (var collection in loadedCollections)
            {
                long collectionBalanceCount = 0;
                Slot collectionSlot = loadedSlots.FirstOrDefault(s => s.slotId == collection.slotId);
                Debug.Log("collectionID: " + collection.collectionId + " selectedSlot: " + collectionSlot + " counts: " + collectionSlot.balanceCounts);
                if (collectionSlot.balanceCounts == null)
                {
                    Debug.Log("baalanceCounts is null" + collectionSlot);
                    continue;
                }
                if (collectionSlot.balanceCounts.TryGetValue(collection.collectionId, out collectionBalanceCount))
                {
                    Debug.Log("Count found: " + collectionBalanceCount);
                    convertedUICollections.Add(UIAsset.ConvertToUIAsset(collection, assetExpressionId, (int)collectionBalanceCount));
                }
                else
                {
                    Debug.Log("Collection not in balance");
                }

            }

            IEnumerable<UIAsset> filteredCollections = FilterBySearch(convertedUICollections, currentSearchString);
            Debug.Log("filtered collections: " + filteredCollections);

            uiManager.DisplayUIAssets(filteredCollections);
            Debug.Log("end of display collections state: " + currentDisplayType);
        }




        private IEnumerator DisplayAssetsForSelectedCollection()
        {
            currentDisplayType = DisplayType.Assets;

            Task<IEnumerable<Asset>> fetchTask = FetchAssetsByCollectionId(selectedCollectionId);
            yield return WaitForTask(fetchTask);
            loadedAssets = fetchTask.Result;

            List<UIAsset> convertedUIAssets = new List<UIAsset>();

            foreach (var asset in loadedAssets)
            {
                AssetCacheManager.Instance.AddToCache(asset);
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
                    yield return StartCoroutine(DisplaySlots());
                    break;

                case DisplayType.Collections:
                    yield return StartCoroutine(DisplayCollectionsForSelectedSlot());
                    break;

                case DisplayType.Assets:
                    yield return StartCoroutine(DisplayAssetsForSelectedCollection());
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
            Debug.Log("starting search coroutine on value change");
            debounceCoroutine = StartCoroutine(DebounceSearch());
        }

        private void BackClickedHandler()
        {
            Debug.Log("state before clicking back: " + currentDisplayType);
            switch (currentDisplayType)
            {
                case DisplayType.Assets:
                    Debug.Log("Back from Assets");
                    selectedCollectionId = "";
                    SelectCollection(selectedCollectionId);
                    break;

                case DisplayType.Collections:
                    selectedSlotId = "";
                    Debug.Log("clsoing from collections: slotId: " + slotId);
                    if (string.IsNullOrEmpty(slotId)) // only show slot selection if slotId was not specified
                    {
                        SelectSlot(selectedSlotId);
                    }
                    else  // a slotId was specified, slot selection should not be shown, closing menu instead
                    {
                        HideInventoryUI();
                    }

                    break;

                case DisplayType.Slots:
                    selectedSlotId = "";
                    HideInventoryUI();
                    break;

                default:
                    Debug.Log("defualt state, should not happen, state: " + currentDisplayType);
                    HideInventoryUI();
                    break;
            }
            Debug.Log("state after clicking back: " + currentDisplayType);
        }

        private void UIAssetSelectedHandler(UIAsset asset)
        {
            Debug.Log("Asset selected: " + asset + " current state: " + currentDisplayType);
            
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
                        PlayerPrefs.SetString("AssetLayerSelectedAssetId", asset.UIAssetId);
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
                                    if (closeOnSelection)
                                    {
                                        uiManager.ToggleInventoryUI();
                                    }
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
                            if (closeOnSelection)
                            {
                                uiManager.ToggleInventoryUI();
                            }

                        }
                    }
                    break;

                default:
                    break;
            }
        }



        private void HideInventoryUI()
        {
            Debug.Log("Hideing inventory state:" + currentDisplayType);
            initialSetupDone = false;
            uiManager.HideInventoryUI();

        }

        private void ToggleInventoryUI(bool open)
        {
            Debug.Log("toggling: " + open);
            if (open)
            {
                StartCoroutine(InitialLoading());
            }
            else
            {
                initialSetupDone = false;
            }

        }

        public void SelectSlot(string slotId)
        {
            Debug.Log("Selecting Slot : " + slotId);
            selectedSlotId = slotId;
            if (string.IsNullOrEmpty(slotId))
            {
                currentDisplayType = DisplayType.Slots;
                selectedSlot = null;
                StartCoroutine(DisplaySlots());
                uiManager.UpdateInventoryTitle(menuName);
            }
            else
            {
                if (loadedSlots != null)
                {
                    currentDisplayType = DisplayType.Collections;
                    Debug.Log("loaded slot not null: " + loadedSlots);
                    selectedSlot = loadedSlots.FirstOrDefault(a => a.slotId == selectedSlotId);
                    if (selectedSlot != null)
                    {
                        Debug.Log("Before updating title");
                        uiManager.UpdateInventoryTitle(selectedSlot.slotName);
                    }
                }
                else
                {
                    Debug.Log("loaded slots are null");
                }

                StartCoroutine(DisplayCollectionsForSelectedSlot());
            }
            Debug.Log("after selecting slot state: " + currentDisplayType);
        }

        public void SelectCollection(string collectionId)
        {
            selectedCollectionId = collectionId;
            if (string.IsNullOrEmpty(collectionId))
            {
                selectedCollection = null;
                currentDisplayType = DisplayType.Collections;
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
                currentDisplayType = DisplayType.Assets;
                StartCoroutine(DisplayAssetsForSelectedCollection());


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
                IEnumerable<Collection> collectionInfos = await manager.GetCollectionInfo(collectionIds);
                return collectionInfos;
            }
            catch (Exception e)
            {
                Debug.Log("Fetch Collections fail: " + e.Message);
                return null;
            }
        }


        public async Task<IEnumerable<Slot>> FetchSlots()
        {

            if (!slotIds.Any())
            {
                // Use the GetAppSlots method to retrieve all slot IDs
                slotIds = new List<string>(await manager.GetAppSlots());
            }
            Debug.Log("slotIds: " + slotIds);

            if (slotIds == null || !slotIds.Any())
            {
                return Enumerable.Empty<Slot>();
            }

            // Fetch details for each slot using GetSlotInfo
            List<Slot> slots = new List<Slot>();

            foreach (var slotId in slotIds)
            {
                if (string.IsNullOrEmpty(slotId))
                {
                    continue;
                }
                Debug.Log("in for each: " + slotId);
                SlotInfo slotInfo = await manager.GetSlotInfo(slotId);
                Debug.Log("getting slot balance now");
                var slotBalance = await manager.GetAssetBalance(slotId, true, true);

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
                        balanceCounts = slotBalance as Dictionary<string, long>
                    });
                }
            }

            return slots;
        }

        public async Task<IEnumerable<Asset>> FetchAssetsByCollectionId(string collectionId)
        {
            IEnumerable<Asset> collectionAssets = await manager.GetBalanceOfCollection(collectionId);
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
            return new UIAsset(slot.slotId, slot.slotName, slot.slotImage, "", (int)slot.balanceCounts.Values.Sum(), UIAssetType.Slot);
        }

        public static UIAsset ConvertToUIAsset(Collection collection, string assetExpressionId, int count)
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
                count,
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
                (int)asset.serial,
                UIAssetType.Asset);
        }
    }
}

