using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Audio source for platformer music (Balkan song)")]
    public AudioSource platformerAudioSource;
    
    [Tooltip("Audio source for chill music (Chill song)")]
    public AudioSource chillAudioSource;
    
    [Tooltip("Audio source for sound effects")]
    public AudioSource sfxAudioSource;
    
    [Header("Sound Effect Clips")]
    [Tooltip("Static noise sound for transitions")]
    public AudioClip staticNoiseClip;
    [Tooltip("Buzzer sound for player spawn")]
    public AudioClip playerSpawnBuzzerClip;
    [Tooltip("Buzzer sound for timer running out")]
    public AudioClip timerBuzzerClip;
    
    [Header("Volume Settings")]
    [Tooltip("Volume for sound effects (0.0 to 1.0)")]
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;
    
    [Header("Crossfade Settings")]
    [Tooltip("Duration of crossfade transitions (should match StaticNoise duration)")]
    public float crossfadeDuration = 2f;
    
    private Coroutine crossfadeCoroutine;
    private Coroutine staticNoiseCoroutine;
    
    public void StartStaticNoiseTransition(bool isPlatformerMode, bool isFromChillMode = false)
    {
        // Stop any existing crossfade
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }
        
        // Only crossfade if transitioning to/from platformer mode
        // Don't crossfade between picking and building mode (both use chill music)
        if (isPlatformerMode && isFromChillMode)
        {
            // Transitioning FROM chill TO platformer - crossfade
            CrossfadeToPlatformer();
        }
        else if (!isPlatformerMode && !isFromChillMode)
        {
            // Transitioning FROM platformer TO chill - crossfade
            CrossfadeToChill();
        }
        // If both isPlatformerMode and isFromChillMode are false, or both are true,
        // then we're transitioning between picking/building modes - no crossfade needed
        
        // Start static noise sound
        if (sfxAudioSource != null && staticNoiseClip != null)
        {
            sfxAudioSource.clip = staticNoiseClip;
            sfxAudioSource.loop = true; // Loop the static noise during transition
            sfxAudioSource.volume = sfxVolume; // Use SFX volume control
            sfxAudioSource.Play();
        }
        
    }
    
    public void EndStaticNoiseTransition()
    {
        // Stop static noise immediately
        if (sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
        }
    }
    
    public void CrossfadeToPlatformer()
    {
        // Stop any existing crossfade
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }
        
        // Start crossfade to platformer music
        crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(chillAudioSource, platformerAudioSource));
    }
    
    public void CrossfadeToChill()
    {
        // Stop any existing crossfade
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }
        
        // Start crossfade to chill music
        crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(platformerAudioSource, chillAudioSource));
    }
    
    IEnumerator CrossfadeCoroutine(AudioSource fromSource, AudioSource toSource)
    {
        // Ensure the target source is playing
        if (toSource != null && !toSource.isPlaying)
        {
            toSource.volume = 0f;
            toSource.Play();
        }
        
        float elapsedTime = 0f;
        
        while (elapsedTime < crossfadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / crossfadeDuration;
            
            // Fade out the source we're transitioning from
            if (fromSource != null)
            {
                fromSource.volume = Mathf.Lerp(1f, 0f, t);
            }
            
            // Fade in the source we're transitioning to
            if (toSource != null)
            {
                toSource.volume = Mathf.Lerp(0f, 1f, t);
            }
            
            yield return null;
        }
        
        // Ensure final volumes are set correctly
        if (fromSource != null)
        {
            fromSource.volume = 0f;
            fromSource.Stop();
        }
        
        if (toSource != null)
        {
            toSource.volume = 1f;
        }
        
        crossfadeCoroutine = null;
    }
    
    // Public method to manually crossfade to platformer music (for testing or other use cases)
    public void CrossfadeToPlatformerManually()
    {
        CrossfadeToPlatformer();
    }
    
    // Public method to manually crossfade to chill music (for testing or other use cases)
    public void CrossfadeToChillManually()
    {
        CrossfadeToChill();
    }
    
    // Public method to stop all audio
    public void StopAllAudio()
    {
        if (platformerAudioSource != null)
        {
            platformerAudioSource.Stop();
        }
        
        if (chillAudioSource != null)
        {
            chillAudioSource.Stop();
        }
        
        if (sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
        }
    }
    
    // Public method to play any sound effect
    public void PlaySoundEffect(AudioClip clip, bool loop = false, float volumeMultiplier = 1f)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.clip = clip;
            sfxAudioSource.loop = loop;
            sfxAudioSource.volume = sfxVolume * volumeMultiplier;
            sfxAudioSource.Play();
        }
    }
    
    // Public method to stop sound effects
    public void StopSoundEffects()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
        }
    }
    
    // Public method to play player spawn buzzer
    public void PlayPlayerSpawnBuzzer()
    {
        PlaySoundEffect(playerSpawnBuzzerClip, loop: false, volumeMultiplier: 1f);
    }
    
    // Public method to play timer buzzer
    public void PlayTimerBuzzer()
    {
        PlaySoundEffect(timerBuzzerClip, loop: false, volumeMultiplier: 1f);
    }
}