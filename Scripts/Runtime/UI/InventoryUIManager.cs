using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using PimDeWitte.UnityMainThreadDispatcher;
using System;
using Task = System.Threading.Tasks.Task;
using UnityEditor;

namespace AssetLayer.Unity
{

    public class InventoryUIManager : MonoBehaviour
    {
        private UIDocument uiDocument;

        public VisualTreeAsset AssetCardTemplate;
        private TemplateContainer clonedTree;

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

        private bool initialElementsHidden = false;
        private int placeholderCardCount = 4;

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
            searchField.RegisterValueChangedCallback(evt =>
            {
                UISearchInitiated?.Invoke(evt.newValue);
            });
        }

        public void RegisterCloseCallback()
        {
            inventoryUI.Q<Button>(UIElementsNames.InventoryCloseButton).RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("Close Button Pressed");
                UICloseInitiated?.Invoke();
            });
        }

        public void RegisterBackCallback()
        {
            inventoryUI.Q<Button>(UIElementsNames.InventoryBackButton).RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("Back Button Pressed");
                UIBackInitiated?.Invoke();
            });
        }



        public void DisplayUIAssets(IEnumerable<UIAsset> uiAssets)
        {
            Debug.Log("DisplayUIAssets: " + uiAssets.ToString());

            if (!initialElementsHidden)
            {
                // If it's the first run-through, hide all existing elements and mark them as placeholders.
                placeholderCardCount = inventoryContainer.childCount;  // Save the initial child count
                for (int i = 0; i < placeholderCardCount; i++)
                {
                    inventoryContainer[i].style.display = DisplayStyle.None;
                }
                initialElementsHidden = true;  // Mark that initial elements have been hidden
            }
            else
            {
                // Remove the old elements, skipping over the initially hidden placeholders.
                int numberOfElementsToRemove = inventoryContainer.childCount - placeholderCardCount;
                for (int i = 0; i < numberOfElementsToRemove; i++)
                {
                    if (inventoryContainer.childCount > placeholderCardCount) // Skip the placeholders.
                    {
                        inventoryContainer.RemoveAt(placeholderCardCount); // Always remove the first non-placeholder element.
                    }
                    else
                    {
                        Debug.LogWarning("inventoryContainer has no more elements to remove, except placeholders.");
                        break;
                    }
                }
            }




            Debug.Log("After hide");

            foreach (var uiAsset in uiAssets)
            {
                StartCoroutine(DownloadAndDisplayAssetImage(uiAsset));
            }

        }



        private async Task DownloadAndDisplayAssetImageAsync(UIAsset uiAsset)
        {
            if (uiAsset == null)
            {
                return;
            }

            try
            {
                Texture2D textureOnMainThread = await LoadImageAsync(uiAsset.ImageURL);

                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    // VisualElement clonedTree = null;
                    VisualElement nftCard = null;
                    Label assetNameLabel = null;
                    VisualElement assetImage = null;
                    Label assetCountLabel = null;

                    try
                    {
                        Debug.Log("for each asset: " + uiAsset);
                        Debug.Log("assetcardtemplate: " + AssetCardTemplate);
                        VisualTreeAsset assetCardTemplateResource = Resources.Load("AssetCard") as VisualTreeAsset;

                        Debug.Log("assetcardtemplate: " + assetCardTemplateResource);
                        if (AssetCardTemplate != null)
                        {
                            clonedTree = assetCardTemplateResource.CloneTree();
                        }
                        else
                        {
                            Debug.Log("template even null");
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in cloning tree: " + e.Message);
                    } 

                    try
                    {
                        if (clonedTree != null)
                        {
                            nftCard = clonedTree.Q<VisualElement>(UIElementsNames.NftCard);
                            Debug.Log("for each asset:2 " + uiAsset);
                        }
                        else
                        {
                            Debug.Log("asset card template not found");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in querying NftCard: " + e.Message);
                    }

                    try
                    {
                        nftCard.RegisterCallback<ClickEvent>(evt => UIAssetSelected.Invoke(uiAsset));
                        Debug.Log("for each asset:3 " + uiAsset);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in registering callback: " + e.Message);
                    }

                    try
                    {
                        assetNameLabel = nftCard.Q<Label>(UIElementsNames.CollectionName);
                        assetImage = nftCard.Q<VisualElement>(UIElementsNames.MenuViewImage);
                        assetCountLabel = nftCard.Q<Label>(UIElementsNames.NftCount);
                        Debug.Log("for each asset:4 " + uiAsset);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in querying UI elements: " + e.Message);
                    }

                    try
                    {
                        assetNameLabel.text = uiAsset.Name;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in setting asset name: " + e.Message);
                    }

                    try
                    {
                        assetCountLabel.text = (uiAsset.AssetType == UIAssetType.Asset ? "#" : "") + uiAsset.CountOrSerial.ToString();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in setting asset count: " + e.Message);
                    }

                    try
                    {
                        Debug.Log("Before image load: " + uiAsset.ImageURL);
                        Debug.Log("After load: " + textureOnMainThread);
                        assetImage.style.backgroundImage = new StyleBackground(textureOnMainThread);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in setting background image: " + e.Message);
                    }

                    try
                    {
                        inventoryContainer.Add(nftCard);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in adding nftCard to inventoryContainer: " + e.Message);
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError("Error in main try-catch: " + e.Message);
            }


        }


        private IEnumerator DownloadAndDisplayAssetImage(UIAsset uiAsset)
        {
            if (uiAsset == null)
            {
                yield break;
            }

                Texture2D textureOnMainThread = null;
                yield return StartCoroutine(LoadImageCoroutine(uiAsset.ImageURL, (texture) => { textureOnMainThread = texture; }));


            
            Label assetNameLabel = null;
            VisualElement assetImage = null;
            Label assetCountLabel = null;

            VisualTreeAsset assetCardTemplateResource = Resources.Load("AssetCard") as VisualTreeAsset;
            TemplateContainer clonedTree2 = null;
            VisualElement nftCard2 = null;

            try
            {
                if (assetCardTemplateResource != null)
                {
                    clonedTree2 = assetCardTemplateResource.Instantiate();
                }
                else
                {
                    Debug.Log("template even null");
                }

            }
            catch (Exception e)
            {
                Debug.LogError("Error in cloning tree: " + e.Message);
            }
            if (clonedTree2 == null)
            {
                yield break;
            }
            try
                    {
                    if (clonedTree2 != null)
                    {
                        nftCard2 = clonedTree2.Q<VisualElement>(UIElementsNames.NftCard);
                    }
                    else
                    {
                        Debug.Log("asset card template not found");
                    }
                        nftCard2.RegisterCallback<ClickEvent>(evt => UIAssetSelected.Invoke(uiAsset));
                        Debug.Log("for each asset:3 " + uiAsset);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in registering callback: " + e.Message);
                    }

                    try
                    {
                        assetNameLabel = nftCard2.Q<Label>(UIElementsNames.CollectionName);
                        assetImage = nftCard2.Q<VisualElement>(UIElementsNames.MenuViewImage);
                        assetCountLabel = nftCard2.Q<Label>(UIElementsNames.NftCount);
                        Debug.Log("for each asset:4 " + uiAsset);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in querying UI elements: " + e.Message);
                    }

                    try
                    {
                        assetNameLabel.text = uiAsset.Name;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in setting asset name: " + e.Message);
                    }

                    try
                    {
                        assetCountLabel.text = (uiAsset.AssetType == UIAssetType.Asset ? "#" : "") + uiAsset.CountOrSerial.ToString();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in setting asset count: " + e.Message);
                    }

                    try
                    {
                        Debug.Log("Before image load: " + uiAsset.ImageURL);
                        Debug.Log("After load: " + textureOnMainThread);
                        assetImage.style.backgroundImage = new StyleBackground(textureOnMainThread);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in setting background image: " + e.Message);
                    }

                    try
                    {
                        inventoryContainer.Add(nftCard2);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error in adding nftCard to inventoryContainer: " + e.Message);
                    }
  


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

        public IEnumerator LoadImageCoroutine(string imageUrl, Action<Texture2D> callback)
        {
            bool isCompleted = false;
            Texture2D resultTexture = null;

            imageDownloader.LoadImage(imageUrl, result =>
            {
                isCompleted = true;
                resultTexture = result;
            });

            yield return new WaitUntil(() => isCompleted);

            callback?.Invoke(resultTexture);
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
}
