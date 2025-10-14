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

    [Header("Hitstop Settings")]
    public HitstopManager hitstopManager;
    [Tooltip("Prefab to spawn for Player 1 on death")]
    public GameObject player1DeathPrefab;
    [Tooltip("Prefab to spawn for Player 2 on death")]
    public GameObject player2DeathPrefab;

    [Header("Collider References")]
    public BoxCollider2D jumpableHead;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    [HideInInspector]
    public bool hasBeenHitStopped = false;
    private bool wasGrounded;
    private float moveInput;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private float lastJumpTime;
    private bool isHoldingJump;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        // Don't process input or movement if player has been hitstopped
        if (hasBeenHitStopped)
        {
            return;
        }
        
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
        // Don't apply physics if player has been hitstopped
        if (hasBeenHitStopped)
        {
            // Keep velocity at zero
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }
        
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

    public void ResetPlayer()
    {
        // Reset hitstop flag
        hasBeenHitStopped = false;
        
        // Restore sprite to normal state
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 11;
        }
        
        // Re-enable hitboxes
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
        
        if (jumpableHead != null)
        {
            jumpableHead.enabled = true;
        }
        
        // Restore gravity and reset physics
        if (rb != null)
        {
            rb.gravityScale = normalGravityScale;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        
        // Ensure time scale is normal
        Time.timeScale = 1f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("hazard"))
        {
            // Only allow one hitstop per player
            if (hasBeenHitStopped)
            {
                return;
            }
            
            // Mark that this player has been hitstopped
            hasBeenHitStopped = true;
            
            // Get the appropriate death prefab based on player mode
            GameObject deathPrefab = playerMode == PlayerMode.Player1 ? player1DeathPrefab : player2DeathPrefab;
            
            // Call hitstop manager to handle the effect (death hitstop)
            if (hitstopManager != null)
            {
                hitstopManager.TriggerHitstop(this, deathPrefab, false);
            }
            
            // Notify GameManager of player death
            GameManager gameManager = GameObject.FindGameObjectWithTag("gamemanager")?.GetComponent<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnPlayerDeath(playerMode);
            }
        }
        else if (other.CompareTag("end"))
        {
            // Only allow one hitstop per player
            if (hasBeenHitStopped)
            {
                return;
            }
            
            // Mark that this player has been hitstopped
            hasBeenHitStopped = true;
            
            // Award point to this player's team
            GameManager gameManager = GameObject.FindGameObjectWithTag("gamemanager")?.GetComponent<GameManager>();
            if (gameManager != null)
            {
                gameManager.AwardPoint(playerMode);
            }
            
            // Get the appropriate death prefab based on player mode (though won't be used for end)
            GameObject deathPrefab = playerMode == PlayerMode.Player1 ? player1DeathPrefab : player2DeathPrefab;
            
            // Call hitstop manager to handle the effect (end hitstop)
            if (hitstopManager != null)
            {
                hitstopManager.TriggerHitstop(this, deathPrefab, true);
            }
            
            // Notify GameManager of player death (reaching end also counts as "death" for round-end logic)
            if (gameManager != null)
            {
                gameManager.OnPlayerDeath(playerMode);
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
