using System.Collections;
using UnityEngine;

public class EnemySpawnIndicator : MonoBehaviour
{
    public SpriteRenderer sr;
    public GameObject smokeParticlePrefab;
    
    private GameObject enemyToSpawn;

    public void SetupIndicator(EnemySpawnData data, float warningTime)
    {
        enemyToSpawn = data.enemyPrefab;
        sr.sprite = data.inkDropSprite;
        sr.color = data.dropColor;
        transform.localScale = Vector3.one * data.dropScale;

        StartCoroutine(SpawnSequence(warningTime));
    }

    private IEnumerator SpawnSequence(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (smokeParticlePrefab != null)
        {
            Instantiate(smokeParticlePrefab, transform.position, Quaternion.identity);
        }

        if (enemyToSpawn != null)
        {
            Instantiate(enemyToSpawn, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}