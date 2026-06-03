using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1.5f;

    private float nextFireTime;

    public bool TryAttack(Transform target, float attackRange)
    {
        if (target == null || Time.time < nextFireTime)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > attackRange)
        {
            return false;
        }

        Shoot();
        nextFireTime = Time.time + fireRate;
        return true;
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            return;
        }

        Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
    }
}
