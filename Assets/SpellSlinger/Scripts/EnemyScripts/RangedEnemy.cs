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
    private readonly Collider2D[] separationHits = new Collider2D[16];
    private ContactFilter2D separationFilter;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 0.8f;
    [SerializeField] private float separationStrength = 1.1f;
    [SerializeField] private float maxSeparationForce = 1f;
    [SerializeField] private LayerMask enemyLayerMask;

    private void Awake()
    {
        // Rigidbody eriştik
        _rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemyLayerMask.value == 0)
        {
            enemyLayerMask = LayerMask.GetMask("EnemyBody");
            if (enemyLayerMask.value == 0)
            {
                enemyLayerMask = LayerMask.GetMask("Enemy");
            }
        }

        separationFilter.useTriggers = false;
        separationFilter.SetLayerMask(enemyLayerMask);
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
            _rb.linearVelocity = GetSeparatedVelocity(Vector2.zero);
        }

        // Bize doğru bakması için
        FlipTowardsPlayer();
    }
    
  
    private void Move(Vector2 direction)
    {
        // Hareket et
        _rb.linearVelocity = GetSeparatedVelocity(direction);
    }

    private Vector2 GetSeparatedVelocity(Vector2 primaryDirection)
    {
        Vector2 desiredDirection = primaryDirection + CalculateSeparationForce();
        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            return Vector2.zero;
        }

        if (desiredDirection.sqrMagnitude > 1f)
        {
            desiredDirection.Normalize();
        }

        return desiredDirection * moveSpeed;
    }

    private Vector2 CalculateSeparationForce()
    {
        if (_rb == null || separationRadius <= 0f || separationStrength <= 0f || maxSeparationForce <= 0f)
        {
            return Vector2.zero;
        }

        int hitCount = Physics2D.OverlapCircle(_rb.position, separationRadius, separationFilter, separationHits);
        Vector2 separation = Vector2.zero;
        int neighborCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = separationHits[i];
            if (hit == null)
            {
                continue;
            }

            Rigidbody2D neighborBody = hit.attachedRigidbody;
            if (neighborBody == _rb || HasProcessedRigidbody(neighborBody, i))
            {
                continue;
            }

            Vector2 neighborPosition = neighborBody != null ? neighborBody.position : (Vector2)hit.transform.position;
            Vector2 awayFromNeighbor = _rb.position - neighborPosition;
            float distanceSqr = awayFromNeighbor.sqrMagnitude;

            if (distanceSqr < 0.0001f)
            {
                awayFromNeighbor = GetFallbackSeparationDirection();
                distanceSqr = 0.0001f;
            }

            float distance = Mathf.Sqrt(distanceSqr);
            if (distance > separationRadius)
            {
                continue;
            }

            float weight = 1f - (distance / separationRadius);
            separation += awayFromNeighbor / distance * weight;
            neighborCount++;
        }

        if (neighborCount == 0)
        {
            return Vector2.zero;
        }

        separation = separation / neighborCount * separationStrength;
        return Vector2.ClampMagnitude(separation, maxSeparationForce);
    }

    private bool HasProcessedRigidbody(Rigidbody2D candidate, int currentIndex)
    {
        if (candidate == null)
        {
            return false;
        }

        for (int i = 0; i < currentIndex; i++)
        {
            Collider2D previousHit = separationHits[i];
            if (previousHit != null && previousHit.attachedRigidbody == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetFallbackSeparationDirection()
    {
        float angle = (Mathf.Abs(gameObject.GetInstanceID()) % 360) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
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
