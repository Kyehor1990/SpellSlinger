using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    [Header("Seviye Durumu")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;
    
    public int pendingLevelUps = 0; 

    public void AddExperience(int amount)
    {
        currentXP += amount;

        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel; 
        currentLevel++;
        pendingLevelUps++;
        
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.2f); 
        
        Debug.Log($"<color=yellow>SEVİYE ATLANDI! Bekleyen Seçim Hakkı: {pendingLevelUps}</color>");
    }
}