using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{

    //debugging key commands
    //p to reset and start new round
    //y enable building mode

    [Header("References")]
    public Player player1;
    public Player player2;
    public CloneRecorder cloneRecorder;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerText;
    public Cursor player1Cursor;
    public Cursor player2Cursor;
    public HitstopManager hitstopManager;

    [Header("Spawn Positions")]
    public Transform player1SpawnPoint;
    public Transform player2SpawnPoint;

    [Header("Settings")]
    [Tooltip("Delay between each clone spawn")]
    public float cloneSpawnDelay = 0.5f;
    [Tooltip("Delay before respawning clones in building mode")]
    public float buildingModeLoopDelay = 2f;
    [Tooltip("Duration of the countdown timer in seconds")]
    public float timerDuration = 30f;
    [Tooltip("Delay before triggering mass hitstop when timer reaches 0")]
    public float timeoutDelay = 1f;

    [Header("Building Mode")]
    [Tooltip("Block prefab to place in building mode")]
    public GameObject currentBlock;
    public GameObject buildingModeUI;
    [Tooltip("Tiles to tint for player 1 in building mode")]
    public GameObject[] spawnProtectionTiles;
    [Tooltip("Tiles to tint for player 2 in building mode")]
    public GameObject[] endProtectionTiles;
    [Tooltip("Color to tint player 1 tiles")]
    public Color SpawnProtectionTintColor = Color.white;
    [Tooltip("Color to tint player 2 tiles")]
    public Color EndProtectionTintColor = Color.black;

    private int roundCounter = 0;
    private bool isResetting = false;
    private bool isBuildingMode = false;
    private Coroutine buildingModeCoroutine;
    private bool player1HasPlacedBlock = false;
    private bool player2HasPlacedBlock = false;
    
    // Timer state
    private float timeRemaining = 0f;
    private bool isTimerRunning = false;
    private bool hasTimedOut = false;
    private bool isTransitioning = false;
    
    // Store original tile colors for restoration
    private System.Collections.Generic.Dictionary<GameObject, Color> player1TileOriginalColors = new System.Collections.Generic.Dictionary<GameObject, Color>();
    private System.Collections.Generic.Dictionary<GameObject, Color> player2TileOriginalColors = new System.Collections.Generic.Dictionary<GameObject, Color>();

    void Start()
    {
        Application.targetFrameRate = 144;
        UpdateRoundText();
        
        // Assign spawn points to clone recorder
        if (cloneRecorder != null)
        {
            cloneRecorder.player1SpawnPoint = player1SpawnPoint;
            cloneRecorder.player2SpawnPoint = player2SpawnPoint;
        }
        
        // Round 0: Spawn players immediately
        if (player1 != null)
        {
            if (player1SpawnPoint != null)
            {
                player1.transform.position = player1SpawnPoint.position;
            }
            player1.ResetPlayer();
            SetPlayerVisibility(player1, true);
            SetPlayerPhysics(player1, true);
            player1.enabled = true;
        }
        
        if (player2 != null)
        {
            if (player2SpawnPoint != null)
            {
                player2.transform.position = player2SpawnPoint.position;
            }
            player2.ResetPlayer();
            SetPlayerVisibility(player2, true);
            SetPlayerPhysics(player2, true);
            player2.enabled = true;
        }
        
        // Start countdown timer for round 0
        timeRemaining = timerDuration;
        isTimerRunning = true;
        hasTimedOut = false;
        UpdateTimerDisplay();
        
        // Start recording for round 0
        if (cloneRecorder != null)
        {
            cloneRecorder.StartRecording(Player.PlayerMode.Player1);
            cloneRecorder.StartRecording(Player.PlayerMode.Player2);
        }
    }

    void Update()
    {
        // Update timer during platforming mode
        if (isTimerRunning && !isBuildingMode && !hasTimedOut)
        {
            timeRemaining -= Time.deltaTime;
            
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                isTimerRunning = false;
                hasTimedOut = true;
                StartCoroutine(TimeoutSequence());
            }
            
            UpdateTimerDisplay();
        }
        
        // Check if all players and clones are disabled (not resetting, not in build mode, not already transitioning)
        if (!isResetting && !isBuildingMode && !isTransitioning)
        {
            if (AreAllEntitiesDisabled())
            {
                isTransitioning = true;
                StartCoroutine(TransitionToBuildMode());
            }
        }
        
        // Press P to reset and start new round
        if (Input.GetKeyDown(KeyCode.P) && !isResetting && !isBuildingMode)
        {
            StartCoroutine(ResetAndStartNewRound());
        }
        
        // Press Y to toggle building mode
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ToggleBuildingMode();
        }

        // Building mode block placement
        if (isBuildingMode)
        {
            // Player 1: Press Q to place block
            if (Input.GetKeyDown(KeyCode.Q) && !player1HasPlacedBlock && player1Cursor != null)
            {
                PlaceBlock(player1Cursor, ref player1HasPlacedBlock);
            }

            // Player 2: Press O to place block
            if (Input.GetKeyDown(KeyCode.O) && !player2HasPlacedBlock && player2Cursor != null)
            {
                PlaceBlock(player2Cursor, ref player2HasPlacedBlock);
            }
        }
    }

    IEnumerator ResetAndStartNewRound(bool exitBuildingMode = false)
    {
        isResetting = true;
        roundCounter++;
        UpdateRoundText();

        // Stop current recording and create clone
        if (cloneRecorder != null)
        {
            cloneRecorder.StopRecording(Player.PlayerMode.Player1);
            cloneRecorder.StopRecording(Player.PlayerMode.Player2);
            
            // Hide and reset all existing clones
            foreach (Clone clone in cloneRecorder.player1Clones)
            {
                if (clone != null)
                {
                    clone.ResetClone(); // Reset hitstop state
                    SetCloneVisibility(clone, false);
                    clone.ResetToStartPosition();
                }
            }
            
            foreach (Clone clone in cloneRecorder.player2Clones)
            {
                if (clone != null)
                {
                    clone.ResetClone(); // Reset hitstop state
                    SetCloneVisibility(clone, false);
                    clone.ResetToStartPosition();
                }
            }
        }

        // Disable players and reset to spawn positions
        if (player1 != null)
        {
            player1.enabled = false;
            SetPlayerVisibility(player1, false);
            SetPlayerPhysics(player1, false);
            if (player1SpawnPoint != null)
            {
                player1.transform.position = player1SpawnPoint.position;
            }
            // Reset velocity
            Rigidbody2D rb1 = player1.GetComponent<Rigidbody2D>();
            if (rb1 != null)
            {
                rb1.linearVelocity = Vector2.zero;
            }
        }

        if (player2 != null)
        {
            player2.enabled = false;
            SetPlayerVisibility(player2, false);
            SetPlayerPhysics(player2, false);
            if (player2SpawnPoint != null)
            {
                player2.transform.position = player2SpawnPoint.position;
            }
            // Reset velocity
            Rigidbody2D rb2 = player2.GetComponent<Rigidbody2D>();
            if (rb2 != null)
            {
                rb2.linearVelocity = Vector2.zero;
            }
        }

        // Stagger clone spawns from oldest to newest (both players at the same time)
        if (cloneRecorder != null)
        {
            // Get the maximum number of clones between both players
            int maxClones = Mathf.Max(cloneRecorder.player1Clones.Count, cloneRecorder.player2Clones.Count);
            
            // Exit building mode after spawning has started
            if (exitBuildingMode && isBuildingMode)
            {
                ToggleBuildingMode();
            }
            
            // Spawn clones from oldest to newest for both players simultaneously
            for (int i = maxClones - 1; i >= 0; i--)
            {
                // Spawn Player 1 clone if it exists at this index
                if (i < cloneRecorder.player1Clones.Count)
                {
                    Clone clone1 = cloneRecorder.player1Clones[i];
                    if (clone1 != null)
                    {
                        clone1.PlayRecordedPath();
                    }
                }
                
                // Spawn Player 2 clone if it exists at this index
                if (i < cloneRecorder.player2Clones.Count)
                {
                    Clone clone2 = cloneRecorder.player2Clones[i];
                    if (clone2 != null)
                    {
                        clone2.PlayRecordedPath();
                    }
                }
                
                // Wait before spawning next set of clones
                yield return new WaitForSeconds(cloneSpawnDelay);
            }
        }

        // Finally, spawn the players
        if (player1 != null)
        {
            player1.ResetPlayer();
            SetPlayerVisibility(player1, true);
            SetPlayerPhysics(player1, true);
            player1.enabled = true;
        }

        if (player2 != null)
        {
            player2.ResetPlayer();
            SetPlayerVisibility(player2, true);
            SetPlayerPhysics(player2, true);
            player2.enabled = true;
        }

        // Start countdown timer when players spawn
        timeRemaining = timerDuration;
        isTimerRunning = true;
        hasTimedOut = false;
        UpdateTimerDisplay();

        // Start recording for the next round
        if (cloneRecorder != null)
        {
            cloneRecorder.StartRecording(Player.PlayerMode.Player1);
            cloneRecorder.StartRecording(Player.PlayerMode.Player2);
        }

        isResetting = false;
    }

    void UpdateRoundText()
    {
        if (roundText != null)
        {
            roundText.text = roundCounter.ToString();
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            // Format: 10.32 (shows seconds with 2 decimal places for 100ths of a second)
            timerText.text = timeRemaining.ToString("F2");
        }
    }

    bool AreAllEntitiesDisabled()
    {
        // Check if both players are disabled
        bool player1Disabled = player1 == null || !player1.enabled;
        bool player2Disabled = player2 == null || !player2.enabled;
        
        if (!player1Disabled || !player2Disabled)
        {
            return false; // At least one player is still active
        }
        
        // Check if all clones are disabled
        if (cloneRecorder != null)
        {
            foreach (Clone clone in cloneRecorder.player1Clones)
            {
                if (clone != null && clone.enabled)
                {
                    return false; // Found an active clone
                }
            }
            
            foreach (Clone clone in cloneRecorder.player2Clones)
            {
                if (clone != null && clone.enabled)
                {
                    return false; // Found an active clone
                }
            }
        }
        
        // All entities are disabled
        return true;
    }

    IEnumerator TransitionToBuildMode()
    {
        // Small delay to let death effects complete
        yield return new WaitForSeconds(1f);
        
        // Increment round counter
        roundCounter++;
        UpdateRoundText();
        
        // Stop current recording and create clones
        if (cloneRecorder != null)
        {
            cloneRecorder.StopRecording(Player.PlayerMode.Player1);
            cloneRecorder.StopRecording(Player.PlayerMode.Player2);
        }
        
        // Reset players to spawn positions but keep them disabled
        if (player1 != null)
        {
            player1.ResetPlayer();
            if (player1SpawnPoint != null)
            {
                player1.transform.position = player1SpawnPoint.position;
            }
            player1.enabled = false;
            SetPlayerVisibility(player1, false);
            SetPlayerPhysics(player1, false);
        }
        
        if (player2 != null)
        {
            player2.ResetPlayer();
            if (player2SpawnPoint != null)
            {
                player2.transform.position = player2SpawnPoint.position;
            }
            player2.enabled = false;
            SetPlayerVisibility(player2, false);
            SetPlayerPhysics(player2, false);
        }
        
        // Toggle to building mode (this will start clone playback)
        if (!isBuildingMode)
        {
            ToggleBuildingMode();
        }
        
        // Reset transition flag after build mode is active
        isTransitioning = false;
    }

    IEnumerator TimeoutSequence()
    {
        // Freeze all player movement for the delay period
        if (player1 != null)
        {
            player1.enabled = false;
        }
        
        if (player2 != null)
        {
            player2.enabled = false;
        }
        
        // Wait for the timeout delay
        yield return new WaitForSeconds(timeoutDelay);
        
        // Trigger hitstop on all remaining active players
        if (player1 != null && !player1.hasBeenHitStopped && hitstopManager != null)
        {
            GameObject deathPrefab = player1.playerMode == Player.PlayerMode.Player1 ? player1.player1DeathPrefab : player1.player2DeathPrefab;
            player1.hasBeenHitStopped = true;
            hitstopManager.TriggerHitstop(player1, deathPrefab, false);
        }
        
        if (player2 != null && !player2.hasBeenHitStopped && hitstopManager != null)
        {
            GameObject deathPrefab = player2.playerMode == Player.PlayerMode.Player1 ? player2.player1DeathPrefab : player2.player2DeathPrefab;
            player2.hasBeenHitStopped = true;
            hitstopManager.TriggerHitstop(player2, deathPrefab, false);
        }
        
        // Trigger hitstop on all remaining active clones
        if (cloneRecorder != null && hitstopManager != null)
        {
            foreach (Clone clone in cloneRecorder.player1Clones)
            {
                if (clone != null && clone.enabled && !clone.hasBeenHitStopped)
                {
                    clone.hasBeenHitStopped = true;
                    hitstopManager.TriggerCloneHitstop(clone, clone.player1DeathPrefab, false);
                }
            }
            
            foreach (Clone clone in cloneRecorder.player2Clones)
            {
                if (clone != null && clone.enabled && !clone.hasBeenHitStopped)
                {
                    clone.hasBeenHitStopped = true;
                    hitstopManager.TriggerCloneHitstop(clone, clone.player2DeathPrefab, false);
                }
            }
        }
    }

    void SetPlayerVisibility(Player player, bool visible)
    {
        if (player != null)
        {
            SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }
    }

    void SetPlayerPhysics(Player player, bool enabled)
    {
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // When disabled, make kinematic (no physics)
                // When enabled, make dynamic (physics active)
                rb.bodyType = enabled ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            }
            
            BoxCollider2D boxCollider = player.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.enabled = enabled;
            }
            
            // Also handle jumpable head collider
            if (player.jumpableHead != null)
            {
                player.jumpableHead.enabled = enabled;
            }
        }
    }

    void SetCloneVisibility(Clone clone, bool visible)
    {
        if (clone != null)
        {
            SpriteRenderer spriteRenderer = clone.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
            
            BoxCollider2D boxCollider = clone.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                boxCollider.enabled = visible;
            }
        }
    }

    void TintBuildModeTiles()
    {
        // Tint player 1 tiles
        if (spawnProtectionTiles != null)
        {
            foreach (GameObject tile in spawnProtectionTiles)
            {
                if (tile != null)
                {
                    SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // Store original color
                        if (!player1TileOriginalColors.ContainsKey(tile))
                        {
                            player1TileOriginalColors[tile] = sr.color;
                        }
                        // Apply tint
                        sr.color = SpawnProtectionTintColor;
                    }
                }
            }
        }

        // Tint player 2 tiles
        if (endProtectionTiles != null)
        {
            foreach (GameObject tile in endProtectionTiles)
            {
                if (tile != null)
                {
                    SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // Store original color
                        if (!player2TileOriginalColors.ContainsKey(tile))
                        {
                            player2TileOriginalColors[tile] = sr.color;
                        }
                        // Apply tint
                        sr.color = EndProtectionTintColor;
                    }
                }
            }
        }
    }

    void UntintBuildModeTiles()
    {
        // Restore player 1 tiles
        if (spawnProtectionTiles != null)
        {
            foreach (GameObject tile in spawnProtectionTiles)
            {
                if (tile != null && player1TileOriginalColors.ContainsKey(tile))
                {
                    SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = player1TileOriginalColors[tile];
                    }
                }
            }
        }

        // Restore player 2 tiles
        if (endProtectionTiles != null)
        {
            foreach (GameObject tile in endProtectionTiles)
            {
                if (tile != null && player2TileOriginalColors.ContainsKey(tile))
                {
                    SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = player2TileOriginalColors[tile];
                    }
                }
            }
        }
    }

    void PlaceBlock(Cursor cursor, ref bool hasPlacedBlock)
    {
        if (cursor == null || currentBlock == null)
            return;

        // Get the currently hovered block from the cursor
        Block hoveredBlock = cursor.GetCurrentBlock();

        if (hoveredBlock != null)
        {
            // Get the position of the tinted square
            Vector3 blockPosition = hoveredBlock.transform.position;

            // Spawn the block prefab at that position
            Instantiate(currentBlock, blockPosition, Quaternion.identity);

            // Mark that this player has placed a block
            hasPlacedBlock = true;

            // Disable the cursor
            cursor.DisableCursor();

            // Check if both players have placed blocks
            if (player1HasPlacedBlock && player2HasPlacedBlock)
            {
                StartCoroutine(ExitBuildingModeAndStartNewRound());
            }
        }
    }

    IEnumerator ExitBuildingModeAndStartNewRound()
    {
        // Small delay to let the player see both blocks placed
        yield return new WaitForSeconds(0.5f);

        // Start a new round (spawning begins)
        if (!isResetting)
        {
            StartCoroutine(ResetAndStartNewRound(true));
        }
    }

    void ToggleBuildingMode()
    {
        isBuildingMode = !isBuildingMode;

        if (isBuildingMode)
        {
            buildingModeUI.SetActive(true);
            // Entering building mode
            // Reset placement flags
            player1HasPlacedBlock = false;
            player2HasPlacedBlock = false;
            
            // Stop the timer
            isTimerRunning = false;
            
            // Tint build mode tiles
            TintBuildModeTiles();
            
            // Disable players
            if (player1 != null)
            {
                player1.enabled = false;
                SetPlayerVisibility(player1, false);
                SetPlayerPhysics(player1, false);
            }

            if (player2 != null)
            {
                player2.enabled = false;
                SetPlayerVisibility(player2, false);
                SetPlayerPhysics(player2, false);
            }

            // Set building mode flag on all clones
            if (cloneRecorder != null)
            {
                foreach (Clone clone in cloneRecorder.player1Clones)
                {
                    if (clone != null)
                    {
                        clone.isBuildingMode = true;
                    }
                }
                
                foreach (Clone clone in cloneRecorder.player2Clones)
                {
                    if (clone != null)
                    {
                        clone.isBuildingMode = true;
                    }
                }
            }

            // Enable cursors
            if (player1Cursor != null)
            {
                player1Cursor.EnableCursor();
            }

            if (player2Cursor != null)
            {
                player2Cursor.EnableCursor();
            }

            // Start looping clone playback
            buildingModeCoroutine = StartCoroutine(BuildingModeCloneLoop());
        }
        else
        {
            buildingModeUI.SetActive(false);
            // Exiting building mode
            // Reset placement flags
            player1HasPlacedBlock = false;
            player2HasPlacedBlock = false;
            
            // Untint build mode tiles
            UntintBuildModeTiles();
            
            // Stop the looping coroutine
            if (buildingModeCoroutine != null)
            {
                StopCoroutine(buildingModeCoroutine);
                buildingModeCoroutine = null;
            }

            // Hide all clones and disable building mode flag
            if (cloneRecorder != null)
            {
                foreach (Clone clone in cloneRecorder.player1Clones)
                {
                    if (clone != null)
                    {
                        clone.isBuildingMode = false;
                        clone.ResetClone(); // Reset hitstop state
                        SetCloneVisibility(clone, false);
                        clone.ResetToStartPosition();
                    }
                }

                foreach (Clone clone in cloneRecorder.player2Clones)
                {
                    if (clone != null)
                    {
                        clone.isBuildingMode = false;
                        clone.ResetClone(); // Reset hitstop state
                        SetCloneVisibility(clone, false);
                        clone.ResetToStartPosition();
                    }
                }
            }

            // Disable cursors
            if (player1Cursor != null)
            {
                player1Cursor.DisableCursor();
            }

            if (player2Cursor != null)
            {
                player2Cursor.DisableCursor();
            }

            // Enable players
            if (player1 != null)
            {
                player1.ResetPlayer();
                SetPlayerVisibility(player1, true);
                SetPlayerPhysics(player1, true);
                player1.enabled = true;
            }

            if (player2 != null)
            {
                player2.ResetPlayer();
                SetPlayerVisibility(player2, true);
                SetPlayerPhysics(player2, true);
                player2.enabled = true;
            }
            
            // Reset timer flags for next round
            hasTimedOut = false;
            isTransitioning = false;
        }
    }

    IEnumerator BuildingModeCloneLoop()
    {
        while (isBuildingMode)
        {
            // Hide and reset all clones first
            if (cloneRecorder != null)
            {
                foreach (Clone clone in cloneRecorder.player1Clones)
                {
                    if (clone != null)
                    {
                        clone.ResetClone(); // Reset hitstop state
                        SetCloneVisibility(clone, false);
                        clone.ResetToStartPosition();
                    }
                }

                foreach (Clone clone in cloneRecorder.player2Clones)
                {
                    if (clone != null)
                    {
                        clone.ResetClone(); // Reset hitstop state
                        SetCloneVisibility(clone, false);
                        clone.ResetToStartPosition();
                    }
                }

                // Stagger clone spawns from oldest to newest (both players at the same time)
                int maxClones = Mathf.Max(cloneRecorder.player1Clones.Count, cloneRecorder.player2Clones.Count);

                // Spawn clones from oldest to newest for both players simultaneously
                for (int i = maxClones - 1; i >= 0; i--)
                {
                    // Spawn Player 1 clone if it exists at this index
                    if (i < cloneRecorder.player1Clones.Count)
                    {
                        Clone clone1 = cloneRecorder.player1Clones[i];
                        if (clone1 != null)
                        {
                            clone1.PlayRecordedPath();
                        }
                    }

                    // Spawn Player 2 clone if it exists at this index
                    if (i < cloneRecorder.player2Clones.Count)
                    {
                        Clone clone2 = cloneRecorder.player2Clones[i];
                        if (clone2 != null)
                        {
                            clone2.PlayRecordedPath();
                        }
                    }

                    // Wait before spawning next set of clones
                    yield return new WaitForSeconds(cloneSpawnDelay);
                }

                // Calculate the longest recording duration
                float maxDuration = 0f;
                foreach (Clone clone in cloneRecorder.player1Clones)
                {
                    if (clone != null && clone.recordedVelocities != null)
                    {
                        float duration = (clone.recordedVelocities.Length * clone.framesBeforeNextVelocity) / (Application.targetFrameRate * clone.playbackSpeed);
                        maxDuration = Mathf.Max(maxDuration, duration);
                    }
                }

                foreach (Clone clone in cloneRecorder.player2Clones)
                {
                    if (clone != null && clone.recordedVelocities != null)
                    {
                        float duration = (clone.recordedVelocities.Length * clone.framesBeforeNextVelocity) / (Application.targetFrameRate * clone.playbackSpeed);
                        maxDuration = Mathf.Max(maxDuration, duration);
                    }
                }

                // Wait for all clones to finish
                yield return new WaitForSeconds(maxDuration);

                // Wait before looping again
                yield return new WaitForSeconds(buildingModeLoopDelay);
            }
        }
    }
}
