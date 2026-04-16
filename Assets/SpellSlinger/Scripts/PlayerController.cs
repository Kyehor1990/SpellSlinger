using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Stat Referansı")]
    public PlayerStatsManager playerStats;
    
    private Rigidbody2D rb;
    private GameInput inputActions;
    private Vector2 moveInput;
    [HideInInspector] public Vector2 lastFacingDirection = Vector2.right;
    private bool isFacingRight = true;

    private float temporarySpeedBuff = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new GameInput();
    }
    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
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
        Move();
    }

    private void Move()
    {
        float baseSpeed = playerStats != null ? playerStats.GetStat(StatType.MoveSpeed) : 5f;
        float currentSpeed = baseSpeed + temporarySpeedBuff;
        
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

    private IEnumerator SpeedBuffRoutine(float bonusSpeed, float duration)
    {
        temporarySpeedBuff += bonusSpeed;
        yield return new WaitForSeconds(duration);
        temporarySpeedBuff -= bonusSpeed;
    }
}