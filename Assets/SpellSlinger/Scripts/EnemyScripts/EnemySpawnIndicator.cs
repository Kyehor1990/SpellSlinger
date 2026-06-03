using System.Collections;
using System;
using UnityEngine;

public class EnemySpawnIndicator : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    public SpriteRenderer sr;
    [SerializeField] private Animator animator;
    [SerializeField] private Vector3 visualOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private Sprite[] defaultInkDropFrames;
    [SerializeField] private Sprite[] eliteInkDropFrames;
    [SerializeField] private Sprite[] bossInkDropFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 15f;
    [SerializeField, Min(0.01f)] private float animatedIndicatorScale = 1f;
    [SerializeField] private Sprite staticFallbackSprite;
    [SerializeField] private Color staticFallbackColor = Color.white;
    [SerializeField, Min(0.01f)] private float staticFallbackScale = 1f;
    public GameObject smokeParticlePrefab;
    
    private GameObject enemyToSpawn;
    private Action<GameObject> onEnemySpawned;
    private EnemySpawner ownerSpawner;
    private int spawnSessionId;
    private Sprite[] activeInkDropFrames;

    public void SetupIndicator(EnemySpawnData data, float warningTime, Action<GameObject> onSpawned = null, EnemySpawner spawner = null, int sessionId = 0)
    {
        if (data == null || data.enemyPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        enemyToSpawn = data.enemyPrefab;
        onEnemySpawned = onSpawned;
        ownerSpawner = spawner;
        spawnSessionId = sessionId;
        ownerSpawner?.RegisterSpawnIndicator(this);
        activeInkDropFrames = GetInkDropFrames(data.tier);
        ApplyVisualOffset();

        if (sr != null)
        {
            if (HasAnimatedInkDropFrames())
            {
                sr.sprite = activeInkDropFrames[0];
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = staticFallbackSprite != null ? staticFallbackSprite : sr.sprite;
                sr.color = staticFallbackColor;
            }
        }

        Transform targetVisualRoot = GetVisualRoot();
        if (targetVisualRoot != null)
        {
            targetVisualRoot.localScale = Vector3.one * (HasAnimatedInkDropFrames() ? animatedIndicatorScale : staticFallbackScale);
        }

        if (animator != null)
        {
            animator.enabled = animator.runtimeAnimatorController != null && !HasAnimatedInkDropFrames();
        }

        StartCoroutine(SpawnSequence(warningTime));
    }

    private void ApplyVisualOffset()
    {
        Transform targetVisualRoot = GetVisualRoot();
        if (targetVisualRoot != null && targetVisualRoot != transform)
        {
            targetVisualRoot.localPosition = visualOffset;
        }
    }

    private Transform GetVisualRoot()
    {
        if (visualRoot != null)
        {
            return visualRoot;
        }

        return sr != null ? sr.transform : null;
    }

    private IEnumerator SpawnSequence(float delay)
    {
        yield return PlayWarningAnimation(Mathf.Max(0f, delay));

        if (!CanSpawnForCurrentSession())
        {
            Destroy(gameObject);
            yield break;
        }

        if (smokeParticlePrefab != null)
        {
            Instantiate(smokeParticlePrefab, transform.position, Quaternion.identity);
        }

        if (enemyToSpawn != null)
        {
            GameObject spawnedEnemy = Instantiate(enemyToSpawn, transform.position, Quaternion.identity);
            onEnemySpawned?.Invoke(spawnedEnemy);
        }

        Destroy(gameObject);
    }

    private IEnumerator PlayWarningAnimation(float duration)
    {
        if (!HasAnimatedInkDropFrames() || sr == null)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }

            yield break;
        }

        if (duration <= 0f)
        {
            sr.sprite = activeInkDropFrames[activeInkDropFrames.Length - 1];
            yield break;
        }

        float configuredFrameDuration = 1f / Mathf.Max(1f, framesPerSecond);
        float durationFrameTime = duration / activeInkDropFrames.Length;
        float frameDuration = Mathf.Min(configuredFrameDuration, durationFrameTime);
        float elapsed = 0f;
        int frameIndex = 0;

        while (elapsed < duration)
        {
            int nextFrameIndex = Mathf.Min(Mathf.FloorToInt(elapsed / frameDuration), activeInkDropFrames.Length - 1);
            if (nextFrameIndex != frameIndex)
            {
                frameIndex = nextFrameIndex;
                sr.sprite = activeInkDropFrames[frameIndex];
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        sr.sprite = activeInkDropFrames[activeInkDropFrames.Length - 1];
    }

    private Sprite[] GetInkDropFrames(EnemyTier tier)
    {
        switch (tier)
        {
            case EnemyTier.Elite:
                return HasFrames(eliteInkDropFrames) ? eliteInkDropFrames : defaultInkDropFrames;
            case EnemyTier.Boss:
                return HasFrames(bossInkDropFrames) ? bossInkDropFrames : defaultInkDropFrames;
            default:
                return defaultInkDropFrames;
        }
    }

    private bool HasAnimatedInkDropFrames()
    {
        return HasFrames(activeInkDropFrames);
    }

    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool CanSpawnForCurrentSession()
    {
        return ownerSpawner == null || ownerSpawner.IsSpawnSessionActive(spawnSessionId);
    }

    private void OnDestroy()
    {
        ownerSpawner?.UnregisterSpawnIndicator(this);
    }
}
