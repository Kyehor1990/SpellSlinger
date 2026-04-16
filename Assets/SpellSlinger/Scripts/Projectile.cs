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

    [HideInInspector] public PlayerHealth sourcePlayerHealth;
    internal List<SpecialMechanic> activeMechanics;

    private int bouncesLeft = 0;
    private bool canSplit = false;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetupModifiers()
    {
        if (activeMechanics == null) return;

        // Hasar Artırımı (%10)
        if (activeMechanics.Contains(SpecialMechanic.DamageBoost)) 
            baseDamage *= 1.10f;

        // Hızlanma (%30 atış hızı)
        if (activeMechanics.Contains(SpecialMechanic.Acceleration)) 
            speed *= 1.30f;

        // Sekme (2 Kere)
        if (activeMechanics.Contains(SpecialMechanic.Bounce)) 
            bouncesLeft = 2;

        // Bölünme (1 Kere bölünebilir)
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
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemyScript = collision.GetComponent<Enemy>();
            
            if (enemyScript != null)
            {
                bool wasDying = enemyScript.IsDying;

                enemyScript.TakeDamage(baseDamage);
                if (sourcePlayerHealth != null) sourcePlayerHealth.ApplyLifeSteal(baseDamage);

                bool diedJustNow = !wasDying && enemyScript.IsDying;

                ApplySpecialMechanic(enemyScript, diedJustNow);
            }

            if (destroyOnHit) Destroy(gameObject);
        }
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
                        SpawnSplitProjectiles();
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
                    baseDamage /= 2f;
                    if (baseDamage < 1f) destroyOnHit = true;
                    break;

                case SpecialMechanic.Acceleration:
                    if (targetDied && sourcePlayerHealth != null)
                    {
                        PlayerController pc = sourcePlayerHealth.GetComponent<PlayerController>();
                        if (pc != null) pc.ApplySpeedBuff(5f, 3f); // 3 Saniyeliğine +5 Hız
                    }
                    break;

                case SpecialMechanic.FireBurn: targetEnemy.ApplyBurn(6f, 3f); break;
                case SpecialMechanic.WaterSlow: targetEnemy.ApplySlow(0.4f, 1.5f); break;
                case SpecialMechanic.RockStun: targetEnemy.AddRockStack(); break;
                case SpecialMechanic.IceArrow: break;
                case SpecialMechanic.AirSlash: break;
                case SpecialMechanic.LightningChain:break;
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

    private void SpawnSplitProjectiles()
    {
        float splitAngle = 25f;
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
            clone.transform.Rotate(0, 0, i * splitAngle);

            Projectile p = clone.GetComponent<Projectile>();
            p.baseDamage = this.baseDamage / 2f;
            p.activeMechanics = new List<SpecialMechanic>(this.activeMechanics);
            
            p.activeMechanics.Remove(SpecialMechanic.Split); 
            p.activeMechanics.Remove(SpecialMechanic.Bounce);
        }
    }
}