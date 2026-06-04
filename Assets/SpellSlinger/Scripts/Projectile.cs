using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Projectile : MonoBehaviour
{
    private const int AreaHitBufferSize = 128;
    private static readonly Collider2D[] areaHitsBuffer = new Collider2D[AreaHitBufferSize];
    private static readonly Dictionary<GameObject, Queue<GameObject>> timedVFXPools = new Dictionary<GameObject, Queue<GameObject>>();

[Header("Enemy Follow Ayarları")]
[Tooltip("If true, this projectile will continuously turn toward the player while moving. Useful for enemy projectiles.")]
public bool enemyFollow = false;

[SerializeField] private string playerTag = "Player";
private Transform playerTarget;
private Vector2 followMoveDirection;


    [Header("Mermi Özellikleri")]
    public float speed = 15f;
    public float baseDamage = 10f; 
    public float lifeTime = 3f;    

    [Header("Davranış Ayarları")]
    public bool destroyOnHit = true;

    [Header("Mekanik Ayarları")]
    public LayerMask enemyLayer; 
    public GameObject iceExplosionPrefab;
    public GameObject executionExplosionPrefab; 

    [Header("Ice Arrow VFX Ayarları")]
    [SerializeField] private GameObject iceImpactVFXPrefab;
    [SerializeField] private float iceImpactVFXScale = 1.15f;
    [SerializeField] private int iceImpactVFXSortingOrder = 20;
    [SerializeField] private bool iceImpactVFXRandomRotation = true;
    
[Header("Lightning VFX Ayarları")]
    public GameObject lightningHitVFXPrefab;
    public float lightningVFXDuration = 1f;

    [Header("Fire VFX Ayarları")]
    [SerializeField] private GameObject fireHitVFXPrefab;
    [SerializeField] private float fireVFXDuration = 1f;
    [SerializeField] private float fireVFXScale = 1f;

    [HideInInspector] public PlayerHealth sourcePlayerHealth;
    internal List<SpecialMechanic> activeMechanics;
    internal List<SpecialMechanicStats> activeMechanicStats;

    [HideInInspector] public GameObject ignoredEnemy; 

    private int bouncesLeft = 0;
    private bool canSplit = false;
    private int splitProjectileCount = 2;
    private int piercesLeft = 0;
    private float pierceDamageMultiplier = 0.5f;
    private PlayerController sourcePlayerController;

    private sealed class TimedVFXPoolReturner : MonoBehaviour
    {
        private GameObject prefab;
        private Coroutine returnRoutine;

        public void ReturnAfter(GameObject sourcePrefab, float delay)
        {
            prefab = sourcePrefab;

            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
            }

            returnRoutine = StartCoroutine(ReturnRoutine(Mathf.Max(0f, delay)));
        }

        private IEnumerator ReturnRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            returnRoutine = null;
            ReturnTimedVFX(prefab, gameObject);
        }

        private void OnDisable()
        {
            if (returnRoutine == null) return;

            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetupModifiers()
    {
        if (activeMechanics == null) return;

        if (activeMechanics.Contains(SpecialMechanic.DamageBoost)) 
            baseDamage *= GetMechanicStats(SpecialMechanic.DamageBoost)?.damageBonusMultiplier ?? 1.10f;

        if (activeMechanics.Contains(SpecialMechanic.Bounce)) 
            bouncesLeft = GetMechanicStats(SpecialMechanic.Bounce)?.bounceCount ?? 2;

        if (activeMechanics.Contains(SpecialMechanic.Split))
        {
            canSplit = true;
            splitProjectileCount = GetMechanicStats(SpecialMechanic.Split)?.splitProjectileCount ?? 2;
        }

        if (activeMechanics.Contains(SpecialMechanic.Pierce))
        {
            WordLevelStats pierceStats = GetMechanicStats(SpecialMechanic.Pierce);
            piercesLeft = pierceStats?.pierceCount ?? 4;
            pierceDamageMultiplier = pierceStats?.pierceDamageMultiplier ?? 0.5f;
        }
    }

private void Update()
{
    Vector3 moveDirection = transform.right;

    if (enemyFollow)
    {
        UpdatePlayerFollowDirection();
        moveDirection = followMoveDirection;
    }

    if (speed > 0)
        transform.position += moveDirection * speed * Time.deltaTime;
}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (ignoredEnemy != null && collision.gameObject == ignoredEnemy) 
            return;

        if (collision.CompareTag("Enemy"))
        {
            collision.TryGetComponent(out Enemy enemyScript);
            
if (enemyScript != null)
            {
                bool wasDying = enemyScript.IsDying; 

bool isIceArrow = activeMechanics != null && activeMechanics.Contains(SpecialMechanic.IceArrow);
                bool isLightning = activeMechanics != null && activeMechanics.Contains(SpecialMechanic.LightningChain);
                bool isWaterSlow = activeMechanics != null && activeMechanics.Contains(SpecialMechanic.WaterSlow);
                bool isCrit = Random.value < 0.1f;
                float finalDamage = baseDamage;
                Color hitColor = DamagePopupManager.Instance.normalColor;
                if (isIceArrow) {
                    hitColor = DamagePopupManager.Instance.iceColor;
                } else if (isLightning) {
                    hitColor = DamagePopupManager.Instance.lightningColor;
                } else if (isWaterSlow) {
                    hitColor = DamagePopupManager.Instance.waterColor;
                }
                if (isCrit) {
                    finalDamage *= 1.5f;
                }
                enemyScript.TakeDamage(finalDamage, isCrit, hitColor);
                if (sourcePlayerHealth != null) sourcePlayerHealth.ApplyLifeSteal(baseDamage);

                bool diedJustNow = !wasDying && enemyScript.IsDying; 

                ApplySpecialMechanic(enemyScript, diedJustNow);
            }

            if (destroyOnHit) Destroy(gameObject);
        }
    }
    private void SpawnLightningVFX(Transform enemyTransform)
    {
        if (lightningHitVFXPrefab == null) return;

        PlayTimedAttachedVFX(lightningHitVFXPrefab, enemyTransform, Vector3.one, lightningVFXDuration);
    }

