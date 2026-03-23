using UnityEngine;

public class InspirationDrop : MonoBehaviour
{
    [Header("Özellikler")]
    public int xpAmount = 10;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerExperience playerXp = collision.GetComponent<PlayerExperience>();
            
            if (playerXp != null)
            {
                playerXp.AddExperience(xpAmount);
                Destroy(gameObject);
            }
        }
    }
}