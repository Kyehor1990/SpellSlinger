using UnityEngine;

public class EnemyFacePlayer : MonoBehaviour
{
    private Enemy enemy;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void FixedUpdate()
    {
        if (rb == null || spriteRenderer == null)
        {
            return;
        }

        float facingUpdateThreshold = enemy != null ? enemy.FacingUpdateThreshold : 0.05f;
        if (Mathf.Abs(rb.linearVelocity.x) <= facingUpdateThreshold)
        {
            return;
        }

        spriteRenderer.flipX = rb.linearVelocity.x > 0f;
    }
}
