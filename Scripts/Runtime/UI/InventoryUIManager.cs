using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Assetlayer.Inventory;
using System.Threading.Tasks;
using PimDeWitte.UnityMainThreadDispatcher;

public class InventoryUIManager : MonoBehaviour
{
    private UIDocument uiDocument;

    public VisualTreeAsset AssetCardTemplate;

    private VisualElement inventoryUI;
    private VisualElement inventoryContainer;

    public GameObject YourUI;
    public string toggleElementName;
    private VisualElement toggleElement;

    public delegate void UISelectionHandler(UIAsset selectedUIAsset);
    public event UISelectionHandler UIAssetSelected;

    public static event System.Action<bool> OnInventoryToggled;

    public delegate void UISearchHandler(string searchText);
    public event UISearchHandler UISearchInitiated;

    public delegate void UIBackHandler();
    public event UIBackHandler UIBackInitiated;

    public delegate void UICloseHandler();
    public event UICloseHandler UICloseInitiated;

    private ImageDownloaderManager imageDownloader;

    private void Awake()
    {
        imageDownloader = GetComponent<ImageDownloaderManager>();
        InitializeUI();
        
    }

    private void InitializeUI()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;
        inventoryUI = root.Q<VisualElement>(UIElementsNames.AssetlayerInventory);
        inventoryContainer = root.Q<VisualElement>(UIElementsNames.InventoryContainer);
        inventoryUI.style.display = DisplayStyle.None;

        if (YourUI != null)
        {
            UIDocument otherUIDocument = YourUI.GetComponent<UIDocument>();
            if (otherUIDocument != null)
            {
                var rootYourUI = otherUIDocument.rootVisualElement;
                toggleElement = rootYourUI.Q<VisualElement>(toggleElementName);

                if (toggleElement != null)
                {
                    toggleElement.RegisterCallback<ClickEvent>(evt => ToggleInventoryUI());
                }
                else
                {
                    Debug.LogError($"VisualElement with name {toggleElementName} not found!");
                }
            }
            else
            {
                Debug.LogError("Provided GameObject does not have a UIDocument component!");
            }
        }

        // Register the event callbacks
        RegisterSearchCallback();
        RegisterCloseCallback();
        RegisterBackCallback();
    }

    public void RegisterSearchCallback()
    {
        var searchField = inventoryUI.Q<TextField>(UIElementsNames.InventoryTextField);
        searchField.RegisterValueChangedCallback(evt => {
            UISearchInitiated?.Invoke(evt.newValue);
        });
    }

    public void RegisterCloseCallback()
    {
        inventoryUI.Q<Button>(UIElementsNames.InventoryCloseButton).RegisterCallback<ClickEvent>(evt => {
            Debug.Log("Close Button Pressed");
            UICloseInitiated?.Invoke();
        });
    }

    public void RegisterBackCallback()
    {
        inventoryUI.Q<Button>(UIElementsNames.InventoryBackButton).RegisterCallback<ClickEvent>(evt => {
            Debug.Log("Back Button Pressed");
            UIBackInitiated?.Invoke();
        });
    }




    public void DisplayUIAssets(IEnumerable<UIAsset> uiAssets)
    {
        Debug.Log("DisplayUIAssets: " + uiAssets.ToString());
        if (inventoryContainer.childCount > 0)
        {
            inventoryContainer.Clear();
        }
        Debug.Log("After clear");
        foreach (var uiAsset in uiAssets)
        {
            DownloadAndDisplayAssetImage(uiAsset);
        }
    }

    private async Task DownloadAndDisplayAssetImage(UIAsset uiAsset)
    {
        if (uiAsset == null)
        {
            return;
        }
        
        Texture2D textureOnMainThread = await LoadImageAsync(uiAsset.ImageURL);

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.Log("for each asset: " + uiAsset);
            var clonedTree = AssetCardTemplate.CloneTree();
            var nftCard = clonedTree.Q<VisualElement>(UIElementsNames.NftCard);
            Debug.Log("for each asset:2 " + uiAsset);
            nftCard.RegisterCallback<ClickEvent>(evt => UIAssetSelected.Invoke(uiAsset));
            Debug.Log("for each asset:3 " + uiAsset);
            var assetNameLabel = nftCard.Q<Label>(UIElementsNames.CollectionName);
            var assetImage = nftCard.Q<VisualElement>(UIElementsNames.MenuViewImage);
            var assetCountLabel = nftCard.Q<Label>(UIElementsNames.NftCount);
            Debug.Log("for each asset:4 " + uiAsset);
            assetNameLabel.text = uiAsset.Name;
            assetCountLabel.text = (uiAsset.AssetType == UIAssetType.Asset ? "#" : "") + uiAsset.CountOrSerial.ToString();
            Debug.Log("Before image load: " + uiAsset.ImageURL);
            Debug.Log("After load" + textureOnMainThread);
            assetImage.style.backgroundImage = new StyleBackground(textureOnMainThread);
            inventoryContainer.Add(nftCard);
        });
    }

    public Task<Texture2D> LoadImageAsync(string imageUrl)
    {
        var tcs = new TaskCompletionSource<Texture2D>();

        imageDownloader.LoadImage(imageUrl, result =>
        {
            tcs.SetResult(result);
        });

        return tcs.Task;
    }




    public void ToggleInventoryUI()
    {
        if (inventoryUI.resolvedStyle.display == DisplayStyle.None)
        {
            inventoryUI.style.display = DisplayStyle.Flex;
            OnInventoryToggled?.Invoke(true);
        }
        else
        {
            inventoryUI.style.display = DisplayStyle.None;
            OnInventoryToggled?.Invoke(false);
        }
    }


    public void HideInventoryUI()
    {
        inventoryUI.style.display = DisplayStyle.None;
        OnInventoryToggled?.Invoke(false);
    }

    public void UpdateInventoryTitle(string newTitle)
    {
        var titleLabel = inventoryUI.Q<Label>(UIElementsNames.InventoryHeader);
        if (titleLabel != null)
        {
            titleLabel.text = newTitle;
        }
        else
        {
            Debug.LogWarning("Inventory title label not found!");
        }
    }

}