private void SpawnIceImpactVFX(Vector3 position)
    {
        if (iceImpactVFXPrefab == null) return;

        Quaternion rotation = iceImpactVFXRandomRotation
            ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
            : Quaternion.identity;

        GameObject vfxInstance = Instantiate(iceImpactVFXPrefab, position, rotation);
        vfxInstance.transform.localScale = Vector3.one * iceImpactVFXScale;

        Collider2D[] colliders = vfxInstance.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        SpriteRenderer[] renderers = vfxInstance.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = iceImpactVFXSortingOrder;
        }
    }

private void SpawnFireHitVFX(Transform enemyTransform)
    {
        if (fireHitVFXPrefab == null) return;

        PlayTimedAttachedVFX(fireHitVFXPrefab, enemyTransform, Vector3.one * fireVFXScale, fireVFXDuration);
    }

    private void PlayTimedAttachedVFX(GameObject prefab, Transform parent, Vector3 localScale, float duration)
    {
        if (prefab == null || parent == null) return;

        GameObject vfxInstance = GetTimedVFX(prefab);
        if (vfxInstance == null) return;

        vfxInstance.transform.SetParent(parent);
        vfxInstance.transform.localPosition = Vector3.zero;
        vfxInstance.transform.localRotation = Quaternion.identity;
        vfxInstance.transform.localScale = localScale;
        vfxInstance.SetActive(true);

        ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        TimedVFXPoolReturner returner = vfxInstance.GetComponent<TimedVFXPoolReturner>();
        if (returner == null)
        {
            returner = vfxInstance.AddComponent<TimedVFXPoolReturner>();
        }

        returner.ReturnAfter(prefab, duration);
    }

    private static GameObject GetTimedVFX(GameObject prefab)
    {
        if (!timedVFXPools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            timedVFXPools[prefab] = pool;
        }

        while (pool.Count > 0)
        {
            GameObject pooledVFX = pool.Dequeue();
            if (pooledVFX != null)
            {
                return pooledVFX;
            }
        }

        return Instantiate(prefab);
    }

    private static void ReturnTimedVFX(GameObject prefab, GameObject vfxInstance)
    {
        if (prefab == null || vfxInstance == null) return;

        vfxInstance.transform.SetParent(null);
        vfxInstance.SetActive(false);

        if (!timedVFXPools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            timedVFXPools[prefab] = pool;
        }

        pool.Enqueue(vfxInstance);
    }

   private void ApplySpecialMechanic(Enemy targetEnemy, bool targetDied)
    {
        if (activeMechanics == null || activeMechanics.Count == 0) return;

        foreach (SpecialMechanic mechanic in activeMechanics)
        {
            switch (mechanic)
            {

                case SpecialMechanic.Bounce:
                    if (bouncesLeft > 0)
                    {
                        bouncesLeft--;
                        destroyOnHit = false; 
                        ignoredEnemy = targetEnemy.gameObject; 

                        Transform nextTarget = FindNearestEnemy(targetEnemy.transform);
                        if (nextTarget != null)
                        {
                            Vector2 dir = (nextTarget.position - transform.position).normalized;
                            transform.right = dir; 
                        }
                        else
                        {
                            destroyOnHit = true; 
                        }
                    }
                    break;

                case SpecialMechanic.Split:
                    if (canSplit)
                    {
                        canSplit = false;
                        destroyOnHit = true; 
                        SpawnSplitProjectiles(targetEnemy); 
                    }
                    break;

                case SpecialMechanic.Execution:
                    if (targetDied)
                    {
                        float executionMultiplier = GetMechanicStats(SpecialMechanic.Execution)?.executionDamageMultiplier ?? 10f;
                        float executionDamage = (targetEnemy.maxHealth / Mathf.Max(0.01f, baseDamage)) * executionMultiplier; 
                        
                        if (executionExplosionPrefab != null) Instantiate(executionExplosionPrefab, targetEnemy.transform.position, Quaternion.identity);

                        int aoeHitCount = Physics2D.OverlapCircle(targetEnemy.transform.position, 2.5f, CreateEnemyContactFilter(), areaHitsBuffer);
                        for (int i = 0; i < aoeHitCount; i++)
                        {
                            Collider2D hit = areaHitsBuffer[i];
                            if (hit != null && hit.gameObject != targetEnemy.gameObject && hit.TryGetComponent(out Enemy hitEnemy))
                            {
                                hitEnemy.TakeDamage(executionDamage);
                            }
                        }
                    }
                    break;

                case SpecialMechanic.Pierce: 
                    if (piercesLeft > 0)
                    {
                        piercesLeft--;
                        destroyOnHit = false;
                        ignoredEnemy = targetEnemy.gameObject;
                        baseDamage *= pierceDamageMultiplier;
                        if (baseDamage < 1f) destroyOnHit = true;
                    }
                    else
                    {
                        destroyOnHit = true;
                    }
                    break;

                case SpecialMechanic.Acceleration: 
                    if (targetDied && sourcePlayerHealth != null)
                    {
                        float accelerationDuration = GetMechanicStats(SpecialMechanic.Acceleration)?.accelerationDuration ?? 3f;
                        PlayerController pc = GetSourcePlayerController();
                        if (pc != null) pc.ApplyAccelerationBuff(accelerationDuration);   
                    }
                    break;


case SpecialMechanic.FireBurn: 
                    WordLevelStats fireStats = GetMechanicStats(SpecialMechanic.FireBurn);
                    targetEnemy.ApplyBurn(fireStats?.burnDamage ?? 6f, fireStats?.burnDuration ?? 3f);
                    SpawnFireHitVFX(targetEnemy.transform);
                    break;

                case SpecialMechanic.WaterSlow: 
                    WordLevelStats waterStats = GetMechanicStats(SpecialMechanic.WaterSlow);
                    targetEnemy.ApplySlow(waterStats?.waterSlowPercent ?? 0.4f, waterStats?.waterSlowDuration ?? 1.5f); 
                    break;

                case SpecialMechanic.RockStun: 
                    targetEnemy.AddRockStack(); 
                    break;

                case SpecialMechanic.IceArrow:
                    WordLevelStats iceStats = GetMechanicStats(SpecialMechanic.IceArrow);
                    if (iceExplosionPrefab != null)
                    {
                        Instantiate(iceExplosionPrefab, transform.position, Quaternion.identity);
                    }

                    SpawnIceImpactVFX(transform.position);
                    
                    int slowHitCount = Physics2D.OverlapCircle(transform.position, iceStats?.iceExplosionRadius ?? 3f, CreateEnemyContactFilter(), areaHitsBuffer);
                    for (int i = 0; i < slowHitCount; i++)
                    {
                        Collider2D hit = areaHitsBuffer[i];
                        if (hit == null || !hit.TryGetComponent(out Enemy caughtEnemy)) continue;

                        if (caughtEnemy != null) caughtEnemy.ApplySlow(iceStats?.iceSlowPercent ?? 0.6f, iceStats?.iceSlowDuration ?? 2f); 
                    }
                    break;

                case SpecialMechanic.AirSlash:
                    destroyOnHit = false; 
                    ignoredEnemy = targetEnemy.gameObject;
                    baseDamage *= GetMechanicStats(SpecialMechanic.AirSlash)?.airSlashDamageMultiplier ?? 0.5f; 
                    
                    if (baseDamage < 1f) destroyOnHit = true; 
                    break;

                case SpecialMechanic.LightningChain:
                    WordLevelStats lightningStats = GetMechanicStats(SpecialMechanic.LightningChain);
                    SpawnLightningVFX(targetEnemy.transform);
                    int nearbyEnemyCount = Physics2D.OverlapCircle(transform.position, lightningStats?.lightningChainRadius ?? 5f, CreateEnemyContactFilter(), areaHitsBuffer);
                    int hitCount = 0;
                    int chainTargetLimit = lightningStats?.lightningChainTargets ?? 3;
                    float chainDamageMultiplier = lightningStats?.lightningChainDamageMultiplier ?? 0.3333333f;

                    for (int i = 0; i < nearbyEnemyCount; i++)
                    {
                        Collider2D col = areaHitsBuffer[i];
                        if (col != null && col.gameObject != targetEnemy.gameObject) 
                        {
                            col.TryGetComponent(out Enemy chainTarget);
                            if (chainTarget != null)
                            {
bool chainCrit = Random.value < 0.05f;
float chainDmg = baseDamage * chainDamageMultiplier;
if (chainCrit) chainDmg *= 1.5f;
chainTarget.TakeDamage(chainDmg, chainCrit, DamagePopupManager.Instance.lightningColor);
                                hitCount++;
                                SpawnLightningVFX(col.transform);
                                Debug.DrawLine(transform.position, col.transform.position, Color.yellow, 0.5f);
                            }
                        }
                        if (hitCount >= chainTargetLimit) break;
                    }
                    break;
            }
        }
    }

    private Transform FindNearestEnemy(Transform excludeTransform)
    {
        Transform bestTarget = null;
        float closestDistSqr = Mathf.Infinity;
        int hitCount = Physics2D.OverlapCircle(transform.position, 8f, CreateEnemyContactFilter(), areaHitsBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = areaHitsBuffer[i];
            if (hit == null) continue;
            if (hit.transform == excludeTransform) continue; 
            
            float distSqr = ((Vector2)transform.position - (Vector2)hit.transform.position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                bestTarget = hit.transform;
            }
        }
        return bestTarget;
    }

    private PlayerController GetSourcePlayerController()
    {
        if (sourcePlayerController == null && sourcePlayerHealth != null)
        {
            sourcePlayerController = sourcePlayerHealth.GetComponent<PlayerController>();
        }

        return sourcePlayerController;
    }

    private ContactFilter2D CreateEnemyContactFilter()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = Physics2D.queriesHitTriggers;
        filter.SetLayerMask(enemyLayer);
        return filter;
    }

    private WordLevelStats GetMechanicStats(SpecialMechanic mechanic)
    {
        if (activeMechanicStats == null) return null;

        for (int i = 0; i < activeMechanicStats.Count; i++)
        {
            SpecialMechanicStats mechanicStats = activeMechanicStats[i];
            if (mechanicStats != null && mechanicStats.mechanic == mechanic)
            {
                return mechanicStats.stats;
            }
        }

        return null;
    }

    private void SpawnSplitProjectiles(Enemy targetEnemy)
    {
        if (splitProjectileCount <= 0) return;

        const float totalSplitAngle = 50f;
        float angleStep = splitProjectileCount > 1 ? totalSplitAngle / (splitProjectileCount - 1) : 0f;
        float startAngle = splitProjectileCount > 1 ? -totalSplitAngle * 0.5f : 0f;

        for (int i = 0; i < splitProjectileCount; i++) 
        {
            GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
            clone.transform.Rotate(0, 0, startAngle + angleStep * i);

            Projectile p = clone.GetComponent<Projectile>();
            p.baseDamage = this.baseDamage / 2f; 
            p.activeMechanics = new List<SpecialMechanic>(this.activeMechanics);
            p.activeMechanicStats = this.activeMechanicStats != null
                ? new List<SpecialMechanicStats>(this.activeMechanicStats)
                : null;
            
            p.ignoredEnemy = targetEnemy.gameObject; 
            
            p.activeMechanics.Remove(SpecialMechanic.Split); 
            p.activeMechanics.Remove(SpecialMechanic.Bounce);
        }
    }

private void CachePlayerTarget()
{
    if (!enemyFollow || playerTarget != null) return;

    GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
    if (playerObject != null)
    {
        playerTarget = playerObject.transform;
    }
}

private void UpdatePlayerFollowDirection()
{
    if (playerTarget == null)
    {
        CachePlayerTarget();
    }

    if (playerTarget == null)
    {
        followMoveDirection = transform.right;
        return;
    }

    Vector2 directionToPlayer = (Vector2)(playerTarget.position - transform.position);

    if (directionToPlayer.sqrMagnitude <= 0.0001f)
    {
        followMoveDirection = transform.right;
        return;
    }

    followMoveDirection = directionToPlayer.normalized;
}
}
