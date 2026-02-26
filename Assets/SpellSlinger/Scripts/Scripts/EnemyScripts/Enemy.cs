using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Düşman Ayarları")]
    public float currentHealth = 20f;
    [SerializeField] private float moveSpeed = 2f;
    
    private Transform playerTransform;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Not: Bu kısım ilerde değişecek
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void FixedUpdate()
    {
        if (playerTransform != null)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // XP düşme ve diğer ölme efektleri burada
        Destroy(gameObject);
    }
}