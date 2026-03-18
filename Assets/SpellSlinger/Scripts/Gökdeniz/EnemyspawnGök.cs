using System.Collections;
using UnityEngine;




public enum EnemyTierG { Small, Ranged, Medium, Elite, Boss }

[System.Serializable]
public class EnemySpawnDataG
{
    public EnemyTier tier;
    public GameObject enemyPrefab;
    
    [Header("Mürekkep Damlası Görseli")]
    public Sprite inkDropSprite;
    public Color dropColor = Color.black;
    public float dropScale = 1f;
}

public class EnemyspawnGök : MonoBehaviour
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

                float randomX = Random.Range(-mapBounds.x, mapBounds.x);
                float randomY = Random.Range(-mapBounds.y, mapBounds.y);
                Vector3 spawnPosition = new Vector3(randomX, randomY, 0f);

                EnemySpawnData selectedEnemy = enemiesToSpawn[Random.Range(0, enemiesToSpawn.Length)];

                GameObject indicator = Instantiate(spawnIndicatorPrefab, spawnPosition, Quaternion.identity);
        
                EnemySpawnIndicator indicatorScript = indicator.GetComponent<EnemySpawnIndicator>();
                if(indicatorScript != null)
                {
                    indicatorScript.SetupIndicator(selectedEnemy, spawnWarningTime);
                }
            }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapBounds.x * 2, mapBounds.y * 2, 0));
    }
}

