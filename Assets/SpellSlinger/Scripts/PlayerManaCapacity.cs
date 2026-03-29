using UnityEngine;
using TMPro;

public class PlayerManaCapacity : MonoBehaviour
{
    [Header("Mana Kapasitesi")]
    public int maxMana = 10;
    public int currentUsedMana = 0;

    [Header("UI Referansı")]
    public TextMeshProUGUI capacityTextUI;

    private void Start()
    {
        UpdateCapacityUI();
    }

    public bool CanEquipWord(int cost)
    {
        if (currentUsedMana + cost <= maxMana)
        {
            return true; // Kapasite yeterli, takabilirsin!
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
        maxMana += amount;
        UpdateCapacityUI();
        Debug.Log($"<color=cyan>Maksimum Mana Artırıldı! Yeni Kapasite: {maxMana}</color>");
    }

    private void UpdateCapacityUI()
    {
        if (capacityTextUI != null)
        {
            capacityTextUI.text = $"Mana: {currentUsedMana} / {maxMana}";
        }
    }
}