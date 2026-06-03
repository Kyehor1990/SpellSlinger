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

public class RunePatternPreview
{
    public List<int> AffectedModifierIndices = new List<int>();
    public List<int> StopObjectIndices = new List<int>();
    public int LeftBoundaryIndex = -1;
    public int RightBoundaryIndex = -1;
    public bool StopsAtLeftSentenceEdge;
    public bool StopsAtRightSentenceEdge;
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

    public static RunePatternPreview GetPreview(IReadOnlyList<WordData> sentence, int objectIndex)
    {
        RunePatternPreview preview = new RunePatternPreview();
        preview.AffectedModifierIndices = GetAffectedModifierIndices(sentence, objectIndex);

        if (sentence == null || objectIndex < 0 || objectIndex >= sentence.Count) return preview;

        WordData objectWord = sentence[objectIndex];
        if (objectWord == null || objectWord.wordType != WordType.Object) return preview;

        switch (objectWord.readingPattern)
        {
            case ReadingPattern.Rightward:
                AddRightwardStopObject(sentence, objectIndex + 1, sentence.Count - 1, 1, preview.StopObjectIndices);
                preview.StopsAtRightSentenceEdge = !HasRightwardStopObject(sentence, objectIndex + 1, sentence.Count - 1, 1);
                preview.RightBoundaryIndex = GetRightBoundaryIndex(sentence, preview.AffectedModifierIndices, objectIndex, false);
                break;

            case ReadingPattern.Leftward:
                AddLeftwardStopObject(sentence, objectIndex - 1, 0, 1, preview.StopObjectIndices);
                preview.StopsAtLeftSentenceEdge = !HasLeftwardStopObject(sentence, objectIndex - 1, 0, 1);
                preview.LeftBoundaryIndex = GetLeftBoundaryIndex(sentence, preview.AffectedModifierIndices, objectIndex, false);
                break;

            case ReadingPattern.SpreadRadius3:
                int spreadLeftLimit = Mathf.Max(0, objectIndex - 3);
                int spreadRightLimit = Mathf.Min(sentence.Count - 1, objectIndex + 3);
                AddLeftwardStopObject(sentence, objectIndex - 1, spreadLeftLimit, 1, preview.StopObjectIndices);
                AddRightwardStopObject(sentence, objectIndex + 1, spreadRightLimit, 1, preview.StopObjectIndices);
                preview.StopsAtLeftSentenceEdge = spreadLeftLimit == 0 && !HasLeftwardStopObject(sentence, objectIndex - 1, spreadLeftLimit, 1);
                preview.StopsAtRightSentenceEdge = spreadRightLimit == sentence.Count - 1 && !HasRightwardStopObject(sentence, objectIndex + 1, spreadRightLimit, 1);
                preview.LeftBoundaryIndex = GetLeftBoundaryIndex(sentence, preview.AffectedModifierIndices, objectIndex, false);
                preview.RightBoundaryIndex = GetRightBoundaryIndex(sentence, preview.AffectedModifierIndices, objectIndex, false);
                break;

            case ReadingPattern.Unlimited:
                AddLeftwardStopObject(sentence, objectIndex - 1, 0, 1, preview.StopObjectIndices);
                AddRightwardStopObject(sentence, objectIndex + 1, sentence.Count - 1, 1, preview.StopObjectIndices);
                preview.StopsAtLeftSentenceEdge = !HasLeftwardStopObject(sentence, objectIndex - 1, 0, 1);
                preview.StopsAtRightSentenceEdge = !HasRightwardStopObject(sentence, objectIndex + 1, sentence.Count - 1, 1);
                preview.LeftBoundaryIndex = GetLeftBoundaryIndex(sentence, preview.AffectedModifierIndices, objectIndex, false);
                preview.RightBoundaryIndex = GetRightBoundaryIndex(sentence, preview.AffectedModifierIndices, objectIndex, false);
                break;

            case ReadingPattern.ForwardOddSteps:
                AddRightwardStopObject(sentence, objectIndex + 1, sentence.Count - 1, 2, preview.StopObjectIndices);
                preview.StopsAtRightSentenceEdge = !HasRightwardStopObject(sentence, objectIndex + 1, sentence.Count - 1, 2);
                preview.RightBoundaryIndex = GetRightBoundaryIndex(sentence, preview.AffectedModifierIndices, objectIndex, true);
                break;

            case ReadingPattern.BackwardOddSteps:
                AddLeftwardStopObject(sentence, objectIndex - 1, 0, 2, preview.StopObjectIndices);
                preview.StopsAtLeftSentenceEdge = !HasLeftwardStopObject(sentence, objectIndex - 1, 0, 2);
                preview.LeftBoundaryIndex = GetLeftBoundaryIndex(sentence, preview.AffectedModifierIndices, objectIndex, true);
                break;
        }

        return preview;
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

    private static void AddRightwardStopObject(IReadOnlyList<WordData> sentence, int startIndex, int endIndex, int step, List<int> stopObjectIndices)
    {
        for (int i = startIndex; i <= endIndex; i += step)
        {
            if (IsObjectBoundary(sentence[i]))
            {
                stopObjectIndices.Add(i);
                return;
            }
        }
    }

    private static void AddLeftwardStopObject(IReadOnlyList<WordData> sentence, int startIndex, int endIndex, int step, List<int> stopObjectIndices)
    {
        for (int i = startIndex; i >= endIndex; i -= step)
        {
            if (IsObjectBoundary(sentence[i]))
            {
                stopObjectIndices.Add(i);
                return;
            }
        }
    }

    private static bool HasRightwardStopObject(IReadOnlyList<WordData> sentence, int startIndex, int endIndex, int step)
    {
        for (int i = startIndex; i <= endIndex; i += step)
        {
            if (IsObjectBoundary(sentence[i])) return true;
        }

        return false;
    }

    private static bool HasLeftwardStopObject(IReadOnlyList<WordData> sentence, int startIndex, int endIndex, int step)
    {
        for (int i = startIndex; i >= endIndex; i -= step)
        {
            if (IsObjectBoundary(sentence[i])) return true;
        }

        return false;
    }

    private static int GetRightBoundaryIndex(IReadOnlyList<WordData> sentence, List<int> modifierIndices, int objectIndex, bool showAtSentenceEdge)
    {
        int boundaryIndex = objectIndex;
        bool hasRightModifier = false;

        foreach (int modifierIndex in modifierIndices)
        {
            if (modifierIndex > objectIndex)
            {
                hasRightModifier = true;
                if (modifierIndex > boundaryIndex) boundaryIndex = modifierIndex;
            }
        }

        if (hasRightModifier || HasObjectBoundaryToRight(sentence, objectIndex) || showAtSentenceEdge && objectIndex + 1 >= sentence.Count)
        {
            return boundaryIndex;
        }

        return -1;
    }

    private static int GetLeftBoundaryIndex(IReadOnlyList<WordData> sentence, List<int> modifierIndices, int objectIndex, bool showAtSentenceEdge)
    {
        int boundaryIndex = objectIndex;
        bool hasLeftModifier = false;

        foreach (int modifierIndex in modifierIndices)
        {
            if (modifierIndex < objectIndex)
            {
                hasLeftModifier = true;
                if (modifierIndex < boundaryIndex) boundaryIndex = modifierIndex;
            }
        }

        if (hasLeftModifier || HasObjectBoundaryToLeft(sentence, objectIndex) || showAtSentenceEdge && objectIndex - 1 < 0)
        {
            return boundaryIndex;
        }

        return -1;
    }

    private static bool HasObjectBoundaryToRight(IReadOnlyList<WordData> sentence, int objectIndex)
    {
        return objectIndex + 1 < sentence.Count && IsObjectBoundary(sentence[objectIndex + 1]);
    }

    private static bool HasObjectBoundaryToLeft(IReadOnlyList<WordData> sentence, int objectIndex)
    {
        return objectIndex - 1 >= 0 && IsObjectBoundary(sentence[objectIndex - 1]);
    }
}

public class SentenceManager : MonoBehaviour
{
    [Header("Oyuncunun Dizdiği Cümle")]
    public List<WordData> currentSentence = new List<WordData>(); 

