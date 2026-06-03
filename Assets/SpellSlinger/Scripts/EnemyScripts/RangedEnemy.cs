using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Movement")]
    [SerializeField] private float stopDistance = 5f;
    [SerializeField] private float retreatDistance = 3f;

    [Header("Ranged Attack")]
    [SerializeField] private float attackRange = 5.5f;

    private EnemyAttack enemyAttack;
    private Vector3 initialScale;

    protected override void Awake()
    {
        base.Awake();
        enemyAttack = GetComponent<EnemyAttack>();
        initialScale = transform.localScale;
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void FixedUpdate()
    {
        if (!CanMove)
        {
            StopMovement();
            return;
        }

        if (PlayerTransform == null)
        {
            StopMovement();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, PlayerTransform.position);
        float effectiveStopDistance = Mathf.Min(stopDistance, attackRange);
        float effectiveRetreatDistance = Mathf.Min(retreatDistance, effectiveStopDistance);

        if (distanceToPlayer < effectiveRetreatDistance)
        {
            Vector2 direction = TryGetDirectionAwayFromPlayer(out Vector2 retreatDirection) ? retreatDirection : Vector2.zero;
            MoveWithSeparation(direction);
        }
        else if (distanceToPlayer > effectiveStopDistance)
        {
            Vector2 direction = TryGetDirectionToPlayer(out Vector2 approachDirection) ? approachDirection : Vector2.zero;
            MoveWithSeparation(direction);
        }
        else
        {
            MoveWithSeparation(Vector2.zero);
        }

        FlipTowardsPlayer();
        enemyAttack?.TryAttack(PlayerTransform, attackRange);
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0f, attackRange);
        stopDistance = Mathf.Max(0f, stopDistance);
        retreatDistance = Mathf.Max(0f, retreatDistance);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void FlipTowardsPlayer()
    {
        if (Rigidbody == null || Mathf.Abs(Rigidbody.linearVelocity.x) <= FacingUpdateThreshold)
        {
            return;
        }

        if (Rigidbody.linearVelocity.x > 0f)
        {
            transform.localScale = initialScale;
        }
        else
        {
            transform.localScale = new Vector3(-initialScale.x, initialScale.y, initialScale.z);
        }
    }
}
