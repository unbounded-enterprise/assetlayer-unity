using UnityEngine;
using UnityEngine.UI;

namespace AssetLayer.Unity
{
    public class QuitApplicationOnClick : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            // Add a click event listener to the Button component
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(QuitApplication);
            }
        }

        // Function to quit the application
        void QuitApplication()
        {
            Debug.Log("Quitting Application");

#if UNITY_ANDROID
            // For Android, you may want to use Android-specific APIs to quit.
            // Optionally use AndroidJavaObject for more refined control.
            Application.Quit();
#elif UNITY_IOS
            // For iOS, you might want to do something else as Apple discourages quitting the application programmatically.
            // E.g., Return to main menu
            Debug.Log("Can't quit on iOS, doing something else instead.");
#elif UNITY_WEBGL
            // For WebGL, maybe just navigate to another web page or reload the current page
            // Use JavaScript to accomplish this
            Application.ExternalEval("window.open('about:blank','_self').close();");
#else
            // For standalone Windows and Mac builds
            Application.Quit();
#endif
        }
    }
}
