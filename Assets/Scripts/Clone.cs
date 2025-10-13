using UnityEngine;

public class Clone : MonoBehaviour
{
    public Vector2[] recordedPath;
    public int framesBeforeNextPosition;
    public float playbackSpeed = 1f;
    
    public int serialNumber;

    private float frameCounter;
    private int currentPositionIndex;
    private bool isPlaying;
    public SpriteRenderer spriteRenderer;

    [SerializeField]Rigidbody2D rb;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {  
        UpdateOpacity();
    }

    void Update()
    {
        if (!isPlaying || recordedPath == null || recordedPath.Length == 0)
            return;

        // Increment frame counter by playback speed
        frameCounter += playbackSpeed;

        // Move to next position every N frames
        if (frameCounter >= framesBeforeNextPosition)
        {
            frameCounter -= framesBeforeNextPosition;

            // Update transform position to current recorded position
            if (currentPositionIndex < recordedPath.Length)
            {
                rb.position = recordedPath[currentPositionIndex];
                currentPositionIndex++;
            }
            else
            {
                // Reached end of path
                isPlaying = false;
            }
        }
    }

    public void ResetToStartPosition()
    {
        // Reset position to the first recorded position
        if (recordedPath != null && recordedPath.Length > 0)
        {
            rb.position = recordedPath[0];
        }
        
        // Reset playback state
        currentPositionIndex = 0;
        frameCounter = 0;
        isPlaying = false;
    }

    public void PlayRecordedPath()
    {
        currentPositionIndex = 0;
        frameCounter = 0;
        isPlaying = true;
                 
        spriteRenderer.enabled = true;
        UpdateOpacity(); // Re-apply opacity to ensure it's correct
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
                color.a = 0.77f;
            }
            else if (serialNumber == 1)
            {
                color.a = 0.38f;
            }
            else
            {
                color.a = 0.07f;
            }
            
            spriteRenderer.color = color;
        }
    }
}
