using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Düşman Ayarları")]
    public float currentHealth = 20f;
    [SerializeField] private float moveSpeed = 2f;
    
    private Transform playerTransform;
    private Rigidbody2D rb;

    [Header("Mürekkep Ölüm Efektleri")]
    public GameObject deathSmokePrefab;
    public GameObject inkStainPrefab;

    [Header("Ganimet (Loot)")]
    public GameObject xpDropPrefab;
    public GameObject coinDropPrefab;

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
        if (xpDropPrefab != null)
        {
            Instantiate(xpDropPrefab, transform.position, Quaternion.identity);
        }

        if (coinDropPrefab != null)
        {
            Instantiate(coinDropPrefab, transform.position, Quaternion.identity);
        }

        if (deathSmokePrefab != null) Instantiate(deathSmokePrefab, transform.position, Quaternion.identity);

        if (inkStainPrefab != null)
        {
            GameObject stain = Instantiate(inkStainPrefab, transform.position, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
            Destroy(stain, 5f); 
        }

        Destroy(gameObject);
    }
}