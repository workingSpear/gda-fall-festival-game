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
    
    // Block hovering
    private Block currentBlock;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Start with cursor disabled
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
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

    public void EnableCursor()
    {
        isCursorEnabled = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    public void DisableCursor()
    {
        isCursorEnabled = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Untint any currently tinted block
        if (currentBlock != null && currentBlock.tint != null)
        {
            currentBlock.tint.color = currentBlock.originalColor;
            currentBlock = null;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object has the "tile" tag
        if (other.CompareTag("tile"))
        {
            // Get the Block component
            Block block = other.GetComponent<Block>();
            
            if (block != null && block.tint != null)
            {
                // If we already have a block tinted, untint it first
                if (currentBlock != null && currentBlock != block && currentBlock.tint != null)
                {
                    currentBlock.tint.color = currentBlock.originalColor;
                }
                
                // Save original color before tinting
                block.originalColor = block.tint.color;
                
                // Store the new block
                currentBlock = block;
                
                // Tint based on player mode
                if (playerMode == Player.PlayerMode.Player1)
                {
                    block.tint.color = Color.red;
                }
                else if (playerMode == Player.PlayerMode.Player2)
                {
                    block.tint.color = Color.blue;
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Check if the object has the "tile" tag
        if (other.CompareTag("tile"))
        {
            // Get the Block component
            Block block = other.GetComponent<Block>();
            
            if (block != null && block == currentBlock && block.tint != null)
            {
                // Restore original color
                block.tint.color = block.originalColor;
                
                // Clear stored reference
                currentBlock = null;
            }
        }
    }
}
