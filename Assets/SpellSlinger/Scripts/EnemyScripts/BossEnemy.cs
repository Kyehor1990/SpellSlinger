using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class BossEnemy : Enemy
{
    private enum DashState
    {
        Chase,
        Windup,
        Dash,
        Recovery
    }

    [Header("Boss Dash")]
    [SerializeField, Min(0f)] private float dashRange = 5f;
    [SerializeField, Min(0f)] private float dashCooldown = 4f;
    [SerializeField, Min(0f)] private float dashWindupTime = 0.6f;
    [SerializeField, Min(0f)] private float dashSpeed = 10f;
    [SerializeField, Min(0f)] private float dashDuration = 0.35f;
    [SerializeField, Min(0f)] private float dashRecoveryTime = 0.4f;

    private DashState dashState = DashState.Chase;
    private Vector2 dashDirection = Vector2.right;
    private float stateTimer;
    private float nextDashTime;
    private bool warnedMissingPlayer;

    protected override void Start()
    {
        base.Start();
        isBoss = true;
    }

    private void OnEnable()
    {
        dashState = DashState.Chase;
        dashDirection = Vector2.right;
        stateTimer = 0f;
        nextDashTime = 0f;
        warnedMissingPlayer = false;
    }

    protected override void FixedUpdate()
    {
        if (!CanMove)
        {
            InterruptDash();
            StopMovement();
            return;
        }

        if (PlayerTransform == null)
        {
            WarnMissingPlayerOnce();
            StopMovement();
            return;
        }

        switch (dashState)
        {
            case DashState.Chase:
                UpdateChase();
                break;
            case DashState.Windup:
                UpdateWindup();
                break;
            case DashState.Dash:
                UpdateDash();
                break;
            case DashState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    private void UpdateChase()
    {
        if (IsDashReady() && IsPlayerInDashRange())
        {
            EnterWindup();
            return;
        }

        Vector2 direction = TryGetDirectionToPlayer(out Vector2 chaseDirection) ? chaseDirection : Vector2.zero;
        MoveWithSeparation(direction);
    }

    private void EnterWindup()
    {
        dashState = DashState.Windup;
        stateTimer = dashWindupTime;
        nextDashTime = Time.time + dashCooldown;
        dashDirection = GetDashDirectionToPlayer();
        StopMovement();

        if (stateTimer <= 0f)
        {
            EnterDash();
        }
    }

    private void UpdateWindup()
    {
        StopMovement();
        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
        {
            EnterDash();
        }
    }

    private void EnterDash()
    {
        dashState = DashState.Dash;
        stateTimer = dashDuration;
    }

    private void UpdateDash()
    {
        if (Rigidbody != null)
        {
            Rigidbody.linearVelocity = dashDirection * dashSpeed;
        }

        stateTimer -= Time.fixedDeltaTime;
        if (stateTimer <= 0f)
        {
            EnterRecovery();
        }
    }

    private void EnterRecovery()
    {
        dashState = DashState.Recovery;
        stateTimer = dashRecoveryTime;
        StopMovement();

        if (stateTimer <= 0f)
        {
            dashState = DashState.Chase;
        }
    }

    private void UpdateRecovery()
    {
        StopMovement();
        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f)
        {
            dashState = DashState.Chase;
        }
    }

    private bool IsDashReady()
    {
        return dashRange > 0f
            && dashSpeed > 0f
            && dashDuration > 0f
            && Time.time >= nextDashTime;
    }

    private bool IsPlayerInDashRange()
    {
        float sqrDashRange = dashRange * dashRange;
        return ((Vector2)PlayerTransform.position - (Vector2)transform.position).sqrMagnitude <= sqrDashRange;
    }

    private Vector2 GetDashDirectionToPlayer()
    {
        Vector2 toPlayer = (Vector2)PlayerTransform.position - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            return toPlayer.normalized;
        }

        if (Rigidbody != null && Rigidbody.linearVelocity.sqrMagnitude > 0.0001f)
        {
            return Rigidbody.linearVelocity.normalized;
        }

        return dashDirection.sqrMagnitude > 0.0001f ? dashDirection.normalized : Vector2.right;
    }

    private void InterruptDash()
    {
        if (dashState == DashState.Chase)
        {
            return;
        }

        dashState = DashState.Chase;
        stateTimer = 0f;
    }

    private void WarnMissingPlayerOnce()
    {
        if (warnedMissingPlayer)
        {
            return;
        }

        Debug.LogWarning($"{nameof(BossEnemy)} on {name} could not find a Player target. Boss movement is stopped until a Player tagged object exists.", this);
        warnedMissingPlayer = true;
    }

    private void OnValidate()
    {
        dashRange = Mathf.Max(0f, dashRange);
        dashCooldown = Mathf.Max(0f, dashCooldown);
        dashWindupTime = Mathf.Max(0f, dashWindupTime);
        dashSpeed = Mathf.Max(0f, dashSpeed);
        dashDuration = Mathf.Max(0f, dashDuration);
        dashRecoveryTime = Mathf.Max(0f, dashRecoveryTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dashRange);
    }
}
