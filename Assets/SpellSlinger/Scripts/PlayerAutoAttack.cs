using System.Collections.Generic;
using UnityEngine;

public class ActiveSpellTimer
{
    public CompiledSpell spell;
    public float currentTimer;
}

public class PlayerAutoAttack : MonoBehaviour
{
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
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        if (enemiesInRange.Length == 0 && spell.targetingLogic != TargetType.Straight) 
            return;

        Transform target = FindTarget(enemiesInRange, spell.targetingLogic);

        if (target != null || spell.targetingLogic == TargetType.Straight)
        {
            Shoot(target, spell);
        }
    }

    private Transform FindTarget(Collider2D[] enemies, TargetType logic)
    {
        Transform bestTarget = null;
        switch (logic)
        {
            case TargetType.NearestEnemy:
                float closestDistance = Mathf.Infinity;
                foreach (Collider2D enemy in enemies)
                {
                    float distance = Vector2.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance) { closestDistance = distance; bestTarget = enemy.transform; }
                }
                break;
            case TargetType.LowestHealth:
                float lowestHP = Mathf.Infinity;
                foreach (Collider2D enemyCollider in enemies)
                {
                    Enemy enemyScript = enemyCollider.GetComponent<Enemy>();
                    if (enemyScript != null && enemyScript.currentHealth < lowestHP)
                    {
                        lowestHP = enemyScript.currentHealth; bestTarget = enemyCollider.transform;
                    }
                }
                break;
            case TargetType.RandomEnemy:
                bestTarget = enemies[Random.Range(0, enemies.Length)].transform;
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
            Vector2 dir = GetComponent<PlayerController>().lastFacingDirection;
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