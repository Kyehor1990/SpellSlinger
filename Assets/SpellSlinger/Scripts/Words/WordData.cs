using UnityEngine;

[CreateAssetMenu(fileName = "NewWord", menuName = "Spellslinger/Word Data")]
public class WordData : ScriptableObject
{
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

    [Header("Gizem & İlerleme")]
    public bool isUnlocked;       
    [TextArea]
    public string secretHint;

    [Header("Ekonomi ve Denge")]
    public int manaCost = 1;
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