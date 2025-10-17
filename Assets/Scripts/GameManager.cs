using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public Player player1;
    public Player player2;
    public CloneRecorder cloneRecorder;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerText;
    public Cursor player1Cursor;
    public Cursor player2Cursor;
    public HitstopManager hitstopManager;
    public AudioManager audioManager;
    
    [Header("Points System")]
    [Tooltip("UI Images for Player 1 points display")]
    public UnityEngine.UI.Image[] player1PointImages;
    [Tooltip("UI Images for Player 2 points display")]
    public UnityEngine.UI.Image[] player2PointImages;
    
    [Header("Picking Mode UI")]
    [Tooltip("Player 1 block name display")]
    public TextMeshProUGUI player1BlockNameText;
    [Tooltip("Player 1 block description display")]
    public TextMeshProUGUI player1BlockDescriptionText;
    [Tooltip("Player 1 block sprite display")]
    public UnityEngine.UI.Image player1BlockImage;
    [Tooltip("Player 2 block name display")]
    public TextMeshProUGUI player2BlockNameText;
    [Tooltip("Player 2 block description display")]
    public TextMeshProUGUI player2BlockDescriptionText;
    [Tooltip("Player 2 block sprite display")]
    public UnityEngine.UI.Image player2BlockImage;
    
    [Header("Status Indicators")]
    [Tooltip("Build module P1 ready indicator")]
    public UnityEngine.UI.Image BuildModuleP1Ready;
    [Tooltip("Build module P1 not ready indicator")]
    public UnityEngine.UI.Image BuildModuleP1NotReady;
    [Tooltip("Score module P1 alive indicator")]
    public UnityEngine.UI.Image ScoreModuleP1Alive;
    [Tooltip("Score module P1 not alive indicator")]
    public UnityEngine.UI.Image ScoreModuleP1NotAlive;
    [Tooltip("Build module P2 ready indicator")]
    public UnityEngine.UI.Image BuildModuleP2Ready;
    [Tooltip("Build module P2 not ready indicator")]
    public UnityEngine.UI.Image BuildModuleP2NotReady;
    [Tooltip("Score module P2 alive indicator")]
    public UnityEngine.UI.Image ScoreModuleP2Alive;
    [Tooltip("Score module P2 not alive indicator")]
    public UnityEngine.UI.Image ScoreModuleP2NotAlive;

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

    [Header("Picking Mode")]
    public GameObject pickingModeUI;
    [Tooltip("Array of selectable objects for picking mode")]
    public GameObject[] pickableObjects;
    [Tooltip("Array of selectable drinks for picking mode")]
    public GameObject[] pickableDrinks;
    [Tooltip("Probability of spawning a drink instead of regular object (0.0 to 1.0)")]
    [Range(0f, 1f)]
    public float drinkSpawnProbability = 0.3f;
    
    [Header("Building Mode")]
    [Tooltip("Block prefab to place in building mode")]
    public GameObject buildingModeUI;
    [Tooltip("Parent transform for all placed blocks")]
    public Transform objectTransformMommy;
    [Tooltip("GENESIS spawn point - 3x3 area around it is blocked")]
    public Transform GENESIS;
    [Tooltip("TERMINUS end point - 3x3 area around it is blocked")]
    public Transform TERMINUS;
    
    [Header("Round Transition")]
    [Tooltip("StaticNoise GameObject to show during round transitions")]
    public GameObject StaticNoise;
    [Tooltip("Duration to show StaticNoise between rounds")]
    public float staticNoiseDuration = 2f;
    
    [Header("Player Spawn Effects")]
    [Tooltip("Particle system prefab for Player 1 spawn")]
    public GameObject player1SpawnParticlePrefab;
    [Tooltip("Particle system prefab for Player 2 spawn")]
    public GameObject player2SpawnParticlePrefab;
    
    [Header("Game Ending UI")]
    [Tooltip("TextMeshPro object for blocks placed message")]
    public TextMeshPro blocksPlacedText;
    [Tooltip("TextMeshPro object for recorded deaths message")]
    public TextMeshPro recordedDeathsText;
    [Tooltip("TextMeshPro object for winner message")]
    public TextMeshPro winnerText;
    
    [Header("Game Ending Prefabs")]
    [Tooltip("Player 1 rigidbody clone prefab for raining effect")]
    public GameObject player1RigidbodyClone;
    [Tooltip("Player 2 rigidbody clone prefab for raining effect")]
    public GameObject player2RigidbodyClone;
    
    [Header("Debug - Occupied Positions")]
    [Tooltip("Show all occupied positions in inspector (read-only)")]
    public bool showOccupiedPositions = true;
    
    [Header("All Occupied Positions (Read-Only)")]
    [SerializeField] private Vector2[] allOccupiedPositions = new Vector2[0];
    [SerializeField] private string occupiedPositionsString = "";

    private int roundCounter = 0;
    private bool isResetting = false;
    private bool isPickingMode = false;
    private bool isBuildingMode = false;
    private bool isWinMode = false;
    public bool isExitingPickingMode = false;
    public bool isMakingSelection = false;
    private Coroutine buildingModeCoroutine;
    private bool player1HasPlacedBlock = false;
    private bool player2HasPlacedBlock = false;
    private bool player1HasPicked = false;
    private bool player2HasPicked = false;
    [SerializeField]private GameObject player1SelectedBlock;
    [SerializeField]private GameObject player2SelectedBlock;
    private System.Collections.Generic.List<GameObject> currentPickableObjects = new System.Collections.Generic.List<GameObject>();
    private System.Collections.Generic.List<GameObject> spawnedPickableObjects = new System.Collections.Generic.List<GameObject>();
    private System.Collections.Generic.List<GameObject> availablePickableDrinks = new System.Collections.Generic.List<GameObject>();
    private System.Collections.Generic.List<GameObject> usedDrinks = new System.Collections.Generic.List<GameObject>();
    
    // Timer state
    private float timeRemaining = 0f;
    private bool isTimerRunning = false;
    private bool hasTimedOut = false;
    private bool isTransitioning = false;
    
    // Disabled blocks tracking
    private System.Collections.Generic.List<Block> disabledBlocks = new System.Collections.Generic.List<Block>();
    
    // Points tracking
    [SerializeField]public int player1Points = 0;
    [SerializeField]public int player2Points = 0;
    
    // Game ending tracking
    private int totalBlocksPlaced = 0;
    private int player1Deaths = 0;
    private int player2Deaths = 0;
    private bool gameEnded = false;
    private System.Collections.Generic.List<GameObject> placedBlockPrefabs = new System.Collections.Generic.List<GameObject>();
    
    // Store original sprite renderer colors for placed objects
    private System.Collections.Generic.Dictionary<SpriteRenderer, Color> placedObjectOriginalColors = new System.Collections.Generic.Dictionary<SpriteRenderer, Color>();

    void Start()
    {
        Application.targetFrameRate = 144;
        
        // Reset used drinks for new game
        usedDrinks.Clear();
        
        UpdateRoundText();
        UpdatePointDisplays();
        
        // Assign spawn points to clone recorder
        if (cloneRecorder != null)
        {
            cloneRecorder.player1SpawnPoint = player1SpawnPoint;
            cloneRecorder.player2SpawnPoint = player2SpawnPoint;
        }
        
        // Round 0: Reset players to spawn positions but keep disabled
        if (player1 != null)
        {
            if (player1SpawnPoint != null)
            {
                player1.transform.position = player1SpawnPoint.position;
            }
            player1.ResetPlayer();
            SetPlayerVisibility(player1, false);
            SetPlayerPhysics(player1, false);
            player1.enabled = false;
        }
        
        if (player2 != null)
        {
            if (player2SpawnPoint != null)
            {
                player2.transform.position = player2SpawnPoint.position;
            }
            player2.ResetPlayer();
            SetPlayerVisibility(player2, false);
            SetPlayerPhysics(player2, false);
            player2.enabled = false;
        }
        
        // Initialize StaticNoise as disabled
        if (StaticNoise != null)
        {
            StaticNoise.SetActive(false);
        }
        
        // Start the spawn sequence with staggering for round 0
        StartCoroutine(InitialPlayerSpawn());
    }

    IEnumerator InitialPlayerSpawn()
    {
        // Spawn Player 1 first
        if (player1 != null)
        {
            player1.ResetPlayer();
            SetPlayerVisibility(player1, true);
            SetPlayerPhysics(player1, true);
            player1.enabled = true;
            
            // Set ScoreModule to Alive when player spawns
            SetScoreModuleAliveState(Player.PlayerMode.Player1, true);
            
            // Spawn particle effect for Player 1
            SpawnPlayerParticleEffect(Player.PlayerMode.Player1, player1.transform.position);
            
            // Play spawn buzzer sound
            if (audioManager != null)
            {
                audioManager.PlayPlayerSpawnBuzzer();
            }
        }
        
        // Spawn player 2 simultaneously (no delay)
        if (player2 != null)
        {
            player2.ResetPlayer();
            SetPlayerVisibility(player2, true);
            SetPlayerPhysics(player2, true);
            player2.enabled = true;
            
            // Set ScoreModule to Alive when player spawns
            SetScoreModuleAliveState(Player.PlayerMode.Player2, true);
            
            // Spawn particle effect for Player 2
            SpawnPlayerParticleEffect(Player.PlayerMode.Player2, player2.transform.position);
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
        
        yield break;
    }

    void Update()
    {
        // Update occupied positions display
        UpdateOccupiedPositions();
        
        // Update timer during platforming mode
        if (isTimerRunning && !isPickingMode && !isBuildingMode && !isWinMode && !hasTimedOut)
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
        
        // Picking mode selection
        if (isPickingMode && !isWinMode)
        {
            // Player 1: Press Q to make selection
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (!player1HasPicked && player1Cursor != null)
            {
                MakeSelection(player1Cursor, ref player1HasPicked);
                }
            }

            // Player 2: Press O to make selection
            if (Input.GetKeyDown(KeyCode.O))
            {
                if (!player2HasPicked && player2Cursor != null)
            {
                MakeSelection(player2Cursor, ref player2HasPicked);
                }
            }
        }
        
        // Check if all players and clones are disabled (not resetting, not in picking/build mode, not already transitioning)
        if (!isResetting && !isPickingMode && !isBuildingMode && !isTransitioning)
        {
            if (AreAllEntitiesDisabled())
            {
                isTransitioning = true;
                StartCoroutine(TransitionToPickingMode());
            }
        }

        // Building mode block placement
        if (isBuildingMode && !isWinMode)
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
        // Don't allow new rounds during win mode
        if (isWinMode) yield break;
        
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

        // Exit building mode if needed (with StaticNoise transition)
        if (exitBuildingMode && isBuildingMode)
        {
            yield return StartCoroutine(ShowStaticNoiseTransition());
            ToggleBuildingMode();
        }
        
        // First, stagger clone spawns from oldest to newest (both players at the same time)
        if (cloneRecorder != null)
        {
            // Get the maximum number of clones between both players
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
        }
        
        // Finally, spawn the players with their own delay (don't wait for clones to finish)
        if (player1 != null)
        {
            player1.ResetPlayer();
            SetPlayerVisibility(player1, true);
            SetPlayerPhysics(player1, true);
            player1.enabled = true;
            
            // Set ScoreModule to Alive when player spawns
            SetScoreModuleAliveState(Player.PlayerMode.Player1, true);
            
            // Spawn particle effect for Player 1
            SpawnPlayerParticleEffect(Player.PlayerMode.Player1, player1.transform.position);
            
            // Play spawn buzzer sound
            if (audioManager != null)
            {
                audioManager.PlayPlayerSpawnBuzzer();
            }
        }
        
        // Spawn player 2 simultaneously (no delay)
        if (player2 != null)
        {
            player2.ResetPlayer();
            SetPlayerVisibility(player2, true);
            SetPlayerPhysics(player2, true);
            player2.enabled = true;
            
            // Set ScoreModule to Alive when player spawns
            SetScoreModuleAliveState(Player.PlayerMode.Player2, true);
            
            // Spawn particle effect for Player 2
            SpawnPlayerParticleEffect(Player.PlayerMode.Player2, player2.transform.position);
        }

        // Show "GO!" sequence and start timer after players spawn
        StartCoroutine(ShowGoSequence());

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
            if (isPickingMode)
            {
                timerText.text = "PICK";
            }
            else if (isBuildingMode)
            {
                timerText.text = "BUILD";
            }
            else if (!isTimerRunning)
            {
                // Platforming mode but timer not running (before players spawn)
                timerText.text = "...";
            }
            else
            {
                // Platforming mode with timer running (after players spawn)
                timerText.text = timeRemaining.ToString("F2");
            }
        }
    }
    
    // Coroutine for "GO!" sequence when players spawn
    IEnumerator ShowGoSequence()
    {
        if (timerText != null)
        {
            timerText.text = "GO!";
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Start the timer after the "GO!" display
        timeRemaining = timerDuration + (roundCounter * 0.25f);
        isTimerRunning = true;
        hasTimedOut = false;
        UpdateTimerDisplay();
    }

    public void AwardPoint(Player.PlayerMode playerMode)
    {
        if (playerMode == Player.PlayerMode.Player1)
        {
            player1Points++;
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            player2Points++;
        }
        
        UpdatePointDisplays();
        
        // Check for game ending condition
        if (player1Points >= 15 || player2Points >= 15)
        {
            StartGameEndingSequence();
        }
    }

    public void OnPlayerDeath(Player.PlayerMode playerMode, bool isRealDeath = true, bool isFromClone = false)
    {
        // Track death for game ending (only real deaths, not reaching end)
        if (isRealDeath)
        {
            if (playerMode == Player.PlayerMode.Player1)
            {
                player1Deaths++;
            }
            else if (playerMode == Player.PlayerMode.Player2)
            {
                player2Deaths++;
            }
        }
        
        // Only set ScoreModule to NotAlive when the actual player dies, not when clones die
        if (!isFromClone)
        {
            SetScoreModuleAliveState(playerMode, false);
        }
        
        // Check if both players are now dead
        bool bothPlayersDead = false;
        
        if (player1 != null && player2 != null)
        {
            bothPlayersDead = player1.hasBeenHitStopped && player2.hasBeenHitStopped;
        }
        
        // If both players are dead, start coroutine to kill all remaining clones with queue delay
        if (bothPlayersDead && cloneRecorder != null && hitstopManager != null)
        {
            StartCoroutine(KillRemainingClones());
        }
    }

    IEnumerator KillRemainingClones()
    {
        if (cloneRecorder == null || hitstopManager == null)
            yield break;
        
        // Kill all remaining active clones with the hitstop queue delay
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

    void UpdatePointDisplays()
    {
        // Update Player 1 point images
        if (player1PointImages != null)
        {
            for (int i = 0; i < player1PointImages.Length; i++)
            {
                if (player1PointImages[i] != null)
                {
                    Color color = player1PointImages[i].color;
                    // Set opacity to 1 if this point has been earned, otherwise 0
                    color.a = i < player1Points ? 1f : 0f;
                    player1PointImages[i].color = color;
                }
            }
        }
        
        // Update Player 2 point images (fills from right to left)
        if (player2PointImages != null)
        {
            for (int i = 0; i < player2PointImages.Length; i++)
            {
                if (player2PointImages[i] != null)
                {
                    Color color = player2PointImages[i].color;
                    // Fill from right to left: rightmost images light up first
                    color.a = i >= (player2PointImages.Length - player2Points) ? 1f : 0f;
                    player2PointImages[i].color = color;
                }
            }
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

    IEnumerator TransitionToPickingMode()
    {
        // Don't transition to picking mode during win mode
        if (isWinMode) yield break;
        
        // Small delay to let death effects complete
        yield return new WaitForSeconds(1f);
        
        // Show StaticNoise during transition to picking mode
        yield return StartCoroutine(ShowStaticNoiseTransition());
        
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
        
        // Enter picking mode
        if (!isPickingMode)
        {
            EnterPickingMode();
        }
        
        // Reset BuildModules to NotReady state for picking mode
        SetBuildModuleReadyState(Player.PlayerMode.Player1, false);
        SetBuildModuleReadyState(Player.PlayerMode.Player2, false);
        
        // Reset transition flag after picking mode is active
        isTransitioning = false;
    }

    IEnumerator TimeoutSequence()
    {
        // Play timer buzzer sound
        if (audioManager != null)
        {
            audioManager.PlayTimerBuzzer();
        }
        
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

    public void RegisterDisabledBlocks(Collider2D[] colliders)
    {
        foreach (Collider2D col in colliders)
        {
            Block block = col.GetComponent<Block>();
            if (block != null && block.isDisabled && !disabledBlocks.Contains(block))
            {
                disabledBlocks.Add(block);
            }
        }
    }
    
    void ReEnableAllDisabledBlocks()
    {
        foreach (Block block in disabledBlocks)
        {
            if (block != null) // Check if block still exists (not destroyed)
            {
                block.EnableBlock();
                
                // Special logic for Fan objects - also enable their children
                if (block is Fan)
                {
                    Fan fan = block as Fan;
                    for (int i = 0; i < fan.transform.childCount; i++)
                    {
                        Transform child = fan.transform.GetChild(i);
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }
        
        // Clear the list of disabled blocks
        disabledBlocks.Clear();
    }
    
    
    void ReEnableBombs()
    {
        // Find Object Transform Mommy
        Transform objectTransformMommy = GameObject.FindGameObjectWithTag("mommy")?.transform;
        if (objectTransformMommy == null)
        {
            return;
        }
        
        // Get all objects with "bomb" tag under Object Transform Mommy
        GameObject[] bombObjects = GameObject.FindGameObjectsWithTag("bomb");
        
        foreach (GameObject bombObject in bombObjects)
        {
            // Check if this bomb is a child of Object Transform Mommy
            if (bombObject.transform.IsChildOf(objectTransformMommy))
            {
                Bomb bomb = bombObject.GetComponent<Bomb>();
                if (bomb != null)
                {
                    // Force enable the bomb using its public method
                    bomb.ForceEnableBomb();
                }
            }
        }
    }
    
    void ReEnableTurrets()
    {
        // Find Object Transform Mommy
        Transform objectTransformMommy = GameObject.FindGameObjectWithTag("mommy")?.transform;
        if (objectTransformMommy == null)
        {
            return;
        }
        
        // Get all objects with "turret" tag under Object Transform Mommy
        GameObject[] turretObjects = GameObject.FindGameObjectsWithTag("turret");
        
        foreach (GameObject turretObject in turretObjects)
        {
            // Check if this turret is a child of Object Transform Mommy
            if (turretObject.transform.IsChildOf(objectTransformMommy))
            {
                Turret turret = turretObject.GetComponentInChildren<Turret>();
                if (turret != null && turret.isDisabled)
                {
                    // Toggle off isDisabled flag
                    turret.isDisabled = false;
                    
                    // Re-enable the sprite renderer from the Turret component
                    if (turret.spriteRenderer != null)
                    {
                        turret.spriteRenderer.enabled = true;
                    }
                    
                    // Re-enable all colliders
                    Collider2D[] colliders = turretObject.GetComponents<Collider2D>();
                    foreach (Collider2D col in colliders)
                    {
                        if (col != null)
                        {
                            col.enabled = true;
                        }
                    }
                }
            }
        }
    }
    
    
    void ResetDrinkEffects()
    {
        // Find DrinkManager and reset all drink effects
        DrinkManager drinkManager = FindFirstObjectByType<DrinkManager>();
        if (drinkManager != null)
        {
            drinkManager.ResetAllPlayerEffects();
            drinkManager.ReEnableAllDrinkables();
        }
    }
    
    public bool IsInBuildingMode()
    {
        // Check if building mode UI is active
        return buildingModeUI != null && buildingModeUI.activeInHierarchy;
    }
    
    public bool IsInPickingMode()
    {
        return isPickingMode;
    }
    
    // Weighted loot table selection for picking mode
    GameObject SelectWeightedObject(System.Collections.Generic.List<int> availableIndices)
    {
        if (availableIndices.Count == 0) return null;
        
        // Create weighted list based on object properties
        System.Collections.Generic.List<WeightedObject> weightedObjects = new System.Collections.Generic.List<WeightedObject>();
        
        foreach (int index in availableIndices)
        {
            GameObject obj = pickableObjects[index];
            if (obj == null) continue;
            
            // Check round restrictions
            if (!IsObjectAvailableForRound(obj)) continue;
            
            // Calculate weight based on object size
            float weight = CalculateObjectWeight(obj);
            if (weight > 0)
            {
                weightedObjects.Add(new WeightedObject { gameObject = obj, weight = weight, index = index });
            }
        }
        
        if (weightedObjects.Count == 0) return null;
        
        // Select based on weights
        float totalWeight = 0f;
        foreach (var weightedObj in weightedObjects)
        {
            totalWeight += weightedObj.weight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var weightedObj in weightedObjects)
        {
            currentWeight += weightedObj.weight;
            if (randomValue <= currentWeight)
            {
                return weightedObj.gameObject;
            }
        }
        
        // Fallback to first object
        return weightedObjects[0].gameObject;
    }
    
    // Check if object is available for current round
    bool IsObjectAvailableForRound(GameObject obj)
    {
        if (obj == null) return false;
        
        string objName = obj.name.ToUpper();
        
        // Drinks not available before round 5
        if (objName.Contains("DRINK") && roundCounter < 5)
        {
            return false;
        }
        
        // BOMB objects not available before round 3
        if (objName.Contains("BOMB") && roundCounter < 3)
        {
            return false;
        }
        
        return true;
    }
    
    // Calculate weight based on object size and round
    float CalculateObjectWeight(GameObject obj)
    {
        if (obj == null) return 1f;
        
        // Try to get Block component to determine size
        Block block = obj.GetComponent<Block>();
        if (block != null)
        {
            if (block.size == 4)
            {
                // Size 4 objects: start at 0.3 weight and gradually decrease to 0 by round 25-30
                if (roundCounter <= 25)
                {
                    // Linear interpolation from 0.3 at round 1 to 0 at round 25
                    float t = (roundCounter - 1) / 24f; // t goes from 0 to 1 as round goes from 1 to 25
                    return Mathf.Lerp(0.3f, 0f, t);
                }
                else
                {
                    // Completely unavailable after round 25
                    return 0f;
                }
            }
            else if (block.size == 1)
            {
                // Size 1 objects: consistent weight
                return 1f;
            }
            else
            {
                // Linear scaling for other sizes with slight round adjustment
                float baseWeight = 1f / block.size;
                if (roundCounter <= 3)
                {
                    return baseWeight * 1.2f; // Slightly more likely in early rounds
                }
                else
                {
                    return baseWeight; // Normal weight in later rounds
                }
            }
        }
        
        // Default weight if no Block component
        return 1f;
    }
    
    // Helper class for weighted selection
    [System.Serializable]
    public class WeightedObject
    {
        public GameObject gameObject;
        public float weight;
        public int index;
    }

    void EnterPickingMode()
    {
        // Don't enter picking mode during win mode
        if (isWinMode) return;
        
        isPickingMode = true;
        
        // Re-enable all disabled blocks when entering picking mode
        ReEnableAllDisabledBlocks();
        
        // Specifically re-enable bombs in Object Transform Mommy
        ReEnableBombs();
        
        // Specifically re-enable turrets in Object Transform Mommy
        ReEnableTurrets();
        
        // Reset all drink effects when entering picking mode
        ResetDrinkEffects();
        
        // Stop the timer during picking mode
        isTimerRunning = false;
        UpdateTimerDisplay();
        
        
        // Show picking mode UI
        if (pickingModeUI != null)
        {
            pickingModeUI.SetActive(true);
        }
        
        // Clear UI when entering picking mode (fresh start for new round)
        ClearBlockInfo(Player.PlayerMode.Player1);
        ClearBlockInfo(Player.PlayerMode.Player2);
        
        // Reset selection flags and selected blocks
        player1HasPicked = false;
        player2HasPicked = false;
        player1SelectedBlock = null;
        player2SelectedBlock = null;
        
        // Fade placed objects to 15% opacity
        FadePlacedObjects();
        
        // Randomly select objects to display
        SelectRandomObjects();
        
        // Disable all players and clones
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
        
        // Disable all clones
        if (cloneRecorder != null)
        {
            foreach (Clone clone in cloneRecorder.player1Clones)
            {
                if (clone != null)
                {
                    clone.enabled = false;
                    SetCloneVisibility(clone, false);
                }
            }
            
            foreach (Clone clone in cloneRecorder.player2Clones)
            {
                if (clone != null)
                {
                    clone.enabled = false;
                    SetCloneVisibility(clone, false);
                }
            }
        }
        
        // Enable cursors in picking mode
        if (player1Cursor != null)
        {
            player1Cursor.EnableCursor(true);
            player1Cursor.SetPickingMode();
        }
        
        if (player2Cursor != null)
        {
            player2Cursor.EnableCursor(true);
            player2Cursor.SetPickingMode();
        }
    }

    void ExitPickingMode()
    {
        // Don't allow picking mode transitions during win mode
        if (isWinMode) return;
        
        isPickingMode = false;
        isExitingPickingMode = true;
        
        // Hide picking mode UI
        if (pickingModeUI != null)
        {
            pickingModeUI.SetActive(false);
        }
        
        // Restore placed objects to full opacity
        RestorePlacedObjects();
        
        // Don't clear block info displays - keep them visible in build mode
        // ClearBlockInfo(Player.PlayerMode.Player1);
        // ClearBlockInfo(Player.PlayerMode.Player2);
        
        // Disable cursors FIRST to prevent OnTriggerExit2D from clearing UI
        if (player1Cursor != null)
        {
            player1Cursor.DisableCursor();
        }
        
        if (player2Cursor != null)
        {
            player2Cursor.DisableCursor();
        }
        
        // Now destroy all spawned pickable objects (cursors are disabled so no UI clearing)
        foreach (GameObject obj in spawnedPickableObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedPickableObjects.Clear();
        
        // Clear current pickable objects
        currentPickableObjects.Clear();
        
        // Reset the exit flag
        isExitingPickingMode = false;
    }

    void FadePlacedObjects()
    {
        // Check if Object Transform Mommy exists
        if (objectTransformMommy == null)
            return;
        
        // Clear the dictionary first
        placedObjectOriginalColors.Clear();
        
        // Get all SpriteRenderers under Object Transform Mommy
        SpriteRenderer[] spriteRenderers = objectTransformMommy.GetComponentsInChildren<SpriteRenderer>();
        
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr != null)
            {
                // Store original color
                placedObjectOriginalColors[sr] = sr.color;
                
                // Set to 15% opacity
                Color fadedColor = sr.color;
                fadedColor.a = 0.15f;
                sr.color = fadedColor;
            }
        }
    }

    void RestorePlacedObjects()
    {
        // Restore all sprite renderers to their original colors
        foreach (var kvp in placedObjectOriginalColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.color = kvp.Value;
            }
        }
        
        // Clear the dictionary
        placedObjectOriginalColors.Clear();
    }

    public void UpdateBlockInfo(Player.PlayerMode playerMode, Block block)
    {
        if (block == null)
            return;
        
        if (playerMode == Player.PlayerMode.Player1)
        {
            // Update Player 1 UI
            if (player1BlockNameText != null)
            {
                player1BlockNameText.text = block.blockName;
            }
            
            if (player1BlockDescriptionText != null)
            {
                player1BlockDescriptionText.text = block.blockDescription;
            }
            
            if (player1BlockImage != null && block.blockSprite != null)
            {
                player1BlockImage.sprite = block.blockSprite;
                player1BlockImage.enabled = true;
            }
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            // Update Player 2 UI
            if (player2BlockNameText != null)
            {
                player2BlockNameText.text = block.blockName;
            }
            
            if (player2BlockDescriptionText != null)
            {
                player2BlockDescriptionText.text = block.blockDescription;
            }
            
            if (player2BlockImage != null && block.blockSprite != null)
            {
                player2BlockImage.sprite = block.blockSprite;
                player2BlockImage.enabled = true;
            }
            
        }
    }

    public void ClearBlockInfo(Player.PlayerMode playerMode)
    {
        if (playerMode == Player.PlayerMode.Player1)
        {
            if (player1BlockNameText != null)
            {
                player1BlockNameText.text = "";
            }
            
            if (player1BlockDescriptionText != null)
            {
                player1BlockDescriptionText.text = "";
            }
            
            if (player1BlockImage != null)
            {
                player1BlockImage.enabled = false;
            }
            
            
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            if (player2BlockNameText != null)
            {
                player2BlockNameText.text = "";
            }
            
            if (player2BlockDescriptionText != null)
            {
                player2BlockDescriptionText.text = "";
            }
            
            if (player2BlockImage != null)
            {
                player2BlockImage.enabled = false;
            }
            
        }
    }

    void SelectRandomObjects()
    {
        currentPickableObjects.Clear();
        
        // Destroy any previously spawned pickable objects
        foreach (GameObject obj in spawnedPickableObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedPickableObjects.Clear();
        
        // Reset available drinks list (only include unused drinks)
        availablePickableDrinks.Clear();
        if (pickableDrinks != null && pickableDrinks.Length > 0)
        {
            foreach (GameObject drink in pickableDrinks)
            {
                if (drink != null && !usedDrinks.Contains(drink))
                {
                    availablePickableDrinks.Add(drink);
                }
            }
        }
        
        if (pickableObjects == null || pickableObjects.Length == 0)
            return;
        
        // Calculate how many objects to select: 3 items until round 5, then 3-5 items
        int numToSelect;
        if (roundCounter < 5)
        {
            numToSelect = 3; // Only 3 items until round 5
        }
        else
        {
            numToSelect = Random.Range(3, 6); // 3-5 items from round 5 onwards
        }
        numToSelect = Mathf.Min(numToSelect, pickableObjects.Length);
        
        // Create a list of indices to randomly select from
        System.Collections.Generic.List<int> availableIndices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < pickableObjects.Length; i++)
        {
            availableIndices.Add(i);
        }
        
        // First, decide if we should spawn a drink this round (only 1 per round)
        // Drinks not available before round 5
        bool shouldSpawnDrink = availablePickableDrinks.Count > 0 && roundCounter >= 5 && Random.Range(0f, 1f) < drinkSpawnProbability;
        GameObject drinkPrefab = null;
        
        if (shouldSpawnDrink)
        {
            // Select one drink for this round
            int randomDrinkIndex = Random.Range(0, availablePickableDrinks.Count);
            drinkPrefab = availablePickableDrinks[randomDrinkIndex];
            
            // Mark this drink as used (permanently)
            usedDrinks.Add(drinkPrefab);
            
            // Remove the drink from available list so it can't be spawned again
            availablePickableDrinks.RemoveAt(randomDrinkIndex);
        }
        
        // Randomly select and spawn objects using weighted loot table
        for (int i = 0; i < numToSelect; i++)
        {
            GameObject prefab = null;
            
            // If we decided to spawn a drink and this is the first spawn, spawn the drink
            if (shouldSpawnDrink && i == 0 && drinkPrefab != null)
            {
                prefab = drinkPrefab;
            }
            else
            {
                // Use weighted selection for regular objects
                prefab = SelectWeightedObject(availableIndices);
                if (prefab != null)
                {
                    // Remove the selected object from available indices
                    int selectedIndex = System.Array.IndexOf(pickableObjects, prefab);
                    if (selectedIndex >= 0 && availableIndices.Contains(selectedIndex))
                    {
                        availableIndices.Remove(selectedIndex);
                    }
                }
            }
            
            if (prefab != null)
            {
                // Generate random position within range (X: -5 to 5, Y: 3 to 4)
                float randomX = Random.Range(-5f, 5f);
                float randomY = Random.Range(3f, 4f);
                Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);
                
                // Spawn the object
                GameObject spawnedObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
                spawnedPickableObjects.Add(spawnedObj);
                currentPickableObjects.Add(spawnedObj);
            }
        }
    }

    void MakeSelection(Cursor cursor, ref bool hasPicked)
    {
        isMakingSelection = true;
        
        // Get the object the cursor is currently hovering over
        GameObject hoveredObject = cursor.GetCurrentPickableObject();
        
        
        if (hoveredObject == null)
        {
            return; // Not hovering over a valid pickable object
        }
        
        // The hoveredObject might be a child with the "object" tag
        // We need to find the root GameObject that was instantiated (the one with the Block component)
        Block block = hoveredObject.GetComponentInParent<Block>();
        
        if (block == null)
        {
            return;
        }
        
        GameObject rootObject = block.gameObject;
        
        // Handle all objects (including drinks) as selectable blocks
        // Get the blockPrefab from the Block component
        GameObject blockPrefab = block.blockPrefab;
        
        if (blockPrefab == null)
        {
            return;
        }
        
        
        // Store the blockPrefab for this player
        if (cursor.playerMode == Player.PlayerMode.Player1)
        {
            player1SelectedBlock = blockPrefab;
        }
        else if (cursor.playerMode == Player.PlayerMode.Player2)
        {
            player2SelectedBlock = blockPrefab;
        }
        
        // Destroy the selected object
        Destroy(rootObject);
        spawnedPickableObjects.Remove(rootObject);
        currentPickableObjects.Remove(rootObject);
        
        // Mark as picked
        hasPicked = true;
        
        // Set BuildModule to Ready when player makes selection
        SetBuildModuleReadyState(cursor.playerMode, true);
        
        // Don't clear the UI - keep it visible in build mode
        // ClearBlockInfo(cursor.playerMode);
        
        // Disable the cursor
        cursor.DisableCursor();
        
        // Check if both players have made selections
        if (player1HasPicked && player2HasPicked)
        {
            StartCoroutine(ExitPickingModeAndEnterBuildMode());
        }
        
        // Reset the selection flag
        isMakingSelection = false;
    }

    IEnumerator ExitPickingModeAndEnterBuildMode()
    {
        // Small delay
        yield return new WaitForSeconds(0.5f);
        
        // Show StaticNoise during transition from picking to building mode
        yield return StartCoroutine(ShowStaticNoiseTransition());
        
        // Exit picking mode
        ExitPickingMode();
        
        // Enter building mode
        if (!isBuildingMode)
        {
            ToggleBuildingMode();
        }
        
        // Reset BuildModules to NotReady state for building mode
        SetBuildModuleReadyState(Player.PlayerMode.Player1, false);
        SetBuildModuleReadyState(Player.PlayerMode.Player2, false);
    }

    void PlaceBlock(Cursor cursor, ref bool hasPlacedBlock)
    {
        if (cursor == null)
            return;
        
        // Get the appropriate block prefab and size for this player
        GameObject blockToPlace = null;
        int blockSize = 1;
        
        if (cursor.playerMode == Player.PlayerMode.Player1)
        {
            blockToPlace = player1SelectedBlock;
            if (player1SelectedBlock != null)
            {
                Block block = player1SelectedBlock.GetComponent<Block>();
                if (block != null)
                {
                    blockSize = block.size;
                }
            }
        }
        else if (cursor.playerMode == Player.PlayerMode.Player2)
        {
            blockToPlace = player2SelectedBlock;
            if (player2SelectedBlock != null)
            {
                Block block = player2SelectedBlock.GetComponent<Block>();
                if (block != null)
                {
                    blockSize = block.size;
                }
            }
        }
        
        if (blockToPlace == null)
            return;

        // Get the cursor's grid-aligned position
        Vector3 blockPosition = cursor.transform.position;
        
        // Round to ensure it's on the grid (should already be snapped, but just to be safe)
        blockPosition.x = Mathf.Round(blockPosition.x * 2f) / 2f;
        blockPosition.y = Mathf.Round(blockPosition.y * 2f) / 2f;
        blockPosition.z = 0f;

            // Spawn the block prefab at that position
        GameObject placedBlock = Instantiate(blockToPlace, blockPosition, Quaternion.identity);
        
        // Set the parent to Object Transform Mommy
        if (objectTransformMommy != null)
        {
            placedBlock.transform.SetParent(objectTransformMommy);
        }

        // Set the original grid position for Fan blocks
        if (placedBlock.GetComponent<Fan>() != null)
        {
            Fan fan = placedBlock.GetComponent<Fan>();
            fan.SetGridPosition(new Vector2(blockPosition.x, blockPosition.y));
        }
        
        // Update occupied positions display
        UpdateOccupiedPositions();

        // No need to manually track blocked positions - we scan dynamically

            // Mark that this player has placed a block
            hasPlacedBlock = true;

        // Track block placement for game ending (only during build mode)
        if (isBuildingMode)
        {
            totalBlocksPlaced++;
            placedBlockPrefabs.Add(blockToPlace);
        }
        
        // Set BuildModule to Ready when player places block
        SetBuildModuleReadyState(cursor.playerMode, true);

            // Disable the cursor
            cursor.DisableCursor();
        
        // Clear the UI after placing the block
        ClearBlockInfo(cursor.playerMode);

            // Check if both players have placed blocks
            if (player1HasPlacedBlock && player2HasPlacedBlock)
            {
                StartCoroutine(ExitBuildingModeAndStartNewRound());
            }
        }


    public bool IsPositionBlocked(Vector2 position)
    {
        // Round to grid to ensure consistent checking
        Vector2 gridPos = new Vector2(
            Mathf.Round(position.x * 2f) / 2f,
            Mathf.Round(position.y * 2f) / 2f
        );
        
        // During building mode, block all positions on y=2
        if (isBuildingMode && Mathf.Abs(gridPos.y - 2f) < 0.01f)
            return true;
        
        // Dynamically scan placed blocks and check if position is blocked
        if (IsPositionBlockedByPlacedBlocks(gridPos))
            return true;
        
        // Check if position is in 3x3 area around GENESIS
        if (IsInProtectedArea(gridPos, GENESIS))
            return true;
        
        // Check if position is in 3x3 area around TERMINUS
        if (IsInProtectedArea(gridPos, TERMINUS))
            return true;
        
        // Check if position is in 3x3 area around Player 1 spawn point
        if (IsInProtectedArea(gridPos, player1SpawnPoint))
            return true;
        
        // Check if position is in 3x3 area around Player 2 spawn point
        if (IsInProtectedArea(gridPos, player2SpawnPoint))
            return true;
        
        return false;
    }

    bool IsPositionBlockedByPlacedBlocks(Vector2 position)
    {
        if (objectTransformMommy == null)
            return false;
        
        // Get all Block components under Object Transform Mommy
        Block[] placedBlocks = objectTransformMommy.GetComponentsInChildren<Block>();
        
        foreach (Block block in placedBlocks)
        {
            if (block == null)
                continue;
            
            // Get the block's anchor position
            Vector2 anchor = new Vector2(block.transform.position.x, block.transform.position.y);
            
            // Special handling for objects with "fanMod" tag - use OGPos from Fan component
            if (block.CompareTag("fanMod"))
            {
                Fan fan = block.GetComponent<Fan>();
                if (fan != null)
                {
                    anchor = fan.OGPos;
                }
            }
            
            // Calculate all positions this block occupies based on its size
            System.Collections.Generic.List<Vector2> occupiedPositions = GetOccupiedPositions(anchor, block.size);
            
            // Check if the queried position matches any occupied position
            foreach (Vector2 occupiedPos in occupiedPositions)
            {
                if (Mathf.Abs(position.x - occupiedPos.x) < 0.01f && Mathf.Abs(position.y - occupiedPos.y) < 0.01f)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    System.Collections.Generic.List<Vector2> GetOccupiedPositions(Vector2 anchor, int blockSize)
    {
        System.Collections.Generic.List<Vector2> positions = new System.Collections.Generic.List<Vector2>();
        
        if (blockSize == 1)
        {
            // Only the anchor position
            positions.Add(anchor);
        }
        else if (blockSize == 2)
        {
            // 2 positions horizontally
            positions.Add(anchor);
            positions.Add(new Vector2(anchor.x + 0.5f, anchor.y));
        }
        else if (blockSize == 3)
        {
            // 3 positions horizontally
            positions.Add(anchor);
            positions.Add(new Vector2(anchor.x + 0.5f, anchor.y));
            positions.Add(new Vector2(anchor.x + 1.0f, anchor.y));
        }
        else if (blockSize == 4)
        {
            // 2x2 grid
            positions.Add(anchor);
            positions.Add(new Vector2(anchor.x, anchor.y + 0.5f));
            positions.Add(new Vector2(anchor.x + 0.5f, anchor.y));
            positions.Add(new Vector2(anchor.x + 0.5f, anchor.y + 0.5f));
        }
        
        return positions;
    }

    bool IsInProtectedArea(Vector2 position, Transform protectedTransform)
    {
        if (protectedTransform == null)
            return false;
        
        Vector2 center = new Vector2(protectedTransform.position.x, protectedTransform.position.y);
        
        // Check if position is within 3x3 grid (1.0 unit in each direction from center)
        // This creates a 3x3 grid: -0.5, 0, +0.5 in both X and Y
        float deltaX = Mathf.Abs(position.x - center.x);
        float deltaY = Mathf.Abs(position.y - center.y);
        
        // Within 1.0 units in both directions means it's in the 3x3 area
        return deltaX <= 1.0f && deltaY <= 1.0f;
    }

    IEnumerator ShowStaticNoiseTransition()
    {
        // Determine which mode we're transitioning to based on context
        bool isPlatformerMode = DetermineTargetMode();
        
        // Determine if we're coming from chill mode (picking or building)
        bool isFromChillMode = isPickingMode || isBuildingMode;
        
        // Start StaticNoise audio and begin crossfade (if needed)
        if (audioManager != null)
        {
            audioManager.StartStaticNoiseTransition(isPlatformerMode, isFromChillMode);
        }
        
        // Show StaticNoise during transition
        if (StaticNoise != null)
        {
            StaticNoise.SetActive(true);
        }
        
        // Wait for StaticNoise duration (should match AudioManager crossfade duration)
        float transitionDuration = audioManager != null ? audioManager.crossfadeDuration : staticNoiseDuration;
        yield return new WaitForSeconds(transitionDuration);
        
        // Hide StaticNoise
        if (StaticNoise != null)
        {
            StaticNoise.SetActive(false);
        }
        
        // Stop StaticNoise audio (crossfade should be complete)
        if (audioManager != null)
        {
            audioManager.EndStaticNoiseTransition();
        }
    }
    
    bool DetermineTargetMode()
    {
        // Determine if we're transitioning to platformer mode or chill mode
        // This is called at the end of StaticNoise transitions
        
        // Check the context of where this transition is being called from
        
        // If we're in building mode, we're about to exit to platformer mode
        if (isBuildingMode)
        {
            return true; // Going to platformer mode
        }
        
        // If we're in picking mode, we're transitioning to building mode (chill music continues)
        if (isPickingMode)
        {
            return false; // Going to chill mode (building mode)
        }
        
        // If we're transitioning from platformer to picking mode
        if (!isPickingMode && !isBuildingMode)
        {
            return false; // Going to chill mode (picking mode)
        }
        
        // Default to platformer mode for safety
        return true;
    }

    IEnumerator ExitBuildingModeAndStartNewRound()
    {
        // Small delay to let the player see both blocks placed
        yield return new WaitForSeconds(0.5f);

        // Start a new round (spawning begins) - ResetAndStartNewRound will handle the StaticNoise transition
        if (!isResetting)
        {
            StartCoroutine(ResetAndStartNewRound(true));
        }
    }

    void ToggleBuildingMode()
    {
        // Don't allow building mode transitions during win mode
        if (isWinMode) return;
        
        isBuildingMode = !isBuildingMode;

        if (isBuildingMode)
        {
            buildingModeUI.SetActive(true);
            // Entering building mode
            // Reset placement flags
            player1HasPlacedBlock = false;
            player2HasPlacedBlock = false;
            
            
            // Stop the timer during build mode
            isTimerRunning = false;
            UpdateTimerDisplay();
            
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

            // Enable cursors with building mode sprites
            if (player1Cursor != null)
            {
                player1Cursor.EnableCursor();
                
                // Get the selected block sprite and size for Player 1
                Sprite p1BlockSprite = null;
                int p1BlockSize = 1;
                if (player1SelectedBlock != null)
                {
                    Block block = player1SelectedBlock.GetComponent<Block>();
                    if (block != null)
                    {
                        p1BlockSprite = block.blockSprite;
                        p1BlockSize = block.size;
                    }
                }
                player1Cursor.SetBuildingMode(p1BlockSprite, p1BlockSize);
            }

            if (player2Cursor != null)
            {
                player2Cursor.EnableCursor();
                
                // Get the selected block sprite and size for Player 2
                Sprite p2BlockSprite = null;
                int p2BlockSize = 1;
                if (player2SelectedBlock != null)
                {
                    Block block = player2SelectedBlock.GetComponent<Block>();
                    if (block != null)
                    {
                        p2BlockSprite = block.blockSprite;
                        p2BlockSize = block.size;
                    }
                }
                player2Cursor.SetBuildingMode(p2BlockSprite, p2BlockSize);
            }

            // Start looping clone playback
            buildingModeCoroutine = StartCoroutine(BuildingModeCloneLoop());
        }
        else
        {
            buildingModeUI.SetActive(false);
            // Exiting building mode (entering platforming mode)
            // Reset placement flags
            player1HasPlacedBlock = false;
            player2HasPlacedBlock = false;
            
            // Show "..." immediately when switching to platforming mode
            isTimerRunning = false;
            UpdateTimerDisplay();
            
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
    
    void SetBuildModuleReadyState(Player.PlayerMode playerMode, bool isReady)
    {
        UnityEngine.UI.Image readyImage = null;
        UnityEngine.UI.Image notReadyImage = null;
        
        if (playerMode == Player.PlayerMode.Player1)
        {
            readyImage = BuildModuleP1Ready;
            notReadyImage = BuildModuleP1NotReady;
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            readyImage = BuildModuleP2Ready;
            notReadyImage = BuildModuleP2NotReady;
        }
        
        if (readyImage != null && notReadyImage != null)
        {
            if (isReady)
            {
                // Ready: Ready=1.0 opacity, NotReady=0.16 opacity
                Color readyColor = readyImage.color;
                readyColor.a = 1.0f;
                readyImage.color = readyColor;
                
                Color notReadyColor = notReadyImage.color;
                notReadyColor.a = 0.16f;
                notReadyImage.color = notReadyColor;
            }
            else
            {
                // Not Ready: Ready=0.16 opacity, NotReady=1.0 opacity
                Color readyColor = readyImage.color;
                readyColor.a = 0.16f;
                readyImage.color = readyColor;
                
                Color notReadyColor = notReadyImage.color;
                notReadyColor.a = 1.0f;
                notReadyImage.color = notReadyColor;
            }
        }
    }
    
    void SetScoreModuleAliveState(Player.PlayerMode playerMode, bool isAlive)
    {
        UnityEngine.UI.Image aliveImage = null;
        UnityEngine.UI.Image notAliveImage = null;
        
        if (playerMode == Player.PlayerMode.Player1)
        {
            aliveImage = ScoreModuleP1Alive;
            notAliveImage = ScoreModuleP1NotAlive;
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            aliveImage = ScoreModuleP2Alive;
            notAliveImage = ScoreModuleP2NotAlive;
        }
        
        if (aliveImage != null && notAliveImage != null)
        {
            if (isAlive)
            {
                // Alive: Alive=1.0 opacity, NotAlive=0.16 opacity
                Color aliveColor = aliveImage.color;
                aliveColor.a = 1.0f;
                aliveImage.color = aliveColor;
                
                Color notAliveColor = notAliveImage.color;
                notAliveColor.a = 0.16f;
                notAliveImage.color = notAliveColor;
            }
            else
            {
                // Not Alive: Alive=0.16 opacity, NotAlive=1.0 opacity
                Color aliveColor = aliveImage.color;
                aliveColor.a = 0.16f;
                aliveImage.color = aliveColor;
                
                Color notAliveColor = notAliveImage.color;
                notAliveColor.a = 1.0f;
                notAliveImage.color = notAliveColor;
            }
        }
    }
    
    void SpawnPlayerParticleEffect(Player.PlayerMode playerMode, Vector3 position)
    {
        GameObject particlePrefab = null;
        
        if (playerMode == Player.PlayerMode.Player1)
        {
            particlePrefab = player1SpawnParticlePrefab;
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            particlePrefab = player2SpawnParticlePrefab;
        }
        
        if (particlePrefab != null)
        {
            GameObject particleEffect = Instantiate(particlePrefab, position, Quaternion.identity);
            
            // Play the particle system if it has one
            ParticleSystem ps = particleEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
            
            // Destroy the particle effect after 2 seconds
            Destroy(particleEffect, 2f);
        }
    }
    
    void StartGameEndingSequence()
    {
        if (gameEnded) return; // Prevent multiple calls
        
        gameEnded = true;
        Debug.Log("Game ending sequence started!");
        
        // Start the death and transition sequence
        StartCoroutine(DeathAndTransitionSequence());
    }
    
    IEnumerator DeathAndTransitionSequence()
    {
        // Kill all players and clones first
        KillAllPlayersAndClones();
        
        // Wait a moment for deaths to process
        yield return new WaitForSeconds(1f);
        
        // Show static transition
        if (StaticNoise != null)
        {
            StaticNoise.SetActive(true);
        }
        
        // Wait for static transition
        yield return new WaitForSeconds(staticNoiseDuration);
        
        // Hide static
        if (StaticNoise != null)
        {
            StaticNoise.SetActive(false);
        }
        
        // Now enter win mode
        isWinMode = true;
        
        // Exit any current modes
        if (isPickingMode) ExitPickingMode();
        if (isBuildingMode) ToggleBuildingMode();
        
        // Disable all game UI and cursors
        if (player1Cursor != null) player1Cursor.DisableCursor();
        if (player2Cursor != null) player2Cursor.DisableCursor();
        
        // Start the three-phase ending sequence
        StartCoroutine(GameEndingSequence());
    }
    
    void KillAllPlayersAndClones()
    {
        // Kill Player 1
        if (player1 != null && !player1.hasBeenHitStopped)
        {
            player1.hasBeenHitStopped = true;
            // Trigger hitstop effect for Player 1
            if (hitstopManager != null)
            {
                GameObject deathPrefab = player1SpawnParticlePrefab; // Use spawn particle as death effect
                hitstopManager.TriggerHitstop(player1, deathPrefab, false);
            }
        }
        
        // Kill Player 2
        if (player2 != null && !player2.hasBeenHitStopped)
        {
            player2.hasBeenHitStopped = true;
            // Trigger hitstop effect for Player 2
            if (hitstopManager != null)
            {
                GameObject deathPrefab = player2SpawnParticlePrefab; // Use spawn particle as death effect
                hitstopManager.TriggerHitstop(player2, deathPrefab, false);
            }
        }
        
        // Kill all Player 1 clones
        if (cloneRecorder != null)
        {
            foreach (Clone clone in cloneRecorder.player1Clones)
            {
                if (clone != null && !clone.hasBeenHitStopped)
                {
                    clone.hasBeenHitStopped = true;
                    // Trigger hitstop effect for clone
                    if (hitstopManager != null)
                    {
                        GameObject deathPrefab = player1SpawnParticlePrefab;
                        hitstopManager.TriggerCloneHitstop(clone, deathPrefab, false);
                    }
                }
            }
            
            // Kill all Player 2 clones
            foreach (Clone clone in cloneRecorder.player2Clones)
            {
                if (clone != null && !clone.hasBeenHitStopped)
                {
                    clone.hasBeenHitStopped = true;
                    // Trigger hitstop effect for clone
                    if (hitstopManager != null)
                    {
                        GameObject deathPrefab = player2SpawnParticlePrefab;
                        hitstopManager.TriggerCloneHitstop(clone, deathPrefab, false);
                    }
                }
            }
        }
    }
    
    IEnumerator GameEndingSequence()
    {
        // Phase 1: Blocks Placed
        yield return StartCoroutine(Phase1_BlocksPlaced());
        
        // Phase 2: Recorded Deaths
        yield return StartCoroutine(Phase2_RecordedDeaths());
        
        // Phase 3: Winner
        yield return StartCoroutine(Phase3_Winner());
    }
    
    IEnumerator Phase1_BlocksPlaced()
    {
        // Enable first text
        if (blocksPlacedText != null)
        {
            blocksPlacedText.text = "<color=#e0dbcd>"+ $"{totalBlocksPlaced}" + "<color=#cc8781>"+ " BLOCKS " + "<color=#d9b472>" + "PLACED.";
            blocksPlacedText.gameObject.SetActive(true);
        }
        
        // Clear the map by disabling all objects under Object Transform Mommy
        if (objectTransformMommy != null)
        {
            foreach (Transform child in objectTransformMommy)
            {
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
        
        // Get all blocks that were placed and make them rain down
        yield return StartCoroutine(RainDownBlocks());
        
        // Wait a bit before next phase
        yield return new WaitForSeconds(2f);
    }
    
    IEnumerator Phase2_RecordedDeaths()
    {
        // Enable second text
        if (recordedDeathsText != null)
        {
            int totalDeaths = player1Deaths + player2Deaths;
            recordedDeathsText.text = "<color=#e0dbcd>"+ $"{totalDeaths}" + "<color=#3da788>"+ " RECORDED " + "<color=#1b807b>" + "DEATHS.";
            recordedDeathsText.gameObject.SetActive(true);
        }
        
        // Rain down player rigidbody clones
        yield return StartCoroutine(RainDownPlayerClones());
        
        // Wait a bit before next phase
        yield return new WaitForSeconds(2f);
    }
    
    IEnumerator Phase3_Winner()
    {
        // Wait 5 seconds before showing winner text
        yield return new WaitForSeconds(5f);
        
        // Enable third text
        if (winnerText != null)
        {
            winnerText.text = $"1 WINNER...";
            winnerText.gameObject.SetActive(true);
        }
        
        // Wait another 5 seconds before announcing the winner
        yield return new WaitForSeconds(5f);
        
        // Announce the winner through RecordedDeathText
        if (recordedDeathsText != null)
        {
            string winner = player1Points >= 15 ? "PLAYER 1" : "PLAYER 2";
            recordedDeathsText.text = $"WINNER: {winner}";
            recordedDeathsText.gameObject.SetActive(true);
        }
        
        // Disable all other text objects
        if (blocksPlacedText != null)
        {
            blocksPlacedText.gameObject.SetActive(false);
        }
        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(false);
        }
    }
    
    IEnumerator RainDownBlocks()
    {
        // Spawn the rigidbody versions of blocks that were placed during build mode
        for (int i = 0; i < placedBlockPrefabs.Count; i++)
        {
            // Random position above screen
            Vector3 spawnPos = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(8f, 12f),
                0f
            );
            
            // Get the rigidbody prefab from the Block component
            if (placedBlockPrefabs[i] != null)
            {
                Block blockComponent = placedBlockPrefabs[i].GetComponent<Block>();
                if (blockComponent != null && blockComponent.blockPrefabRigidBody != null)
                {
                    // Spawn the rigidbody version of the block
                    GameObject fallingBlock = Instantiate(blockComponent.blockPrefabRigidBody, spawnPos, Quaternion.identity);
                    
                    // Set random rotation
                    fallingBlock.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                }
                else
                {
                    // Fallback: spawn the original block and add rigidbody
                    GameObject fallingBlock = Instantiate(placedBlockPrefabs[i], spawnPos, Quaternion.identity);
                    
                    // Add rigidbody if it doesn't have one
                    Rigidbody2D rb = fallingBlock.GetComponent<Rigidbody2D>();
                    if (rb == null)
                    {
                        rb = fallingBlock.AddComponent<Rigidbody2D>();
                    }
                    
                    // Set random rotation
                    fallingBlock.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                }
            }
            
            // Wait 0.1 seconds between spawns
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    IEnumerator RainDownPlayerClones()
    {
        // Rain down Player 1 clones
        for (int i = 0; i < player1Deaths; i++)
        {
            if (player1RigidbodyClone != null)
            {
                Vector3 spawnPos = new Vector3(
                    Random.Range(-5f, 5f),
                    Random.Range(8f, 12f),
                    0f
                );
                
                GameObject clone = Instantiate(player1RigidbodyClone, spawnPos, Quaternion.identity);
                clone.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Rain down Player 2 clones
        for (int i = 0; i < player2Deaths; i++)
        {
            if (player2RigidbodyClone != null)
            {
                Vector3 spawnPos = new Vector3(
                    Random.Range(-5f, 5f),
                    Random.Range(8f, 12f),
                    0f
                );
                
                GameObject clone = Instantiate(player2RigidbodyClone, spawnPos, Quaternion.identity);
                clone.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void UpdateOccupiedPositions()
    {
        if (!showOccupiedPositions) return;
        
        System.Collections.Generic.List<Vector2> allPositions = new System.Collections.Generic.List<Vector2>();
        
        if (objectTransformMommy != null)
        {
            // Get all Block components under Object Transform Mommy
            Block[] placedBlocks = objectTransformMommy.GetComponentsInChildren<Block>();
            
            foreach (Block block in placedBlocks)
            {
                if (block == null) continue;
                
                // Get the block's anchor position
                Vector2 anchor = new Vector2(block.transform.position.x, block.transform.position.y);
                
                // Special handling for objects with "fanMod" tag - use OGPos from Fan component
                if (block.CompareTag("fanMod"))
                {
                    Fan fan = block.GetComponent<Fan>();
                    if (fan != null)
                    {
                        anchor = fan.OGPos;
                    }
                }
                
                // Calculate all positions this block occupies based on its size
                System.Collections.Generic.List<Vector2> occupiedPositions = GetOccupiedPositions(anchor, block.size);
                allPositions.AddRange(occupiedPositions);
            }
        }
        
        // Update the serialized fields for inspector display
        allOccupiedPositions = allPositions.ToArray();
        
        // Create a readable string representation
        occupiedPositionsString = $"Total Occupied Positions: {allPositions.Count}\n\n";
        for (int i = 0; i < allPositions.Count; i++)
        {
            occupiedPositionsString += $"  {i + 1}: ({allPositions[i].x:F2}, {allPositions[i].y:F2})\n";
        }
    }
}
