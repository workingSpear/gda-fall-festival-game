using UnityEngine;
using System.Collections.Generic;

public class CloneRecorder : MonoBehaviour
{
    public Rigidbody2D player1Rb, player2Rb;
    public int framesBeforeNextSnapShot = 4;
    public GameObject clonePrefab;
    [Tooltip("Speed multiplier for clone playback (1.0 = normal speed, 2.0 = 2x speed, 0.5 = half speed)")]
    public float playbackSpeed = 1f;
    
    // Recording velocities instead of positions
    public List<Vector2> player1Velocities, player2Velocities;
    
    // Recording starting positions
    private Vector2 player1StartPosition, player2StartPosition;

    // Arrays to store all recorded velocity arrays
    public List<Vector2[]> player1RecordedVelocities;
    public List<Vector2[]> player2RecordedVelocities;
    
    // Starting positions for each recording
    public List<Vector2> player1StartPositions;
    public List<Vector2> player2StartPositions;
    
    // List to keep track of spawned clones
    public List<Clone> player1Clones;
    public List<Clone> player2Clones;

    private int player1FrameCounter;
    private int player2FrameCounter;
    private bool isRecordingPlayer1;
    private bool isRecordingPlayer2;

    public Sprite player1CloneSprite, player2CloneSprite;

    void Start()
    {
        player1Velocities = new List<Vector2>();
        player2Velocities = new List<Vector2>();
        player1RecordedVelocities = new List<Vector2[]>();
        player2RecordedVelocities = new List<Vector2[]>();
        player1StartPositions = new List<Vector2>();
        player2StartPositions = new List<Vector2>();
        player1Clones = new List<Clone>();
        player2Clones = new List<Clone>();
    }

    void Update()
    {
        // Record Player 1 velocity
        if (isRecordingPlayer1 && player1Rb != null)
        {
            player1FrameCounter++;
            
            if (player1FrameCounter >= framesBeforeNextSnapShot)
            {
                player1FrameCounter = 0;
                player1Velocities.Add(player1Rb.linearVelocity);
            }
        }

        // Record Player 2 velocity
        if (isRecordingPlayer2 && player2Rb != null)
        {
            player2FrameCounter++;
            
            if (player2FrameCounter >= framesBeforeNextSnapShot)
            {
                player2FrameCounter = 0;
                player2Velocities.Add(player2Rb.linearVelocity);
            }
        }
    }

    //New architecture:
    //Record velocities every N frames and store in a list
    //Also record the starting position when recording begins
    //When recording stops, create a clone with the velocity data and starting position
    //The clone will use physics to replay the movements
    public void StartRecording(Player.PlayerMode playerMode)
    {
        if (playerMode == Player.PlayerMode.Player1)
        {
            player1Velocities.Clear();
            isRecordingPlayer1 = true;
            player1FrameCounter = 0;
            // Save starting position
            if (player1Rb != null)
            {
                player1StartPosition = player1Rb.position;
            }
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            player2Velocities.Clear();
            isRecordingPlayer2 = true;
            player2FrameCounter = 0;
            // Save starting position
            if (player2Rb != null)
            {
                player2StartPosition = player2Rb.position;
            }
        }
    }

    public void StopRecording(Player.PlayerMode playerMode)
    {
        if (playerMode == Player.PlayerMode.Player1)
        {
            isRecordingPlayer1 = false;
            
            // Convert velocity list to array
            Vector2[] recordedVelocities = player1Velocities.ToArray();
            player1RecordedVelocities.Add(recordedVelocities);
            player1StartPositions.Add(player1StartPosition);
            
            // Create a clone and pass it the recorded velocities
            if (clonePrefab != null && recordedVelocities.Length > 0)
            {
                // Update existing clones' serial numbers
                foreach (Clone existingClone in player1Clones)
                {
                    if (existingClone != null)
                    {
                        existingClone.serialNumber++;
                        existingClone.UpdateOpacity();
                    }
                }
                
                // Create new clone at starting position
                GameObject cloneObj = Instantiate(clonePrefab, player1StartPosition, Quaternion.identity);
                Clone clone = cloneObj.GetComponent<Clone>();
                
                if (clone != null)
                {
                    clone.spriteRenderer.sprite = player1CloneSprite;
                    clone.recordedVelocities = recordedVelocities;
                    clone.startPosition = player1StartPosition;
                    clone.framesBeforeNextVelocity = framesBeforeNextSnapShot;
                    clone.playbackSpeed = playbackSpeed;
                    clone.serialNumber = 0; // Newest clone
                    clone.UpdateOpacity();
                    // Don't call PlayRecording here - GameManager handles staggered spawning
                    player1Clones.Insert(0, clone); // Insert at beginning
                }
            }
            
            // Clear the list for next recording
            player1Velocities.Clear();
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            isRecordingPlayer2 = false;
            
            // Convert velocity list to array
            Vector2[] recordedVelocities = player2Velocities.ToArray();
            player2RecordedVelocities.Add(recordedVelocities);
            player2StartPositions.Add(player2StartPosition);
            
            // Create a clone and pass it the recorded velocities
            if (clonePrefab != null && recordedVelocities.Length > 0)
            {
                // Update existing clones' serial numbers
                foreach (Clone existingClone in player2Clones)
                {
                    if (existingClone != null)
                    {
                        existingClone.serialNumber++;
                        existingClone.UpdateOpacity();
                    }
                }
                
                // Create new clone at starting position
                GameObject cloneObj = Instantiate(clonePrefab, player2StartPosition, Quaternion.identity);
                Clone clone = cloneObj.GetComponent<Clone>();
                
                if (clone != null)
                {
                    clone.spriteRenderer.sprite = player2CloneSprite;
                    clone.recordedVelocities = recordedVelocities;
                    clone.startPosition = player2StartPosition;
                    clone.framesBeforeNextVelocity = framesBeforeNextSnapShot;
                    clone.playbackSpeed = playbackSpeed;
                    clone.serialNumber = 0; // Newest clone
                    clone.UpdateOpacity();
                    // Don't call PlayRecording here - GameManager handles staggered spawning
                    player2Clones.Insert(0, clone); // Insert at beginning
                }
            }
            
            // Clear the list for next recording
            player2Velocities.Clear();
        }
    }
}
