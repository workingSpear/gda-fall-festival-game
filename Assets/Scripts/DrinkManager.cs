using UnityEngine;
using System.Collections.Generic;

public class DrinkManager : MonoBehaviour
{
    [Header("Drink Effects")]
    [Tooltip("Layer name for Clone layer")]
    public string cloneLayerName = "Clone";
    
    // Track players affected by drinks
    private Dictionary<Player, int> affectedPlayers = new Dictionary<Player, int>();
    
    // Track disabled drinkable blocks
    private List<Drinkable> disabledDrinkables = new List<Drinkable>();
    
    // Store original values for reset
    private Dictionary<Player, Vector3> originalScales = new Dictionary<Player, Vector3>();
    private Dictionary<Player, float> originalMoveSpeeds = new Dictionary<Player, float>();
    private Dictionary<Player, float> originalJumpForces = new Dictionary<Player, float>();
    
    public void ApplyDrinkEffect(int drinkType, Player player, Drinkable drinkableBlock)
    {
        if (player == null || drinkableBlock == null) return;
        
        // Store the drink effect for this player
        affectedPlayers[player] = drinkType;
        
        // Disable the drinkable block so it can't be used again this round
        DisableDrinkableBlock(drinkableBlock);
        
        switch (drinkType)
        {
            case 0:
                ApplyCloneEffect(player);
                break;
            case 1:
                ApplyInvertedControlsEffect(player);
                break;
            case 2:
                ApplyShrinkSpeedJumpEffect(player);
                break;
            default:
                Debug.LogWarning($"Unknown drink type: {drinkType}");
                break;
        }
    }
    
    void DisableDrinkableBlock(Drinkable drinkableBlock)
    {
        if (drinkableBlock == null) return;
        
        // Add to disabled list if not already there
        if (!disabledDrinkables.Contains(drinkableBlock))
        {
            disabledDrinkables.Add(drinkableBlock);
        }
        
        // Disable the block's collider so it can't be triggered again
        Collider2D collider = drinkableBlock.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Make the block invisible (optional visual feedback)
        SpriteRenderer spriteRenderer = drinkableBlock.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }
    
    void ApplyCloneEffect(Player player)
    {
        if (player == null) return;
        
        // Change player's layer to "Clone"
        int cloneLayer = LayerMask.NameToLayer(cloneLayerName);
        if (cloneLayer != -1)
        {
            player.gameObject.layer = cloneLayer;
        }
        else
        {
            Debug.LogWarning($"Layer '{cloneLayerName}' not found!");
        }
    }
    
    void ApplyInvertedControlsEffect(Player player)
    {
        if (player == null) return;
        
        // Set inverted controls flag on player
        player.hasInvertedControls = true;
        
        // Apply inverted controls to all clones of this player
        ApplyInvertedControlsToClones(player.playerMode);
    }
    
    void ApplyInvertedControlsToClones(Player.PlayerMode playerMode)
    {
        // Find CloneRecorder to access clones
        CloneRecorder cloneRecorder = FindFirstObjectByType<CloneRecorder>();
        if (cloneRecorder == null) return;
        
        if (playerMode == Player.PlayerMode.Player1)
        {
            // Apply to all Player 1 clones
            foreach (Clone clone in cloneRecorder.player1Clones)
            {
                if (clone != null)
                {
                    clone.hasInvertedControls = true;
                }
            }
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            // Apply to all Player 2 clones
            foreach (Clone clone in cloneRecorder.player2Clones)
            {
                if (clone != null)
                {
                    clone.hasInvertedControls = true;
                }
            }
        }
    }
    
    void ApplyShrinkSpeedJumpEffect(Player player)
    {
        if (player == null) return;
        
        // Apply to the player
        ApplyShrinkSpeedJumpToPlayer(player);
        
        // Apply to all clones of the same player
        Player.PlayerMode playerMode = player.playerMode;
        
        // Find CloneRecorder to access clones
        CloneRecorder cloneRecorder = FindFirstObjectByType<CloneRecorder>();
        if (cloneRecorder != null)
        {
            if (playerMode == Player.PlayerMode.Player1)
            {
                // Apply to all Player 1 clones
                foreach (Clone clone in cloneRecorder.player1Clones)
                {
                    if (clone != null)
                    {
                        ApplyShrinkSpeedJumpToClone(clone);
                    }
                }
            }
            else if (playerMode == Player.PlayerMode.Player2)
            {
                // Apply to all Player 2 clones
                foreach (Clone clone in cloneRecorder.player2Clones)
                {
                    if (clone != null)
                    {
                        ApplyShrinkSpeedJumpToClone(clone);
                    }
                }
            }
        }
    }
    
