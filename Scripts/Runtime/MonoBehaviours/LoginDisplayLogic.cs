using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Don't forget to include this to access Button and Text classes

public class LoginDisplayLogic : MonoBehaviour
{
    public Text header;
    public Button loginButton;
    public GameObject emailInput;

    // Start is called before the first frame update
    void Start()
    {
        // Handle UI elements based on the current platform
        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            // Only show the loginButton and header
            header.text = "Please Login (opens browser)";
            loginButton.gameObject.SetActive(true);
            emailInput.SetActive(false);
        }
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // Hide everything but the header
            header.text = "Processing login";
            loginButton.gameObject.SetActive(false);
            emailInput.SetActive(false);
        }
        else if (Application.platform == RuntimePlatform.Android ||
                 Application.platform == RuntimePlatform.IPhonePlayer)
        {
            // Show everything
            header.text = "Enter your email to login";
            loginButton.gameObject.SetActive(true);
            emailInput.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Your logic here
    }
}
