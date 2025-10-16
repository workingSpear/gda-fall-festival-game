using UnityEngine;

public class Block : MonoBehaviour
{
    public int size; //1 block, 2 block, 3 blocks, maximum of 4 blocks
    public string blockName, blockDescription;
    public Sprite blockSprite;
    public GameObject blockPrefab;
    public GameObject blockPrefabRigidBody;

    public SpriteRenderer tint;
    public Color originalColor;
    
    [Header("Block State")]
    [Tooltip("Whether this block is currently disabled")]
    public bool isDisabled = false;
    
    public SpriteRenderer spriteRenderer;
    protected Color originalSpriteColor;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
        }
    }
    
    public void DisableBlock()
    {
        isDisabled = true;
        
        // Disable all components that make the block functional
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
        
        // Disable specific block behaviors
        DisableBlockBehavior();
        
        // Make block invisible when disabled
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }
    
    public void EnableBlock()
    {
        isDisabled = false;
        
        // Re-enable all colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = true;
        }
        
        // Re-enable specific block behaviors
        EnableBlockBehavior();
        
        // Make block visible again when enabled
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }
    
    protected virtual void DisableBlockBehavior()
    {
        // Override in specific block types to disable their behavior
        // For example: disable turret shooting, disable bouncy behavior, etc.
    }
    
    protected virtual void EnableBlockBehavior()
    {
        // Override in specific block types to re-enable their behavior
        // For example: re-enable turret shooting, re-enable bouncy behavior, etc.
    }
}
