using UnityEngine;
using TMPro;

namespace AssetLayer.Unity
{

    public class TutorialText2 : MonoBehaviour
    {
        private TextMeshPro textMesh;

        private void Awake()
        {
            // Get the TextMeshPro component attached to the same GameObject this script is attached to
            textMesh = GetComponent<TextMeshPro>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                textMesh.enabled = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                textMesh.enabled = false;
            }
        }
    }
}
