using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Düşman Ayarları")]
    public float currentHealth = 20f;
    [SerializeField] private float moveSpeed = 2f;
    public bool isBoss = false;
    
    private float originalSpeed;
    private bool isStunned = false;
    private int rockStacks = 0;
    [HideInInspector] public float maxHealth;
    
    private Coroutine rockStackResetCoroutine;
    private Coroutine slowCoroutine; 
    private Coroutine animationCoroutine;
    
    private Transform playerTransform;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] colliders;
    private Collider2D bodyCollider;
    private Vector2 smoothedSeparationForce;
    private Vector2 cachedSeparationForce;
    private int separationFixedStep;
    private int separationUpdateOffset;
    private readonly Collider2D[] separationHits = new Collider2D[16];
    private ContactFilter2D separationFilter;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1f;
    [SerializeField] private float separationStrength = 1.4f;
    [SerializeField] private float maxSeparationForce = 1.2f;
    [SerializeField, Min(1)] private int separationUpdateInterval = 2;
    [SerializeField] private LayerMask enemyLayerMask;

    [Header("Close Range Stabilization")]
    [SerializeField, Min(0f)] private float closeRangeDeadzone = 0.2f;
    [SerializeField, Min(0f)] private float facingUpdateThreshold = 0.05f;

    [Header("Sprite Animasyonu")]
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private float frameRate = 0.15f;
    [SerializeField] private bool isAnimating = false;

    [Header("Mürekkep Ölüm Efektleri")]
    public GameObject deathSmokePrefab;
    public GameObject inkStainPrefab;
    private bool isDying = false;
    public bool IsDying => isDying;
    
    [Header("Death State")]
    private int deadEnemyLayer;

    [Header("Ganimet (Loot)")]
    public GameObject xpDropPrefab;
    public GameObject coinDropPrefab;
    public Vector2Int xpDropAmount = new Vector2Int(1, 3);
    public Vector2Int coinDropAmount = new Vector2Int(1, 2);
    public float scatterRadius = 0.6f;

    protected Transform PlayerTransform => playerTransform;
    protected Rigidbody2D Rigidbody => rb;
    protected bool CanMove => !isDying && !isStunned;
    public float FacingUpdateThreshold => facingUpdateThreshold;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        colliders = GetComponents<Collider2D>();
        bodyCollider = GetBodyCollider();
        deadEnemyLayer = LayerMask.NameToLayer("DeadEnemy");

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
        separationUpdateOffset = Mathf.Abs(GetInstanceID()) % Mathf.Max(1, separationUpdateInterval);
    }

    protected virtual void Start()
    {
        maxHealth = currentHealth;
        originalSpeed = moveSpeed;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Sprite animasyonunu başlat
        if (walkSprites != null && walkSprites.Length > 0)
        {
            StartAnimation();
        }
    }

    protected virtual void Update()
    {
        // Hareket ederken animasyonu oynat
        if (isAnimating && walkSprites != null && walkSprites.Length > 0 && rb != null && rb.linearVelocity.magnitude > 0.1f)
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

    protected virtual void FixedUpdate()
    {
        if (!CanMove)
        {
            StopMovement();
            return;
        }

        if (playerTransform != null)
        {
            Vector2 direction = TryGetDirectionToPlayer(out Vector2 chaseDirection) ? chaseDirection : Vector2.zero;
            MoveWithSeparation(direction);
        }
    }

    protected bool TryGetDirectionToPlayer(out Vector2 direction)
    {
        direction = Vector2.zero;
        if (playerTransform == null)
        {
            return false;
        }

        Vector2 toPlayer = playerTransform.position - transform.position;
        if (toPlayer.sqrMagnitude <= closeRangeDeadzone * closeRangeDeadzone)
        {
            return false;
        }

        direction = toPlayer.normalized;
        return true;
    }

    protected bool TryGetDirectionAwayFromPlayer(out Vector2 direction)
    {
        direction = Vector2.zero;
        if (playerTransform == null)
        {
            return false;
        }

        Vector2 awayFromPlayer = transform.position - playerTransform.position;
        if (awayFromPlayer.sqrMagnitude <= closeRangeDeadzone * closeRangeDeadzone)
        {
            return false;
        }

        direction = awayFromPlayer.normalized;
        return true;
    }

    protected void MoveWithSeparation(Vector2 primaryDirection)
    {
        if (rb == null)
        {
            return;
        }

        rb.linearVelocity = GetSeparatedVelocity(primaryDirection);
    }

    protected void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    protected Vector2 GetSeparatedVelocity(Vector2 primaryDirection)
    {
        if (ShouldRefreshSeparationForce())
        {
            cachedSeparationForce = CalculateSeparationForce();
        }

        Vector2 targetSeparationForce = cachedSeparationForce;
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

    private bool ShouldRefreshSeparationForce()
    {
        int interval = Mathf.Max(1, separationUpdateInterval);
        int updateOffset = separationUpdateOffset % interval;
        bool shouldRefresh = separationFixedStep == 0 || separationFixedStep % interval == updateOffset;
        separationFixedStep++;
        return shouldRefresh;
    }

    private Vector2 CalculateSeparationForce()
    {
        if (rb == null || bodyCollider == null || separationRadius <= 0f || separationStrength <= 0f || maxSeparationForce <= 0f)
        {
            return Vector2.zero;
        }

        int hitCount = Physics2D.OverlapCircle(rb.position, GetSeparationQueryRadius(), separationFilter, separationHits);
        Vector2 separation = Vector2.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = separationHits[i];
            if (hit == null || hit == bodyCollider || hit.isTrigger)
            {
                continue;
            }

            Rigidbody2D neighborBody = hit.attachedRigidbody;
            if (neighborBody == rb || HasProcessedRigidbody(neighborBody, i))
            {
                continue;
            }

            Vector2 neighborPosition = neighborBody != null ? neighborBody.position : (Vector2)hit.transform.position;
            Vector2 awayFromNeighbor = rb.position - neighborPosition;
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
        foreach (Collider2D enemyCollider in colliders)
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

public void TakeDamage(float damageAmount, bool isCritical = false, Color damageColor = default)


    {
        if (isDying) return;
        
        currentHealth -= damageAmount;
        
if (DamagePopupManager.Instance != null)
        {
Color popupColor = damageColor == default ? Color.white : damageColor;
DamagePopupManager.Instance.ShowDamage(transform.position, damageAmount, popupColor, isCritical);

        }


        if (currentHealth <= 0)
        {
            StartCoroutine(PrepareToExplode());
        }
    }

    #region Status Effects (Durum Efektleri)

    public void ApplyBurn(float totalDamageOverTime, float duration)
    {
        if (!isDying) StartCoroutine(BurnRoutine(totalDamageOverTime, duration));
    }

    private IEnumerator BurnRoutine(float totalDamage, float duration)
    {
        float ticks = duration;
        float damagePerTick = totalDamage / ticks;

        for (int i = 0; i < ticks; i++)
        {
TakeDamage(damagePerTick, false, DamagePopupManager.Instance.burnColor);
            yield return new WaitForSeconds(1f);

        }
    }

    public void ApplySlow(float slowPercentage, float duration)
    {
        if (isDying) return;

        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowRoutine(slowPercentage, duration));
    }

    private IEnumerator SlowRoutine(float slowPercentage, float duration)
    {
        moveSpeed = originalSpeed * (1f - slowPercentage);
        yield return new WaitForSeconds(duration);
        moveSpeed = originalSpeed;
        slowCoroutine = null;
    }

    public void AddRockStack()
    {
        if (isDying) return;

        rockStacks++;
        if (rockStackResetCoroutine != null) StopCoroutine(rockStackResetCoroutine);

        if (rockStacks >= 3)
        {
            float stunDuration = isBoss ? 1f : 2f;
            StartCoroutine(StunRoutine(stunDuration));
            rockStacks = 0;
        }
        else
        {
            rockStackResetCoroutine = StartCoroutine(ResetRockStacks(3f)); 
        }
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    private IEnumerator ResetRockStacks(float delay)
    {
        yield return new WaitForSeconds(delay);
        rockStacks = 0;
    }

    #endregion

    #region Death Mechanics
    
    private IEnumerator PrepareToExplode()
    {
        isDying = true;
        gameObject.layer = deadEnemyLayer;
        
        foreach (Collider2D enemyCollider in colliders)
        {
            if (enemyCollider != null) enemyCollider.enabled = false;
        }
        
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        if (rockStackResetCoroutine != null) StopCoroutine(rockStackResetCoroutine);
        
        float delay = 1.0f; 
        float timer = 0;
        Vector3 originalScale = transform.localScale;

        while (timer < delay)
        {
            timer += Time.deltaTime;
            float pulse = 1f + Mathf.PingPong(timer * 15f, 0.2f); 
            transform.localScale = originalScale * pulse;

            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(timer * 10f, 1f));

            yield return null; 
        }

        Die();
    }

    private void Die()
    {
        int xpCount = Random.Range(xpDropAmount.x, xpDropAmount.y + 1);
        ScatterDrops(xpDropPrefab, xpCount);

        int coinCount = Random.Range(coinDropAmount.x, coinDropAmount.y + 1);
        ScatterDrops(coinDropPrefab, coinCount);

        if (EffectPoolManager.instance != null)
            EffectPoolManager.instance.PlayOrganEffect(transform.position);

        if (inkStainPrefab != null)
        {
            GameObject stain = Instantiate(inkStainPrefab, transform.position, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
            Destroy(stain, 5f); 
        }

        Destroy(gameObject);
    }

    private void ScatterDrops(GameObject prefab, int count)
    {
        if (prefab == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
    
    #endregion
}
