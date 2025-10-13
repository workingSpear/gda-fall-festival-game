using UnityEngine;

public class Player : MonoBehaviour
{
    public enum PlayerMode
    {
        Player1,
        Player2
    }

    [Header("Player Settings")]
    public PlayerMode playerMode = PlayerMode.Player1;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Gravity Settings")]
    public float normalGravityScale = 2f;
    public float peakGravityScale = 4f;
    [Tooltip("Gravity at the peak of jump for hangtime effect")]
    public float hangtimeGravityScale = 0.5f;
    [Tooltip("Velocity threshold to trigger hangtime (lower = longer hangtime)")]
    public float hangtimeThreshold = 2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Jump Feel")]
    [Tooltip("Time window to buffer jump input before landing")]
    public float jumpBufferTime = 0.15f;
    [Tooltip("Time window to allow jump after leaving ground")]
    public float coyoteTime = 0.15f;
    [Tooltip("Minimum time between jumps to prevent double jumping")]
    public float jumpCooldown = 0.2f;
    [Tooltip("Velocity to set when jump button is released early")]
    public float jumpCutVelocity = 2f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool wasGrounded;
    private float moveInput;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private float lastJumpTime;
    private bool isHoldingJump;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = normalGravityScale;
        lastJumpTime = -jumpCooldown; // Allow jumping immediately at start
        
        // Create ground check if it doesn't exist
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    void Update()
    {
        // Check if player is grounded
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Update coyote time counter
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            isHoldingJump = false; // Reset jump hold state when landing
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Update jump buffer counter
        jumpBufferCounter -= Time.deltaTime;

        // Get input based on player mode
        HandleInput();
    }

    void FixedUpdate()
    {
        // Apply horizontal movement
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Apply jump with buffering, coyote time, and cooldown
        // Can jump if: (grounded OR coyote time active) AND jump buffer active AND cooldown elapsed
        bool cooldownComplete = Time.time >= lastJumpTime + jumpCooldown;
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && cooldownComplete)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0; // Consume the jump buffer
            lastJumpTime = Time.time; // Record jump time
            isHoldingJump = true; // Start tracking jump hold
        }

        // Variable jump height: cut jump short if button released while rising
        if (!isHoldingJump && rb.linearVelocity.y > jumpCutVelocity)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpCutVelocity);
        }

        // Adjust gravity based on jump state
        if (!isGrounded)
        {
            // Check if player is at the peak of jump (hangtime)
            if (Mathf.Abs(rb.linearVelocity.y) < hangtimeThreshold)
            {
                rb.gravityScale = hangtimeGravityScale;
            }
            else if (rb.linearVelocity.y < 0) // Falling
            {
                rb.gravityScale = peakGravityScale;
            }
            else // Rising
            {
                rb.gravityScale = normalGravityScale;
            }
        }
        else // On ground
        {
            rb.gravityScale = normalGravityScale;
        }
    }

    void HandleInput()
    {
        if (playerMode == PlayerMode.Player1)
        {
            // Player 1 controls: A/D for movement, W for jump
            moveInput = 0f;
            if (Input.GetKey(KeyCode.A))
                moveInput = -1f;
            if (Input.GetKey(KeyCode.D))
                moveInput = 1f;

            // Set jump buffer when jump is pressed
            if (Input.GetKeyDown(KeyCode.W))
            {
                jumpBufferCounter = jumpBufferTime;
            }

            // Track jump button release for variable jump height
            if (Input.GetKeyUp(KeyCode.W))
            {
                isHoldingJump = false;
            }
        }
        else if (playerMode == PlayerMode.Player2)
        {
            // Player 2 controls: J/L for movement, I for jump
            moveInput = 0f;
            if (Input.GetKey(KeyCode.J))
                moveInput = -1f;
            if (Input.GetKey(KeyCode.L))
                moveInput = 1f;

            // Set jump buffer when jump is pressed
            if (Input.GetKeyDown(KeyCode.I))
            {
                jumpBufferCounter = jumpBufferTime;
            }

            // Track jump button release for variable jump height
            if (Input.GetKeyUp(KeyCode.I))
            {
                isHoldingJump = false;
            }
        }
    }

    // Visualize ground check in editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
