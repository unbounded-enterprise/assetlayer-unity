using Unity.VisualScripting;
using UnityEngine;

namespace AssetLayer.Unity
{

    public class SpherePlayerController : MonoBehaviour
    {
        public float moveSpeed = 5.0f;
        public float deceleration = 0.9f;
        public float jumpForce = 2.0f;
        public float gravityScale = 1.5f;
        public Transform groundCheck; // Position marking where to check if the player is grounded.
        public float groundCheckRadius; // Radius of the ground check sphere.
        public LayerMask groundLayer; // Layer(s) to consider as ground.
        public AudioSource jumpSound; // AudioSource to play jump sound.
        public float respawnHeight = -10f;

        private bool isGrounded; // Whether or not the player is currently grounded.
        private Rigidbody rb;
        private Camera mainCamera;

        private Vector2 touchStartPos; // To store the start position of touch
        private bool isDragging = false; // To check if the user is dragging the touch

        public Vector3 lastPlayerDirection;


        void Start()
        {
            rb = GetComponent<Rigidbody>();
            mainCamera = Camera.main;
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            // Find main camera if it's null
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Find groundCheck if it's null
            if (groundCheck == null)
            {
                GameObject groundCheckGameObject = GameObject.Find("GroundCheck");
                if (groundCheckGameObject != null)
                {
                    groundCheck = groundCheckGameObject.transform;
                }

            }

            // Find jumpSound if it's null
            if (jumpSound == null)
            {
                jumpSound = GetComponentInChildren<AudioSource>();
            }

            if (transform.position.y < respawnHeight)
            {
                // Reset the player's position
                transform.position = new Vector3(0, 1, 0);
                // Reset the player's velocity
                rb.velocity = Vector3.zero;
                // Reset the player's angular velocity
                rb.angularVelocity = Vector3.zero;
            }
            // Jumping
            if (Input.GetButtonDown("Jump"))
            {
                rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
                if (jumpSound != null)
                {
                    jumpSound.Play(); // Play jump sound.
                }
            }

            // Mobile Jumping
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                // Start touch
                if (touch.phase == TouchPhase.Began)
                {
                    touchStartPos = touch.position;
                }
                // End touch
                else if (touch.phase == TouchPhase.Ended && isDragging)
                {
                    isDragging = false;
                    rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
                    if (jumpSound != null)
                    {
                        jumpSound.Play();
                    }
                }
                // Drag touch
                else if (touch.phase == TouchPhase.Moved)
                {
                    isDragging = true;
                }
            }
        }

        void FixedUpdate()
        {
            // Find main camera if it's null
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            if (mainCamera == null)
            {
                return;
            }

            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
            movement = mainCamera.transform.TransformDirection(movement);
            movement.y = 0f;

            rb.AddForce(movement * moveSpeed);

            // Additional gravity
            rb.AddForce(new Vector3(0, -gravityScale * rb.mass * Physics.gravity.y, 0));

            // Apply deceleration
            if (movement.magnitude < 0.01f)
            {
                rb.velocity = rb.velocity * deceleration;
            }

            // Mobile Controls
            if (isDragging)
            {
                Vector2 touchDelta = (Vector2)Input.GetTouch(0).position - touchStartPos;
                float mobileMult = 3f;
                moveHorizontal = mobileMult * touchDelta.x / Screen.width; // Normalize to range [-1,1]
                moveVertical = mobileMult * touchDelta.y / Screen.height;  // Normalize to range [-1,1]

                movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
                movement = mainCamera.transform.TransformDirection(movement);
                movement.y = 0f;
                // Record the last direction of movement


                rb.AddForce(movement * moveSpeed * 10);

            }
            lastPlayerDirection = movement.normalized;
            if (lastPlayerDirection != Vector3.zero) // Ensure the player has moved
            {
                float cameraFollowSpeed = 0.66f; // Adjust this to control the speed at which the camera follows
                Quaternion targetRotation = Quaternion.LookRotation(lastPlayerDirection);
                mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, Time.deltaTime * cameraFollowSpeed);
            }
        }
    }
}