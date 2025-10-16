using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HitstopManager : MonoBehaviour
{
    [Header("Hitstop Settings")]
    public SpriteRenderer hitStop;
    [Tooltip("Duration of hitstop effect when hitting a hazard")]
    public float hitStopDuration = 0.15f;
    [Tooltip("Buffer time after hitstop to prevent flickering on repeated hits")]
    public float hitStopBuffer = 0.3f;
    [Tooltip("Delay before applying next hitstop during buffer period")]
    public float hitStopQueueDelay = 0.05f;

    [Header("Death Settings")]
    public ScreenShake screenShake;
    [Tooltip("Duration of screen shake on death")]
    public float deathShakeDuration = 0.5f;
    [Tooltip("Intensity of screen shake on death")]
    public float deathShakeIntensity = 0.3f;
    [Tooltip("Time before death object is destroyed")]
    public float deathObjectLifetime = 5f;
    
    [Header("Player Death Shake Settings")]
    [Tooltip("Separate ScreenShake component for player death effects")]
    public ScreenShake playerDeathScreenShake;
    [Tooltip("Multiplier for player death shake intensity (applied to base deathShakeIntensity)")]
    [SerializeField] private float playerDeathShakeMultiplier = 2f;
    [Tooltip("Duration of each individual shake burst during continuous player death shake")]
    [SerializeField] private float playerDeathShakeBurstDuration = 0.1f;
    [Tooltip("Interval between shake bursts during continuous player death shake")]
    [SerializeField] private float playerDeathShakeBurstInterval = 0.05f;

    [Header("End Settings")]
    [Tooltip("Particle system prefab to spawn when reaching the end")]
    public GameObject endParticlePrefab;
    
    [Header("Sprite Cycling Settings")]
    [Tooltip("Array of SpriteRenderers to cycle through after each hitstop")]
    public SpriteRenderer[] cyclingSprites;
    [Tooltip("Color to tint sprites when Player 1 triggers hitstop")]
    public Color player1TintColor = Color.red;
    [Tooltip("Color to tint sprites when Player 2 triggers hitstop")]
    public Color player2TintColor = Color.blue;

    private int activeHitstops = 0; // Track how many hitstops are active
    private List<EffectInfo> pendingEffects = new List<EffectInfo>();
    private Coroutine deathSequenceCoroutine;
    private bool isCoordinatingDeaths = false; // Prevents multiple coordinators
    private float sharedBufferEndTime = 0f; // Shared buffer end time for all objects
    private float lastHitstopStartTime = 0f; // Track when last hitstop started
    private bool isInBuffer = false; // Track if we're in buffer period
    private bool hasPlayerDied = false; // Track if a player (not clone) has died
    private Coroutine continuousShakeCoroutine; // Track continuous shake coroutine
    private int currentSpriteIndex = 0; // Track which sprite to enable next
    private Coroutine disableSpritesCoroutine; // Track the disable sprites coroutine
    private Color[] originalSpriteColors; // Store original colors of cycling sprites

    private class EffectInfo
    {
        public GameObject prefab;
        public Vector3 position;
        public MonoBehaviour objectToDisable; // Player or Clone component
        public SpriteRenderer spriteRenderer;
        public Rigidbody2D rb;
        public BoxCollider2D boxCollider;
        public BoxCollider2D jumpableHead;
        public Color originalColor;
        public int originalSortingOrder;
        public bool isEndTrigger; // True if this is an "end" trigger, false if "hazard"
    }

    public void TriggerHitstop(Player player, GameObject deathPrefab, bool isEnd = false)
    {
        StartCoroutine(HitStopEffect(player, deathPrefab, isEnd));
    }

    public void TriggerCloneHitstop(Clone clone, GameObject deathPrefab, bool isEnd = false)
    {
        StartCoroutine(CloneHitStopEffect(clone, deathPrefab, isEnd));
    }

    void ExtendSharedBuffer()
    {
        // Extend or start the shared buffer period
        sharedBufferEndTime = Time.realtimeSinceStartup + hitStopBuffer;
    }

    IEnumerator HitStopEffect(Player player, GameObject deathPrefab, bool isEnd)
    {
        // If we're in buffer period, add a queue delay before starting
        if (isInBuffer)
        {
            float delayUntil = lastHitstopStartTime + hitStopQueueDelay;
            while (Time.realtimeSinceStartup < delayUntil)
            {
                yield return null;
            }
        }
        
        // Mark when this hitstop starts
        lastHitstopStartTime = Time.realtimeSinceStartup;
        isInBuffer = true;
        
        // Get components from player
        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        BoxCollider2D boxCollider = player.GetComponent<BoxCollider2D>();
        
        // Store original values
        Color originalColor = Color.white;
        int originalSortingOrder = 0;
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalSortingOrder = spriteRenderer.sortingOrder;
            
            // Set sprite to black and change sorting order
            spriteRenderer.color = Color.black;
            spriteRenderer.sortingOrder = 50;
        }
        
        // Enable hitStop sprite with color based on trigger type and player mode
        if (hitStop != null)
        {
            hitStop.enabled = true;
            Color hitStopColor;
            
            if (isEnd)
            {
                hitStopColor = Color.white; // White for end trigger
            }
            else
            {
                hitStopColor = player.playerMode == Player.PlayerMode.Player1 ? Color.red : Color.blue;
            }
            
            // Make player death more obvious with higher opacity and pulsing effect
            hitStopColor.a = 200f / 255f; // Much higher opacity for player deaths (200/255 instead of 33/255)
            hitStop.color = hitStopColor;
            
            // Start pulsing effect for player deaths
            if (!isEnd)
            {
                StartCoroutine(PulseHitStopForPlayer());
            }
        }
        
        // Disable gravity and stop movement
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
        
        // Disable hitboxes
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
        
        if (player.jumpableHead != null)
        {
            player.jumpableHead.enabled = false;
        }
        
        // Cycle through SpriteRenderers before time freeze
        CycleSprites(player.playerMode);
        
        // Track active hitstops and freeze time
        activeHitstops++;
        Time.timeScale = 0f;
        
        // Local timing for this hitstop
        float hitStopEndTime = Time.realtimeSinceStartup + hitStopDuration;
        
        // Wait for hitstop duration (using realtime since time is frozen)
        while (Time.realtimeSinceStartup < hitStopEndTime)
        {
            yield return null;
        }
        
        // This hitstop is done, decrement counter
        activeHitstops--;
        
        // Only resume time if no other hitstops are active
        if (activeHitstops <= 0)
        {
            activeHitstops = 0; // Safety clamp
            Time.timeScale = 1f;
        }
        
        // Extend the shared buffer (if this is first object, starts buffer; otherwise extends it)
        ExtendSharedBuffer();
        
        // Mark that a player has died and start continuous shake during buffer (after buffer is set)
        hasPlayerDied = true;
        if (playerDeathScreenShake != null && continuousShakeCoroutine == null && sharedBufferEndTime > Time.realtimeSinceStartup)
        {
            continuousShakeCoroutine = StartCoroutine(ContinuousShakeDuringBuffer());
        }
        
        // Wait for the SHARED buffer to end (not a local buffer)
        while (Time.realtimeSinceStartup < sharedBufferEndTime)
        {
            yield return null;
        }
        
        // Shared buffer period is now over - add to effect list for cleanup
        
        // Add effect info to pending list with all cleanup info
        pendingEffects.Add(new EffectInfo 
        { 
            prefab = deathPrefab, 
            position = player.transform.position,
            objectToDisable = player,
            spriteRenderer = spriteRenderer,
            rb = rb,
            boxCollider = boxCollider,
            jumpableHead = player.jumpableHead,
            originalColor = originalColor,
            originalSortingOrder = originalSortingOrder,
            isEndTrigger = isEnd
        });
        
        // Start effect sequence coordinator if not already running
        if (!isCoordinatingDeaths)
        {
            isCoordinatingDeaths = true;
            deathSequenceCoroutine = StartCoroutine(DeathSequenceCoordinator());
        }
        
        // DO NOT disable or cleanup here - coordinator will do it
    }

    IEnumerator CloneHitStopEffect(Clone clone, GameObject deathPrefab, bool isEnd)
    {
        // If we're in buffer period, add a queue delay before starting
        if (isInBuffer)
        {
            float delayUntil = lastHitstopStartTime + hitStopQueueDelay;
            while (Time.realtimeSinceStartup < delayUntil)
            {
                yield return null;
            }
        }
        
        // Mark when this hitstop starts
        lastHitstopStartTime = Time.realtimeSinceStartup;
        isInBuffer = true;
        
        // Get components from clone
        SpriteRenderer spriteRenderer = clone.spriteRenderer;
        Rigidbody2D rb = clone.GetComponent<Rigidbody2D>();
        BoxCollider2D boxCollider = clone.GetComponent<BoxCollider2D>();
        
        // Store original values
        Color originalColor = Color.white;
        int originalSortingOrder = 0;
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalSortingOrder = spriteRenderer.sortingOrder;
            
            // Set sprite to black and change sorting order
            spriteRenderer.color = Color.black;
            spriteRenderer.sortingOrder = 50;
        }
        
        // Enable hitStop sprite with color based on trigger type and player mode
        if (hitStop != null)
        {
            hitStop.enabled = true;
            Color hitStopColor;
            
            if (isEnd)
            {
                hitStopColor = Color.white; // White for end trigger
            }
            else
            {
                hitStopColor = clone.playerMode == Player.PlayerMode.Player1 ? Color.red : Color.blue;
            }
            
            hitStopColor.a = 33f / 255f; // Set opacity to 33/255
            hitStop.color = hitStopColor;
        }
        
        // Disable gravity and stop movement
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
        
        // Disable hitboxes
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
        
        if (clone.jumpableHead != null)
        {
            clone.jumpableHead.enabled = false;
        }
        
        // Cycle through SpriteRenderers before time freeze
        CycleSprites(clone.playerMode);
        
        // Track active hitstops and freeze time
        activeHitstops++;
        Time.timeScale = 0f;
        
        // Local timing for this hitstop
        float hitStopEndTime = Time.realtimeSinceStartup + hitStopDuration;
        
        // Wait for hitstop duration (using realtime since time is frozen)
        while (Time.realtimeSinceStartup < hitStopEndTime)
        {
            yield return null;
        }
        
        // This hitstop is done, decrement counter
        activeHitstops--;
        
        // Only resume time if no other hitstops are active
        if (activeHitstops <= 0)
        {
            activeHitstops = 0; // Safety clamp
            Time.timeScale = 1f;
        }
        
        // Extend the shared buffer (if this is first object, starts buffer; otherwise extends it)
        ExtendSharedBuffer();
        
        // Wait for the SHARED buffer to end (not a local buffer)
        while (Time.realtimeSinceStartup < sharedBufferEndTime)
        {
            yield return null;
        }
        
        // Shared buffer period is now over - add to effect list for cleanup
        
        // Add effect info to pending list with all cleanup info
        pendingEffects.Add(new EffectInfo 
        { 
            prefab = deathPrefab, 
            position = clone.transform.position,
            objectToDisable = clone,
            spriteRenderer = spriteRenderer,
            rb = rb,
            boxCollider = boxCollider,
            jumpableHead = clone.jumpableHead,
            originalColor = originalColor,
            originalSortingOrder = originalSortingOrder,
            isEndTrigger = isEnd
        });
        
        // Start effect sequence coordinator if not already running
        if (!isCoordinatingDeaths)
        {
            isCoordinatingDeaths = true;
            deathSequenceCoroutine = StartCoroutine(DeathSequenceCoordinator());
        }
        
        // DO NOT disable or cleanup here - coordinator will do it
    }

    IEnumerator DeathSequenceCoordinator()
    {
        // Wait for the shared buffer to end (all objects share the same buffer end time)
        while (Time.realtimeSinceStartup < sharedBufferEndTime)
        {
            yield return null;
        }
        
        // Give one extra frame for all coroutines to add their effect info
        yield return null;
        
        // If no effects were collected, exit
        if (pendingEffects.Count == 0)
        {
            isCoordinatingDeaths = false;
            deathSequenceCoroutine = null;
            yield break;
        }
        
        // Count how many effects occurred
        int effectCount = pendingEffects.Count;
        
        // STEP 1: Cleanup all affected objects RIGHT NOW (restore visuals, disable hitStop sprite, disable objects)
        foreach (EffectInfo effectInfo in pendingEffects)
        {
            // Restore sprite visuals
            if (effectInfo.spriteRenderer != null)
            {
                effectInfo.spriteRenderer.color = effectInfo.originalColor;
                effectInfo.spriteRenderer.sortingOrder = effectInfo.originalSortingOrder;
                effectInfo.spriteRenderer.enabled = false; // Hide sprite
            }
            
            // Disable the object
            if (effectInfo.objectToDisable != null)
            {
                effectInfo.objectToDisable.enabled = false;
            }
            
            // Disable physics
            if (effectInfo.rb != null)
            {
                effectInfo.rb.bodyType = RigidbodyType2D.Kinematic;
            }
            
            // Disable colliders
            if (effectInfo.boxCollider != null)
            {
                effectInfo.boxCollider.enabled = false;
            }
            
            if (effectInfo.jumpableHead != null)
            {
                effectInfo.jumpableHead.enabled = false;
            }
        }
        
        // Disable hitStop sprite (shared across all)
        if (hitStop != null)
        {
            hitStop.enabled = false;
        }
        
        // Stop any existing disable sprites coroutine and start a new one
        if (disableSpritesCoroutine != null)
        {
            StopCoroutine(disableSpritesCoroutine);
        }
        disableSpritesCoroutine = StartCoroutine(DisableSpritesAfterBuffer());
        
        // STEP 2: Spawn all effect prefabs and collect their particle systems
        List<ParticleSystem> particleSystems = new List<ParticleSystem>();
        
        foreach (EffectInfo effectInfo in pendingEffects)
        {
            // Choose the correct prefab based on trigger type
            GameObject prefabToSpawn = effectInfo.isEndTrigger ? endParticlePrefab : effectInfo.prefab;
            
            if (prefabToSpawn != null)
            {
                GameObject effectObject = Instantiate(prefabToSpawn, effectInfo.position, Quaternion.identity);
                
                // Get particle system from the effect object
                ParticleSystem ps = effectObject.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    particleSystems.Add(ps);
                    // Stop it initially to prevent auto-play
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                
                // Schedule destruction
                Destroy(effectObject, deathObjectLifetime);
            }
        }
        
        // STEP 3: Play all particle systems simultaneously in the same frame
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Play(true);
        }
        
        // STEP 4: Trigger screen shake ONCE based on number of effects (square root scaling)
        if (screenShake != null && effectCount > 0)
        {
            float scaledDuration = deathShakeDuration * Mathf.Sqrt(effectCount);
            float scaledIntensity = deathShakeIntensity * Mathf.Sqrt(effectCount);
            screenShake.Shake(scaledDuration, scaledIntensity);
        }
        
        // Stop continuous shake if it was running
        if (continuousShakeCoroutine != null)
        {
            StopCoroutine(continuousShakeCoroutine);
            continuousShakeCoroutine = null;
        }
        
        // Clear pending effects and reset flags
        pendingEffects.Clear();
        isCoordinatingDeaths = false;
        sharedBufferEndTime = 0f;
        lastHitstopStartTime = 0f;
        isInBuffer = false;
        hasPlayerDied = false;
        deathSequenceCoroutine = null;
    }
    
    IEnumerator ContinuousShakeDuringBuffer()
    {
        // Continuous shake during the buffer period when a player dies
        while (hasPlayerDied && Time.realtimeSinceStartup < sharedBufferEndTime)
        {
            if (playerDeathScreenShake != null)
            {
                // Use configurable intense shake for player deaths
                float shakeIntensity = deathShakeIntensity * playerDeathShakeMultiplier;
                playerDeathScreenShake.Shake(playerDeathShakeBurstDuration, shakeIntensity);
            }
            
            // Wait for configurable interval before next shake (use realtime since time might be frozen)
            yield return new WaitForSecondsRealtime(playerDeathShakeBurstInterval);
        }
        
        continuousShakeCoroutine = null;
    }
    
    IEnumerator PulseHitStopForPlayer()
    {
        // Pulsing effect for player deaths to make them more obvious
        if (hitStop == null) yield break;
        
        Color baseColor = hitStop.color;
        float baseAlpha = baseColor.a;
        
        while (hasPlayerDied && hitStop.enabled)
        {
            // Pulse between base alpha and full opacity
            float pulseAlpha = baseAlpha + (1f - baseAlpha) * Mathf.Sin(Time.realtimeSinceStartup * 10f);
            Color pulseColor = baseColor;
            pulseColor.a = pulseAlpha;
            hitStop.color = pulseColor;
            
            yield return null;
        }
        
        // Reset to base color when done
        if (hitStop != null)
        {
            hitStop.color = baseColor;
        }
    }
    
    void CycleSprites(Player.PlayerMode playerMode)
    {
        Debug.Log($"CycleSprites called. Array length: {(cyclingSprites != null ? cyclingSprites.Length : 0)}, Current index: {currentSpriteIndex}, In buffer: {isInBuffer}, Player: {playerMode}");
        
        // Only proceed if we have a valid array
        if (cyclingSprites == null || cyclingSprites.Length == 0)
        {
            Debug.Log("Cycling sprites array is null or empty");
            return;
        }
        
        // Initialize original colors if not done yet
        if (originalSpriteColors == null || originalSpriteColors.Length != cyclingSprites.Length)
        {
            InitializeOriginalColors();
        }
        
        // If we're in buffer period, just enable the next sprite without disabling others
        if (isInBuffer)
        {
            Debug.Log("In buffer period - enabling next sprite without disabling others");
            if (cyclingSprites[currentSpriteIndex] != null)
            {
                cyclingSprites[currentSpriteIndex].enabled = true;
                ApplyTintToSprite(cyclingSprites[currentSpriteIndex], playerMode);
                Debug.Log($"Enabled additional sprite at index {currentSpriteIndex}: {cyclingSprites[currentSpriteIndex].name}");
            }
        }
        else
        {
            // Not in buffer - disable all and enable only current sprite
            foreach (SpriteRenderer sprite in cyclingSprites)
            {
                if (sprite != null)
                {
                    sprite.enabled = false;
                }
            }
            
            // Enable ONLY the current sprite
            if (cyclingSprites[currentSpriteIndex] != null)
            {
                cyclingSprites[currentSpriteIndex].enabled = true;
                ApplyTintToSprite(cyclingSprites[currentSpriteIndex], playerMode);
                Debug.Log($"Enabled ONLY sprite at index {currentSpriteIndex}: {cyclingSprites[currentSpriteIndex].name}");
            }
        }
        
        // Move to next sprite for next hitstop (cycle back to 0 if at end)
        currentSpriteIndex = (currentSpriteIndex + 1) % cyclingSprites.Length;
    }
    
    IEnumerator DisableSpritesAfterBuffer()
    {
        Debug.Log($"DisableSpritesAfterBuffer started, waiting for buffer to end at {sharedBufferEndTime}");
        
        // Wait for the buffer period to end
        while (Time.realtimeSinceStartup < sharedBufferEndTime)
        {
            yield return null;
        }
        
        Debug.Log("Buffer ended, disabling all cycling sprites and resetting cycle");
        
        // Disable all cycling sprites after buffer ends
        if (cyclingSprites != null)
        {
            foreach (SpriteRenderer sprite in cyclingSprites)
            {
                if (sprite != null)
                {
                    sprite.enabled = false;
                    Debug.Log($"Disabled sprite: {sprite.name}");
                }
            }
        }
        
        // Reset sprite colors to original
        ResetSpriteColors();
        
        // Reset the cycle to the first sprite
        currentSpriteIndex = 0;
        Debug.Log("Reset sprite cycle to index 0");
        
        // Clear the coroutine reference
        disableSpritesCoroutine = null;
    }
    
    void InitializeOriginalColors()
    {
        if (cyclingSprites != null)
        {
            originalSpriteColors = new Color[cyclingSprites.Length];
            for (int i = 0; i < cyclingSprites.Length; i++)
            {
                if (cyclingSprites[i] != null)
                {
                    originalSpriteColors[i] = cyclingSprites[i].color;
                }
            }
            Debug.Log($"Initialized original colors for {cyclingSprites.Length} sprites");
        }
    }
    
    void ApplyTintToSprite(SpriteRenderer sprite, Player.PlayerMode playerMode)
    {
        if (sprite != null)
        {
            Color tintColor = playerMode == Player.PlayerMode.Player1 ? player1TintColor : player2TintColor;
            sprite.color = tintColor;
            Debug.Log($"Applied {tintColor} tint to sprite {sprite.name} for {playerMode}");
        }
    }
    
    void ResetSpriteColors()
    {
        if (cyclingSprites != null && originalSpriteColors != null)
        {
            for (int i = 0; i < cyclingSprites.Length; i++)
            {
                if (cyclingSprites[i] != null && i < originalSpriteColors.Length)
                {
                    cyclingSprites[i].color = originalSpriteColors[i];
                }
            }
            Debug.Log("Reset all sprite colors to original");
        }
    }
}

