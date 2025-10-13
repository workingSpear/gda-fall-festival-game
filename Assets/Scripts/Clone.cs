using UnityEngine;

public class Clone : MonoBehaviour
{
    public Vector2[] recordedVelocities;
    public Vector2 startPosition;
    public int framesBeforeNextVelocity;
    public float playbackSpeed = 1f;
    
    public int serialNumber;

    private float frameCounter;
    private int currentVelocityIndex;
    private bool isPlaying;
    public SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        // Set up rigidbody for physics-based playback
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.gravityScale = 2f; // Match player gravity
        }
    }

    void Start()
    {  
        UpdateOpacity();
    }

    void Update()
    {
        if (!isPlaying || recordedVelocities == null || recordedVelocities.Length == 0)
            return;

        // Increment frame counter by playback speed
        frameCounter += playbackSpeed;

        // Apply next velocity every N frames
        if (frameCounter >= framesBeforeNextVelocity)
        {
            frameCounter -= framesBeforeNextVelocity;

            // Apply the recorded velocity to rigidbody
            if (currentVelocityIndex < recordedVelocities.Length && rb != null)
            {
                rb.linearVelocity = recordedVelocities[currentVelocityIndex];
                currentVelocityIndex++;
            }
            else
            {
                // Reached end of recording
                isPlaying = false;
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
    }

    public void ResetToStartPosition()
    {
        // Reset to starting position
        if (rb != null)
        {
            rb.position = startPosition;
            rb.linearVelocity = Vector2.zero;
        }
        
        // Reset playback state
        currentVelocityIndex = 0;
        frameCounter = 0;
        isPlaying = false;
    }

    public void PlayRecordedPath()
    {
        currentVelocityIndex = 0;
        frameCounter = 0;
        isPlaying = true;
        
        // Reset to start position
        if (rb != null)
        {
            rb.position = startPosition;
            rb.linearVelocity = Vector2.zero;
        }
                 
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
