using UnityEngine;

public class Turret : MonoBehaviour
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
    
    [SerializeField] private GameObject closestPlayer;
    private float nextFireTime = 0f;
    private float activationTime;
    
    void Start()
    {
        // Set the activation time (when turret can start firing)
        activationTime = Time.time + startDelay;
    }
    
    void Update()
    {
        // Find the closest player
        FindClosestPlayer();
        
        // Aim at the closest player (even during start delay)
        if (closestPlayer != null)
        {
            AimAtTarget(closestPlayer.transform.position);
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
        
        if (players.Length == 0)
        {
            closestPlayer = null;
            return;
        }
        
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;
        
        // Find the closest enabled player
        foreach (GameObject player in players)
        {
            // Check if the player's Player component is enabled
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent == null || !playerComponent.enabled)
            {
                continue; // Skip disabled players
            }
            
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = player;
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
}
