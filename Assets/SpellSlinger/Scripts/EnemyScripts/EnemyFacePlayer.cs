using UnityEngine;

public class EnemyFacePlayer : MonoBehaviour
{
    private Transform player;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    void FixedUpdate()
    {
        if (player == null || spriteRenderer == null) return;
        
  
        spriteRenderer.flipX = player.position.x > transform.position.x;
    }
}