using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Audio source for platforming music")]
    public AudioSource platformerAudioSource;
    [Tooltip("Audio source for non-platforming music (picking/building modes)")]
    public AudioSource nonPlatformerAudioSource;
    
    [Header("Crossfade Settings")]
    [Tooltip("Duration of the crossfade in seconds")]
    public float crossfadeDuration = 2f;
    
    private Coroutine currentCrossfade;
    
    public void CrossfadeFromPlatformer()
    {
        // Stop any existing crossfade
        if (currentCrossfade != null)
        {
            StopCoroutine(currentCrossfade);
        }
        
        // Start crossfade from platformer to non-platformer
        currentCrossfade = StartCoroutine(CrossfadeCoroutine(platformerAudioSource, nonPlatformerAudioSource));
    }
    
    public void CrossfadeToPlatformer()
    {
        // Stop any existing crossfade
        if (currentCrossfade != null)
        {
            StopCoroutine(currentCrossfade);
        }
        
        // Start crossfade from non-platformer to platformer
        currentCrossfade = StartCoroutine(CrossfadeCoroutine(nonPlatformerAudioSource, platformerAudioSource));
    }
    
    IEnumerator CrossfadeCoroutine(AudioSource fadeOut, AudioSource fadeIn)
    {
        // Ensure fade-in source is playing
        if (!fadeIn.isPlaying)
        {
            fadeIn.Play();
        }
        
        float elapsed = 0f;
        float startVolumeFadeOut = fadeOut.volume;
        float startVolumeFadeIn = fadeIn.volume;
        
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / crossfadeDuration;
            
            // Fade out the first audio source
            fadeOut.volume = Mathf.Lerp(startVolumeFadeOut, 0f, t);
            
            // Fade in the second audio source
            fadeIn.volume = Mathf.Lerp(startVolumeFadeIn, 1f, t);
            
            yield return null;
        }
        
        // Ensure final volumes are set
        fadeOut.volume = 0f;
        fadeIn.volume = 1f;
        
        // Stop the faded out audio source
        fadeOut.Stop();
        
        currentCrossfade = null;
    }
}
