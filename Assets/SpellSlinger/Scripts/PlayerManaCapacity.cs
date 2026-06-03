using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerManaCapacity : MonoBehaviour
{
    public int currentUsedMana = 0;

    [Header("UI Referansi")]
    [FormerlySerializedAs("capacityTextUI")]
    [SerializeField] private TMP_Text manaText;

    [Header("Stat Referansi")]
    public PlayerStatsManager playerStats;

    private void Start()
    {
        RefreshManaUI();
    }

    public bool CanEquipWord(int cost)
    {
        int maxMana = GetMaxMana();

        if (currentUsedMana + cost <= maxMana)
        {
            return true;
        }
        else
        {
            Debug.Log("<color=red>Yetersiz Mana Kapasitesi! Bu kelimeyi takamazsin.</color>");
            return false;
        }
    }

    public void EquipWord(int cost)
    {
        currentUsedMana += cost;
        RefreshManaUI();
    }

    public void UnequipWord(int cost)
    {
        currentUsedMana -= cost;
        if (currentUsedMana < 0) currentUsedMana = 0;

        RefreshManaUI();
    }

    public void SetUsedMana(int usedMana)
    {
        currentUsedMana = Mathf.Max(0, usedMana);
        RefreshManaUI();
    }

    public void IncreaseMaxMana(int amount)
    {
        RefreshManaUI();
        Debug.Log($"<color=cyan>Maksimum Mana Artirildi! Yeni Kapasite: {GetMaxMana()}</color>");
    }

    public void RefreshManaUI()
    {
        if (manaText == null) return;

        manaText.text = $"{currentUsedMana}/{GetMaxMana()}";
    }

    private int GetMaxMana()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStatsManager>();
        }

        return playerStats != null ? (int)playerStats.GetStat(StatType.MaxMana) : 0;
    }
}
