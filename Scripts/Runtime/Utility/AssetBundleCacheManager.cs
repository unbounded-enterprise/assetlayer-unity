using System.Collections.Generic;
using UnityEngine;

namespace AssetLayer.Unity
{

    public class AssetBundleCacheManager
    {
        // The instance for the Singleton pattern
        private static AssetBundleCacheManager _instance;
        public static AssetBundleCacheManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AssetBundleCacheManager();
                return _instance;
            }
        }

        // Dictionary to cache downloaded AssetBundles.
        public Dictionary<string, AssetBundle> CachedBundles { get; private set; } = new Dictionary<string, AssetBundle>();

        private AssetBundleCacheManager() { }  // Make the constructor private to prevent additional instantiations
    }
}
