using UnityEngine;

public class Clone : MonoBehaviour
{
    public Vector2[] recordedVelocities;
    public Vector2 startPosition;
    public int framesBeforeNextVelocity;
    public float playbackSpeed = 1f;
    
    public int serialNumber;
    public bool isBuildingMode = false;
    public Player.PlayerMode playerMode;

    [Header("Hitstop Settings")]
    [Tooltip("Prefab to spawn for Player 1 clone on death")]
    public GameObject player1DeathPrefab;
    [Tooltip("Prefab to spawn for Player 2 clone on death")]
    public GameObject player2DeathPrefab;

    [Header("Collider References")]
    public BoxCollider2D jumpableHead;

    private float frameCounter;
    private int currentVelocityIndex;
    private bool isPlaying;
    public SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Color originalColor;
    [HideInInspector]
    public bool hasBeenHitStopped = false;
    [HideInInspector]
    public bool hasInvertedControls = false;
    
    // Portal teleportation cooldown
    private bool canTeleport = true;
    [SerializeField] HitstopManager hitstopManager;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        // Find hitstop manager from gamemanager tag
        GameObject gameManager = GameObject.FindGameObjectWithTag("gamemanager");
        if (gameManager != null)
        {
            hitstopManager = gameManager.GetComponent<HitstopManager>();
        }
        
        // Store original sprite color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Set up rigidbody for physics-based playback
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 2f; // Match player gravity
        }
    }

    void Start()
    {  
        UpdateOpacity();
    }

    void Update()
    {
        // Don't process if clone has been hitstopped
        if (hasBeenHitStopped)
        {
            return;
        }
        
        if (!isPlaying || recordedVelocities == null || recordedVelocities.Length == 0)
            return;

        // Increment frame counter by playback speed
        frameCounter += playbackSpeed;

        // Apply next velocity every N frames
        if (frameCounter >= framesBeforeNextVelocity)
        {
            frameCounter -= framesBeforeNextVelocity;

            // Apply the recorded velocity to rigidbody
            if (currentVelocityIndex < recordedVelocities.Length && rb != null)
            {
                Vector2 velocity = recordedVelocities[currentVelocityIndex];
                
                // Invert X component if controls are inverted
                if (hasInvertedControls)
                {
                    velocity.x = -velocity.x;
                }
                
                rb.linearVelocity = velocity;
                currentVelocityIndex++;
            }
            else
            {
                // Reached end of recording
                isPlaying = false;
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    
                    // Keep clones movable after playback finishes
                    // Only change visual appearance, not physics
                    if (!isBuildingMode)
                    {
                        TurnSpriteBlack();
                    }
                }
            }
        }
    }

    public void ResetToStartPosition()
    {
        // Reset to starting position
        if (rb != null)
        {
            rb.position = startPosition;
            rb.linearVelocity = Vector2.zero;
        }
        
        // Reset playback state
        currentVelocityIndex = 0;
        frameCounter = 0;
        isPlaying = false;
        
        // Restore original sprite color
        RestoreSpriteColor();
    }

    public void ResetClone()
    {
        // Reset hitstop flag
        hasBeenHitStopped = false;
        
        // Restore sprite to normal state
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
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
        
        // Restore physics
        if (rb != null)
        {
            rb.gravityScale = 2f;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        
        // Ensure time scale is normal
        Time.timeScale = 1f;
    }

    public void PlayRecordedPath()
    {
        // Ensure clone is enabled (might have been disabled by hitstop)
        this.enabled = true;
        
        // Reset clone state (including hitstop)
        ResetClone();
        
        currentVelocityIndex = 0;
        frameCounter = 0;
        isPlaying = true;
        
        // Reset to start position and restore physics
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // Allow physics during playback
            rb.position = startPosition;
            rb.linearVelocity = Vector2.zero;
        }
                 
        spriteRenderer.enabled = true;
        
        // Enable box collider when clone is enabled
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
        
        // Restore original sprite color
        RestoreSpriteColor();
        
        UpdateOpacity(); // Re-apply opacity to ensure it's correct
    }

    void TurnSpriteBlack()
    {
        if (spriteRenderer != null)
        {
            // Set to gray with full opacity
            spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    void RestoreSpriteColor()
    {
        if (spriteRenderer != null)
        {
            // Restore original color including alpha
            spriteRenderer.color = originalColor;
        }
    }

    public void UpdateOpacity()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            
            // Newest clone (serialNumber 0) = 77% opacity
            // Second newest (serialNumber 1) = 38% opacity  
            // All others = 7% opacity
            if (serialNumber == 0)
            {
                color.a = 0.5f;
            }
            else if (serialNumber == 1)
            {
                color.a = 0.2f;
            }
            else
            {
                color.a = 0.03f;
            }
            
            spriteRenderer.color = color;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("hazard"))
        {
            // Don't trigger hazard collision in building mode
            if (isBuildingMode)
            {
                return;
            }
            
            // Only allow one hitstop per clone
            if (hasBeenHitStopped)
            {
                return;
            }
            
            // Mark that this clone has been hitstopped
            hasBeenHitStopped = true;
            
            // Get the appropriate death prefab based on player mode
            GameObject deathPrefab = playerMode == Player.PlayerMode.Player1 ? player1DeathPrefab : player2DeathPrefab;
            
            // Call hitstop manager to handle the effect (death hitstop)
            if (hitstopManager != null)
            {
                hitstopManager.TriggerCloneHitstop(this, deathPrefab, false);
            }
        }
        else if (other.CompareTag("portal"))
        {
            // Handle portal teleportation
            Tear tear = other.GetComponent<Tear>();
            if (tear != null && tear.IsLinked() && canTeleport && !tear.IsOnCooldown())
            {
                // Get the linked tear
                Tear linkedTear = tear.GetLinkedTear();
                if (linkedTear != null && linkedTear.gameObject.activeInHierarchy && !linkedTear.IsOnCooldown())
                {
                    // Teleport to the linked tear's position
                    transform.position = linkedTear.transform.position;
                    
                    // Trigger cooldown on both tears
                    tear.TriggerCooldown();
                    linkedTear.TriggerCooldown();
                    
                    // Disable teleportation until clone exits the trigger
                    canTeleport = false;
                    
                    // Preserve velocity if clone has rigidbody
                    Rigidbody2D rb = GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        // Keep the current velocity
                        // (velocity is already preserved by the rigidbody)
                    }
                }
            }
        }
        else if (other.CompareTag("endClone"))
        {
            // Don't trigger end collision in building mode
            if (isBuildingMode)
            {
                return;
            }
            
            // Only allow one hitstop per clone
            if (hasBeenHitStopped)
            {
                return;
            }
            
            // Mark that this clone has been hitstopped
            hasBeenHitStopped = true;
            
            // Award point to this clone's team
            GameManager gameManager = GameObject.FindGameObjectWithTag("gamemanager")?.GetComponent<GameManager>();
            if (gameManager != null)
            {
                gameManager.AwardPoint(playerMode);
            }
            
            // Get the appropriate death prefab based on player mode (though won't be used for end)
            GameObject deathPrefab = playerMode == Player.PlayerMode.Player1 ? player1DeathPrefab : player2DeathPrefab;
            
            // Call hitstop manager to handle the effect (end hitstop)
            if (hitstopManager != null)
            {
                hitstopManager.TriggerCloneHitstop(this, deathPrefab, true);
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("portal"))
        {
            // Re-enable teleportation when clone exits the portal
            canTeleport = true;
        }
    }
}
