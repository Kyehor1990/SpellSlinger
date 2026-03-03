using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float lifeTime = 3f; 

    private void Start()
    {
       
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            GetComponent<Rigidbody2D>().linearVelocity = direction * speed;
        }

        
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
       
    }
  
   
}
