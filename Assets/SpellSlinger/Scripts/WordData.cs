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
    public float baseCooldown = 1.5f; 
    
    [Header("Gizem & İlerleme")]
    public bool isUnlocked;       
    [TextArea]
    public string secretHint;     
}

public enum WordType
{
    Object,     // Ateş Topu, Lazer
    Modifier    // Alan Hasarı, Seken, Patlayan, Cooldown Düşüren vb.
}

public enum TargetType
{
    None,           
    Straight,       // (Artık oyuncunun baktığı/hareket ettiği yön olarak kullanabiliriz)
    NearestEnemy,   
    RandomEnemy,    
    LowestHealth    
}

public enum PlacementCondition
{
    Anywhere,       
    MustBeFirst,    
    MustBeLast,     
    NextToElement   
    // İleride buraya virgül koyup "MustBeInMiddle" gibi şeyler ekleyebilirsin.
}