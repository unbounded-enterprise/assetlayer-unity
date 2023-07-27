using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalProximity : MonoBehaviour
{
    public Material defaultMaterial;
    public Material nearMaterial;
    public Material enterMaterial;
    private MeshRenderer meshRenderer; // Cache the MeshRenderer
    public string NftIdToLoad; 

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
            if (NftIdToLoad != "")
            {
                SceneImporter.Instance.SetNftId(NftIdToLoad);
            }
            SceneImporter.Instance.LoadScene();
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
