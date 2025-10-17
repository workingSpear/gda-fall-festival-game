using UnityEngine;

public class Tear : MonoBehaviour
{
    [Header("Portal Settings")]
    [Tooltip("The other tear this one is linked to")]
    public Tear linkedTear;
    
    [Tooltip("Tag for portal detection")]
    public string portalTag = "portal";
    
    [Header("Cooldown Settings")]
    [Tooltip("Cooldown time before teleporter can be used again")]
    public float teleportCooldown = 2f;
    [Tooltip("Opacity while on cooldown (0-1)")]
    public float cooldownOpacity = 0.3f;
    [Tooltip("Opacity when unlinked (0-1)")]
    public float unlinkedOpacity = 0.5f;
    
    private bool isOnCooldown = false;
    private float lastTeleportTime = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    void Start()
    {
        // Initialize sprite renderer and original color
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Find the other tear if not already linked
        if (linkedTear == null)
        {
            FindLinkedTear();
        }
    }
    
    void OnEnable()
    {
        // Check for linked tear every time the block is enabled
        if (linkedTear == null)
        {
            FindLinkedTear();
        }
    }
    
    void OnDisable()
    {
        // Delink tears when this tear is disabled
        if (linkedTear != null)
        {
            // Remove the link from the other tear
            linkedTear.linkedTear = null;
            Debug.Log($"Tear {gameObject.name} delinked from {linkedTear.gameObject.name}");
            
            // Clear this tear's link
            linkedTear = null;
        }
    }
    
    void FindLinkedTear()
    {
        // Find all tears in the scene
        Tear[] allTears = FindObjectsOfType<Tear>();
        
        foreach (Tear tear in allTears)
        {
            if (tear != this && tear.linkedTear == null)
            {
                // Link this tear to the other
                this.linkedTear = tear;
                tear.linkedTear = this;
                Debug.Log($"Tear {gameObject.name} linked to {tear.gameObject.name}");
                
                // Update opacity for both tears
                UpdateOpacity();
                tear.UpdateOpacity();
                break;
            }
        }
        
        // If no unlinked tear found, check if there's already a linked tear that needs re-linking
        if (linkedTear == null)
        {
            foreach (Tear tear in allTears)
            {
                if (tear != this && tear.linkedTear == this)
                {
                    // This tear is already linked to us, establish the connection
                    this.linkedTear = tear;
                    Debug.Log($"Tear {gameObject.name} re-linked to {tear.gameObject.name}");
                    
                    // Update opacity for both tears
                    UpdateOpacity();
                    tear.UpdateOpacity();
                    break;
                }
            }
        }
    }
    
    void Update()
    {
        // Handle cooldown and opacity changes
        if (isOnCooldown)
        {
            // Check if cooldown has ended
            if (Time.time - lastTeleportTime >= teleportCooldown)
            {
                isOnCooldown = false;
                // Restore appropriate opacity based on linking status
                UpdateOpacity();
            }
        }
        else
        {
            // Update opacity based on current state (linked/unlinked)
            UpdateOpacity();
        }
    }
    
    void UpdateOpacity()
    {
        if (spriteRenderer != null)
        {
            // Don't change opacity during Building or Picking mode
            GameManager gameManager = GameObject.FindGameObjectWithTag("gamemanager")?.GetComponent<GameManager>();
            if (gameManager != null && (gameManager.IsInBuildingMode() || gameManager.IsInPickingMode()))
            {
                return; // Skip opacity changes during Building or Picking mode
            }
            
            if (isOnCooldown)
            {
                // Cooldown opacity (lowest priority)
                Color cooldownColor = originalColor;
                cooldownColor.a = cooldownOpacity;
                spriteRenderer.color = cooldownColor;
            }
            else if (linkedTear == null)
            {
                // Unlinked opacity
                Color unlinkedColor = originalColor;
                unlinkedColor.a = unlinkedOpacity;
                spriteRenderer.color = unlinkedColor;
            }
            else
            {
                // Normal opacity (linked and not on cooldown)
                spriteRenderer.color = originalColor;
            }
        }
    }
    
    // Method to trigger cooldown when teleportation occurs
    public void TriggerCooldown()
    {
        // Always trigger cooldown (reset if already on cooldown)
        isOnCooldown = true;
        lastTeleportTime = Time.time;
        
        Debug.Log($"Tear {gameObject.name} triggered cooldown");
        
        // Update opacity to show cooldown state
        UpdateOpacity();
    }
    
    // Method to check if teleporter is on cooldown
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
    
    // Teleportation is now handled by the Player and Clone components
    // The Tear component only manages the linking between tears
    
    // Method to check if this tear is properly linked
    public bool IsLinked()
    {
        return linkedTear != null;
    }
    
    // Method to get the linked tear
    public Tear GetLinkedTear()
    {
        return linkedTear;
    }
}
