using System.Collections;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

public enum EnemyTier { Small, Ranged, Medium, Elite, Boss }

[System.Serializable]
public class EnemySpawnData
{
    public EnemyTier tier;
    public GameObject enemyPrefab;
    
    [Header("Mürekkep Damlası Görseli")]
    public Sprite inkDropSprite;
    public Color dropColor = Color.black;
    public float dropScale = 1f;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Arena Ayarları")]
    public Vector2 mapBounds = new Vector2(15f, 10f);

    [Header("Doğma Ayarları")]
    public GameObject spawnIndicatorPrefab;
    public float timeBetweenSpawns = 2f;
    public float spawnWarningTime = 1.5f;
    
    [Header("Düşman Havuzu")]
    public EnemySpawnData[] enemiesToSpawn;

    private void OnEnable()
    {
        StopAllCoroutines(); 
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

                
                if (!enabled) break; 

                EnemySpawnData selectedEnemy = enemiesToSpawn[Random.Range(0, enemiesToSpawn.Length)];
                SpawnEnemy(selectedEnemy);
            }
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

    public bool SpawnEnemy(EnemySpawnData selectedEnemy, Action<GameObject> onSpawned = null)
    {
        if (selectedEnemy == null || spawnIndicatorPrefab == null)
        {
            return false;
        }

        GameObject indicator = Instantiate(spawnIndicatorPrefab, GetRandomSpawnPosition(), Quaternion.identity);
        EnemySpawnIndicator indicatorScript = indicator.GetComponent<EnemySpawnIndicator>();
        if(indicatorScript != null)
        {
            indicatorScript.SetupIndicator(selectedEnemy, spawnWarningTime, onSpawned);
            return true;
        }

        return false;
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
