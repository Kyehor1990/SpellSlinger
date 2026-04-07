using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("Stat Seçenekleri (Havuz)")]
    public List<StatUpgradeData> allPossibleUpgrades; 
    
    [Header("UI Referansları")]
    public GameObject upgradePanel;
    public Transform cardContainer;
    public GameObject upgradeCardPrefab;

    [Header("Durum")]
    public bool isUpgradePhaseActive = false;
    private int remainingPicks = 0;

    [Header("Oyuncu Referansları")]
    public PlayerManaCapacity playerMana;
    public PlayerHealth playerHealth;
    public PlayerStatsManager playerStats;

    private void Start()
    {
        upgradePanel.SetActive(false);
    }

    public void StartUpgradePhase(int levelUpsToProcess)
    {
        isUpgradePhaseActive = true;
        remainingPicks = levelUpsToProcess;
        
        upgradePanel.SetActive(true);
        Time.timeScale = 0f; 
        
        ShowUpgradeChoices();
    }

    private void ShowUpgradeChoices()
    {
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        List<StatUpgradeData> chosenUpgrades = GetRandomUpgrades(3);

        foreach (StatUpgradeData data in chosenUpgrades)
        {
            GameObject newCard = Instantiate(upgradeCardPrefab, cardContainer);
            newCard.GetComponent<UpgradeCardUI>().SetupCard(data, this);
        }
    }

   private List<StatUpgradeData> GetRandomUpgrades(int count)
    {
        List<StatUpgradeData> availablePool = new List<StatUpgradeData>();
        foreach (var upgrade in allPossibleUpgrades)
        {
            if (!playerStats.IsStatMaxed(upgrade.statToIncrease))
            {
                availablePool.Add(upgrade);
            }
        }

        List<StatUpgradeData> selected = new List<StatUpgradeData>();

        for (int i = 0; i < count; i++)
        {
            if (availablePool.Count == 0) break;

            int randomIndex = Random.Range(0, availablePool.Count);
            selected.Add(availablePool[randomIndex]);
            
            availablePool.RemoveAt(randomIndex); 
        }

        return selected;
    }

    public void OnUpgradeSelected(StatUpgradeData chosenUpgrade)
    {
        Debug.Log($"<color=orange>Seçilen Yükseltme: {chosenUpgrade.upgradeName}</color>");
        
        playerStats.ApplyUpgrade(chosenUpgrade.statToIncrease, chosenUpgrade.increaseAmount);

        switch (chosenUpgrade.statToIncrease)
        {
            case StatType.MaxMana:
                playerMana.IncreaseMaxMana(Mathf.RoundToInt(chosenUpgrade.increaseAmount));
                break;
                
            case StatType.MaxHealth:
                playerHealth.Heal(chosenUpgrade.increaseAmount);
                break;
        }

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
        isUpgradePhaseActive = false;
        upgradePanel.SetActive(false);
    }
}