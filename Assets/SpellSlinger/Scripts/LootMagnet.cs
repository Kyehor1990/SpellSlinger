using UnityEngine;

public class LootMagnet : MonoBehaviour
{
    public PlayerStatsManager playerStats;
    private CircleCollider2D magnetCollider;
    
    [Header("Ayarlar")]
    public float baseRadius = 2f;

    private void Awake()
    {
        magnetCollider = GetComponent<CircleCollider2D>();
    }

    private void Update()
    {
        if (playerStats != null)
        {
            magnetCollider.radius = baseRadius + playerStats.GetStat(StatType.PickupRadius);
        }
    }
}