using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum EnemyTier { Small, Ranged, Medium, Elite, Boss }

[System.Serializable]
public class EnemySpawnData
{
    public EnemyTier tier;
    public GameObject enemyPrefab;

    [Header("Ink Drop Visual")]
    public Sprite inkDropSprite;
    public Color dropColor = Color.black;
    public float dropScale = 1f;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Arena Settings")]
    public Vector2 mapBounds = new Vector2(15f, 10f);

    [Header("Spawn Settings")]
    public GameObject spawnIndicatorPrefab;
    public float timeBetweenSpawns = 2f;
    public float spawnWarningTime = 1.5f;

    [Header("Enemy Catalog")]
    public EnemySpawnData[] enemiesToSpawn;

    private IReadOnlyList<EnemySpawnOption> activeSpawnOptions;
    private bool useWaveSpawnOptions;
    private bool warnedAboutMissingSpawnOptions;

    private void OnEnable()
    {
        StopAllCoroutines();
        warnedAboutMissingSpawnOptions = false;
        StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator SpawnRoutine()
    {
        while (enabled)
        {
            yield return new WaitForSeconds(timeBetweenSpawns);

            if (!enabled)
            {
                break;
            }

            EnemySpawnData selectedEnemy = GetRandomEnemySpawnData();
            if (selectedEnemy == null)
            {
                WarnMissingSpawnOptionsOnce();
                continue;
            }

            SpawnEnemy(selectedEnemy);
        }
    }

    public bool UseWaveSpawnOptions(IReadOnlyList<EnemySpawnOption> spawnOptions)
    {
        activeSpawnOptions = spawnOptions;
        useWaveSpawnOptions = true;
        warnedAboutMissingSpawnOptions = false;
        return HasValidWaveSpawnOptions();
    }

    public void UseDefaultSpawnPool()
    {
        activeSpawnOptions = null;
        useWaveSpawnOptions = false;
        warnedAboutMissingSpawnOptions = false;
    }

    public bool HasValidDefaultSpawnPool()
    {
        if (enemiesToSpawn == null)
        {
            return false;
        }

        foreach (EnemySpawnData enemy in enemiesToSpawn)
        {
            if (enemy != null && enemy.enemyPrefab != null)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetSpawnData(EnemyTier tier, out EnemySpawnData spawnData)
    {
        if (enemiesToSpawn != null)
        {
            foreach (EnemySpawnData enemy in enemiesToSpawn)
            {
                if (enemy != null && enemy.tier == tier && enemy.enemyPrefab != null)
                {
                    spawnData = enemy;
                    return true;
                }
            }
        }

        spawnData = null;
        return false;
    }

    public bool TryGetSpawnData(GameObject enemyPrefab, out EnemySpawnData spawnData)
    {
        if (enemyPrefab == null)
        {
            spawnData = null;
            return false;
        }

        if (enemiesToSpawn != null)
        {
            foreach (EnemySpawnData enemy in enemiesToSpawn)
            {
                if (enemy != null && enemy.enemyPrefab == enemyPrefab)
                {
                    spawnData = enemy;
                    return true;
                }
            }
        }

        spawnData = new EnemySpawnData
        {
            enemyPrefab = enemyPrefab
        };
        return true;
    }

    public bool SpawnEnemy(GameObject enemyPrefab, Action<GameObject> onSpawned = null)
    {
        if (!TryGetSpawnData(enemyPrefab, out EnemySpawnData spawnData))
        {
            return false;
        }

        return SpawnEnemy(spawnData, onSpawned);
    }

    public bool SpawnEnemy(EnemySpawnData selectedEnemy, Action<GameObject> onSpawned = null)
    {
        if (selectedEnemy == null || selectedEnemy.enemyPrefab == null || spawnIndicatorPrefab == null)
        {
            return false;
        }

        GameObject indicator = Instantiate(spawnIndicatorPrefab, GetRandomSpawnPosition(), Quaternion.identity);
        EnemySpawnIndicator indicatorScript = indicator.GetComponent<EnemySpawnIndicator>();
        if (indicatorScript != null)
        {
            indicatorScript.SetupIndicator(selectedEnemy, spawnWarningTime, onSpawned);
            return true;
        }

        Destroy(indicator);
        return false;
    }

    private EnemySpawnData GetRandomEnemySpawnData()
    {
        if (useWaveSpawnOptions)
        {
            return GetWeightedWaveSpawnData();
        }

        return GetRandomDefaultSpawnData();
    }

    private EnemySpawnData GetWeightedWaveSpawnData()
    {
        if (!HasValidWaveSpawnOptions())
        {
            return null;
        }

        float totalWeight = 0f;
        foreach (EnemySpawnOption option in activeSpawnOptions)
        {
            if (IsValidSpawnOption(option))
            {
                totalWeight += option.spawnWeight;
            }
        }

        float roll = Random.Range(0f, totalWeight);
        EnemySpawnData fallback = null;

        foreach (EnemySpawnOption option in activeSpawnOptions)
        {
            if (!IsValidSpawnOption(option))
            {
                continue;
            }

            TryGetSpawnData(option.enemyPrefab, out EnemySpawnData spawnData);
            fallback = spawnData;

            if (roll < option.spawnWeight)
            {
                return spawnData;
            }

            roll -= option.spawnWeight;
        }

        return fallback;
    }

    private EnemySpawnData GetRandomDefaultSpawnData()
    {
        int validCount = 0;

        if (enemiesToSpawn != null)
        {
            foreach (EnemySpawnData enemy in enemiesToSpawn)
            {
                if (enemy != null && enemy.enemyPrefab != null)
                {
                    validCount++;
                }
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int selectedIndex = Random.Range(0, validCount);
        foreach (EnemySpawnData enemy in enemiesToSpawn)
        {
            if (enemy == null || enemy.enemyPrefab == null)
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                return enemy;
            }

            selectedIndex--;
        }

        return null;
    }

    private bool HasValidWaveSpawnOptions()
    {
        if (activeSpawnOptions == null)
        {
            return false;
        }

        foreach (EnemySpawnOption option in activeSpawnOptions)
        {
            if (IsValidSpawnOption(option))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidSpawnOption(EnemySpawnOption option)
    {
        return option != null && option.enemyPrefab != null && option.spawnWeight > 0f;
    }

    private void WarnMissingSpawnOptionsOnce()
    {
        if (warnedAboutMissingSpawnOptions)
        {
            return;
        }

        string source = useWaveSpawnOptions ? "current wave config" : "default enemy catalog";
        Debug.LogWarning($"EnemySpawner has no valid enemies in the {source}. Null prefabs and zero or negative weights are ignored.");
        warnedAboutMissingSpawnOptions = true;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float randomX = Random.Range(-mapBounds.x, mapBounds.x);
        float randomY = Random.Range(-mapBounds.y, mapBounds.y);
        return new Vector3(randomX, randomY, 0f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapBounds.x * 2, mapBounds.y * 2, 0));
    }
}
