using System.Collections.Generic;
using UnityEngine;

namespace AssetLayer.Unity
{
    public class AssetCacheManager
    {
        // The instance for the Singleton pattern
        private static AssetCacheManager _instance;
        public static AssetCacheManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AssetCacheManager();
                return _instance;
            }
        }

        // Dictionary to cache Asset objects by assetId.
        public Dictionary<string, Asset> CachedAssets { get; private set; } = new Dictionary<string, Asset>();

        private AssetCacheManager() { }  // Make the constructor private to prevent additional instantiations

        // Optional: Utility methods for adding, removing, and checking assets in cache
        public void AddToCache(Asset asset)
        {
            if (asset != null && !string.IsNullOrEmpty(asset.assetId))
            {
                CachedAssets[asset.assetId] = asset;
            }
        }

        public bool IsInCache(string assetId)
        {
            return CachedAssets.ContainsKey(assetId);
        }

        public Asset GetFromCache(string assetId)
        {
            CachedAssets.TryGetValue(assetId, out Asset asset);
            return asset;
        }

        public void RemoveFromCache(string assetId)
        {
            if (IsInCache(assetId))
            {
                CachedAssets.Remove(assetId);
            }
        }
    }
}
