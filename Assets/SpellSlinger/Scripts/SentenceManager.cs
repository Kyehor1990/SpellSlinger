using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CompiledSpell
{
    public string spellName;
    public GameObject projectilePrefab;
    public float totalDamage;
    public float totalCooldown;
    public bool spawnsOnTarget;
    public TargetType targetingLogic;
    
    public List<SpecialMechanic> specialMechanics = new List<SpecialMechanic>();
}

public static class RunePatternResolver
{
    public static List<int> GetAffectedModifierIndices(IReadOnlyList<WordData> sentence, int objectIndex)
    {
        List<int> modifierIndices = new List<int>();

        if (sentence == null || objectIndex < 0 || objectIndex >= sentence.Count) return modifierIndices;

        WordData objectWord = sentence[objectIndex];
        if (objectWord == null || objectWord.wordType != WordType.Object) return modifierIndices;

        switch (objectWord.readingPattern)
        {
            case ReadingPattern.Rightward:
                AddRightwardModifiers(sentence, objectIndex + 1, sentence.Count - 1, 1, modifierIndices);
                break;

            case ReadingPattern.Leftward:
                AddLeftwardModifiers(sentence, objectIndex - 1, 0, 1, modifierIndices);
                break;

            case ReadingPattern.SpreadRadius3:
                AddLeftwardModifiers(sentence, objectIndex - 1, Mathf.Max(0, objectIndex - 3), 1, modifierIndices);
                AddRightwardModifiers(sentence, objectIndex + 1, Mathf.Min(sentence.Count - 1, objectIndex + 3), 1, modifierIndices);
                break;

            case ReadingPattern.ForwardOddSteps:
                AddRightwardModifiers(sentence, objectIndex + 1, sentence.Count - 1, 2, modifierIndices);
                break;

            case ReadingPattern.BackwardOddSteps:
                AddLeftwardModifiers(sentence, objectIndex - 1, 0, 2, modifierIndices);
                break;

            case ReadingPattern.Unlimited:
                AddLeftwardModifiers(sentence, objectIndex - 1, 0, 1, modifierIndices);
                AddRightwardModifiers(sentence, objectIndex + 1, sentence.Count - 1, 1, modifierIndices);
                break;
        }

        return modifierIndices;
    }

    private static void AddRightwardModifiers(IReadOnlyList<WordData> sentence, int startIndex, int endIndex, int step, List<int> modifierIndices)
    {
        for (int i = startIndex; i <= endIndex; i += step)
        {
            if (IsObjectBoundary(sentence[i])) break;
            modifierIndices.Add(i);
        }
    }

    private static void AddLeftwardModifiers(IReadOnlyList<WordData> sentence, int startIndex, int endIndex, int step, List<int> modifierIndices)
    {
        for (int i = startIndex; i >= endIndex; i -= step)
        {
            if (IsObjectBoundary(sentence[i])) break;
            modifierIndices.Add(i);
        }
    }

    private static bool IsObjectBoundary(WordData word)
    {
        return word != null && word.wordType == WordType.Object;
    }
}

public class SentenceManager : MonoBehaviour
{
    [Header("Oyuncunun Dizdiği Cümle")]
    public List<WordData> currentSentence = new List<WordData>(); 

    [Header("UI Bağlantıları (YENİ)")]
    public Transform sentencePanel;

    public void RebuildSentenceFromUI()
    {
        currentSentence.Clear(); 

        if (sentencePanel == null) return;

        foreach (Transform child in sentencePanel)
        {
            DraggableWord wordUI = child.GetComponent<DraggableWord>();
            if (wordUI != null)
            {
                currentSentence.Add(wordUI.myWordData.wordData);
            }
        }

        PlayerAutoAttack autoAttack = FindFirstObjectByType<PlayerAutoAttack>();
        if (autoAttack != null)
        {
            autoAttack.UpdateActiveSpells();
        }

        Debug.Log($"<color=cyan>Cümle Güncellendi! Sıra: {GetSentenceNames()}</color>");
    }

    private string GetSentenceNames()
    {
        string names = "";
        foreach(var word in currentSentence) names += word.runeText + " - ";
        return names;
    }
   public List<CompiledSpell> ParseSentence()
    {
        List<CompiledSpell> activeSpells = new List<CompiledSpell>();
        
        if (currentSentence.Count == 0) return activeSpells;

        for (int i = 0; i < currentSentence.Count; i++)
        {
            WordData currentWord = currentSentence[i];

            if (currentWord.wordType == WordType.Object)
            {
                CompiledSpell newSpell = new CompiledSpell();
                newSpell.spellName = currentWord.translatedText;
                newSpell.projectilePrefab = currentWord.projectilePrefab;
                newSpell.totalCooldown = currentWord.baseCooldown;
                newSpell.targetingLogic = currentWord.targetingLogic;
                newSpell.spawnsOnTarget = currentWord.spawnsOnTarget;
                newSpell.totalDamage = currentWord.baseDamage;

                if (currentWord.mechanicToAdd != SpecialMechanic.None)
                {
                    newSpell.specialMechanics.Add(currentWord.mechanicToAdd);
                }

                ApplyModifiersBasedOnPattern(currentWord, newSpell, i);

                activeSpells.Add(newSpell);
            }
        }

        return activeSpells;
    }

    private void ApplyModifiersBasedOnPattern(WordData objWord, CompiledSpell spell, int startIndex)
    {
        foreach (int modifierIndex in RunePatternResolver.GetAffectedModifierIndices(currentSentence, startIndex))
        {
            AddModifierToSpell(currentSentence[modifierIndex], spell);
        }
    }

    private void AddModifierToSpell(WordData modifierWord, CompiledSpell spell)
    {
        spell.totalCooldown -= modifierWord.cooldownReduction;
        if (spell.totalCooldown < 0.1f) spell.totalCooldown = 0.1f; 

        if (modifierWord.mechanicToAdd != SpecialMechanic.None && !spell.specialMechanics.Contains(modifierWord.mechanicToAdd))
        {
            spell.specialMechanics.Add(modifierWord.mechanicToAdd);
        }
    }
}
