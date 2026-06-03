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

    public bool IsMaxLevel()
    {
        return wordData == null || wordData.IsMaxLevel(level);
    }
}

public class PlayerInventory : MonoBehaviour
{
    [Header("UI Referanslari")]
    public GameObject wordUIPrefab;
    public Transform inventoryPanel;

    [Header("Sahip Olunan Kelimeler")]
    public List<OwnedWord> myWords = new List<OwnedWord>();

    public OwnedWord GetOrCreateOwnedWord(WordData wordData)
    {
        if (wordData == null) return null;

        OwnedWord existingWord = myWords.Find(word => word != null && word.wordData == wordData);
        if (existingWord != null) return existingWord;

        OwnedWord addedWord = new OwnedWord(wordData);
        myWords.Add(addedWord);
        Debug.Log($"<color=green>Baslangic envanterine eklendi: {wordData.runeText}</color>");
        return addedWord;
    }

    public void AddWord(WordData newWord)
    {
        OwnedWord addedWord = new OwnedWord(newWord);
        myWords.Add(addedWord);
        Debug.Log($"<color=green>Envantere Eklendi: {newWord.runeText}</color>");

        GameObject newWordUI = Instantiate(wordUIPrefab, inventoryPanel);
        DraggableWord draggableWord = newWordUI.GetComponent<DraggableWord>();
        if (draggableWord != null)
        {
            draggableWord.Setup(addedWord);
        }
    }

    public void TryUpgradeWord(OwnedWord wordToUpgrade)
    {
        if (wordToUpgrade == null || wordToUpgrade.wordData == null) return;

        OwnedWord duplicateWord = myWords.Find(w => w != null
                                                 && w.wordData == wordToUpgrade.wordData
                                                 && w.level == wordToUpgrade.level
                                                 && w != wordToUpgrade);

        if (TryMergeWords(wordToUpgrade, duplicateWord, out OwnedWord upgradedWord, out _))
        {
            Debug.Log($"<color=magenta>Birlestirme basarili! {upgradedWord.wordData.runeText} artik Seviye {upgradedWord.level}!</color>");
        }
        else
        {
            Debug.Log($"<color=red>Yukseltme basarisiz! Envanterinde ayni seviyede baska bir '{wordToUpgrade.wordData.runeText}' yok.</color>");
        }
    }

    public bool CanMergeWords(OwnedWord wordToKeep, OwnedWord wordToConsume)
    {
        if (wordToKeep == null || wordToConsume == null) return false;
        if (wordToKeep == wordToConsume) return false;
        if (wordToKeep.wordData == null || wordToConsume.wordData == null) return false;
        if (wordToKeep.wordData != wordToConsume.wordData) return false;
        if (wordToKeep.level != wordToConsume.level) return false;
        if (wordToKeep.IsMaxLevel() || wordToConsume.IsMaxLevel()) return false;

        return true;
    }

    public bool TryMergeWords(OwnedWord wordToKeep, OwnedWord wordToConsume, out OwnedWord upgradedWord, out OwnedWord consumedWord)
    {
        upgradedWord = null;
        consumedWord = null;

        if (!CanMergeWords(wordToKeep, wordToConsume)) return false;

        if (!myWords.Contains(wordToKeep))
        {
            myWords.Add(wordToKeep);
        }

        myWords.Remove(wordToConsume);
        wordToKeep.level = Mathf.Min(wordToKeep.level + 1, wordToKeep.wordData.MaxLevel);

        upgradedWord = wordToKeep;
        consumedWord = wordToConsume;
        return true;
    }
}
