using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //Changed this from UnityEngine.UIElements
using System;
using TMPro;

namespace AssetLayer.Unity
{
    public class InventoryUIManagerUnityUI : MonoBehaviour
    {
        public Transform inventoryContainer; // Parent object for the inventory items
        [SerializeField] private TMP_InputField searchField;
        [SerializeField] private TextMeshProUGUI titleLabel;
        private ImageDownloaderManager imageDownloader;

        [SerializeField] private GameObject assetCardTemplate;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject inventoryUIGameObject;

        public delegate void UISelectionHandler(UIAsset selectedUIAsset);
        public event UISelectionHandler UIAssetSelected;

        public static event System.Action<bool> OnInventoryToggled;

        public delegate void UISearchHandler(string searchText);
        public event UISearchHandler UISearchInitiated;

        public delegate void UIBackHandler();
        public event UIBackHandler UIBackInitiated;

        public delegate void UICloseHandler();
        public event UICloseHandler UICloseInitiated;

        public float animationTime = 0.3f;




        public GameObject toggleElement; // The button or element to toggle the inventory UI

        private void Awake()
        {
            imageDownloader = GetComponent<ImageDownloaderManager>();
            InitializeUI();

        }

        private void InitializeUI()
        {
            if (inventoryUIGameObject == null || inventoryContainer == null || searchField == null || closeButton == null || backButton == null)
            {
                Debug.LogError("Please assign all the public GameObjects and UI Components in the inspector.");
                return;
            }

            inventoryUIGameObject.SetActive(false); // Equivalent to setting display to none

            if (toggleElement != null)
            {
                Button toggleButton = toggleElement.GetComponent<Button>();
                if (toggleButton != null)
                    toggleButton.onClick.AddListener(ToggleInventoryUI);
                else
                    Debug.LogError($"GameObject with name {toggleElement.name} does not have a Button component!");
            }

            RegisterSearchCallback();
            RegisterCloseCallback();
            RegisterBackCallback();
        }

        public void RegisterSearchCallback()
        {
            if (searchField != null)
            {
                searchField.onValueChanged.AddListener((string searchText) =>
                {
                    UISearchInitiated?.Invoke(searchText);
                });
            }
            else
            {
                Debug.LogError("TMP Search Field is not assigned in the Inspector.");
            }
        }

        public void RegisterCloseCallback()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    Debug.Log("Close Button Pressed");
                    UICloseInitiated?.Invoke();
                });
            }
            else
            {
                Debug.LogError("Close Button is not assigned in the Inspector.");
            }
        }

        public void RegisterBackCallback()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() =>
                {
                    Debug.Log("Back Button Pressed");
                    UIBackInitiated?.Invoke();
                });
            }
            else
            {
                Debug.LogError("Back Button is not assigned in the Inspector.");
            }
        }

        public void ToggleInventoryUI()
        {
            if (inventoryUIGameObject != null)
            {
                if (!inventoryUIGameObject.activeSelf)
                {
                    inventoryUIGameObject.SetActive(true);
                    OnInventoryToggled?.Invoke(true);
                    StartCoroutine(ScaleObject(Vector3.zero, Vector3.one, animationTime, () =>
                    {
                       
                    }));
                }
                else
                {
                    StartCoroutine(ScaleObject(Vector3.one, Vector3.zero, animationTime, () =>
                    {
                        inventoryUIGameObject.SetActive(false);
                        OnInventoryToggled?.Invoke(false);
                    }));
                }
            }
            else
            {
                Debug.LogError("InventoryUI GameObject is not assigned in the Inspector.");
            }
        }

        public void HideInventoryUI()
        {
            if (inventoryUIGameObject != null)
            {
                StopAllCoroutines(); // Stop ongoing animations if any
                OnInventoryToggled?.Invoke(false);
                StartCoroutine(ScaleObject(Vector3.one, Vector3.zero, animationTime, () =>
                {
                    inventoryUIGameObject.SetActive(false);
                    
                }));
            }
            else
            {
                Debug.LogError("InventoryUI GameObject is not assigned in the Inspector.");
            }
        }

        private IEnumerator ScaleObject(Vector3 startScale, Vector3 endScale, float time, Action onComplete)
        {
            float elapsedTime = 0;
            inventoryUIGameObject.transform.localScale = startScale;
            while (elapsedTime < time)
            {
                inventoryUIGameObject.transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / time);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            inventoryUIGameObject.transform.localScale = endScale;
            onComplete?.Invoke();
        }

        public void UpdateInventoryTitle(string newTitle)
        {
            if (titleLabel != null)
            {
                titleLabel.text = newTitle;
            }
        }

        public void DisplayUIAssets(IEnumerable<UIAsset> uiAssets)
        {
            Debug.Log("DisplayUIAssets: " + uiAssets.ToString());

            foreach (Transform child in inventoryContainer.transform)
            {
                Destroy(child.gameObject); // Remove the old elements.
            }

            Debug.Log("After hide");
            foreach (var uiAsset in uiAssets)
            {
                StartCoroutine(DownloadAndDisplayAssetImage(uiAsset));
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

            GameObject clonedCard = Instantiate(assetCardTemplate, inventoryContainer.transform);

            // Get AssetCardElements component from the instantiated prefab
            AssetCardElements assetCardElements = clonedCard.GetComponent<AssetCardElements>();
            if (assetCardElements != null)
            {
                assetCardElements.AssetNameLabel.text = uiAsset.Name;
                assetCardElements.AssetCountLabel.text = (uiAsset.AssetType == UIAssetType.Asset ? "#" : "") + uiAsset.CountOrSerial.ToString();
                assetCardElements.AssetImage.sprite = Sprite.Create(textureOnMainThread, new Rect(0.0f, 0.0f, textureOnMainThread.width, textureOnMainThread.height), new Vector2(0.5f, 0.5f), 100.0f);
            }

            assetCardElements.selectButton.onClick.AddListener(() => UIAssetSelected.Invoke(uiAsset));
            LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryContainer.GetComponent<RectTransform>());
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

    }
}
