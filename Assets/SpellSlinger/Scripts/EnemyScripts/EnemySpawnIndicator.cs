using System.Collections;
using System;
using UnityEngine;

public class EnemySpawnIndicator : MonoBehaviour
{
    public SpriteRenderer sr;
    public GameObject smokeParticlePrefab;
    
    private GameObject enemyToSpawn;
    private Action<GameObject> onEnemySpawned;
    private EnemySpawner ownerSpawner;
    private int spawnSessionId;

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

        if (sr != null)
        {
            sr.sprite = data.inkDropSprite;
            sr.color = data.dropColor;
        }

        transform.localScale = Vector3.one * data.dropScale;

        StartCoroutine(SpawnSequence(warningTime));
    }

    private IEnumerator SpawnSequence(float delay)
    {
        yield return new WaitForSeconds(delay);

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

    private bool CanSpawnForCurrentSession()
    {
        return ownerSpawner == null || ownerSpawner.IsSpawnSessionActive(spawnSessionId);
    }

    private void OnDestroy()
    {
        ownerSpawner?.UnregisterSpawnIndicator(this);
    }
}
