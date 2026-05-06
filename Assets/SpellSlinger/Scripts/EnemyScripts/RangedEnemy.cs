using UnityEngine;
using System.Collections;

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
    
    [Header("İstatistikler")]
    // Enemy Canı
    [SerializeField] private float currentHealth = 15f;

    [Header("Sprite Animasyonu")]
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private float frameRate = 0.15f;
    [SerializeField] private bool isAnimating = false;
    private Coroutine animationCoroutine;
    private SpriteRenderer spriteRenderer;
    
    private Transform _playerTransform;
    private Rigidbody2D _rb;
    private Vector3 _initialScale;
    private void Awake()
    {
        // Rigidbody eriştik
        _rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
private void Start()
    {
        _initialScale = transform.localScale;
        // Oyuncuyu Tag ile bulduk ve transformunu bulduk
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;

        // Sprite animasyonunu başlat
        if (walkSprites != null && walkSprites.Length > 0)
        {
            StartAnimation();
        }
    }

    private void Update()
    {
        // Hareket ederken animasyonu oynat
        if (isAnimating && walkSprites != null && walkSprites.Length > 0 && _rb != null && _rb.linearVelocity.magnitude > 0.1f)
        {
            if (animationCoroutine == null)
            {
                animationCoroutine = StartCoroutine(PlayWalkAnimation());
            }
        }
    }

    private void StartAnimation()
    {
        isAnimating = true;
        if (spriteRenderer != null && walkSprites.Length > 0)
        {
            spriteRenderer.sprite = walkSprites[0];
        }
    }

    private IEnumerator PlayWalkAnimation()
    {
        int currentFrame = 0;
        while (isAnimating && walkSprites.Length > 0)
        {
            if (spriteRenderer != null && currentFrame < walkSprites.Length)
            {
                spriteRenderer.sprite = walkSprites[currentFrame];
            }
            
            yield return new WaitForSeconds(frameRate);
            
            currentFrame++;
            if (currentFrame >= walkSprites.Length)
            {
                currentFrame = 0;
            }
        }
        animationCoroutine = null;
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
    
  
    private void Move(Vector2 direction)
    {
        // Hareket et
        _rb.linearVelocity = direction * moveSpeed;
    }
    private void FlipTowardsPlayer()
    {
        // Her zaman oyuncuya dönsün diye yazıldı ( Eskiden sabit bir scale di artık Kendi girdiğimiz scale i tutuyor)
        if (_playerTransform.position.x > transform.position.x)
        {
            transform.localScale = _initialScale;
        }
        
        else
        {
            transform.localScale = new Vector3(-_initialScale.x, _initialScale.y, _initialScale.z);
        }
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
