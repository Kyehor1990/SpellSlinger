using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WordLevelStats
{
    [Min(1)] public int level = 1;

    [Header("Object")]
    [Min(0f)] public float damage = 10f;
    [Min(1)] public int projectileCount = 1;

    [Header("Modifier")]
    [Min(0f)] public float cooldownReduction = 0f;
    [Min(0)] public int bounceCount = 2;
    [Min(0)] public int splitProjectileCount = 2;
    [Min(0)] public int pierceCount = 4;
    [Range(0f, 1f)] public float pierceDamageMultiplier = 0.5f;
    [Min(0f)] public float executionDamageMultiplier = 10f;
    [Min(0f)] public float accelerationDuration = 3f;
    [Min(0f)] public float damageBonusMultiplier = 1.1f;

    [Header("Element Effects")]
    [Min(0f)] public float burnDamage = 6f;
    [Min(0f)] public float burnDuration = 3f;
    [Range(0f, 1f)] public float waterSlowPercent = 0.4f;
    [Min(0f)] public float waterSlowDuration = 1.5f;
    [Range(0f, 1f)] public float iceSlowPercent = 0.6f;
    [Min(0f)] public float iceSlowDuration = 2f;
    [Min(0f)] public float iceExplosionRadius = 3f;
    [Min(0f)] public float lightningChainRadius = 5f;
    [Min(0)] public int lightningChainTargets = 3;
    [Min(0f)] public float lightningChainDamageMultiplier = 0.3333333f;
    [Range(0f, 1f)] public float airSlashDamageMultiplier = 0.5f;

    public static WordLevelStats FromLegacy(WordData wordData, int level)
    {
        WordLevelStats stats = new WordLevelStats();
        stats.level = Mathf.Max(1, level);

        if (wordData == null) return stats;

        stats.damage = Mathf.Max(0f, wordData.baseDamage);
        stats.cooldownReduction = Mathf.Max(0f, wordData.cooldownReduction);
        return stats;
    }
}

[CreateAssetMenu(fileName = "NewWord", menuName = "Spellslinger/Word Data")]
public class WordData : ScriptableObject
{
    public const int DefaultMaxLevel = 3;

    public WordRarity rarity = WordRarity.Common;
    [Header("Kelime Kimliği")]
    public string runeText;       
    public string translatedText; 
    
    [Header("Görseller ve Prefablar")]
    public Sprite wordIcon;
    public GameObject projectilePrefab; 
    public bool spawnsOnTarget;
    
    [Header("Oyun Mantığı & Koşullar")]
    public WordType wordType;           
    public TargetType targetingLogic;   
    public PlacementCondition condition;
    
    [Header("Sadece Objec")]
    public float baseCooldown = 1.5f; 
    public float baseDamage = 10f;
    public ReadingPattern readingPattern;

    [Header("Sadece Modifier")]
    public float cooldownReduction = 0f;
    public SpecialMechanic mechanicToAdd = SpecialMechanic.None; 

    [Header("Seviye Ayarlari")]
    [SerializeField, Min(1)] private int maxLevel = DefaultMaxLevel;
    [SerializeField] private List<WordLevelStats> levelStats = new List<WordLevelStats>();

    [Header("Gizem & İlerleme")]
    public bool isUnlocked;       
    [TextArea]
    public string secretHint;

    [Header("Ekonomi ve Denge")]
    public int manaCost = 1;
    [Min(0)]
    public int shopPrice = 10;

    public int MaxLevel => Mathf.Max(1, maxLevel);

    public WordLevelStats GetStatsForLevel(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 1, MaxLevel);

        if (levelStats != null)
        {
            for (int i = 0; i < levelStats.Count; i++)
            {
                WordLevelStats stats = levelStats[i];
                if (stats != null && stats.level == clampedLevel)
                {
                    return stats;
                }
            }
        }

        return WordLevelStats.FromLegacy(this, clampedLevel);
    }

    public bool IsMaxLevel(int level)
    {
        return level >= MaxLevel;
    }

    private void OnValidate()
    {
        maxLevel = Mathf.Max(1, maxLevel);
        shopPrice = Mathf.Max(0, shopPrice);

        if (levelStats == null) return;

        for (int i = 0; i < levelStats.Count; i++)
        {
            if (levelStats[i] == null)
            {
                levelStats[i] = WordLevelStats.FromLegacy(this, Mathf.Min(i + 1, MaxLevel));
            }

            levelStats[i].level = Mathf.Clamp(levelStats[i].level, 1, MaxLevel);
        }
    }
}

public enum WordType { Object, Modifier }
public enum TargetType { None, Straight, NearestEnemy, RandomEnemy, LowestHealth }
public enum PlacementCondition { Anywhere, MustBeFirst, MustBeLast, NextToElement }
public enum WordRarity { Common, Uncommon, Rare, Epic, Legendary }

public enum ReadingPattern
{
    None,
    SpreadRadius3,
    Leftward,
    Rightward,
    ForwardOddSteps,
    BackwardOddSteps,
    Unlimited
}

public enum SpecialMechanic
{
    None,
    FireBurn,
    WaterSlow,
    AirSlash,
    RockStun,
    LightningChain,
    IceArrow,
    Explosive,
    Bounce,
    Split,
    Execution,
    Pierce,
    Acceleration,
    DamageBoost
}
