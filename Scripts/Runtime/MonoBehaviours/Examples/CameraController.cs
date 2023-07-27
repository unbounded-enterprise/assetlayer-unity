using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    public float rotationSpeed = 1;
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 10f;
    private float currentZoom;
    public float groundLevel = 0.0f; // Set this to your ground level

    float mouseX, mouseY;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        currentZoom = minZoom + ((maxZoom + minZoom) / 10.0f);
    }

    void Update()
    {
        // If the player object is not assigned, try to find it
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    void LateUpdate()
    {
        // Only proceed if the player object is assigned
        if (player != null)
        {
            // Only update camera rotation when right mouse button is pressed
            if (Input.GetMouseButton(1))
            {
                mouseX += Input.GetAxis("Mouse X") * rotationSpeed;
                mouseY -= Input.GetAxis("Mouse Y") * rotationSpeed;
                mouseY = Mathf.Clamp(mouseY, -35, 60);

                transform.LookAt(player.transform);
                // player.transform.rotation = Quaternion.Euler(0, mouseX, 0);
                transform.rotation = Quaternion.Euler(mouseY, mouseX, 0);
            }

            // Zooming with Mouse Wheel
            currentZoom -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            Vector3 newPosition = player.transform.position - transform.forward * currentZoom;

            // Check if the new position is below the ground level
            if (newPosition.y < groundLevel)
            {
                newPosition.y = groundLevel;
            }

            transform.position = newPosition;
        }
    }
}
