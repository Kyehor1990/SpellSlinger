using UnityEngine;

[CreateAssetMenu(fileName = "NewWord", menuName = "Spellslinger/Word Data")]
public class WordData : ScriptableObject
{
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
    public float damageBonus = 0f;
    public float cooldownReduction = 0f;
    public SpecialMechanic mechanicToAdd = SpecialMechanic.None; 

    [Header("Gizem & İlerleme")]
    public bool isUnlocked;       
    [TextArea]
    public string secretHint;     
}

public enum WordType { Object, Modifier }
public enum TargetType { None, Straight, NearestEnemy, RandomEnemy, LowestHealth }
public enum PlacementCondition { Anywhere, MustBeFirst, MustBeLast, NextToElement }
public enum ReadingPattern { None, RightwardUntilBlocked, LeftwardUntilBlocked, EvenSpacesRight }

public enum SpecialMechanic
{
    None,
    Piercing,
    Poisonous,
    Bouncing,
    Explosive
}