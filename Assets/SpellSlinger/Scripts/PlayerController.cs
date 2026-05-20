using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Stat Referansı")]
    public PlayerStatsManager playerStats;

    [Header("Acceleration Buff")]
    [SerializeField] private float accelerationBuffPercent = 0.30f;
    [SerializeField] private float accelerationDuration = 3f;
    
    private Rigidbody2D rb;
    private GameInput inputActions;
    private Vector2 moveInput;
    [HideInInspector] public Vector2 lastFacingDirection = Vector2.right;
    private bool isFacingRight = true;

    private float temporarySpeedBuff = 0f;
    private Coroutine accelerationRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new GameInput();
    }
    private void OnEnable() => inputActions.Enable();
    private void OnDisable()
    {
        inputActions.Disable();
        RemoveAccelerationBuff();
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        if (moveInput != Vector2.zero)
        {
            lastFacingDirection = moveInput.normalized;
        }

        if (moveInput.x > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput.x < 0 && isFacingRight)
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Move();
    }

    private void Move()
    {
        float baseSpeed = playerStats != null ? playerStats.GetStat(StatType.MoveSpeed) : 5f;
        float normalSpeed = baseSpeed + temporarySpeedBuff;
        float currentSpeed = normalSpeed * GetAccelerationMultiplier();
        
        rb.linearVelocity = moveInput.normalized * currentSpeed;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    public void ApplySpeedBuff(float bonusSpeed, float duration)
    {
        StartCoroutine(SpeedBuffRoutine(bonusSpeed, duration));
    }

    public void ApplyAccelerationBuff()
    {
        ApplyAccelerationBuff(accelerationDuration);
    }

    public void ApplyAccelerationBuff(float duration)
    {
        if (duration <= 0f) return;

        if (accelerationRoutine != null)
        {
            StopCoroutine(accelerationRoutine);
        }

        accelerationRoutine = StartCoroutine(AccelerationBuffRoutine(duration));
    }

    public void RemoveAccelerationBuff()
    {
        if (accelerationRoutine != null)
        {
            StopCoroutine(accelerationRoutine);
            accelerationRoutine = null;
        }
    }

    private float GetAccelerationMultiplier()
    {
        return accelerationRoutine != null ? 1f + accelerationBuffPercent : 1f;
    }

    private IEnumerator AccelerationBuffRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        accelerationRoutine = null;
    }

    private IEnumerator SpeedBuffRoutine(float bonusSpeed, float duration)
    {
        temporarySpeedBuff += bonusSpeed;
        yield return new WaitForSeconds(duration);
        temporarySpeedBuff -= bonusSpeed;
    }
}
