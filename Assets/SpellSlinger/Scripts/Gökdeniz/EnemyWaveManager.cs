using System.Collections;
using TMPro;
using UnityEngine;

public class EnemyWaveManager : MonoBehaviour
{
    private const string BossTimerLabel = "BOSS";

    [System.Serializable]
    public class WaveConfig
    {
        public int waveNumber = 1;
        public bool isBossWave;
    }

    [Header("Dalga Ayarları")]
    public float waveDuration = 60f;       
    public int currentWave = 1;
    [SerializeField] private WaveConfig[] waveConfigs;

    [Header("Zorluk Ayarları")]
    public float initialSpawnDelay = 2f;   
    public float difficultyMultiplier = 0.8f; 

    [Header("Referanslar")]
    public EnemySpawner spawner;
    public PlayerExperience playerExp;
    public UpgradeManager upgradeManager;
    public ShopManager shopManager;
    public PlayerController playerController;
    [SerializeField] private TMP_Text waveTimerText;

    private float timer;
    private bool isWaveActive;
    private bool isCurrentWaveBossWave;
    private GameObject currentBoss;
    private bool isWaitingForBossSpawn;
    private bool isWaitingForNextWaveStart;
    private Coroutine waveRoutine;

    private void Start()
    {
        if (playerController == null && playerExp != null)
        {
            playerController = playerExp.GetComponent<PlayerController>();
        }

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        waveRoutine = StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        while (true)
        {
            
            BeginWave();
            
            float currentDelay = initialSpawnDelay * Mathf.Pow(difficultyMultiplier, currentWave - 1);
            spawner.timeBetweenSpawns = currentDelay;

            if (isCurrentWaveBossWave)
            {
                StartBossWave();
            }
            else
            {
                spawner.enabled = true;
            }
            Debug.Log($"<color=green><b>[DALGA {currentWave} BAŞLADI]</b></color> Süre: {waveDuration}s | Spawn Hızı: {currentDelay:F2}s");

            Debug.Log($"Dalga {currentWave} Başladı! Spawn Hızı: {currentDelay}s");

            if (isCurrentWaveBossWave)
            {
                yield return new WaitUntil(() => !isWaitingForBossSpawn && currentBoss == null);
            }
            else
            {
                while (timer > 0)
                {
                    timer -= Time.deltaTime;
                    UpdateWaveTimerUI();
                    yield return null;
                }
            }

         
            timer = 0f;
            isWaveActive = false;
            UpdateWaveTimerUI(true);
            spawner.enabled = false; 
            playerController?.RemoveAccelerationBuff();
            ClearAllEnemies();
            
            CollectAllCoinsInScene();
            CollectAllXpInScene();
            
            
            yield return new WaitForSeconds(1f);
            UpdateWaveTimerUI(true);
            
            
            
            Debug.Log($"Dalga {currentWave} Bitti. Hazırlan!");

            if (playerExp != null && playerExp.pendingLevelUps > 0)
            {
                upgradeManager.StartUpgradePhase(playerExp.pendingLevelUps);
                yield return new WaitUntil(() => upgradeManager.isUpgradePhaseActive == false);
                playerExp.pendingLevelUps = 0; 
            }

            shopManager.OpenShop();
            isWaitingForNextWaveStart = true;
            yield return new WaitUntil(() => shopManager.isShopActive == false);

            isWaitingForNextWaveStart = false;
            currentWave++;
        }

    }

    private void BeginWave()
    {
        isWaveActive = true;
        isCurrentWaveBossWave = IsBossWave(currentWave);
        timer = waveDuration;
        currentBoss = null;
        isWaitingForBossSpawn = false;
        isWaitingForNextWaveStart = false;
        UpdateWaveTimerUI(true);
    }

    public bool StartNextWaveNow()
    {
        if (!isWaitingForNextWaveStart || isWaveActive)
        {
            return false;
        }

        isWaitingForNextWaveStart = false;
        currentWave++;
        RestartWaveRoutine();
        return true;
    }

    public void PreviewUpcomingWaveTimer()
    {
        PreviewWaveTimer(currentWave + 1);
    }

    public void PreviewWaveTimer(int waveNumber)
    {
        SetWaveTimerText(IsBossWave(waveNumber), waveDuration, true);
    }

    private bool IsBossWave(int waveNumber)
    {
        if (waveConfigs == null)
        {
            return false;
        }

        foreach (WaveConfig waveConfig in waveConfigs)
        {
            if (waveConfig != null && waveConfig.waveNumber == waveNumber)
            {
                return waveConfig.isBossWave;
            }
        }

        return false;
    }

    private void StartBossWave()
    {
        UpdateWaveTimerUI(true);

        spawner.enabled = false;

        if (spawner.TryGetSpawnData(EnemyTier.Boss, out EnemySpawnData bossSpawnData))
        {
            isWaitingForBossSpawn = true;
            bool spawnStarted = spawner.SpawnEnemy(bossSpawnData, spawnedBoss =>
            {
                currentBoss = spawnedBoss;
                isWaitingForBossSpawn = false;

                Enemy bossEnemy = currentBoss != null ? currentBoss.GetComponent<Enemy>() : null;
                if (bossEnemy != null)
                {
                    bossEnemy.isBoss = true;
                }
            });

            if (spawnStarted)
            {
                return;
            }
        }

        Debug.LogWarning($"Wave {currentWave} is marked as a boss wave, but EnemySpawner could not spawn a Boss. Falling back to timed wave.");
        isWaitingForBossSpawn = false;
        isCurrentWaveBossWave = false;
        spawner.enabled = true;
        UpdateWaveTimerUI(true);
    }

    private void UpdateWaveTimerUI(bool forceRefresh = false)
    {
        if (waveTimerText == null)
        {
            return;
        }

        SetWaveTimerText(isCurrentWaveBossWave, timer, forceRefresh);
    }

    private void SetWaveTimerText(bool isBossWave, float remainingTime, bool forceRefresh)
    {
        if (waveTimerText == null)
        {
            return;
        }

        if (isBossWave)
        {
            waveTimerText.text = BossTimerLabel;
            ForceWaveTimerRefresh(forceRefresh);
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime);
        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        waveTimerText.text = $"{minutes:00}:{seconds:00}";
        ForceWaveTimerRefresh(forceRefresh);
    }

    private void ForceWaveTimerRefresh(bool forceRefresh)
    {
        if (!forceRefresh)
        {
            return;
        }

        waveTimerText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
    }

    private void RestartWaveRoutine()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
        }

        waveRoutine = StartCoroutine(WaveRoutine());
    }

    private void CollectAllCoinsInScene()
    {
        CoinDrop[] coins = Object.FindObjectsByType<CoinDrop>(FindObjectsSortMode.None);

        Transform playerTransform = playerExp.transform;

        foreach (var coin in coins)
        {
            coin.ForceFollow(playerTransform);
        }
    }

    private void CollectAllXpInScene()
    {
        InspirationDrop [] xp = Object.FindObjectsByType<InspirationDrop>(FindObjectsSortMode.None);
        Transform playerTransform = playerExp.transform;
        foreach (var Xp in xp)
        {
            Xp.ForceFollow(playerTransform);
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
