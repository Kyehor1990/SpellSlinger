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
    
    private Transform playerTransform;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

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
        col = GetComponent<Collider2D>();
        deadEnemyLayer = LayerMask.NameToLayer("DeadEnemy");
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
            rb.linearVelocity = direction * moveSpeed;
        }
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
        
        if (col != null) col.enabled = false;
        
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