using UnityEngine;

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
            GameObject groundCheckGameObject =  GameObject.Find("GroundCheck");
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
    }

    void FixedUpdate()
    {
        // Find main camera if it's null
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
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
    }
}
