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
    private GameObject currentPickableObject; // Track current pickable object in picking mode
    private Sprite originalCursorSprite;
    private int currentBlockSize = 1; // Track the size of the currently selected block
    
    // Key repeat for grid movement
    private float keyHoldTime = 0f;
    private KeyCode lastHeldKey = KeyCode.None;
    private float initialDelay = 0.3f; // Delay before continuous movement starts
    private float repeatRate = 0.1f; // Time between repeated movements
    private float nextMoveTime = 0f;
    
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
            // In building mode, ensure grid alignment each frame
            if (!isPickingMode)
            {
                // Check if position is off-grid and correct it
                Vector3 pos = transform.position;
                float xRemainder = Mathf.Abs((pos.x * 2f) - Mathf.Round(pos.x * 2f));
                float yRemainder = Mathf.Abs((pos.y * 2f) - Mathf.Round(pos.y * 2f));
                
                // If off-grid by more than a small epsilon, snap to grid
                if (xRemainder > 0.01f || yRemainder > 0.01f)
                {
                    SnapToGrid();
                }
                
                // Check if cursor is stuck in a blocked area and snap out if needed
                if (IsAnyPositionBlocked(transform.position))
                {
                    SnapToNearestValidPosition();
                }
            }
            
            HandleInput();
            
            // Only use smooth movement in picking mode
            if (isPickingMode)
            {
                MoveCursor();
            }
        }
    }

    void HandleInput()
    {
        // In building mode (not picking mode), use grid-based movement
        if (!isPickingMode)
        {
            HandleGridInput();
        }
        else
        {
            // In picking mode, use smooth movement
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
    }

    void SnapToGrid()
    {
        // Snap cursor position to 0.5 unit grid
        Vector3 pos = transform.position;
        
        // Round to nearest 0.5 increment
        pos.x = Mathf.Round(pos.x * 2f) / 2f;
        pos.y = Mathf.Round(pos.y * 2f) / 2f;
        
        // Clamp to bounds
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        
        transform.position = pos;
    }

    void SnapToNearestValidPosition()
    {
        Vector3 currentPos = transform.position;
        Vector2 currentGrid = new Vector2(currentPos.x, currentPos.y);
        
        // Search in expanding rings for a valid position
        float searchRadius = 0.5f;
        int maxSearchSteps = 20; // Search up to 10 units away
        
        for (int step = 1; step <= maxSearchSteps; step++)
        {
            searchRadius = step * 0.5f;
            
            // Check positions in a ring at this radius
            for (float angle = 0; angle < 360f; angle += 45f) // Check 8 directions
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * searchRadius;
                Vector3 testPos = currentPos + new Vector3(offset.x, offset.y, 0);
                
                // Snap to grid
                testPos.x = Mathf.Round(testPos.x * 2f) / 2f;
                testPos.y = Mathf.Round(testPos.y * 2f) / 2f;
                
                // Check if within bounds
                if (testPos.x < minX || testPos.x > maxX || testPos.y < minY || testPos.y > maxY)
                    continue;
                
                // Check if this position is valid (not blocked)
                if (!IsAnyPositionBlocked(testPos))
                {
                    // Found a valid position!
                    transform.position = testPos;
                    Debug.Log($"{playerMode} Cursor was stuck, snapped to valid position: {testPos}");
                    return;
                }
            }
        }
        
        // If no valid position found, snap to a default safe position (center of bounds)
        Vector3 safePos = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        safePos.x = Mathf.Round(safePos.x * 2f) / 2f;
        safePos.y = Mathf.Round(safePos.y * 2f) / 2f;
        transform.position = safePos;
        Debug.LogWarning($"{playerMode} Cursor was stuck, snapped to center: {safePos}");
    }

    void HandleGridInput()
    {
        // Grid-based movement for building mode (0.5 unit grid)
        Vector3 currentPos = transform.position;
        Vector3 newPos = currentPos;
        KeyCode pressedKey = KeyCode.None;
        
        if (playerMode == Player.PlayerMode.Player1)
        {
            // Player 1 controls: WASD
            if (Input.GetKey(KeyCode.W))
            {
                pressedKey = KeyCode.W;
                if (ShouldMove(KeyCode.W))
                {
                    newPos.y += 0.5f;
                }
            }
            else if (Input.GetKey(KeyCode.S))
            {
                pressedKey = KeyCode.S;
                if (ShouldMove(KeyCode.S))
                {
                    newPos.y -= 0.5f;
                }
            }
            else if (Input.GetKey(KeyCode.A))
            {
                pressedKey = KeyCode.A;
                if (ShouldMove(KeyCode.A))
                {
                    newPos.x -= 0.5f;
                }
            }
            else if (Input.GetKey(KeyCode.D))
            {
                pressedKey = KeyCode.D;
                if (ShouldMove(KeyCode.D))
                {
                    newPos.x += 0.5f;
                }
            }
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            // Player 2 controls: IJKL
            if (Input.GetKey(KeyCode.I))
            {
                pressedKey = KeyCode.I;
                if (ShouldMove(KeyCode.I))
                {
                    newPos.y += 0.5f;
                }
            }
            else if (Input.GetKey(KeyCode.K))
            {
                pressedKey = KeyCode.K;
                if (ShouldMove(KeyCode.K))
                {
                    newPos.y -= 0.5f;
                }
            }
            else if (Input.GetKey(KeyCode.J))
            {
                pressedKey = KeyCode.J;
                if (ShouldMove(KeyCode.J))
                {
                    newPos.x -= 0.5f;
                }
            }
            else if (Input.GetKey(KeyCode.L))
            {
                pressedKey = KeyCode.L;
                if (ShouldMove(KeyCode.L))
                {
                    newPos.x += 0.5f;
                }
            }
        }
        
        // Update key repeat tracking
        if (pressedKey == KeyCode.None)
        {
            // No key pressed, reset timing
            keyHoldTime = 0f;
            lastHeldKey = KeyCode.None;
            nextMoveTime = 0f;
        }

        // Check if we actually moved
        if (newPos != currentPos)
        {
            // Clamp to bounds
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
            
            // Check if any position the cursor would occupy is blocked
            if (IsAnyPositionBlocked(newPos))
            {
                // Position is blocked, don't move
                return;
            }
            
            transform.position = newPos;
        }
    }

    bool ShouldMove(KeyCode key)
    {
        // On first press, always move
        if (Input.GetKeyDown(key))
        {
            lastHeldKey = key;
            keyHoldTime = 0f;
            nextMoveTime = Time.time + initialDelay;
            return true;
        }
        
        // Check if this is the same key being held
        if (key == lastHeldKey)
        {
            keyHoldTime += Time.deltaTime;
            
            // After initial delay, allow repeated movement
            if (keyHoldTime >= initialDelay && Time.time >= nextMoveTime)
            {
                nextMoveTime = Time.time + repeatRate;
                return true;
            }
        }
        else
        {
            // Different key pressed, reset
            lastHeldKey = key;
            keyHoldTime = 0f;
            nextMoveTime = Time.time + initialDelay;
        }
        
        return false;
    }

    bool IsAnyPositionBlocked(Vector3 position)
    {
        GameManager gameManager = GameObject.FindGameObjectWithTag("gamemanager")?.GetComponent<GameManager>();
        if (gameManager == null)
            return false;
        
        Vector2 anchor = new Vector2(position.x, position.y);
        
        // Check all positions based on current block size
        if (currentBlockSize == 1)
        {
            // Check only the anchor position
            return gameManager.IsPositionBlocked(anchor);
        }
        else if (currentBlockSize == 2)
        {
            // Check 2 positions horizontally
            if (gameManager.IsPositionBlocked(anchor)) return true;
            if (gameManager.IsPositionBlocked(new Vector2(anchor.x + 0.5f, anchor.y))) return true;
            return false;
        }
        else if (currentBlockSize == 3)
        {
            // Check 3 positions horizontally
            if (gameManager.IsPositionBlocked(anchor)) return true;
            if (gameManager.IsPositionBlocked(new Vector2(anchor.x + 0.5f, anchor.y))) return true;
            if (gameManager.IsPositionBlocked(new Vector2(anchor.x + 1.0f, anchor.y))) return true;
            return false;
        }
        else if (currentBlockSize == 4)
        {
            // Check all 4 positions in the 2x2 grid
            if (gameManager.IsPositionBlocked(anchor)) return true;
            if (gameManager.IsPositionBlocked(new Vector2(anchor.x, anchor.y + 0.5f))) return true;
            if (gameManager.IsPositionBlocked(new Vector2(anchor.x + 0.5f, anchor.y))) return true;
            if (gameManager.IsPositionBlocked(new Vector2(anchor.x + 0.5f, anchor.y + 0.5f))) return true;
            return false;
        }
        
        return false;
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
                
                // Don't clear UI if we're exiting picking mode or making a selection (objects being destroyed)
                GameObject gameManagerObj = GameObject.FindGameObjectWithTag("gamemanager");
                if (gameManagerObj != null)
                {
                    GameManager gameManager = gameManagerObj.GetComponent<GameManager>();
                    if (gameManager != null && !gameManager.isExitingPickingMode && !gameManager.isMakingSelection)
                    {
                        gameManager.ClearBlockInfo(playerMode);
                    }
                }
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
        
        // Reset cursor to size 1
        currentBlockSize = 1;
        transform.localScale = new Vector3(0.5f, 0.5f, 1f);
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
        
        // Snap to grid when entering building mode
        SnapToGrid();
    }
}
