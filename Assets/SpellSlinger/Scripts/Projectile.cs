using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Projectile : MonoBehaviour
{
    private const int AreaHitBufferSize = 128;
    private static readonly Collider2D[] areaHitsBuffer = new Collider2D[AreaHitBufferSize];
    private static readonly Dictionary<GameObject, Queue<GameObject>> timedVFXPools = new Dictionary<GameObject, Queue<GameObject>>();

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

    [HideInInspector] public GameObject ignoredEnemy; 

    private int bouncesLeft = 0;
    private bool canSplit = false;
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
            baseDamage *= 1.10f;

        if (activeMechanics.Contains(SpecialMechanic.Bounce)) 
            bouncesLeft = 2;

        if (activeMechanics.Contains(SpecialMechanic.Split)) 
            canSplit = true;
    }

    private void Update()
    {
        if (speed > 0)
            transform.position += transform.right * speed * Time.deltaTime;
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
                        float executionDamage = (targetEnemy.maxHealth / baseDamage) * 10f; 
                        
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
                    destroyOnHit = false;
                    ignoredEnemy = targetEnemy.gameObject; 
                    baseDamage /= 2f;
                    if (baseDamage < 1f) destroyOnHit = true;
                    break;

                case SpecialMechanic.Acceleration: 
                    if (targetDied && sourcePlayerHealth != null)
                    {
                        PlayerController pc = GetSourcePlayerController();
                        if (pc != null) pc.ApplyAccelerationBuff();   
                    }
                    break;


case SpecialMechanic.FireBurn: 
                    targetEnemy.ApplyBurn(6f, 3f);
                    SpawnFireHitVFX(targetEnemy.transform);
                    break;

                case SpecialMechanic.WaterSlow: 
                    targetEnemy.ApplySlow(0.4f, 1.5f); 
                    break;

                case SpecialMechanic.RockStun: 
                    targetEnemy.AddRockStack(); 
                    break;

                case SpecialMechanic.IceArrow:
                    if (iceExplosionPrefab != null)
                    {
                        Instantiate(iceExplosionPrefab, transform.position, Quaternion.identity);
                    }

                    SpawnIceImpactVFX(transform.position);
                    
                    int slowHitCount = Physics2D.OverlapCircle(transform.position, 3f, CreateEnemyContactFilter(), areaHitsBuffer);
                    for (int i = 0; i < slowHitCount; i++)
                    {
                        Collider2D hit = areaHitsBuffer[i];
                        if (hit == null || !hit.TryGetComponent(out Enemy caughtEnemy)) continue;

                        if (caughtEnemy != null) caughtEnemy.ApplySlow(0.6f, 2f); 
                    }
                    break;

                case SpecialMechanic.AirSlash:
                    destroyOnHit = false; 
                    ignoredEnemy = targetEnemy.gameObject;
                    baseDamage /= 2f; 
                    
                    if (baseDamage < 1f) destroyOnHit = true; 
                    break;

                case SpecialMechanic.LightningChain:
                    SpawnLightningVFX(targetEnemy.transform);
                    int nearbyEnemyCount = Physics2D.OverlapCircle(transform.position, 5f, CreateEnemyContactFilter(), areaHitsBuffer);
                    int hitCount = 0;

                    for (int i = 0; i < nearbyEnemyCount; i++)
                    {
                        Collider2D col = areaHitsBuffer[i];
                        if (col != null && col.gameObject != targetEnemy.gameObject) 
                        {
                            col.TryGetComponent(out Enemy chainTarget);
                            if (chainTarget != null)
                            {
bool chainCrit = Random.value < 0.05f;
float chainDmg = baseDamage / 3f;
if (chainCrit) chainDmg *= 1.5f;
chainTarget.TakeDamage(chainDmg, chainCrit, DamagePopupManager.Instance.lightningColor);
                                hitCount++;
                                SpawnLightningVFX(col.transform);
                                Debug.DrawLine(transform.position, col.transform.position, Color.yellow, 0.5f);
                            }
                        }
                        if (hitCount >= 3) break;
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

    private void SpawnSplitProjectiles(Enemy targetEnemy)
    {
        float splitAngle = 25f; 
        for (int i = -1; i <= 1; i += 2) 
        {
            GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
            clone.transform.Rotate(0, 0, i * splitAngle);

            Projectile p = clone.GetComponent<Projectile>();
            p.baseDamage = this.baseDamage / 2f; 
            p.activeMechanics = new List<SpecialMechanic>(this.activeMechanics);
            
            p.ignoredEnemy = targetEnemy.gameObject; 
            
            p.activeMechanics.Remove(SpecialMechanic.Split); 
            p.activeMechanics.Remove(SpecialMechanic.Bounce);
        }
    }
}
