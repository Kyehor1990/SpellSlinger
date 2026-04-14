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

    [Header("Mekanik")]
    public LayerMask enemyLayer;

    [HideInInspector] public PlayerHealth sourcePlayerHealth;
    internal List<SpecialMechanic> activeMechanics;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (speed > 0)
        {
            transform.position += transform.right * speed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemyScript = collision.GetComponent<Enemy>();
            
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(baseDamage);
                if (sourcePlayerHealth != null)
                {
                    sourcePlayerHealth.ApplyLifeSteal(baseDamage);
                }

                ApplySpecialMechanic(enemyScript);
            }

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }

    private void ApplySpecialMechanic(Enemy targetEnemy)
    {
        if (activeMechanics == null || activeMechanics.Count == 0) return;

        foreach (SpecialMechanic mechanic in activeMechanics)
        {
            switch (mechanic)
            {
                case SpecialMechanic.FireBurn:
                    targetEnemy.ApplyBurn(6f, 3f); 
                    break;

                case SpecialMechanic.WaterSlow:
                    targetEnemy.ApplySlow(0.5f, 1f); 
                    break;

                case SpecialMechanic.AirSlash:
                    destroyOnHit = false; 
                    baseDamage /= 2f; 
                    
                    if (baseDamage < 1f) destroyOnHit = true; 
                    break;

                case SpecialMechanic.RockStun:
                    targetEnemy.AddRockStack(); 
                    break;

                case SpecialMechanic.LightningChain:
                    Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 4f, enemyLayer);
                    int hitCount = 0;

                    foreach (Collider2D col in nearbyEnemies)
                    {
                        if (col.gameObject != targetEnemy.gameObject)
                        {
                            Enemy chainTarget = col.GetComponent<Enemy>();
                            if (chainTarget != null)
                            {
                                chainTarget.TakeDamage(baseDamage / 3f);
                                hitCount++;
                            }
                        }
                        if (hitCount >= 3) break;
                    }
                    break;
            }
        }
    }

}