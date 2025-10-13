using UnityEngine;
using System.Collections.Generic;

public class CloneRecorder : MonoBehaviour
{
    public Rigidbody2D player1Rb, player2Rb;
    public int framesBeforeNextSnapShot = 4;
    public GameObject clonePrefab;
    public List<Vector2> player1Positions, player2Positions;

    // 2D array to store all recorded position arrays
    public List<Vector2[]> player1RecordedPaths;
    public List<Vector2[]> player2RecordedPaths;
    
    // List to keep track of spawned clones
    public List<Clone> player1Clones;
    public List<Clone> player2Clones;

    private int frameCounter;
    private bool isRecordingPlayer1;
    private bool isRecordingPlayer2;

    void Start()
    {
        player1Positions = new List<Vector2>();
        player2Positions = new List<Vector2>();
        player1RecordedPaths = new List<Vector2[]>();
        player2RecordedPaths = new List<Vector2[]>();
        player1Clones = new List<Clone>();
        player2Clones = new List<Clone>();

        StartRecording(Player.PlayerMode.Player1);
    }

    void Update()
    {
        // Increment frame counter
        frameCounter++;

        // Record position every N frames
        if (frameCounter >= framesBeforeNextSnapShot)
        {
            frameCounter = 0;

            if (isRecordingPlayer1 && player1Rb != null)
            {
                player1Positions.Add(player1Rb.position);
            }

            if (isRecordingPlayer2 && player2Rb != null)
            {
                player2Positions.Add(player2Rb.position);
            }
        }
    }

    //architecture is gonna look basically like this:
    //recording this clone...so according to framesBeforeNextSnapShot, we're gonna record the X and Y position of the player inside of a Vector2 and store it inside of a list.
    //when the recording stops, create a clone and pass it the List of positions that were recorded.
    //clear the original list in preparation for the next recording.
    public void StartRecording(Player.PlayerMode playerMode)
    {
        if (playerMode == Player.PlayerMode.Player1)
        {
            player1Positions.Clear();
            isRecordingPlayer1 = true;
            frameCounter = 0;
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            player2Positions.Clear();
            isRecordingPlayer2 = true;
            frameCounter = 0;
        }
    }

    public void StopRecording(Player.PlayerMode playerMode)
    {
        if (playerMode == Player.PlayerMode.Player1)
        {
            isRecordingPlayer1 = false;
            
            // Convert list to array
            Vector2[] recordedPath = player1Positions.ToArray();
            player1RecordedPaths.Add(recordedPath);
            
            // Create a clone and pass it the recorded path
            if (clonePrefab != null && recordedPath.Length > 0)
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
                
                // Create new clone
                GameObject cloneObj = Instantiate(clonePrefab, recordedPath[0], Quaternion.identity);
                Clone clone = cloneObj.GetComponent<Clone>();
                
                if (clone != null)
                {
                    clone.recordedPath = recordedPath;
                    clone.framesBeforeNextPosition = framesBeforeNextSnapShot;
                    clone.serialNumber = 0; // Newest clone
                    clone.UpdateOpacity();
                    clone.PlayRecordedPath();
                    player1Clones.Insert(0, clone); // Insert at beginning
                }
            }
            
            // Clear the list for next recording
            player1Positions.Clear();
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            isRecordingPlayer2 = false;
            
            // Convert list to array
            Vector2[] recordedPath = player2Positions.ToArray();
            player2RecordedPaths.Add(recordedPath);
            
            // Create a clone and pass it the recorded path
            if (clonePrefab != null && recordedPath.Length > 0)
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
                
                // Create new clone
                GameObject cloneObj = Instantiate(clonePrefab, recordedPath[0], Quaternion.identity);
                Clone clone = cloneObj.GetComponent<Clone>();
                
                if (clone != null)
                {
                    clone.recordedPath = recordedPath;
                    clone.framesBeforeNextPosition = framesBeforeNextSnapShot;
                    clone.serialNumber = 0; // Newest clone
                    clone.UpdateOpacity();
                    clone.PlayRecordedPath();
                    player2Clones.Insert(0, clone); // Insert at beginning
                }
            }
            
            // Clear the list for next recording
            player2Positions.Clear();
        }
    }
}
