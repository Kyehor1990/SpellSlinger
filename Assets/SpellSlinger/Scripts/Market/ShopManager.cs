using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Market Verileri")]
    public List<WordData> allAvailableWords;
    public int wordCost = 10;
    public int rerollCost = 5;

    [Header("Arka Plan Referansları")]
    public PlayerWallet playerWallet;
    public PlayerInventory playerInventory;

    [Header("UI Referansları")]
    public GameObject shopPanel;
    public TextMeshProUGUI moneyText;
    public Button[] slotButtons;
    public TextMeshProUGUI[] slotNames;
    public TextMeshProUGUI[] slotPrices;
    private WordData[] currentShopWords = new WordData[3];

    [HideInInspector] public bool isShopActive = false;

    private void Start()
    {
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
        for (int i = 0; i < currentShopWords.Length; i++)
        {
            WordData randomWord = allAvailableWords[Random.Range(0, allAvailableWords.Count)];
            currentShopWords[i] = randomWord;

            slotNames[i].text = randomWord.runeText;
            slotPrices[i].text = wordCost.ToString() + " Altın";
            
            slotButtons[i].interactable = true; 
        }
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
        if (currentShopWords[slotIndex] == null) return;

        if (playerWallet.SpendMoney(wordCost))
        {
            playerInventory.AddWord(currentShopWords[slotIndex]);
            
            currentShopWords[slotIndex] = null;
            slotButtons[slotIndex].interactable = false;
            slotNames[slotIndex].text = "SATILDI";
            slotPrices[slotIndex].text = "-";
        }
    }

    public void CloseShop()
    {
        isShopActive = false;
        shopPanel.SetActive(false);
        Time.timeScale = 1f;
    }

}