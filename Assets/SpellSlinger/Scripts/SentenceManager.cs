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

                ApplyModifiersBasedOnPattern(currentWord, newSpell, i);

                activeSpells.Add(newSpell);
            }
        }

        return activeSpells;
    }

    private void ApplyModifiersBasedOnPattern(WordData objWord, CompiledSpell spell, int startIndex)
    {
        switch (objWord.readingPattern)
        {
            case ReadingPattern.Rightward:
                for (int i = startIndex + 1; i < currentSentence.Count; i++)
                {
                    if (currentSentence[i].wordType == WordType.Object) break;
                    AddModifierToSpell(currentSentence[i], spell);
                }
                break;

            case ReadingPattern.Leftward:
                for (int i = startIndex - 1; i >= 0; i--)
                {
                    if (currentSentence[i].wordType == WordType.Object) break;
                    AddModifierToSpell(currentSentence[i], spell);
                }
                break;

            case ReadingPattern.SpreadRadius3:
                for (int i = startIndex - 1; i >= Mathf.Max(0, startIndex - 3); i--)
                {
                    if (currentSentence[i].wordType == WordType.Object) break;
                    AddModifierToSpell(currentSentence[i], spell);
                }
                for (int i = startIndex + 1; i <= Mathf.Min(currentSentence.Count - 1, startIndex + 3); i++)
                {
                    if (currentSentence[i].wordType == WordType.Object) break;
                    AddModifierToSpell(currentSentence[i], spell);
                }
                break;

            case ReadingPattern.ForwardOddSteps:
                for (int i = startIndex + 1; i < currentSentence.Count; i += 2)
                {
                    if (currentSentence[i].wordType == WordType.Object) break;
                    AddModifierToSpell(currentSentence[i], spell);
                }
                break;

            case ReadingPattern.BackwardOddSteps:
                for (int i = startIndex - 1; i >= 0; i -= 2)
                {
                    if (currentSentence[i].wordType == WordType.Object) break;
                    AddModifierToSpell(currentSentence[i], spell);
                }
                break;

            case ReadingPattern.Unlimited:
                for (int i = startIndex - 1; i >= 0; i--)
                {
                    if (currentSentence[i].wordType == WordType.Object) break;
                    AddModifierToSpell(currentSentence[i], spell);
                }
                for (int i = startIndex + 1; i < currentSentence.Count; i++)
                {
                    if (currentSentence[i].wordType == WordType.Object) break;
                    AddModifierToSpell(currentSentence[i], spell);
                }
                break;
        }
    }

    private void AddModifierToSpell(WordData modifierWord, CompiledSpell spell)
    {
        spell.totalDamage += modifierWord.damageBonus;
        spell.totalCooldown -= modifierWord.cooldownReduction;
        if (spell.totalCooldown < 0.1f) spell.totalCooldown = 0.1f; 

        if (modifierWord.mechanicToAdd != SpecialMechanic.None && !spell.specialMechanics.Contains(modifierWord.mechanicToAdd))
        {
            spell.specialMechanics.Add(modifierWord.mechanicToAdd);
        }
    }
}