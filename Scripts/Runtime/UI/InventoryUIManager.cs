using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryUIManager
{
    private VisualElement inventoryUI;
    private VisualElement inventoryContainer;
    private VisualTreeAsset nftCardTemplate;

    public delegate void UISelectionHandler(UIAsset selectedUIAsset);

    public event UISelectionHandler UIAssetSelected;

    public static event System.Action<Collection> CollectionSelected;

    public static event System.Action<bool> OnInventoryToggled;

    public delegate void UISearchHandler(string searchText);
    public event UISearchHandler UISearchInitiated;

    public delegate void UIBackHandler();
    public event UIBackHandler UIBackInitiated;

    public delegate void UICloseHandler();
    public event UICloseHandler UICloseInitiated;



    public InventoryUIManager(UIDocument uiDocument, VisualTreeAsset template)
    {
        var root = uiDocument.rootVisualElement;
        inventoryUI = root.Q<VisualElement>(UIElementsNames.AssetlayerInventory);
        inventoryContainer = root.Q<VisualElement>(UIElementsNames.InventoryContainer);
        nftCardTemplate = template;
        inventoryUI.style.display = DisplayStyle.None;

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
        inventoryContainer.Clear();

        foreach (var uiAsset in uiAssets)
        {
            var clonedTree = nftCardTemplate.CloneTree();
            var nftCard = clonedTree.Q<VisualElement>(UIElementsNames.NftCard);

            // Note: Adjust this callback if you want different behaviors based on asset type
            nftCard.RegisterCallback<ClickEvent>(evt => UIAssetSelected.Invoke(uiAsset));

            var assetNameLabel = nftCard.Q<Label>(UIElementsNames.CollectionName);
            var assetImage = nftCard.Q<VisualElement>(UIElementsNames.MenuViewImage);
            var assetCountLabel = nftCard.Q<Label>(UIElementsNames.NftCount);

            assetNameLabel.text = uiAsset.Name;
            assetCountLabel.text = (uiAsset.AssetType == UIAssetType.Asset ? "#" : "") + uiAsset.CountOrSerial.ToString();

            Texture2D texture = ImageLoader.LoadImage(uiAsset.ImageURL);
            assetImage.style.backgroundImage = new StyleBackground(texture);

            inventoryContainer.Add(nftCard);
        }
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
