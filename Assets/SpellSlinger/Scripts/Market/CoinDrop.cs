using UnityEngine;

public class CoinDrop : MonoBehaviour
{
    private Transform target;
    private bool isFollowing;
    public float moveSpeed = 8f;

    [Header("Coin Özellikleri")]
    public int coinValue = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
       
        if (other.CompareTag("Player"))
        {
            target = other.transform.root;
            isFollowing = true;
        }
    }

    private void Update()
    {
        if (isFollowing && target != null)
        {
            moveSpeed += 0.5f;
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

           
            float sqrDistance = (transform.position - target.position).sqrMagnitude;
            if (sqrDistance < 0.01f)
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
      
        if (target != null && target.TryGetComponent(out PlayerWallet wallet)) 
        {
            wallet.AddMoney(coinValue);
        }
       
        Destroy(gameObject);
    }
}