using System.Collections.Generic;
using UnityEngine;


public class Projectile : MonoBehaviour
{
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
    
    [Header("Lightning VFX Ayarları")]
    public GameObject lightningHitVFXPrefab;
    public float lightningVFXDuration = 1f;

    [HideInInspector] public PlayerHealth sourcePlayerHealth;
    internal List<SpecialMechanic> activeMechanics;

    [HideInInspector] public GameObject ignoredEnemy; 

    private int bouncesLeft = 0;
    private bool canSplit = false;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetupModifiers()
    {
        if (activeMechanics == null) return;

        if (activeMechanics.Contains(SpecialMechanic.DamageBoost)) 
            baseDamage *= 1.10f;

        if (activeMechanics.Contains(SpecialMechanic.Acceleration)) 
            speed *= 1.30f;

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
            Enemy enemyScript = collision.GetComponent<Enemy>();
            
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

        GameObject vfxInstance = Instantiate(
            lightningHitVFXPrefab, 
            enemyTransform.position, 
            Quaternion.identity
        );
        vfxInstance.transform.SetParent(enemyTransform);
        vfxInstance.transform.localPosition = Vector3.zero;
        vfxInstance.transform.localRotation = Quaternion.identity;
        vfxInstance.transform.localScale = Vector3.one;
        
        ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }

        
        Destroy(vfxInstance, lightningVFXDuration);
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

                        Collider2D[] aoeHits = Physics2D.OverlapCircleAll(targetEnemy.transform.position, 2.5f, enemyLayer);
                        foreach (var hit in aoeHits)
                        {
                            if (hit.gameObject != targetEnemy.gameObject)
                                hit.GetComponent<Enemy>()?.TakeDamage(executionDamage);
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
                        PlayerController pc = sourcePlayerHealth.GetComponent<PlayerController>();
                        if (pc != null) pc.ApplySpeedBuff(5f, 3f); 
                    }
                    break;


                case SpecialMechanic.FireBurn: 
                    targetEnemy.ApplyBurn(6f, 3f); 
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
                    
                    Collider2D[] slowHits = Physics2D.OverlapCircleAll(transform.position, 3f, enemyLayer);
                    foreach (Collider2D hit in slowHits)
                    {
                        Enemy caughtEnemy = hit.GetComponent<Enemy>();
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
                    Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 5f, enemyLayer);
                    int hitCount = 0;

                    foreach (Collider2D col in nearbyEnemies)
                    {
                        if (col.gameObject != targetEnemy.gameObject) 
                        {
                            Enemy chainTarget = col.GetComponent<Enemy>();
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 8f, enemyLayer);
        Transform bestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit.transform == excludeTransform) continue; 
            
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = hit.transform;
            }
        }
        return bestTarget;
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