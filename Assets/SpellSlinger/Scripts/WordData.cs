using UnityEngine;

[CreateAssetMenu(fileName = "NewWord", menuName = "Spellslinger/Word Data")]
public class WordData : ScriptableObject
{
    [Header("Kelime Kimliği")]
    public string runeText;       //Uydurma isim
    public string translatedText;   //İngilizce anlamı
    
    [Header("Görseller")]
    public Sprite wordIcon;       
    
    [Header("Oyun Mantığı & Koşullar")]
    public WordType wordType;           
    public TargetType targetingLogic;   
    public PlacementCondition condition;
    
    [Tooltip("Bu kelime bir nesneyse (Object) kaç saniyede bir ateşlenecek?")]
    public float baseCooldown = 1.5f; 
    
    [Header("Gizem & İlerleme")]
    public bool isUnlocked;       
    [TextArea]
    public string secretHint;     
}

public enum WordType
{
    Object,     // Projectiles, Area of Effect, Buff/Debuff, Summon vb.
    Modifier    // Alan Hasarı, Seken, Patlayan, Cooldown Düşüren vb.
}

public enum TargetType
{
    None,           
    Straight,
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
}