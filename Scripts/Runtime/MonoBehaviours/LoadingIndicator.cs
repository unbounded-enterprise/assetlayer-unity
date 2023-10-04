using UnityEngine;

namespace AssetLayer.Unity
{

    public class LoadingIndicator : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        void Update()
        {
            transform.Rotate(0, 0, -100 * Time.deltaTime);
        }
    }
}
