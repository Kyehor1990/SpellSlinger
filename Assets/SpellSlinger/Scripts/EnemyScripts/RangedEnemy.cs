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
    private Collider2D bodyCollider;
    private Vector2 smoothedSeparationForce;
    private readonly Collider2D[] separationHits = new Collider2D[16];
    private ContactFilter2D separationFilter;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1f;
    [SerializeField] private float separationStrength = 1.4f;
    [SerializeField] private float maxSeparationForce = 1.2f;
    [SerializeField] private LayerMask enemyLayerMask;

    private void Awake()
    {
        // Rigidbody eriştik
        _rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetBodyCollider();

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
        Vector2 targetSeparationForce = CalculateSeparationForce();
        float smoothing = 1f - Mathf.Exp(-12f * Time.fixedDeltaTime);
        smoothedSeparationForce = Vector2.Lerp(smoothedSeparationForce, targetSeparationForce, smoothing);

        Vector2 desiredDirection = primaryDirection + smoothedSeparationForce;
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
        if (_rb == null || bodyCollider == null || separationRadius <= 0f || separationStrength <= 0f || maxSeparationForce <= 0f)
        {
            return Vector2.zero;
        }

        int hitCount = Physics2D.OverlapCircle(_rb.position, GetSeparationQueryRadius(), separationFilter, separationHits);
        Vector2 separation = Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = separationHits[i];
            if (hit == null || hit == bodyCollider || hit.isTrigger)
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
            float centerDistance = awayFromNeighbor.magnitude;

            if (centerDistance < 0.0001f)
            {
                awayFromNeighbor = GetFallbackSeparationDirection();
            }
            else
            {
                awayFromNeighbor /= centerDistance;
            }

            float separationDistance = centerDistance;
            ColliderDistance2D colliderDistance = bodyCollider.Distance(hit);
            if (colliderDistance.isValid)
            {
                separationDistance = Mathf.Max(colliderDistance.distance, 0f);
            }

            if (separationDistance > separationRadius)
            {
                continue;
            }

            float weight = 1f - (separationDistance / separationRadius);
            separation += awayFromNeighbor * weight * weight;
        }

        return Vector2.ClampMagnitude(separation * separationStrength, maxSeparationForce);
    }

    private Collider2D GetBodyCollider()
    {
        Collider2D[] enemyColliders = GetComponents<Collider2D>();
        foreach (Collider2D enemyCollider in enemyColliders)
        {
            if (enemyCollider != null && enemyCollider.enabled && !enemyCollider.isTrigger)
            {
                return enemyCollider;
            }
        }

        return null;
    }

    private float GetSeparationQueryRadius()
    {
        Bounds bodyBounds = bodyCollider.bounds;
        return separationRadius + Mathf.Max(bodyBounds.extents.x, bodyBounds.extents.y);
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
