using UnityEngine;
using System.Collections;

public class HealthBarShake : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    
    public float shakeDuration = 0.2f; // Ne kadar sürsün?
    public float shakeMagnitude = 5.0f; // Ne kadar titresin?

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    public void TriggerShake()
    {
        StopAllCoroutines(); // Eğer zaten titriyorsa baştan başla
        StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            rectTransform.anchoredPosition = originalPosition + new Vector2(x, y);

            elapsed += Time.deltaTime;
            yield return null; 
        }

      
        rectTransform.anchoredPosition = originalPosition;
    }
}