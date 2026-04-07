using UnityEngine;

public enum StatType 
{ 
    MaxHealth, 
    MoveSpeed, 
    Armor, 
    DamageMultiplier,
    MaxMana,
    Evasion,
    Luck,
    PickupRadius,
    HealthRegen,
    LifeSteal
}

[CreateAssetMenu(fileName = "NewStatUpgrade", menuName = "Spellslinger/Stat Upgrade")]
public class StatUpgradeData : ScriptableObject
{
    public string upgradeName;
    [TextArea]
    public string description;
    public Sprite icon;
    
    public StatType statToIncrease;
    public float increaseAmount;
}