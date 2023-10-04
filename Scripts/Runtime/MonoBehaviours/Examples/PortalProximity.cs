using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetLayer.Unity
{

    public class PortalProximity : MonoBehaviour
    {
        public Material defaultMaterial;
        public Material nearMaterial;
        public Material enterMaterial;
        private MeshRenderer meshRenderer; // Cache the MeshRenderer
        public string AssetIdToLoad;
        public string SceneNameToLoad;

        [SerializeField]
        private SceneImporter sceneImporter;

        void Start()
        {
            meshRenderer = GetComponentInParent<MeshRenderer>(); // Get the MeshRenderer from the parent
            if (meshRenderer == null)
            {
                Debug.LogError("Parent of this GameObject does not have a MeshRenderer");
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                meshRenderer.material = nearMaterial;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                meshRenderer.material = defaultMaterial;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                meshRenderer.material = enterMaterial;

                // Check if SceneNameToLoad is specified, otherwise use the NftIdToLoad
                if (!string.IsNullOrEmpty(SceneNameToLoad))
                {
                    SceneManager.LoadScene(SceneNameToLoad);
                }
                else
                {
                    if (!string.IsNullOrEmpty(AssetIdToLoad))
                    {
                        sceneImporter.SetAssetId(AssetIdToLoad);

                    }
                    sceneImporter.LoadScene();

                }
            }
        }

        void OnCollisionExit(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                meshRenderer.material = nearMaterial;

            }
        }
    }
}
