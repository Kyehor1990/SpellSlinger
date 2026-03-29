using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OwnedWord
{
    public WordData wordData;
    public int level = 1;

    public OwnedWord(WordData data)
    {
        wordData = data;
        level = 1;
    }
}

public class PlayerInventory : MonoBehaviour
{
    [Header("UI Referansları")]
    public GameObject wordUIPrefab;
    public Transform inventoryPanel;

    [Header("Sahip Olunan Kelimeler")]
    public List<OwnedWord> myWords = new List<OwnedWord>();

    public void AddWord(WordData newWord)
    {
        OwnedWord addedWord = new OwnedWord(newWord);
        myWords.Add(addedWord);
        Debug.Log($"<color=green>Envantere Eklendi: {newWord.runeText}</color>");

        GameObject newWordUI = Instantiate(wordUIPrefab, inventoryPanel);
        newWordUI.GetComponent<DraggableWord>().Setup(addedWord);
    }

    public void TryUpgradeWord(OwnedWord wordToUpgrade)
    {
        OwnedWord duplicateWord = myWords.Find(w => w.wordData == wordToUpgrade.wordData 
                                                 && w.level == wordToUpgrade.level 
                                                 && w != wordToUpgrade);

        if (duplicateWord != null)
        {
            myWords.Remove(duplicateWord);

            wordToUpgrade.level++;
            
            Debug.Log($"<color=magenta>BİRLEŞTİRME BAŞARILI! {wordToUpgrade.wordData.runeText} artık Seviye {wordToUpgrade.level}!</color>");
        }
        else
        {
            Debug.Log($"<color=red>Yükseltme başarısız! Envanterinde aynı seviyede başka bir '{wordToUpgrade.wordData.runeText}' yok.</color>");
        }
    }
}