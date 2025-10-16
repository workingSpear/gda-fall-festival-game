using UnityEngine;

public class Turret : Block
{
    [Header("Turret Settings")]
    [Tooltip("Projectile prefab to fire")]
    public GameObject projectilePrefab;
    [Tooltip("Time between shots in seconds")]
    public float fireRate = 2f;
    [Tooltip("Delay before turret starts firing")]
    public float startDelay = 5f;
    [Tooltip("Point where projectiles spawn (optional, uses turret position if null)")]
    public Transform firePoint;
    [Tooltip("Speed of the projectile")]
    public float projectileSpeed = 10f;
    
    [Header("Laser Indicator Settings")]
    [Tooltip("LineRenderer for the laser indicator")]
    public LineRenderer laserLine;
    [Tooltip("Maximum range of the laser")]
    public float laserRange = 20f;
    [Tooltip("Layers that can block the laser")]
    public LayerMask laserBlockingLayers = -1;
    [Tooltip("Color of the laser")]
    public Color laserColor = Color.red;
    [Tooltip("Width of the laser")]
    public float laserWidth = 0.1f;
    
    [Header("Targeting Range Settings")]
    [Tooltip("Maximum number of tiles away the turret will target players")]
    public int maxTileRange = 5;
    [Tooltip("Size of each tile (should match your grid system)")]
    public float tileSize = 0.5f;
    [Tooltip("Show range indicator (for debugging/visual feedback)")]
    public bool showRangeIndicator = true;
    [Tooltip("Color of the range indicator")]
    public Color rangeIndicatorColor = new Color(1f, 0f, 0f, 0.3f);
    
    [SerializeField] private GameObject closestPlayer;
    private float nextFireTime = 0f;
    private float activationTime;
    private GameObject previousTarget; // Track previous target to detect when target is lost
    
    void Start()
    {
        // Initialize sprite renderer (since we override Start, we need to do this manually)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
        }
        
        // Set the activation time (when turret can start firing)
        activationTime = Time.time + startDelay;
        
