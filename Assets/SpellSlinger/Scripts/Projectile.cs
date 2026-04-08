using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Mermi Özellikleri")]
    public float speed = 15f;
    public float baseDamage = 10f; 
    public float lifeTime = 3f;    

    [Header("Davranış Ayarları")]
    public bool destroyOnHit = true;

    [HideInInspector] public PlayerHealth sourcePlayerHealth;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (speed > 0)
        {
            transform.position += transform.right * speed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemyScript = collision.GetComponent<Enemy>();
            
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(baseDamage);
                if (sourcePlayerHealth != null)
                {
                    sourcePlayerHealth.ApplyLifeSteal(baseDamage);
                }
            }

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
}