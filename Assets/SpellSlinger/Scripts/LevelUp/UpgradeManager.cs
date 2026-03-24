using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("Stat Seçenekleri (Havuz)")]
    public List<StatUpgradeData> allPossibleUpgrades; 
    
    [Header("Durum")]
    public bool isUpgradePhaseActive = false;
    private int remainingPicks = 0;

    public void StartUpgradePhase(int levelUpsToProcess)
    {
        isUpgradePhaseActive = true;
        remainingPicks = levelUpsToProcess;
        
        Debug.Log($"<color=cyan>Yükseltme Ekranı Açıldı! Oyuncunun {remainingPicks} adet seçim hakkı var.</color>");
        
        ShowUpgradeChoices();
    }

    private void ShowUpgradeChoices()
    {
        Debug.Log("Ekranda 3 adet rastgele Stat belirdi...");
        
        Invoke("SimulatePlayerMakingAChoice", 1f); 
    }

    public void OnUpgradeSelected(StatUpgradeData chosenUpgrade)
    {
        Debug.Log($"Oyuncu {chosenUpgrade.upgradeName} seçeneğini seçti!");

        remainingPicks--;

        if (remainingPicks > 0)
        {
            ShowUpgradeChoices();
        }
        else
        {
            CloseUpgradePhase();
        }
    }

    private void CloseUpgradePhase()
    {
        Debug.Log("<color=cyan>Tüm seçimler yapıldı. Yeni dalga başlıyor!</color>");
        isUpgradePhaseActive = false;
    }

    private void SimulatePlayerMakingAChoice()
    {
        if (allPossibleUpgrades.Count > 0)
        {
            StatUpgradeData randomChoice = allPossibleUpgrades[Random.Range(0, allPossibleUpgrades.Count)];
            OnUpgradeSelected(randomChoice);
        }
        else
        {
            Debug.LogError("Yükseltme havuzu boş! En az 1 tane StatUpgradeData ekleyin.");
            remainingPicks--;
            if (remainingPicks <= 0) CloseUpgradePhase();
        }
    }
}