    void ApplyShrinkSpeedJumpToPlayer(Player player)
    {
        if (player == null) return;
        
        // Store original values if not already stored
        if (!originalScales.ContainsKey(player))
        {
            originalScales[player] = player.transform.localScale;
            originalMoveSpeeds[player] = player.moveSpeed;
            originalJumpForces[player] = player.jumpForce;
        }
        
        // Shrink the player
        player.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        
        // Increase movement speed
        player.moveSpeed *= 1.5f;
        
        // Increase jump force
        player.jumpForce *= 1.3f;
    }
    
    void ApplyShrinkSpeedJumpToClone(Clone clone)
    {
        if (clone == null) return;
        
        // Shrink the clone
        clone.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        
        // Note: Clones might not have moveSpeed and jumpForce variables
        // We'll need to check Clone.cs to see what variables are available
    }
    
    public void ResetPlayerEffects(Player player)
    {
        if (player == null) return;
        
        // Remove player from affected players list
        if (affectedPlayers.ContainsKey(player))
        {
            affectedPlayers.Remove(player);
        }
        
        // Reset player layer to default (Player)
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            player.gameObject.layer = playerLayer;
        }
        
        // Reset inverted controls
        player.hasInvertedControls = false;
        
        // Reset shrink/speed/jump effects
        ResetShrinkSpeedJumpEffects(player);
        
        // Reset inverted controls for all clones of this player
        ResetInvertedControlsForClones(player.playerMode);
    }
    
    void ResetShrinkSpeedJumpEffects(Player player)
    {
        if (player == null) return;
        
        // Reset player values to original
        if (originalScales.ContainsKey(player))
        {
            player.transform.localScale = originalScales[player];
            originalScales.Remove(player);
        }
        else
        {
            player.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        }
        
        if (originalMoveSpeeds.ContainsKey(player))
        {
            player.moveSpeed = originalMoveSpeeds[player];
            originalMoveSpeeds.Remove(player);
        }
        
        if (originalJumpForces.ContainsKey(player))
        {
            player.jumpForce = originalJumpForces[player];
            originalJumpForces.Remove(player);
        }
        
        // Reset all clones of this player
        Player.PlayerMode playerMode = player.playerMode;
        
        // Find CloneRecorder to access clones
        CloneRecorder cloneRecorder = FindFirstObjectByType<CloneRecorder>();
        if (cloneRecorder != null)
        {
            if (playerMode == Player.PlayerMode.Player1)
            {
                // Reset all Player 1 clones
                foreach (Clone clone in cloneRecorder.player1Clones)
                {
                    if (clone != null)
                    {
                        ResetShrinkSpeedJumpForClone(clone);
                    }
                }
            }
            else if (playerMode == Player.PlayerMode.Player2)
            {
                // Reset all Player 2 clones
                foreach (Clone clone in cloneRecorder.player2Clones)
                {
                    if (clone != null)
                    {
                        ResetShrinkSpeedJumpForClone(clone);
                    }
                }
            }
        }
    }
    
    void ResetShrinkSpeedJumpForClone(Clone clone)
    {
        if (clone == null) return;
        
        // Reset clone scale to original size
        clone.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
    }
    
    void ResetInvertedControlsForClones(Player.PlayerMode playerMode)
    {
        // Find CloneRecorder to access clones
        CloneRecorder cloneRecorder = FindFirstObjectByType<CloneRecorder>();
        if (cloneRecorder == null) return;
        
        if (playerMode == Player.PlayerMode.Player1)
        {
            // Reset for all Player 1 clones
            foreach (Clone clone in cloneRecorder.player1Clones)
            {
                if (clone != null)
                {
                    clone.hasInvertedControls = false;
                }
            }
        }
        else if (playerMode == Player.PlayerMode.Player2)
        {
            // Reset for all Player 2 clones
            foreach (Clone clone in cloneRecorder.player2Clones)
            {
                if (clone != null)
                {
                    clone.hasInvertedControls = false;
                }
            }
        }
    }
    
    public void ResetAllPlayerEffects()
    {
        // Reset all affected players
        List<Player> playersToReset = new List<Player>(affectedPlayers.Keys);
        foreach (Player player in playersToReset)
        {
            ResetPlayerEffects(player);
        }
        affectedPlayers.Clear();
    }
    
    public void ReEnableAllDrinkables()
    {
        // Re-enable all disabled drinkable blocks
        foreach (Drinkable drinkable in disabledDrinkables)
        {
            if (drinkable != null)
            {
                ReEnableDrinkableBlock(drinkable);
            }
        }
        disabledDrinkables.Clear();
    }
    
    void ReEnableDrinkableBlock(Drinkable drinkableBlock)
    {
        if (drinkableBlock == null) return;
        
        // Re-enable the block's collider
        Collider2D collider = drinkableBlock.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        
        // Make the block visible again
        SpriteRenderer spriteRenderer = drinkableBlock.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }
}
