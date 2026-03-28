using UnityEngine;

public class CoinDrop : MonoBehaviour
{
    public int coinValue = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerWallet wallet = collision.GetComponent<PlayerWallet>();
            if (wallet != null)
            {
                wallet.AddMoney(coinValue);
                Destroy(gameObject);
            }
        }
    }
}