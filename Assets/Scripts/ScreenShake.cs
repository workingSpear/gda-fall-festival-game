using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        originalPosition = transform.localPosition;
    }

    public void Shake(float duration, float intensity)
    {
        // Stop any existing shake
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, intensity));
    }

    IEnumerator ShakeCoroutine(float duration, float intensity)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Calculate shake amount with falloff over time
            float remainingTime = 1f - (elapsed / duration);
            float currentIntensity = intensity * remainingTime;
            
            // Generate random offset in 2D
            float xOffset = Random.Range(-1f, 1f) * currentIntensity;
            float yOffset = Random.Range(-1f, 1f) * currentIntensity;
            
            // Apply shake
            transform.localPosition = originalPosition + new Vector3(xOffset, yOffset, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset to original position
        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }
}
