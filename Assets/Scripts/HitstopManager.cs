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

    [Header("End Settings")]
    [Tooltip("Particle system prefab to spawn when reaching the end")]
    public GameObject endParticlePrefab;

    private int activeHitstops = 0; // Track how many hitstops are active
    private List<EffectInfo> pendingEffects = new List<EffectInfo>();
    private Coroutine deathSequenceCoroutine;
    private bool isCoordinatingDeaths = false; // Prevents multiple coordinators
    private float sharedBufferEndTime = 0f; // Shared buffer end time for all objects
    private float lastHitstopStartTime = 0f; // Track when last hitstop started
    private bool isInBuffer = false; // Track if we're in buffer period

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
        
        if (player.jumpableHead != null)
        {
            player.jumpableHead.enabled = false;
        }
        
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
        
        // Clear pending effects and reset flags
        pendingEffects.Clear();
        isCoordinatingDeaths = false;
        sharedBufferEndTime = 0f;
        lastHitstopStartTime = 0f;
        isInBuffer = false;
        deathSequenceCoroutine = null;
    }
}

