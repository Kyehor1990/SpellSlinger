using UnityEngine;

public class PlayerAutoAttack : MonoBehaviour
{
    [Header("Büyü Ayarları")]
    public WordData activeWord; 
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Tarama Ayarları")]
    public float attackRange = 10f;    
    public LayerMask enemyLayer;

    private float cooldownTimer;

    private void Update()
    {
        if (activeWord == null) return;

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer <= 0f)
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);

        if (enemiesInRange.Length == 0 && activeWord.targetingLogic != TargetType.Straight) 
            return;

        Transform target = FindTarget(enemiesInRange);

        if (target != null || activeWord.targetingLogic == TargetType.Straight)
        {
            Shoot(target);
            cooldownTimer = activeWord.baseCooldown; 
        }
    }

    private Transform FindTarget(Collider2D[] enemies)
    {
        Transform bestTarget = null;

        switch (activeWord.targetingLogic)
        {
            case TargetType.NearestEnemy:
                float closestDistance = Mathf.Infinity;
                foreach (Collider2D enemy in enemies)
                {
                    float distanceToEnemy = Vector2.Distance(transform.position, enemy.transform.position);
                    if (distanceToEnemy < closestDistance)
                    {
                        closestDistance = distanceToEnemy;
                        bestTarget = enemy.transform;
                    }
                }
                break;

            case TargetType.LowestHealth:
                float lowestHP = Mathf.Infinity;
                foreach (Collider2D enemyCollider in enemies)
                {
                    Enemy enemyScript = enemyCollider.GetComponent<Enemy>();
                    if (enemyScript != null && enemyScript.currentHealth < lowestHP)
                    {
                        lowestHP = enemyScript.currentHealth;
                        bestTarget = enemyCollider.transform;
                    }
                }
                break;

            case TargetType.RandomEnemy:
                int randomIndex = Random.Range(0, enemies.Length);
                bestTarget = enemies[randomIndex].transform;
                break;

            case TargetType.Straight:
                bestTarget = null;
                break;
        }

        return bestTarget;
    }

private void Shoot(Transform target)
    {
        if (activeWord.projectilePrefab == null) return;

        Vector3 spawnPosition = firePoint.position;

        if (activeWord.spawnsOnTarget && target != null)
        {
            spawnPosition = target.position;
        }

        GameObject bullet = Instantiate(activeWord.projectilePrefab, spawnPosition, Quaternion.identity);

        if (target != null && !activeWord.spawnsOnTarget)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else if (activeWord.targetingLogic == TargetType.Straight)
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