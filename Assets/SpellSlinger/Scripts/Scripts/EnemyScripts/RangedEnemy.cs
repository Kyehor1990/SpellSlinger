using UnityEngine;

public class RangedEnemy : MonoBehaviour
// Bu kod Ranged Enemynin davranışları için yazılmıştır -96
{
    [Header("Hareket Ayarları")]
    // Enemynin normal yürüme hızı
    [SerializeField] private float moveSpeed = 2f;
    // Enemynin durup ateş etmeye başlayacağı mesafe
    [SerializeField] private float stopDistance = 5f;
    // Oyuncunun ne kadar yaklaştığına bağlı kaçmaya başlayacağı mesafe
    [SerializeField] private float retreatDistance = 3f;
    
    
    [Header("Saldırı Ayarları")]
    // Enemynin atacağı mermi prefabı
    [SerializeField] private GameObject bulletPrefab;
    // Merminin çıktığı nokta
    [SerializeField] private Transform firePoint;
    // Kaç saniyede ateş ediceğinin belirlendiği yer
    [SerializeField] private float fireRate = 1.5f;      
    private float _nextFireTime;
    
    
    [Header("İstatistikler")]
    // Enemy Canı
    [SerializeField] private float currentHealth = 15f;
    
    private Transform _playerTransform;
    private Rigidbody2D _rb;
    
    private void Awake()
    {
        // Rigidbody eriştik
        _rb = GetComponent<Rigidbody2D>();
    }
    
    private void Start()
    {
        // Oyuncuyu Tag ile bulduk ve transformunu bulduk
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }
    
    private void FixedUpdate()
    {
        if (_playerTransform == null) return;
        // Oyuncu ile arasında ki mesafeyi belirle
        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer < retreatDistance)
        {
           // Oyuncudan kaçmaya başladığı yer
            Vector2 direction = (transform.position - _playerTransform.position).normalized;
            Move(direction);
        }
        else if (distanceToPlayer > stopDistance)
        {
           // Oyuncudan uzak kaldığı için yaklaşır
            Vector2 direction = (_playerTransform.position - transform.position).normalized;
            Move(direction);
        }
        else
        {
           // Her şey okeyse ateş etmeye başlayacağı için yerinde durur
            _rb.linearVelocity = Vector2.zero;
        }

        // Bize doğru bakması için
        FlipTowardsPlayer();
    }
    
    private void Update()
    {
        
    }
    private void Move(Vector2 direction)
    {
        // Hareket et
        _rb.linearVelocity = direction * moveSpeed;
    }
    private void FlipTowardsPlayer()
    {
        // Her zaman oyuncuya bakması için
        if (_playerTransform.position.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        // Ölünce ne olucak her şey buraya
        Destroy(gameObject);
    }
}
