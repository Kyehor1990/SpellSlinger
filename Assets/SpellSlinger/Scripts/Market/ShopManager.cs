using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Market Verileri")]
    public List<WordData> allAvailableWords;
    public int wordCost = 10;
    public int rerollCost = 5;

    [Header("Referanslar")]
    public PlayerWallet playerWallet;
    public PlayerInventory playerInventory;
    public EnemyWaveManager waveManager;

    private WordData[] currentShopWords = new WordData[3]; 

    public void OpenShop()
    {
        Debug.Log("<color=cyan>--- MARKET AÇILDI ---</color>");
        
        RollShopItems();
    }

    public void RollShopItems()
    {
        Debug.Log("Market Rafları Yenileniyor...");
        for (int i = 0; i < currentShopWords.Length; i++)
        {
            currentShopWords[i] = allAvailableWords[Random.Range(0, allAvailableWords.Count)];
            Debug.Log($"Raf {i+1}: {currentShopWords[i].runeText} ({wordCost} Altın)");
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
        if (currentShopWords[slotIndex] == null) 
        {
            Debug.Log("Bu raf boş!");
            return;
        }

        if (playerWallet.SpendMoney(wordCost))
        {
            playerInventory.AddWord(currentShopWords[slotIndex]);
            
            currentShopWords[slotIndex] = null; 
            Debug.Log("Satın alım başarılı!");
            
        }
    }

    public void CloseShop()
    {
        Debug.Log("<color=cyan>--- MARKET KAPANDI, YENİ DALGA BAŞLIYOR ---</color>");
        
        Time.timeScale = 1f;
    }
}