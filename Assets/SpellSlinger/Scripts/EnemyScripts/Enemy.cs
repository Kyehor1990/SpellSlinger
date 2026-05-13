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
    private readonly Collider2D[] separationHits = new Collider2D[16];
    private ContactFilter2D separationFilter;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 0.8f;
    [SerializeField] private float separationStrength = 1.1f;
    [SerializeField] private float maxSeparationForce = 1f;
    [SerializeField] private LayerMask enemyLayerMask;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        colliders = GetComponents<Collider2D>();
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
    }

private void Start()
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

    private void Update()
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

    private void FixedUpdate()
    {
        if (isDying) 
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isStunned) 
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (playerTransform != null)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = GetSeparatedVelocity(direction);
        }
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
        if (rb == null || separationRadius <= 0f || separationStrength <= 0f || maxSeparationForce <= 0f)
        {
            return Vector2.zero;
        }

        int hitCount = Physics2D.OverlapCircle(rb.position, separationRadius, separationFilter, separationHits);
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
            if (neighborBody == rb || HasProcessedRigidbody(neighborBody, i))
            {
                continue;
            }

            Vector2 neighborPosition = neighborBody != null ? neighborBody.position : (Vector2)hit.transform.position;
            Vector2 awayFromNeighbor = rb.position - neighborPosition;
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
