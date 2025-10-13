using UnityEngine;

public class Clone : MonoBehaviour
{
    public Vector2[] recordedPath;
    public int framesBeforeNextPosition;
    
    public int serialNumber;

    private int frameCounter;
    private int currentPositionIndex;
    private bool isPlaying;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateOpacity();
    }

    void Update()
    {
        if (!isPlaying || recordedPath == null || recordedPath.Length == 0)
            return;

        // Increment frame counter
        frameCounter++;

        // Move to next position every N frames
        if (frameCounter >= framesBeforeNextPosition)
        {
            frameCounter = 0;

            // Update transform position to current recorded position
            if (currentPositionIndex < recordedPath.Length)
            {
                transform.position = recordedPath[currentPositionIndex];
                currentPositionIndex++;
            }
            else
            {
                // Reached end of path
                isPlaying = false;
            }
        }
    }

    public void PlayRecordedPath()
    {
        currentPositionIndex = 0;
        frameCounter = 0;
        isPlaying = true;
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
