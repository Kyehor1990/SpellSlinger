using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WordMergeAndLevelStatsTests
{
    [Test]
    public void MergeSameWordSameLevelUpgradesKeptCopyAndConsumesOtherCopy()
    {
        GameObject inventoryObject = new GameObject("Inventory");
        PlayerInventory inventory = inventoryObject.AddComponent<PlayerInventory>();
        WordData wordData = CreateWord("Fireball", WordType.Object);
        OwnedWord keptWord = new OwnedWord(wordData);
        OwnedWord consumedWord = new OwnedWord(wordData);

        inventory.myWords.Add(keptWord);
        inventory.myWords.Add(consumedWord);

        bool merged = inventory.TryMergeWords(keptWord, consumedWord, out OwnedWord upgradedWord, out OwnedWord removedWord);

        Assert.IsTrue(merged);
        Assert.AreSame(keptWord, upgradedWord);
        Assert.AreSame(consumedWord, removedWord);
        Assert.AreEqual(2, keptWord.level);
        Assert.Contains(keptWord, inventory.myWords);
        Assert.IsFalse(inventory.myWords.Contains(consumedWord));

        Object.DestroyImmediate(inventoryObject);
    }

    [Test]
    public void MergeRejectsDifferentLevelsAndMaxLevelCopies()
    {
        GameObject inventoryObject = new GameObject("Inventory");
        PlayerInventory inventory = inventoryObject.AddComponent<PlayerInventory>();
        WordData wordData = CreateWord("Fireball", WordType.Object);
        OwnedWord levelOne = new OwnedWord(wordData);
        OwnedWord levelTwo = new OwnedWord(wordData) { level = 2 };
        OwnedWord maxOne = new OwnedWord(wordData) { level = 3 };
        OwnedWord maxTwo = new OwnedWord(wordData) { level = 3 };

        Assert.IsFalse(inventory.CanMergeWords(levelOne, levelTwo));
        Assert.IsFalse(inventory.CanMergeWords(maxOne, maxTwo));

        Object.DestroyImmediate(inventoryObject);
    }

    [Test]
    public void ParseSentenceUsesOwnedWordLevelStatsForObjectsAndModifiers()
    {
        GameObject sentenceObject = new GameObject("SentenceManager");
        SentenceManager sentenceManager = sentenceObject.AddComponent<SentenceManager>();

        WordData objectWord = CreateWord("Fireball", WordType.Object);
        objectWord.baseCooldown = 1f;
        objectWord.readingPattern = ReadingPattern.Rightward;
        SetLevelStats(objectWord, new WordLevelStats { level = 2, damage = 25f, projectileCount = 3 });

        WordData modifierWord = CreateWord("Damage Bonus", WordType.Modifier);
        modifierWord.mechanicToAdd = SpecialMechanic.DamageBoost;
        SetLevelStats(modifierWord, new WordLevelStats { level = 2, cooldownReduction = 0.2f, damageBonusMultiplier = 1.5f });

        sentenceManager.currentSentence.Add(objectWord);
        sentenceManager.currentSentence.Add(modifierWord);
        sentenceManager.currentOwnedSentence.Add(new OwnedWord(objectWord) { level = 2 });
        sentenceManager.currentOwnedSentence.Add(new OwnedWord(modifierWord) { level = 2 });

        List<CompiledSpell> spells = sentenceManager.ParseSentence();

        Assert.AreEqual(1, spells.Count);
        Assert.AreEqual(25f, spells[0].totalDamage);
        Assert.AreEqual(3, spells[0].projectileCount);
        Assert.AreEqual(0.8f, spells[0].totalCooldown, 0.001f);
        Assert.Contains(SpecialMechanic.DamageBoost, spells[0].specialMechanics);
        Assert.AreEqual(1.5f, spells[0].GetStatsForMechanic(SpecialMechanic.DamageBoost).damageBonusMultiplier);

        Object.DestroyImmediate(sentenceObject);
    }

    private static WordData CreateWord(string runeText, WordType wordType)
    {
        WordData word = ScriptableObject.CreateInstance<WordData>();
        word.runeText = runeText;
        word.translatedText = runeText;
        word.wordType = wordType;
        return word;
    }

    private static void SetLevelStats(WordData wordData, params WordLevelStats[] stats)
    {
        FieldInfo levelStatsField = typeof(WordData).GetField("levelStats", BindingFlags.NonPublic | BindingFlags.Instance);
        levelStatsField.SetValue(wordData, new List<WordLevelStats>(stats));
    }
}
