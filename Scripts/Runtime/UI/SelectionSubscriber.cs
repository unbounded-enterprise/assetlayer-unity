using UnityEngine;
using UnityEngine.Events;
using AssetLayer.Unity;

namespace AssetLayer.Unity
{

    public class SelectionSubscriber : MonoBehaviour
    {
        // Create a custom UnityEvent that accepts an Asset parameter.
        [System.Serializable]
        public class AssetUnityEvent : UnityEvent<Asset> { }

        // This is the event you can set in the Unity Editor.
        public AssetUnityEvent onAssetSelectedInEditor;

        [System.Obsolete]
        private void Start()
        {
            // Safety check: Warn if no method has been assigned in the Unity Editor.
            if (onAssetSelectedInEditor == null || onAssetSelectedInEditor.GetPersistentEventCount() == 0)
            {
                Debug.LogWarning("No method has been assigned to the onAssetSelectedInEditor event!");
                return;
            }

            // Find all Inventory instances.
            Inventory[] inventories = FindObjectsOfType<Inventory>();

            // Subscribe to each of them.
            foreach (Inventory inventory in inventories)
            {
                inventory.onAssetSelection.AddListener(HandleAssetSelection);
            }
        }

        [System.Obsolete]
        private void OnDestroy()
        {
            // Clean up - unsubscribe from all Inventory instances.
            Inventory[] inventories = FindObjectsOfType<Inventory>();
            foreach (Inventory inventory in inventories)
            {
                inventory.onAssetSelection.RemoveListener(HandleAssetSelection);
            }
        }

        private void HandleAssetSelection(Asset selectedAsset)
        {
            // When the onAssetSelection event in Inventory is fired, this method will be called.
            // It will then invoke the method set in the Unity Editor.
            onAssetSelectedInEditor?.Invoke(selectedAsset);
        }
    }
}