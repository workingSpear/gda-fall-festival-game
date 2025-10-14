using UnityEngine;

public class Cursor : MonoBehaviour
{
    [Header("Player Settings")]
    public Player.PlayerMode playerMode = Player.PlayerMode.Player1;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    [Header("Bounds")]
    public float minX = -5.25f;
    public float maxX = 5.25f;
    public float minY = -2.24f;
    public float maxY = 2.24f;
    
    private Vector2 moveInput;
    private SpriteRenderer spriteRenderer;
    private bool isCursorEnabled = false;
    private bool isPickingMode = false;

    [Header("Sprite Settings")]
    public Sprite pickingCursorSprite;
    public Sprite buildingCursorSprite;
    
    [Header("Block Settings")]
    public float blockSize = 1f;
    public SpriteRenderer blockSprite;
    
    // Block hovering
    private Block currentBlock;
    private System.Collections.Generic.List<Block> currentTintedBlocks = new System.Collections.Generic.List<Block>();
    private GameObject currentPickableObject; // Track current pickable object in picking mode
    private Sprite originalCursorSprite;
    private int currentBlockSize = 1; // Track the size of the currently selected block
    
    public Block GetCurrentBlock()
    {
        return currentBlock;
    }
    
    public GameObject GetCurrentPickableObject()
    {
        // First, try to return the stored pickable object
        if (currentPickableObject != null)
        {
            return currentPickableObject;
        }
        
        // If no stored object, do a direct overlap check at cursor position
        // This is more robust and handles cases where trigger events might be missed
        Collider2D[] colliders = Physics2D.OverlapPointAll(transform.position);
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("object"))
            {
                Debug.Log($"{playerMode} GetCurrentPickableObject found object via overlap: {col.gameObject.name}");
                
                // Return the root GameObject (the one with the Block component)
                Block block = col.GetComponentInParent<Block>();
                if (block != null)
                {
                    Debug.Log($"{playerMode} Returning root object: {block.gameObject.name}");
                    return block.gameObject;
                }
                
                // Fallback to the child if no Block component found
                return col.gameObject;
            }
        }
        
        Debug.Log($"{playerMode} GetCurrentPickableObject returned null");
        return null;
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Save the original cursor sprite
        if (spriteRenderer != null)
        {
            originalCursorSprite = spriteRenderer.sprite;
            spriteRenderer.enabled = false;
        }
        
        // Disable box collider at start
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
    }

    void Update()
    {
        // Only handle input and move if cursor is enabled
        if (isCursorEnabled)
        {
            HandleInput();
            MoveCursor();
        }
    }

    void HandleInput()
    {
        moveInput = Vector2.zero;

        if (playerMode == Player.PlayerMode.Player1)
        {
            // Player 1 controls: WASD
            if (Input.GetKey(KeyCode.W))
                moveInput.y = 1f;
            if (Input.GetKey(KeyCode.S))
                moveInput.y = -1f;
            if (Input.GetKey(KeyCode.A))
                moveInput.x = -1f;
            if (Input.GetKey(KeyCode.D))
                moveInput.x = 1f;
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            // Player 2 controls: IJKL
            if (Input.GetKey(KeyCode.I))
                moveInput.y = 1f;
            if (Input.GetKey(KeyCode.K))
                moveInput.y = -1f;
            if (Input.GetKey(KeyCode.J))
                moveInput.x = -1f;
            if (Input.GetKey(KeyCode.L))
                moveInput.x = 1f;
        }
        
        // Normalize to ensure consistent speed in all directions
        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    void MoveCursor()
    {
        // Move the cursor based on normalized input (framerate independent with Time.deltaTime)
        transform.position += (Vector3)moveInput * moveSpeed * Time.deltaTime;
        
        // Clamp cursor position within bounds
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);
        transform.position = clampedPosition;
    }

    public void EnableCursor(bool pickingMode = false)
    {
        isCursorEnabled = true;
        isPickingMode = pickingMode;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        
        // Enable block sprite when cursor is enabled
        if (blockSprite != null)
        {
            blockSprite.enabled = true;
        }
        
        // Enable box collider when cursor is enabled
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
    }

    public void DisableCursor()
    {
        isCursorEnabled = false;
        isPickingMode = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Disable block sprite when cursor is disabled
        if (blockSprite != null)
        {
            blockSprite.enabled = false;
        }
        
        // Disable box collider when cursor is disabled
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
        
        // Untint any currently tinted block
        UntintCurrentBlock();
    }

    void UntintCurrentBlock()
    {
        // Untint all currently tinted blocks
        foreach (Block block in currentTintedBlocks)
        {
            if (block != null && block.tint != null)
            {
                block.tint.color = block.originalColor;
            }
        }
        currentTintedBlocks.Clear();
        currentBlock = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // In picking mode, detect "object" tag and update UI
        if (other.CompareTag("object") && isCursorEnabled && isPickingMode)
        {
            Debug.Log($"{playerMode} Cursor entered trigger with object: {other.gameObject.name}");
            
            Block block = other.GetComponentInParent<Block>();
            if (block != null)
            {
                // Store the root GameObject (the one with the Block component) instead of the child
                currentPickableObject = block.gameObject;
                Debug.Log($"{playerMode} Stored currentPickableObject as: {currentPickableObject.name}");
                
                // Find GameManager and update block info
                GameObject gameManagerObj = GameObject.FindGameObjectWithTag("gamemanager");
                if (gameManagerObj != null)
                {
                    GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
                    if (gameManager != null)
                    {
                        gameManager.UpdateBlockInfo(playerMode, block);
                    }
                }
            }
            else
            {
                // Fallback: store the child if no Block component found
                currentPickableObject = other.gameObject;
                Debug.Log($"{playerMode} No Block component found, stored child object: {currentPickableObject.name}");
            }
        }
        // Check if the object has the "tile" tag
        // Skip tile tinting if in picking mode
        else if (other.CompareTag("tile") && isCursorEnabled && !isPickingMode)
        {
            // Get the Block component
            Block block = other.GetComponent<Block>();
            
            if (block != null && block.tint != null)
            {
                // IMPORTANT: Untint any previously tinted blocks first
                UntintCurrentBlock();
                
                // Find all tiles to tint based on current block size
                TintMultipleTiles(block);
            }
        }
    }

    void TintMultipleTiles(Block anchorBlock)
    {
        // Store the anchor block as current
        currentBlock = anchorBlock;
        
        // Get tint color based on player mode
        Color tintColor = playerMode == Player.PlayerMode.Player1 ? Color.red : Color.blue;
        
        if (currentBlockSize == 1)
        {
            // Only tint the single block
            anchorBlock.originalColor = anchorBlock.tint.color;
            anchorBlock.tint.color = tintColor;
            currentTintedBlocks.Add(anchorBlock);
        }
        else if (currentBlockSize == 4)
        {
            // Tint a 2x2 grid of blocks starting from anchor block
            Vector3 anchorPos = anchorBlock.transform.position;
            
            // Define the 2x2 pattern: anchor block + right + up + diagonal
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(0, 0, 0),      // Anchor tile
                new Vector3(1, 0, 0),      // Right
                new Vector3(0, 1, 0),      // Up
                new Vector3(1, 1, 0)       // Diagonal (up-right)
            };
            
            foreach (Vector3 offset in offsets)
            {
                Vector3 targetPos = anchorPos + offset;
                Collider2D col = Physics2D.OverlapPoint(targetPos);
                
                if (col != null && col.CompareTag("tile"))
                {
                    Block block = col.GetComponent<Block>();
                    if (block != null && block.tint != null)
                    {
                        block.originalColor = block.tint.color;
                        block.tint.color = tintColor;
                        currentTintedBlocks.Add(block);
                    }
                }
            }
        }
        else if (currentBlockSize == 2)
        {
            // Tint 2 blocks horizontally from anchor
            Vector3 anchorPos = anchorBlock.transform.position;
            
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(0, 0, 0),      // Anchor tile
                new Vector3(1, 0, 0)       // Right
            };
            
            foreach (Vector3 offset in offsets)
            {
                Vector3 targetPos = anchorPos + offset;
                Collider2D col = Physics2D.OverlapPoint(targetPos);
                
                if (col != null && col.CompareTag("tile"))
                {
                    Block block = col.GetComponent<Block>();
                    if (block != null && block.tint != null)
                    {
                        block.originalColor = block.tint.color;
                        block.tint.color = tintColor;
                        currentTintedBlocks.Add(block);
                    }
                }
            }
        }
        else if (currentBlockSize == 3)
        {
            // Tint 3 blocks horizontally from anchor
            Vector3 anchorPos = anchorBlock.transform.position;
            
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(0, 0, 0),      // Anchor tile
                new Vector3(1, 0, 0),      // Right
                new Vector3(2, 0, 0)       // Right x2
            };
            
            foreach (Vector3 offset in offsets)
            {
                Vector3 targetPos = anchorPos + offset;
                Collider2D col = Physics2D.OverlapPoint(targetPos);
                
                if (col != null && col.CompareTag("tile"))
                {
                    Block block = col.GetComponent<Block>();
                    if (block != null && block.tint != null)
                    {
                        block.originalColor = block.tint.color;
                        block.tint.color = tintColor;
                        currentTintedBlocks.Add(block);
                    }
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // In picking mode, clear pickable object reference when leaving "object"
        if (other.CompareTag("object") && isCursorEnabled && isPickingMode)
        {
            Debug.Log($"{playerMode} Cursor exited trigger with object: {other.gameObject.name}");
            
            // Get the root GameObject to compare with currentPickableObject
            Block block = other.GetComponentInParent<Block>();
            GameObject rootObject = block != null ? block.gameObject : other.gameObject;
            
            // Clear the current pickable object if it matches
            if (currentPickableObject == rootObject)
            {
                currentPickableObject = null;
                Debug.Log($"{playerMode} Cursor cleared currentPickableObject");
                
                // Don't clear the UI - let it persist after selection
                // GameObject gameManagerObj = GameObject.FindGameObjectWithTag("gamemanager");
                // if (gameManagerObj != null)
                // {
                //     GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
                //     if (gameManager != null)
                //     {
                //         gameManager.ClearBlockInfo(playerMode);
                //     }
                // }
            }
        }
        // Check if the object has the "tile" tag
        else if (other.CompareTag("tile"))
        {
            // Get the Block component
            Block block = other.GetComponent<Block>();
            
            // Only untint if this is one of the currently tinted blocks
            if (block != null && currentTintedBlocks.Contains(block))
            {
                UntintCurrentBlock();
            }
        }
    }

    public void SetPickingMode()
    {
        if (spriteRenderer != null && pickingCursorSprite != null)
        {
            spriteRenderer.sprite = pickingCursorSprite;
        }
        
        // Hide block sprite in picking mode
        if (blockSprite != null)
        {
            blockSprite.enabled = false;
        }
        
        // Untint any currently tinted block
        UntintCurrentBlock();
    }

    public void SetBuildingMode(Sprite selectedBlockSprite, int blockSize)
    {
        // Store the block size
        currentBlockSize = blockSize;
        
        // Switch to building cursor sprite
        if (spriteRenderer != null && buildingCursorSprite != null)
        {
            spriteRenderer.sprite = buildingCursorSprite;
        }
        
        // Update block sprite to show selected block
        if (blockSprite != null && selectedBlockSprite != null)
        {
            blockSprite.sprite = selectedBlockSprite;
            blockSprite.enabled = true;
        }
        
        // Scale cursor based on block size
        if (blockSize == 4)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (blockSize == 1)
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
        else if (blockSize == 2)
        {
            transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        }
        else if (blockSize == 3)
        {
            transform.localScale = new Vector3(0.875f, 0.875f, 1f);
        }
    }
}
