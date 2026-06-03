using System.Collections.Generic;
using UnityEngine;

public class ActiveSpellTimer
{
    public CompiledSpell spell;
    public float currentTimer;
}

public class PlayerAutoAttack : MonoBehaviour
{
    private const int EnemyScanBufferSize = 128;

    [Header("Referanslar")]
    public SentenceManager sentenceManager;
    public Transform firePoint;

    [Header("Tarama Ayarları")]
    public float attackRange = 10f;
    public LayerMask enemyLayer;

    [Header("Stat & Oyuncu Referansları")]
    public PlayerStatsManager playerStats;
    public PlayerHealth playerHealth;

    private List<ActiveSpellTimer> spellTimers = new List<ActiveSpellTimer>();
    private readonly Collider2D[] enemiesInRangeBuffer = new Collider2D[EnemyScanBufferSize];
    private ContactFilter2D enemyScanFilter;
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        enemyScanFilter.useTriggers = Physics2D.queriesHitTriggers;
        enemyScanFilter.SetLayerMask(enemyLayer);
    }

    private void Start()
    {
        UpdateActiveSpells();
    }

    public void UpdateActiveSpells()
    {
        spellTimers.Clear();
        List<CompiledSpell> newSpells = sentenceManager.ParseSentence();

        foreach (CompiledSpell spell in newSpells)
        {
            ActiveSpellTimer newTimer = new ActiveSpellTimer();
            newTimer.spell = spell;
            newTimer.currentTimer = spell.totalCooldown;
            spellTimers.Add(newTimer);
        }
    }

    private void Update()
    {
        if (spellTimers.Count == 0) return;

        foreach (ActiveSpellTimer timer in spellTimers)
        {
            timer.currentTimer -= Time.deltaTime;

            if (timer.currentTimer <= 0f)
            {
                TryAttackWithSpell(timer.spell);
                timer.currentTimer = timer.spell.totalCooldown;
            }
        }
    }

    private void TryAttackWithSpell(CompiledSpell spell)
    {
        int enemyCount = Physics2D.OverlapCircle(transform.position, attackRange, enemyScanFilter, enemiesInRangeBuffer);

        if (enemyCount == 0 && spell.targetingLogic != TargetType.Straight) 
            return;

        Transform target = FindTarget(enemiesInRangeBuffer, enemyCount, spell.targetingLogic);

        if (target != null || spell.targetingLogic == TargetType.Straight)
        {
            Shoot(target, spell);
        }
    }

    private Transform FindTarget(Collider2D[] enemies, int enemyCount, TargetType logic)
    {
        Transform bestTarget = null;
        switch (logic)
        {
            case TargetType.NearestEnemy:
                float closestDistanceSqr = Mathf.Infinity;
                for (int i = 0; i < enemyCount; i++)
                {
                    Collider2D enemy = enemies[i];
                    if (enemy == null) continue;

                    float distanceSqr = ((Vector2)transform.position - (Vector2)enemy.transform.position).sqrMagnitude;
                    if (distanceSqr < closestDistanceSqr) { closestDistanceSqr = distanceSqr; bestTarget = enemy.transform; }
                }
                break;
            case TargetType.LowestHealth:
                float lowestHP = Mathf.Infinity;
                for (int i = 0; i < enemyCount; i++)
                {
                    Collider2D enemyCollider = enemies[i];
                    if (enemyCollider == null || !enemyCollider.TryGetComponent(out Enemy enemyScript)) continue;

                    if (enemyScript != null && enemyScript.currentHealth < lowestHP)
                    {
                        lowestHP = enemyScript.currentHealth; bestTarget = enemyCollider.transform;
                    }
                }
                break;
            case TargetType.RandomEnemy:
                int startIndex = Random.Range(0, enemyCount);
                for (int i = 0; i < enemyCount; i++)
                {
                    Collider2D enemy = enemies[(startIndex + i) % enemyCount];
                    if (enemy != null)
                    {
                        bestTarget = enemy.transform;
                        break;
                    }
                }
                break;
            case TargetType.Straight:
                bestTarget = null;
                break;
        }
        return bestTarget;
    }

    private void Shoot(Transform target, CompiledSpell spell)
    {
        if (spell.projectilePrefab == null) return;

        Vector3 spawnPosition = firePoint.position;
        if (spell.spawnsOnTarget && target != null) spawnPosition = target.position;

        GameObject bullet = Instantiate(spell.projectilePrefab, spawnPosition, Quaternion.identity);

       Projectile bulletScript = bullet.GetComponent<Projectile>();
        if (bulletScript != null)
        {
            float multiplier = playerStats != null ? playerStats.GetStat(StatType.DamageMultiplier) : 1f;
            bulletScript.baseDamage = spell.totalDamage * multiplier;
            bulletScript.sourcePlayerHealth = this.playerHealth;
            
            bulletScript.activeMechanics = spell.specialMechanics;

            bulletScript.SetupModifiers();
        }

        if (target != null && !spell.spawnsOnTarget)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else if (spell.targetingLogic == TargetType.Straight)
        {
            Vector2 dir = playerController != null ? playerController.lastFacingDirection : Vector2.right;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
