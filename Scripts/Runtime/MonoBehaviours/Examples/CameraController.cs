using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Assetlayer.Inventory;

namespace Assetlayer.Controls
{

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
            player = GameObject.FindGameObjectWithTag("Player");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            currentZoom = minZoom + ((maxZoom + minZoom) / 10.0f);
            InventoryUIManager.OnInventoryToggled += InventoryToggled;
        }

        public void InventoryToggled(bool showing)
        {
            
            if (showing)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            } else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
        }

        void Update()
        {
            // If the player object is not assigned, try to find it
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }

            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    mouseX += touch.deltaPosition.x * rotationSpeed;
                    mouseY -= touch.deltaPosition.y * rotationSpeed;
                    mouseY = Mathf.Clamp(mouseY, -35, 60);
                }
            }

            // Mobile pinch to zoom
            if (Input.touchCount == 2)
            {
                Touch touch1 = Input.GetTouch(0);
                Touch touch2 = Input.GetTouch(1);

                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;

                float prevMagnitude = (touch1PrevPos - touch2PrevPos).magnitude;
                float currentMagnitude = (touch1.position - touch2.position).magnitude;

                float difference = currentMagnitude - prevMagnitude;

                currentZoom -= difference * zoomSpeed * Time.deltaTime;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
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
                    mouseX += Input.GetAxis("Mouse X") * rotationSpeed * 0.5f;
                    mouseY -= Input.GetAxis("Mouse Y") * rotationSpeed * 0.5f;
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
}