    [Header("UI Bağlantıları (YENİ)")]
    public Transform sentencePanel;

    [Header("Baslangic Cumlesi")]
    [SerializeField] private bool buildStartingSentenceFromCurrentSentence = true;
    [SerializeField] private GameObject wordUIPrefab;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerManaCapacity manaCapacity;
    [SerializeField] private bool logSentenceUpdates = false;

    private void Awake()
    {
        BuildStartingSentenceUI();
    }

    public void RebuildSentenceFromUI()
    {
        currentSentence.Clear(); 

        if (sentencePanel == null)
        {
            manaCapacity?.RefreshManaUI();
            return;
        }

        foreach (Transform child in sentencePanel)
        {
            DraggableWord wordUI = child.GetComponent<DraggableWord>();
            if (wordUI != null)
            {
                currentSentence.Add(wordUI.myWordData.wordData);
            }
        }

        SyncManaCapacityFromSentenceUI();

        PlayerAutoAttack autoAttack = FindFirstObjectByType<PlayerAutoAttack>();
        if (autoAttack != null)
        {
            autoAttack.UpdateActiveSpells();
        }

        SpellBuilderUIFeedback.Instance?.ClearPreview();

        if (!logSentenceUpdates) return;
        Debug.Log($"<color=cyan>Cümle Güncellendi! Sıra: {GetSentenceNames()}</color>");
    }

