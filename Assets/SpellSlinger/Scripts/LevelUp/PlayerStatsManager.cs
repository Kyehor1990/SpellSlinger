using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatTracker
{
    public StatType statType;
    public float baseValue;
    public int maxUpgradeLimit = 5;

    [HideInInspector] public int currentUpgrades = 0;
    [HideInInspector] public float currentValue;
}

public class PlayerStatsManager : MonoBehaviour
{
    [Header("Oyuncu Stat Ayarları")]
    [Tooltip("Buraya oyundaki tüm statları ekleyip başlangıç değerlerini ve max sınırlarını girin.")]
    public List<StatTracker> startingStats;

    private Dictionary<StatType, StatTracker> statDict = new Dictionary<StatType, StatTracker>();

    private void Awake()
    {
        foreach (var stat in startingStats)
        {
            stat.currentValue = stat.baseValue;
            statDict.Add(stat.statType, stat);
        }
    }

    public float GetStat(StatType type)
    {
        if (statDict.ContainsKey(type)) return statDict[type].currentValue;
        return 0f;
    }

    public bool IsStatMaxed(StatType type)
    {
        if (statDict.ContainsKey(type))
        {
            return statDict[type].currentUpgrades >= statDict[type].maxUpgradeLimit;
        }
        return false;
    }

    public void ApplyUpgrade(StatType type, float amount)
    {
        if (statDict.ContainsKey(type))
        {
            statDict[type].currentValue += amount;
            statDict[type].currentUpgrades++;
            Debug.Log($"{type} yükseltildi! Yeni Değer: {statDict[type].currentValue} (Seviye: {statDict[type].currentUpgrades}/{statDict[type].maxUpgradeLimit})");
        }
    }
}