        // Setup laser line renderer
        SetupLaserLine();
    }
    
    void SetupLaserLine()
    {
        // Create LineRenderer if not assigned
        if (laserLine == null)
        {
            GameObject laserObject = new GameObject("Laser Line");
            laserObject.transform.SetParent(transform);
            laserLine = laserObject.AddComponent<LineRenderer>();
        }
        
        // Configure the laser line
        laserLine.material = new Material(Shader.Find("Sprites/Default"));
        laserLine.startColor = laserColor;
        laserLine.endColor = laserColor;
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth;
        laserLine.positionCount = 2;
        laserLine.useWorldSpace = true;
        laserLine.sortingOrder = 10; // Make sure it renders on top
        
        // Initially hide the laser
        laserLine.enabled = false;
    }
    
    void Update()
    {
        // Don't function if disabled
        if (isDisabled)
        {
            return;
        }
        
        // Find the closest player
        FindClosestPlayer();
        
        // Check if target was lost and reset fire cooldown
        if (previousTarget != null && closestPlayer == null)
        {
            // Target was lost, reset fire cooldown
            nextFireTime = Time.time + fireRate;
        }
        // Check if a new target was acquired (no target before, target now)
        else if (previousTarget == null && closestPlayer != null)
        {
            // New target acquired, reset fire cooldown
            nextFireTime = Time.time + fireRate;
        }
        
        // Update previous target
        previousTarget = closestPlayer;
        
        // Aim at the closest player (even during start delay)
        if (closestPlayer != null)
        {
            AimAtTarget(closestPlayer.transform.position);
            // Update laser indicator
            UpdateLaserLine();
        }
        else
        {
            // Hide laser if no target
            if (laserLine != null)
            {
                laserLine.enabled = false;
            }
        }
        
        // Draw range indicator if enabled
        if (showRangeIndicator)
        {
            DrawRangeIndicator();
        }
        
        // Only fire after start delay has passed
        if (Time.time >= activationTime)
        {
            // Fire at intervals
            if (closestPlayer != null && Time.time >= nextFireTime)
            {
                FireProjectile();
                nextFireTime = Time.time + fireRate;
            }
        }
    }
    
    void FindClosestPlayer()
    {
        // Find all objects with the "Player" tag
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        // Find all objects with the "clone" tag
        GameObject[] clones = GameObject.FindGameObjectsWithTag("clone");
        
        // Combine both arrays
        GameObject[] allTargets = new GameObject[players.Length + clones.Length];
        players.CopyTo(allTargets, 0);
        clones.CopyTo(allTargets, players.Length);
        
        if (allTargets.Length == 0)
        {
            closestPlayer = null;
            return;
        }
        
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;
        float maxRangeInUnits = maxTileRange * tileSize;
        
        // Find the closest enabled target within tile range
        foreach (GameObject target in allTargets)
        {
            // Check if it's a player and if the Player component is enabled
            Player playerComponent = target.GetComponent<Player>();
            if (playerComponent != null && !playerComponent.enabled)
            {
                continue; // Skip disabled players
            }
            
            // Check if it's a clone and if it's not been hitstopped
            Clone cloneComponent = target.GetComponent<Clone>();
            if (cloneComponent != null && cloneComponent.hasBeenHitStopped)
            {
                continue; // Skip hitstopped clones
            }
            
            float distance = Vector2.Distance(transform.position, target.transform.position);
            
            // Only consider targets within the tile range
            if (distance <= maxRangeInUnits && distance < closestDistance)
            {
                closestDistance = distance;
                closest = target;
            }
        }
        
        closestPlayer = closest;
    }
    
    void AimAtTarget(Vector3 targetPosition)
    {
        // Calculate direction to target
        Vector2 direction = (targetPosition - transform.position).normalized;
        
        // Calculate angle in degrees (add 180 to correct offset)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
        
        // Apply rotation to turret
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    
    void UpdateLaserLine()
    {
        if (laserLine == null || closestPlayer == null)
            return;
        
        // Get the fire point position
        Vector3 startPos = firePoint != null ? firePoint.position : transform.position;
        
        // Calculate direction to target
        Vector2 direction = (closestPlayer.transform.position - startPos).normalized;
        
        // Perform raycast to see if laser hits something
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, laserRange, laserBlockingLayers);
        
        // Set laser line positions
        if (hit.collider != null)
        {
            // Laser hits something - end at hit point
            laserLine.SetPosition(0, startPos);
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            // Laser doesn't hit anything - extend to max range
            Vector3 endPos = startPos + (Vector3)(direction * laserRange);
            laserLine.SetPosition(0, startPos);
            laserLine.SetPosition(1, endPos);
        }
        
        // Show the laser line
        laserLine.enabled = true;
        
        // Change laser color based on whether turret can fire
        if (Time.time >= activationTime)
        {
            // Turret is active - bright red laser
            laserLine.startColor = laserColor;
            laserLine.endColor = laserColor;
        }
        else
        {
            // Turret is still in start delay - dimmer laser
            Color dimColor = laserColor;
            dimColor.a = 0.5f;
            laserLine.startColor = dimColor;
            laserLine.endColor = dimColor;
        }
    }
    
    void FireProjectile()
    {
        if (projectilePrefab == null || closestPlayer == null)
            return;
        
        // Use fire point if available, otherwise use turret position
        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        
        // Calculate direction to the closest player
        Vector2 direction = (closestPlayer.transform.position - spawnPosition).normalized;
        
        // Spawn the projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        
        // Set the projectile's velocity
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }
        
        // Optionally rotate the projectile to face the direction it's moving
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    
    void DrawRangeIndicator()
    {
        // Draw a square range indicator around the turret
        float rangeInUnits = maxTileRange * tileSize;
        Vector3 center = transform.position;
        
        // Define the four corners of the square range
        Vector3 topLeft = center + new Vector3(-rangeInUnits, rangeInUnits, 0);
        Vector3 topRight = center + new Vector3(rangeInUnits, rangeInUnits, 0);
        Vector3 bottomLeft = center + new Vector3(-rangeInUnits, -rangeInUnits, 0);
        Vector3 bottomRight = center + new Vector3(rangeInUnits, -rangeInUnits, 0);
        
        // Draw the square outline using Debug.DrawLine
        Debug.DrawLine(topLeft, topRight, rangeIndicatorColor);
        Debug.DrawLine(topRight, bottomRight, rangeIndicatorColor);
        Debug.DrawLine(bottomRight, bottomLeft, rangeIndicatorColor);
        Debug.DrawLine(bottomLeft, topLeft, rangeIndicatorColor);
        
        // Optional: Draw diagonal lines to make it more visible
        Debug.DrawLine(topLeft, bottomRight, rangeIndicatorColor);
        Debug.DrawLine(topRight, bottomLeft, rangeIndicatorColor);
    }
    
    protected override void DisableBlockBehavior()
    {
        // Hide laser line when disabled
        if (laserLine != null)
        {
            laserLine.enabled = false;
        }
        
        // Stop turret behavior without disabling the component
        // The Block's DisableBlock() will handle sprite renderer and colliders
        // We just need to stop the turret from shooting
        isDisabled = true;
    }
    
    protected override void EnableBlockBehavior()
    {
        // Re-enable the turret's functionality
        isDisabled = false;
    }
    
    public void ForceEnableTurret()
    {
        // Force enable the turret completely
        isDisabled = false;
        
        // Force enable the sprite renderer
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
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
        
        // Force enable this component (Turret script)
        this.enabled = true;
        
        // Ensure isDisabled is false
        isDisabled = false;
    }
}