    private void BuildStartingSentenceUI()
    {
        if (!buildStartingSentenceFromCurrentSentence || sentencePanel == null || currentSentence.Count == 0) return;

        if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (manaCapacity == null) manaCapacity = FindFirstObjectByType<PlayerManaCapacity>();
        if (wordUIPrefab == null && playerInventory != null) wordUIPrefab = playerInventory.wordUIPrefab;

        if (wordUIPrefab == null)
        {
            Debug.LogWarning("Baslangic cumlesi UI'a aktarilamadi: Word UI Prefab referansi eksik.");
            return;
        }

        List<WordData> startingSentence = new List<WordData>(currentSentence);
        Dictionary<WordData, int> requiredCounts = new Dictionary<WordData, int>();

        foreach (WordData wordData in startingSentence)
        {
            if (wordData == null) continue;

            if (!requiredCounts.ContainsKey(wordData))
            {
                requiredCounts[wordData] = 0;
            }

            requiredCounts[wordData]++;
            if (CountWordInSentencePanel(wordData) >= requiredCounts[wordData]) continue;

            OwnedWord ownedWord = playerInventory != null
                ? playerInventory.GetOrCreateOwnedWord(wordData)
                : new OwnedWord(wordData);

            DraggableWord existingInventoryWord = FindWordUI(playerInventory != null ? playerInventory.inventoryPanel : null, wordData);
            DraggableWord sentenceWord = existingInventoryWord != null
                ? existingInventoryWord
                : Instantiate(wordUIPrefab, sentencePanel).GetComponent<DraggableWord>();

            if (sentenceWord == null) continue;

            sentenceWord.transform.SetParent(sentencePanel, false);
            sentenceWord.transform.SetAsLastSibling();
            sentenceWord.parentAfterDrag = sentencePanel;
            sentenceWord.isFromInventory = false;
            sentenceWord.Setup(ownedWord);
        }

        SyncManaCapacityFromSentenceUI();
        RebuildSentenceFromUI();
    }

    private int CountWordInSentencePanel(WordData wordData)
    {
        int count = 0;

        foreach (Transform child in sentencePanel)
        {
            DraggableWord wordUI = child.GetComponent<DraggableWord>();
            if (wordUI != null && wordUI.myWordData != null && wordUI.myWordData.wordData == wordData)
            {
                count++;
            }
        }

        return count;
    }

    private DraggableWord FindWordUI(Transform panel, WordData wordData)
    {
        if (panel == null) return null;

        foreach (Transform child in panel)
        {
            DraggableWord wordUI = child.GetComponent<DraggableWord>();
            if (wordUI != null && wordUI.myWordData != null && wordUI.myWordData.wordData == wordData)
            {
                return wordUI;
            }
        }

        return null;
    }

    private void SyncManaCapacityFromSentenceUI()
    {
        if (manaCapacity == null || sentencePanel == null) return;

        int usedMana = 0;
        foreach (Transform child in sentencePanel)
        {
            DraggableWord wordUI = child.GetComponent<DraggableWord>();
            if (wordUI != null && wordUI.myWordData != null && wordUI.myWordData.wordData != null)
            {
                usedMana += wordUI.myWordData.wordData.manaCost;
            }
        }

        manaCapacity.SetUsedMana(usedMana);
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
