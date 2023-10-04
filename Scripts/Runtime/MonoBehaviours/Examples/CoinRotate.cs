using UnityEngine;

namespace AssetLayer.Unity
{

    public class RotateCoin : MonoBehaviour
    {
        // Rotation speed
        public float rotationSpeed = 100f;

        // Update is called once per frame
        void Update()
        {
            // Rotate the coin
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
