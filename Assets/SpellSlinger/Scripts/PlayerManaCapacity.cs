using UnityEngine;
using TMPro;

public class PlayerManaCapacity : MonoBehaviour
{
    public int currentUsedMana = 0;

    [Header("UI Referansı")]
    public TextMeshProUGUI capacityTextUI;

    [Header("Stat Referansı")]
    public PlayerStatsManager playerStats;

    private void Start()
    {
        UpdateCapacityUI();
    }

    public bool CanEquipWord(int cost)
    {
        int maxMana = (int)playerStats.GetStat(StatType.MaxMana);

        if (currentUsedMana + cost <= maxMana)
        {
            return true;
        }
        else
        {
            Debug.Log("<color=red>Yetersiz Mana Kapasitesi! Bu kelimeyi takamazsın.</color>");
            return false;
        }
    }

    public void EquipWord(int cost)
    {
        currentUsedMana += cost;
        UpdateCapacityUI();
    }

    public void UnequipWord(int cost)
    {
        currentUsedMana -= cost;
        if (currentUsedMana < 0) currentUsedMana = 0; 
        
        UpdateCapacityUI();
    }

    public void IncreaseMaxMana(int amount)
    {
        UpdateCapacityUI();
        Debug.Log($"<color=cyan>Maksimum Mana Artırıldı! Yeni Kapasite: {playerStats.GetStat(StatType.MaxMana)}</color>");
    }

    private void UpdateCapacityUI()
    {
        if (capacityTextUI != null && playerStats != null)
        {
            int currentMaxMana = (int)playerStats.GetStat(StatType.MaxMana);
            capacityTextUI.text = $"Mana: {currentUsedMana} / {currentMaxMana}";
        }
    }
}