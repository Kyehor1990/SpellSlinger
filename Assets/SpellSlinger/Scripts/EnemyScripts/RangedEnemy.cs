using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Movement")]
    [SerializeField] private float stopDistance = 5f;
    [SerializeField] private float retreatDistance = 3f;

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

        if (distanceToPlayer < retreatDistance)
        {
            Vector2 direction = (transform.position - PlayerTransform.position).normalized;
            MoveWithSeparation(direction);
        }
        else if (distanceToPlayer > stopDistance)
        {
            Vector2 direction = (PlayerTransform.position - transform.position).normalized;
            MoveWithSeparation(direction);
        }
        else
        {
            MoveWithSeparation(Vector2.zero);
        }

        FlipTowardsPlayer();
        enemyAttack?.TryAttack(PlayerTransform);
    }

    private void FlipTowardsPlayer()
    {
        if (PlayerTransform.position.x > transform.position.x)
        {
            transform.localScale = initialScale;
        }
        else
        {
            transform.localScale = new Vector3(-initialScale.x, initialScale.y, initialScale.z);
        }
    }
}
