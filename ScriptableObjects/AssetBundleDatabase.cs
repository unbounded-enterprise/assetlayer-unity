using System.Collections.Generic;
using UnityEngine;

namespace AssetLayer.Unity
{
    [CreateAssetMenu(fileName = "AssetBundleDatabase", menuName = "AssetLayer/Database", order = 1)]
    public class AssetBundleDatabase : ScriptableObject
    {
        public List<AssetBundleData> bundles = new List<AssetBundleData>();
    }

    [System.Serializable]
    public class AssetBundleData
    {
        public string prefabPath;
        public string hash;
        public string collectionId;
        public string collectionName;
        public string slotId;
        public string version;
    }
}


