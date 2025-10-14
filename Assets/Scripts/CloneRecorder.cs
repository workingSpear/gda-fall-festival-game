using UnityEngine;
using System.Collections.Generic;

public class CloneRecorder : MonoBehaviour
{
    public Rigidbody2D player1Rb, player2Rb;
    public Transform player1SpawnPoint, player2SpawnPoint;
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

    // Momentum preservation during hitstop
    private Vector2 player1PreservedVelocity;
    private Vector2 player2PreservedVelocity;
    private bool player1InHitstop;
    private bool player2InHitstop;

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
        // Recording continues even during hitstop (Time.timeScale = 0)
        // This ensures the clone captures the full sequence including frozen moments
        
        // Record Player 1 velocity
        if (isRecordingPlayer1 && player1Rb != null)
        {
            player1FrameCounter++;
            
            if (player1FrameCounter >= framesBeforeNextSnapShot)
            {
                player1FrameCounter = 0;
                
                Vector2 currentVelocity = player1Rb.linearVelocity;
                
                // Detect hitstop: frozen in time (Time.timeScale = 0) with zero velocity
                if (Time.timeScale == 0f && currentVelocity == Vector2.zero)
                {
                    if (!player1InHitstop)
                    {
                        // Just entered hitstop, preserve current velocity
                        player1InHitstop = true;
                    }
                    // Record the preserved velocity to maintain momentum
                    player1Velocities.Add(player1PreservedVelocity);
                }
                else
                {
                    // Normal recording
                    player1InHitstop = false;
                    // Update preserved velocity if not zero (to capture momentum before hitstop)
                    if (currentVelocity != Vector2.zero)
                    {
                        player1PreservedVelocity = currentVelocity;
                    }
                    player1Velocities.Add(currentVelocity);
                }
            }
        }

        // Record Player 2 velocity
        if (isRecordingPlayer2 && player2Rb != null)
        {
            player2FrameCounter++;
            
            if (player2FrameCounter >= framesBeforeNextSnapShot)
            {
                player2FrameCounter = 0;
                
                Vector2 currentVelocity = player2Rb.linearVelocity;
                
                // Detect hitstop: frozen in time (Time.timeScale = 0) with zero velocity
                if (Time.timeScale == 0f && currentVelocity == Vector2.zero)
                {
                    if (!player2InHitstop)
                    {
                        // Just entered hitstop, preserve current velocity
                        player2InHitstop = true;
                    }
                    // Record the preserved velocity to maintain momentum
                    player2Velocities.Add(player2PreservedVelocity);
                }
                else
                {
                    // Normal recording
                    player2InHitstop = false;
                    // Update preserved velocity if not zero (to capture momentum before hitstop)
                    if (currentVelocity != Vector2.zero)
                    {
                        player2PreservedVelocity = currentVelocity;
                    }
                    player2Velocities.Add(currentVelocity);
                }
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
            player1InHitstop = false;
            player1PreservedVelocity = Vector2.zero;
            // Save starting position from spawn point
            if (player1SpawnPoint != null)
            {
                player1StartPosition = player1SpawnPoint.position;
            }
            else if (player1Rb != null)
            {
                player1StartPosition = player1Rb.position;
            }
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            player2Velocities.Clear();
            isRecordingPlayer2 = true;
            player2FrameCounter = 0;
            player2InHitstop = false;
            player2PreservedVelocity = Vector2.zero;
            // Save starting position from spawn point
            if (player2SpawnPoint != null)
            {
                player2StartPosition = player2SpawnPoint.position;
            }
            else if (player2Rb != null)
            {
                player2StartPosition = player2Rb.position;
            }
        }
    }

    Vector2[] PurgeZeroVelocitySequences(List<Vector2> velocities)
    {
        List<Vector2> optimizedVelocities = new List<Vector2>();
        
        // Count leading zeros (zeros at the beginning)
        int leadingZeros = 0;
        for (int i = 0; i < velocities.Count; i++)
        {
            if (velocities[i] == Vector2.zero)
            {
                leadingZeros++;
            }
            else
            {
                // Found first non-zero value, stop counting
                break;
            }
        }
        
        // Only keep up to 5 leading zeros
        int zerosToKeep = Mathf.Min(leadingZeros, 5);
        for (int i = 0; i < zerosToKeep; i++)
        {
            optimizedVelocities.Add(Vector2.zero);
        }
        
        // Add everything else starting from the first non-zero value
        // This includes all middle zeros and trailing zeros
        for (int i = leadingZeros; i < velocities.Count; i++)
        {
            optimizedVelocities.Add(velocities[i]);
        }
        
        return optimizedVelocities.ToArray();
    }

    public void StopRecording(Player.PlayerMode playerMode)
    {
        if (playerMode == Player.PlayerMode.Player1)
        {
            isRecordingPlayer1 = false;
            
            // Purge excessive zero velocity sequences and convert to array
            Vector2[] recordedVelocities = PurgeZeroVelocitySequences(player1Velocities);
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
                    clone.playerMode = Player.PlayerMode.Player1;
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
            
            // Purge excessive zero velocity sequences and convert to array
            Vector2[] recordedVelocities = PurgeZeroVelocitySequences(player2Velocities);
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
                    clone.playerMode = Player.PlayerMode.Player2;
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
