using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Market Verileri")]
    public List<WordData> allAvailableWords;
    public int rerollCost = 5;

    [Header("Arka Plan Referansları")]
    public PlayerWallet playerWallet;
    public PlayerInventory playerInventory;
    public PlayerStatsManager playerStats;
    [SerializeField] private EnemyWaveManager enemyWaveManager;

    [Header("UI Referansları")]
    public GameObject shopPanel;
    public TextMeshProUGUI moneyText;
    public Button[] slotButtons;
    public TextMeshProUGUI[] slotNames;
    public TextMeshProUGUI[] slotPrices;
    private WordData[] currentShopWords = new WordData[5];

    [HideInInspector] public bool isShopActive = false;

    [Header("Olasılık Ağırlıkları")]
    public float baseCommon = 50f;
    public float baseUncommon = 25f;
    public float baseRare = 15f;
    public float baseEpic = 9f;
    public float baseLegendary = 1f;

    private void Start()
    {
        if (enemyWaveManager == null)
        {
            enemyWaveManager = Object.FindFirstObjectByType<EnemyWaveManager>();
        }

        shopPanel.SetActive(false);
    }

    public void OpenShop()
    {
        isShopActive = true;
        shopPanel.SetActive(true);
        Time.timeScale = 0f;

        RollShopItems();
    }

    public void RollShopItems()
    {
        float luck = playerStats != null ? playerStats.GetStat(StatType.Luck) : 0f;

        float currentCommon = Mathf.Max(0, baseCommon - (luck * 1.5f)); 
        float currentUncommon = Mathf.Max(0, baseUncommon - (luck * 1f));
        float currentRare = baseRare + (luck * 0.5f);
        float currentEpic = baseEpic + (luck * 0.7f);
        float currentLegendary = baseLegendary + (luck * 0.3f);
        
        float totalWeight = currentCommon + currentUncommon + currentRare + currentEpic + currentLegendary;

        for (int i = 0; i < currentShopWords.Length; i++)
        {
            WordRarity selectedRarity = RollRarity(currentCommon, currentUncommon, currentRare, currentEpic, currentLegendary, totalWeight);
            
            List<WordData> filteredWords = allAvailableWords.FindAll(w => w.rarity == selectedRarity);

            WordData selectedWord;
            
            if (filteredWords.Count > 0)
            {
                selectedWord = filteredWords[Random.Range(0, filteredWords.Count)];
            }
            else
            {
                selectedWord = allAvailableWords[Random.Range(0, allAvailableWords.Count)];
            }

            currentShopWords[i] = selectedWord;

            slotNames[i].text = selectedWord.runeText;
            slotPrices[i].text = GetWordPrice(selectedWord).ToString() + " Altın";
            slotButtons[i].interactable = true; 
        }
    }

    private WordRarity RollRarity(float c, float u,float r, float e, float l, float total)
    {
        float randomVal = Random.Range(0f, total);
        
        if (randomVal <= c) return WordRarity.Common;
        randomVal -= c;
        if (randomVal <= u) return WordRarity.Uncommon;
        randomVal -= u;
        if (randomVal <= r) return WordRarity.Rare;
        randomVal -= r;
        if (randomVal <= e) return WordRarity.Epic;
        
        return WordRarity.Legendary;
    }

    public void TryReroll()
    {
        if (playerWallet.SpendMoney(rerollCost))
        {
            RollShopItems();
        }
    }

    public void TryBuyWord(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentShopWords.Length) return;
        if (currentShopWords[slotIndex] == null) return;

        WordData selectedWord = currentShopWords[slotIndex];
        int price = GetWordPrice(selectedWord);

        if (playerWallet.SpendMoney(price))
        {
            playerInventory.AddWord(selectedWord);
            
            currentShopWords[slotIndex] = null;
            slotButtons[slotIndex].interactable = false;
            slotNames[slotIndex].text = "SATILDI";
            slotPrices[slotIndex].text = "-";
        }
    }

    public void CloseShop()
    {
        if (!isShopActive)
        {
            return;
        }

        isShopActive = false;
        shopPanel.SetActive(false);
        Time.timeScale = 1f;

        enemyWaveManager?.StartNextWaveNow();
    }

    private int GetWordPrice(WordData word)
    {
        return word != null ? Mathf.Max(0, word.shopPrice) : 0;
    }
}
