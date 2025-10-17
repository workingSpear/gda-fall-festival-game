using UnityEngine;

public class Fan : Block
{
    [Header("Fan Settings")]
    [Tooltip("Direction the fan blows (normalized vector)")]
    public Vector2 blowDirection = Vector2.right;
    
    [Tooltip("Force of the fan push")]
    public float fanForce = 10f;
    [Tooltip("Maximum force that can be applied to players (prevents excessive velocity)")]
    public float maxPlayerForce = 15f;
    
    [Tooltip("Tag for objects that can be blown by the fan")]
    public string fanTag = "fan";
    
    [Header("Rotation Settings")]
    [Tooltip("Cooldown time between rotations in seconds")]
    public float rotationCooldown = 0.5f;
    [Tooltip("Pivot point for rotation (set in inspector)")]
    public Transform rotationPivot;
    
    private float lastRotationTime = 0f;
    
    [Header("Original Position")]
    [Tooltip("Original position where the fan was placed (set automatically)")]
    public Vector2 OGPos; // Original position for occupied tile calculations
    
    private void Start()
    {
        // Normalize the blow direction to ensure consistent force
        blowDirection = blowDirection.normalized;
    }
    
    // Method to set the original grid position (called when placed)
    public void SetGridPosition(Vector2 position)
    {
        OGPos = position;
    }
    
    // Get the fan force with player limits applied
    public Vector2 GetLimitedFanForce()
    {
        Vector2 forceVector = blowDirection * fanForce;
        
        // Limit the force magnitude to maxPlayerForce
        if (forceVector.magnitude > maxPlayerForce)
        {
            forceVector = forceVector.normalized * maxPlayerForce;
        }
        
        return forceVector;
    }
    
    
    public void RotateFan()
    {
        // Check if game is in building mode
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null && gameManager.IsInBuildingMode())
        {
            return; // Cannot rotate during building mode
        }
        
        // Check cooldown
        if (Time.time - lastRotationTime < rotationCooldown)
        {
            return; // Still on cooldown
        }
        
        // Rotate the fan 90 degrees around the pivot point
        if (rotationPivot != null)
        {
            transform.RotateAround(rotationPivot.position, Vector3.forward, 90);
        }
        else
        {
            // Fallback to center rotation if no pivot is set
            transform.Rotate(0, 0, 90);
        }
        
        // Update the blow direction to match the new rotation
        // Rotate the blow direction vector by 90 degrees
        float angle = 90f * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        
        Vector2 newDirection = new Vector2(
            blowDirection.x * cos - blowDirection.y * sin,
            blowDirection.x * sin + blowDirection.y * cos
        );
        
        blowDirection = newDirection.normalized;
        
        // Update the last rotation time
        lastRotationTime = Time.time;
    }
    
    protected override void DisableBlockBehavior()
    {
        // Disable all fan children when the block is disabled
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.gameObject.SetActive(false);
        }
    }
    
    protected override void EnableBlockBehavior()
    {
        // Re-enable all fan children when the block is enabled
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.gameObject.SetActive(true);
        }
    }
    
    
    // Fan blowing is now handled by the Player and Clone components
    // The Fan component only provides the configuration (direction and force)
}
