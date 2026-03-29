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
        List<StatUpgradeData> poolCopy = new List<StatUpgradeData>(allPossibleUpgrades);
        List<StatUpgradeData> selected = new List<StatUpgradeData>();

        for (int i = 0; i < count; i++)
        {
            if (poolCopy.Count == 0) break;

            int randomIndex = Random.Range(0, poolCopy.Count);
            selected.Add(poolCopy[randomIndex]);
            
            poolCopy.RemoveAt(randomIndex); 
        }

        return selected;
    }

    public void OnUpgradeSelected(StatUpgradeData chosenUpgrade)
    {
        Debug.Log($"<color=orange>Seçilen Yükseltme: {chosenUpgrade.upgradeName}</color>");
        
        // Statlar burda arttırlacak. Örneğin:
        // PlayerStats.Instance.IncreaseHealth(chosenUpgrade.healthIncrease); gibisinden

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