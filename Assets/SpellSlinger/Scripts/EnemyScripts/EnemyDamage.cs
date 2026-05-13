using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField, Min(0)] private int damageAmount = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }
}
