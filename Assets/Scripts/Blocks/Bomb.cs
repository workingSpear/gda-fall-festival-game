using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bomb : Block
{
    [Header("Bomb Settings")]
    [Tooltip("Countdown sprites for 3, 2, 1")]
    public Sprite countdown3Sprite;
    public Sprite countdown2Sprite;
    public Sprite countdown1Sprite;
    [Tooltip("Countdown duration in seconds")]
    public float countdownDuration = 3f;
    [Tooltip("Screen shake intensity when bomb explodes")]
    public float explosionShakeIntensity = 0.5f;
    [Tooltip("Screen shake duration when bomb explodes")]
    public float explosionShakeDuration = 0.3f;
    [Tooltip("Radius of explosion effect (in tiles) - 1 = directly adjacent tiles, 2 = 2 tiles away")]
    public float explosionRadius = 1f;

    [Tooltip("GameObject that represents the player death radius")]
    [SerializeField] GameObject playerDeathRadius;
    
    private bool isCountingDown = false;
    private Coroutine countdownCoroutine;
    private Transform objectTransformMommy;
    
    void OnEnable()
    {
        // Find Object Transform Mommy when bomb is enabled
        GameObject mommyObject = GameObject.FindGameObjectWithTag("mommy");
        if (mommyObject != null)
        {
            objectTransformMommy = mommyObject.transform;
        }
        else
        {
            Debug.LogWarning("Object with tag 'mommy' not found!");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if collided with a player and bomb is not disabled and not already counting down
        if ((collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("clone")) && !isDisabled && !isCountingDown)
        {
            // Don't activate bombs during building mode
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null && gameManager.IsInBuildingMode())
            {
                return; // Exit early if in building mode
            }
            
            StartCountdown();
        }
    }
    
    void StartCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        
        isCountingDown = true;
        countdownCoroutine = StartCoroutine(CountdownSequence());
    }
    
    IEnumerator CountdownSequence()
    {
        // Show 3
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && countdown3Sprite != null)
        {
            sr.sprite = countdown3Sprite;
        }
        yield return new WaitForSeconds(1f);
        
        // Show 2
        if (sr != null && countdown2Sprite != null)
        {
            sr.sprite = countdown2Sprite;
        }
        yield return new WaitForSeconds(1f);
        
        // Show 1
        if (sr != null && countdown1Sprite != null)
        {
            sr.sprite = countdown1Sprite;
        }
        yield return new WaitForSeconds(1f);
        
        // Trigger screen shake before disabling the bomb
        TriggerExplosionShake();

        // Show the player death radius
        playerDeathRadius.SetActive(true);

        yield return new WaitForSeconds(0.1f);
        
        // Disable the bomb (like other blocks) instead of destroying it
        DisableBomb();
    }
    
    void DisableBomb()
    {
        // Use the Block's disable system (this will call DisableBlockBehavior() automatically)
        DisableBlock();

        playerDeathRadius.SetActive(false);
        
        // Register this bomb with GameManager AFTER disabling so it can be re-enabled
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            Collider2D bombCollider = GetComponent<Collider2D>();
            if (bombCollider != null)
            {
                Collider2D[] bombColliders = { bombCollider };
                gameManager.RegisterDisabledBlocks(bombColliders);
            }
        }
    }
    
    protected override void DisableBlockBehavior()
    {
        // Stop any running countdown when bomb is disabled through Block system
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        // Reset countdown state
        isCountingDown = false;

        playerDeathRadius.SetActive(false);
    }
    
    protected override void EnableBlockBehavior()
    {
        // Reset bomb state when re-enabled
        isCountingDown = false;
        countdownCoroutine = null;
        
        // Force re-enable all components (override Block's EnableBlock if it's not working)
        ForceReEnableComponents();
    }
    
    void ForceReEnableComponents()
    {
        // Force enable the SpriteRenderer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            // Reset sprite to countdown 3 sprite when re-enabled
            if (countdown3Sprite != null)
            {
                sr.sprite = countdown3Sprite;
            }
        }
        
        // Force enable all colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
        
        // Force enable this component (Bomb script)
        this.enabled = true;
        
        // Ensure isDisabled is false
        isDisabled = false;
    }
    
    public void ForceEnableBomb()
    {
        // Public method to force enable the bomb (can be called from GameManager if needed)
        ForceReEnableComponents();
    }
    
    void TriggerExplosionShake()
    {
        // Find the HitstopManager to trigger screen shake
        HitstopManager hitstopManager = FindFirstObjectByType<HitstopManager>();
        if (hitstopManager != null && hitstopManager.screenShake != null)
        {
            hitstopManager.screenShake.Shake(explosionShakeDuration, explosionShakeIntensity);
        }
        
        // Disable blocks within explosion radius
        DisableNearbyBlocks();
    }
    
    void DisableNearbyBlocks()
    {
        float tileSize = 0.5f; // Match the grid system tile size
        Vector2 bombPos = transform.position;
        
        // Use the cached Object Transform Mommy reference
        if (objectTransformMommy == null)
        {
            Debug.LogWarning("Object Transform Mommy not found! Make sure the object has the 'mommy' tag.");
            return;
        }
        
        List<Collider2D> nearbyColliders = new List<Collider2D>();
        HashSet<Block> blocksToDisable = new HashSet<Block>(); // Use HashSet to avoid duplicates
        
        // Get all blocks within Object Transform Mommy
        Block[] allBlocks = objectTransformMommy.GetComponentsInChildren<Block>();
        
        // Also get all objects with "turret" tag specifically
        GameObject[] turretObjects = GameObject.FindGameObjectsWithTag("turret");
        
        // Check all tiles within the explosion radius in a square grid pattern
        int radius = Mathf.RoundToInt(explosionRadius);
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                // Skip the center tile (the bomb itself)
                if (x == 0 && y == 0) continue;
                
                // Calculate the position of this tile
                Vector2 checkPos = bombPos + new Vector2(x * tileSize, y * tileSize);
                
                // Check each block in Object Transform Mommy to see if it occupies this position
                foreach (Block block in allBlocks)
                {
                    if (block == null || block == this) continue; // Skip destroyed blocks and bomb itself
                    
                    // Get all positions this block occupies based on its size
                    Vector2 blockAnchor = new Vector2(block.transform.position.x, block.transform.position.y);
                    List<Vector2> occupiedPositions = GetOccupiedPositions(blockAnchor, block.size);
                    
                    // Check if any of the block's occupied positions match the explosion tile
                    foreach (Vector2 occupiedPos in occupiedPositions)
                    {
                        float distance = Vector2.Distance(occupiedPos, checkPos);
                        if (distance < tileSize * 0.25f) // Small tolerance for position matching
                        {
                            blocksToDisable.Add(block);
                            break; // Found this block is affected, move to next block
                        }
                    }
                }
                
                // Also check turrets specifically by tag
                foreach (GameObject turretObject in turretObjects)
                {
                    if (turretObject == null || turretObject == this.gameObject) continue; // Skip destroyed turrets and bomb itself
                    
                    // Make sure this turret is under Object Transform Mommy
                    if (!turretObject.transform.IsChildOf(objectTransformMommy)) continue;
                    
                    // Check if this turret is close enough to the check position
                    Vector2 turretPos = turretObject.transform.position;
                    float distance = Vector2.Distance(turretPos, checkPos);
                    
                    if (distance < tileSize * 0.25f) // Small tolerance for position matching
                    {
                        // Get the Turret component and add it to blocks to disable
                        Turret turret = turretObject.GetComponentInChildren<Turret>();
                        if (turret != null)
                        {
                            blocksToDisable.Add(turret);
                        }
                        
                        break; // Found a turret at this position, move to next position
                    }
                }
            }
        }
        
        // Disable all affected blocks
        foreach (Block block in blocksToDisable)
        {
            if (block != null)
            {
                block.DisableBlock();
                Collider2D col = block.GetComponent<Collider2D>();
                if (col != null)
                {
                    nearbyColliders.Add(col);
                }
            }
        }
        
        // Notify GameManager about disabled blocks so they can be re-enabled during picking phase
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.RegisterDisabledBlocks(nearbyColliders.ToArray());
        }
    }
    
    List<Vector2> GetOccupiedPositions(Vector2 anchor, int blockSize)
    {
        List<Vector2> positions = new List<Vector2>();
        
        if (blockSize == 1)
        {
            // Single tile
            positions.Add(anchor);
        }
        else if (blockSize == 2)
        {
            // 2 positions horizontally
            positions.Add(anchor);
            positions.Add(new Vector2(anchor.x + 0.5f, anchor.y));
        }
        else if (blockSize == 3)
        {
            // 3 positions horizontally
            positions.Add(anchor);
            positions.Add(new Vector2(anchor.x + 0.5f, anchor.y));
            positions.Add(new Vector2(anchor.x + 1.0f, anchor.y));
        }
        else if (blockSize == 4)
        {
            // 2x2 grid - this is the key fix for size 4 blocks
            positions.Add(anchor);                                    // Bottom-left
            positions.Add(new Vector2(anchor.x, anchor.y + 0.5f));    // Top-left
            positions.Add(new Vector2(anchor.x + 0.5f, anchor.y));    // Bottom-right
            positions.Add(new Vector2(anchor.x + 0.5f, anchor.y + 0.5f)); // Top-right
        }
        
        return positions;
    }
}
