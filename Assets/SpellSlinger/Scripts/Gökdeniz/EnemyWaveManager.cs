using System.Collections;
using UnityEngine;

public class EnemyWaveManager : MonoBehaviour
{
    [Header("Dalga Ayarları")]
    public float waveDuration = 60f;       
    public float breakDuration = 5f;        // İki dalga arasında ki saniye (şimdilik)
    public int currentWave = 1;

    [Header("Zorluk Ayarları")]
    public float initialSpawnDelay = 2f;   
    public float difficultyMultiplier = 0.8f; 

    [Header("Referanslar")]
    public EnemySpawner spawner;
    public PlayerExperience playerExp;
    public UpgradeManager upgradeManager;

    private float timer;
    private bool isWaveActive;

    private void Start()
    {
        StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        while (true)
        {
            
            isWaveActive = true;
            timer = waveDuration;
            
            float currentDelay = initialSpawnDelay * Mathf.Pow(difficultyMultiplier, currentWave - 1);
            spawner.timeBetweenSpawns = currentDelay;
            spawner.enabled = true; 
            Debug.Log($"<color=green><b>[DALGA {currentWave} BAŞLADI]</b></color> Süre: {waveDuration}s | Spawn Hızı: {currentDelay:F2}s");

            Debug.Log($"Dalga {currentWave} Başladı! Spawn Hızı: {currentDelay}s");

            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

         
            isWaveActive = false;
            spawner.enabled = false; 
            
            ClearAllEnemies(); // Ekrandakileri düşmanlar silinir.
            
            Debug.Log($"Dalga {currentWave} Bitti. Hazırlan!");

            if (playerExp != null && playerExp.pendingLevelUps > 0)
            {
                upgradeManager.StartUpgradePhase(playerExp.pendingLevelUps);
                yield return new WaitUntil(() => upgradeManager.isUpgradePhaseActive == false);
                playerExp.pendingLevelUps = 0; 
            }
            else
            {
                yield return new WaitForSeconds(breakDuration);
            }
            
            currentWave++;
        }
            
    }

    private void ClearAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
    }
}
