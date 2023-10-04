using UnityEngine;

namespace AssetLayer.Unity
{
    public class SingletonGameobject : MonoBehaviour
    {
        // Static reference to the GameManager instance
        private static SingletonGameobject instance;

        private void Awake()
        {
            // Check if an instance already exists
            if (instance == null)
            {
                // If not, set this instance as the singleton
                instance = this;
                // Make sure this object persists between scenes
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // If an instance already exists, destroy this duplicate
                Destroy(gameObject);
            }
        }
    }
}
