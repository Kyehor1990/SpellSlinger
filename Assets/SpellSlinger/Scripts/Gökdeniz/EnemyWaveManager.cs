using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class WaveConfig
{
    public string waveName;
    [Min(0f)] public float duration = 60f;
    public bool isBossWave;
    public GameObject bossPrefab;
    public List<EnemySpawnOption> enemySpawnOptions = new List<EnemySpawnOption>();
}

[System.Serializable]
public class EnemySpawnOption
{
    public GameObject enemyPrefab;
    [Min(0f)] public float spawnWeight = 1f;
}

public class EnemyWaveManager : MonoBehaviour
{
    private const string BossTimerLabel = "BOSS";

    [Header("Wave Settings")]
    public float waveDuration = 60f;
    public int currentWave = 1;
    [SerializeField] private List<WaveConfig> waveConfigs = new List<WaveConfig>();

    [Header("Difficulty Settings")]
    public float initialSpawnDelay = 2f;
    public float difficultyMultiplier = 0.8f;

    [Header("References")]
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
    private WaveConfig currentWaveConfig;
    private int spawnSessionId;
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
            if (spawner != null)
            {
                spawner.timeBetweenSpawns = currentDelay;
            }

            bool hasNormalSpawns = ConfigureSpawnerForCurrentWave();

            if (isCurrentWaveBossWave)
            {
                StartBossWave(hasNormalSpawns);
            }
            else if (hasNormalSpawns && spawner != null)
            {
                spawner.enabled = true;
            }
            else
            {
                Debug.LogWarning($"Wave {currentWave} has no valid enemy spawn options. No normal enemies will spawn.");
            }

            Debug.Log($"<color=green><b>[WAVE {currentWave} STARTED]</b></color> {GetWaveDisplayName(currentWaveConfig)} | Duration: {timer}s | Spawn Delay: {currentDelay:F2}s");

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
            EndCurrentWaveSpawns();
            UpdateWaveTimerUI(true);

            playerController?.RemoveAccelerationBuff();
            ClearAllEnemies();

            CollectAllCoinsInScene();
            CollectAllXpInScene();

            yield return new WaitForSeconds(1f);
            UpdateWaveTimerUI(true);

            Debug.Log($"Wave {currentWave} finished. Get ready!");

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
        spawnSessionId++;
        isWaveActive = true;
        currentWaveConfig = GetWaveConfig(currentWave);
        isCurrentWaveBossWave = currentWaveConfig != null && currentWaveConfig.isBossWave;
        timer = GetWaveDuration(currentWaveConfig);
        currentBoss = null;
        isWaitingForBossSpawn = false;
        isWaitingForNextWaveStart = false;
        spawner?.BeginSpawnSession(spawnSessionId, IsSpawnSessionActive);
        UpdateWaveTimerUI(true);
    }

    private void EndCurrentWaveSpawns()
    {
        isWaveActive = false;
        spawner?.EndSpawnSession(spawnSessionId);
        spawnSessionId++;

        if (spawner != null)
        {
            spawner.enabled = false;
        }
    }

    private bool IsSpawnSessionActive(int sessionId)
    {
        return isWaveActive && sessionId == spawnSessionId;
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
        WaveConfig waveConfig = GetWaveConfig(waveNumber);
        SetWaveTimerText(waveConfig != null && waveConfig.isBossWave, GetWaveDuration(waveConfig), true);
    }

    private WaveConfig GetWaveConfig(int waveNumber)
    {
        if (waveConfigs == null || waveNumber <= 0)
        {
            return null;
        }

        int waveIndex = waveNumber - 1;
        if (waveIndex < 0 || waveIndex >= waveConfigs.Count)
        {
            return null;
        }

        return waveConfigs[waveIndex];
    }

    private float GetWaveDuration(WaveConfig waveConfig)
    {
        if (waveConfig == null)
        {
            return waveDuration;
        }

        return Mathf.Max(0f, waveConfig.duration);
    }

    private string GetWaveDisplayName(WaveConfig waveConfig)
    {
        if (waveConfig == null || string.IsNullOrWhiteSpace(waveConfig.waveName))
        {
            return $"Wave {currentWave}";
        }

        return waveConfig.waveName;
    }

    private bool ConfigureSpawnerForCurrentWave()
    {
        if (spawner == null)
        {
            Debug.LogWarning("EnemyWaveManager has no EnemySpawner reference.");
            return false;
        }

        if (currentWaveConfig == null)
        {
            spawner.UseDefaultSpawnPool();
            return spawner.HasValidDefaultSpawnPool();
        }

        return spawner.UseWaveSpawnOptions(currentWaveConfig.enemySpawnOptions);
    }

    private void StartBossWave(bool spawnNormalEnemiesDuringBoss)
    {
        UpdateWaveTimerUI(true);

        if (spawner != null)
        {
            spawner.enabled = spawnNormalEnemiesDuringBoss;
        }

        if (spawner == null)
        {
            isWaitingForBossSpawn = false;
            currentBoss = null;
            return;
        }

        GameObject bossPrefab = currentWaveConfig != null ? currentWaveConfig.bossPrefab : null;
        if (bossPrefab == null)
        {
            Debug.LogWarning($"Wave {currentWave} is marked as a boss wave, but no boss prefab is assigned. The boss wave will end immediately.");
            isWaitingForBossSpawn = false;
            currentBoss = null;
            return;
        }

        isWaitingForBossSpawn = true;
        bool spawnStarted = spawner.SpawnEnemy(bossPrefab, spawnedBoss =>
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

        Debug.LogWarning($"Wave {currentWave} is marked as a boss wave, but EnemySpawner could not spawn the assigned boss prefab. The boss wave will end immediately.");
        isWaitingForBossSpawn = false;
        currentBoss = null;
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

        if (playerExp == null)
        {
            return;
        }

        Transform playerTransform = playerExp.transform;

        foreach (var coin in coins)
        {
            coin.ForceFollow(playerTransform);
        }
    }

    private void CollectAllXpInScene()
    {
        InspirationDrop[] xp = Object.FindObjectsByType<InspirationDrop>(FindObjectsSortMode.None);

        if (playerExp == null)
        {
            return;
        }

        Transform playerTransform = playerExp.transform;
        foreach (var inspirationDrop in xp)
        {
            inspirationDrop.ForceFollow(playerTransform);
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
