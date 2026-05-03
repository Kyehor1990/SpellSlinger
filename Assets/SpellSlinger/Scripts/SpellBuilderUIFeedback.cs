using UnityEngine;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class SpellBuilderUIFeedback : MonoBehaviour
{
    public static SpellBuilderUIFeedback Instance { get; private set; }

    [Header("Referanslar")]
    public Transform sentencePanel;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private bool showConnectionLines = true;
    [SerializeField] private SpellBuilderConnectionLines connectionLines;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (!showConnectionLines) return;

        if (connectionLines == null)
        {
            connectionLines = GetComponent<SpellBuilderConnectionLines>();
        }

        if (connectionLines == null)
        {
            connectionLines = gameObject.AddComponent<SpellBuilderConnectionLines>();
        }

        connectionLines.SetSentencePanel(sentencePanel as RectTransform);
    }

    public void ClearPreview()
    {
        ClearVisiblePreview();
    }

    public void ClearHoverPreview()
    {
        ClearVisiblePreview();
    }

    private void ClearVisiblePreview()
    {
        ClearVisiblePreview(false, true);
    }

    private void ClearVisiblePreview(bool immediate, bool refreshSharedCounts)
    {
        if (connectionLines != null) connectionLines.ClearConnections();
        if (summaryText != null) summaryText.text = string.Empty;

        if (sentencePanel == null) return;
        foreach (Transform child in sentencePanel)
        {
            DraggableWord wordUI = child.GetComponent<DraggableWord>();
            if (wordUI != null)
            {
                if (immediate) wordUI.ResetVisualsImmediate();
                else wordUI.ResetVisuals();
            }
        }

        if (refreshSharedCounts)
        {
            RefreshSharedModifierCounts();
        }
    }

    public void RefreshSharedModifierCounts()
    {
        if (sentencePanel == null) return;

        List<DraggableWord> sentenceWords = new List<DraggableWord>();
        List<WordData> sentenceData = new List<WordData>();
        BuildSentenceLists(sentenceWords, sentenceData);
        UpdateSharedModifierCounts(sentenceWords, sentenceData);
    }

    public void PreviewPattern(DraggableWord hoveredWord)
    {
        if (hoveredWord == null ||
            hoveredWord.myWordData == null ||
            hoveredWord.myWordData.wordData == null ||
            hoveredWord.myWordData.wordData.wordType != WordType.Object)
        {
            ClearHoverPreview();
            return;
        }

        ShowPattern(hoveredWord);
    }

    private void ShowPattern(DraggableWord objectWord)
    {
        ClearVisiblePreview(true, false);

        if (objectWord == null ||
            objectWord.myWordData == null ||
            objectWord.myWordData.wordData == null ||
            objectWord.myWordData.wordData.wordType != WordType.Object)
        {
            return;
        }

        List<DraggableWord> sentenceWords = new List<DraggableWord>();
        List<WordData> sentenceData = new List<WordData>();
        BuildSentenceLists(sentenceWords, sentenceData);
        UpdateSharedModifierCounts(sentenceWords, sentenceData);

        int startIndex = sentenceWords.IndexOf(objectWord);
        if (startIndex == -1) return;

        objectWord.SetFeedbackState(RuneFeedbackState.SelectedObject);

        RunePatternPreview preview = RunePatternResolver.GetPreview(sentenceData, startIndex);
        foreach (int modifierIndex in preview.AffectedModifierIndices)
        {
            if (modifierIndex < 0 || modifierIndex >= sentenceWords.Count) continue;
            sentenceWords[modifierIndex].SetFeedbackState(RuneFeedbackState.ReadModifier);
        }

        if (preview.LeftBoundaryIndex != -1)
        {
            sentenceWords[preview.LeftBoundaryIndex].ShowBoundary(false, true);
        }

        if (preview.RightBoundaryIndex != -1)
        {
            sentenceWords[preview.RightBoundaryIndex].ShowBoundary(true, false);
        }

        foreach (int stopObjectIndex in preview.StopObjectIndices)
        {
            if (stopObjectIndex < 0 || stopObjectIndex >= sentenceWords.Count) continue;
            sentenceWords[stopObjectIndex].SetFeedbackState(RuneFeedbackState.BlockingObject);
        }

        if (showConnectionLines && connectionLines != null && connectionLines.isActiveAndEnabled)
        {
            connectionLines.ShowConnections(objectWord, sentenceWords, preview, null);
        }

        UpdateSummary(objectWord, sentenceWords, preview);

        foreach (DraggableWord word in sentenceWords)
        {
            if (word != objectWord && word.myWordData.wordData.wordType == WordType.Modifier)
            {
                if (!word.isHighlighted)
                {
                    word.SetFeedbackState(RuneFeedbackState.UnusedModifier);
                }
            }
        }
    }

    private void BuildSentenceLists(List<DraggableWord> sentenceWords, List<WordData> sentenceData)
    {
        foreach (Transform child in sentencePanel)
        {
            DraggableWord wordUI = child.GetComponent<DraggableWord>();
            if (wordUI != null && wordUI.gameObject.activeSelf && wordUI.myWordData != null && wordUI.myWordData.wordData != null)
            {
                sentenceWords.Add(wordUI);
                sentenceData.Add(wordUI.myWordData.wordData);
            }
        }
    }

    private int[] UpdateSharedModifierCounts(List<DraggableWord> sentenceWords, List<WordData> sentenceData)
    {
        int[] readCounts = new int[sentenceWords.Count];

        for (int i = 0; i < sentenceData.Count; i++)
        {
            if (sentenceData[i].wordType != WordType.Object) continue;

            RunePatternPreview preview = RunePatternResolver.GetPreview(sentenceData, i);
            foreach (int modifierIndex in preview.AffectedModifierIndices)
            {
                readCounts[modifierIndex]++;
            }
        }

        for (int i = 0; i < sentenceWords.Count; i++)
        {
            sentenceWords[i].SetSharedCount(readCounts[i]);
        }

        return readCounts;
    }

    private void UpdateSummary(DraggableWord objectWord, List<DraggableWord> sentenceWords, RunePatternPreview preview)
    {
        if (summaryText == null) return;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"{GetDisplayName(objectWord)} reads:");

        if (preview.AffectedModifierIndices.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (int modifierIndex in preview.AffectedModifierIndices)
            {
                if (modifierIndex < 0 || modifierIndex >= sentenceWords.Count) continue;
                builder.AppendLine($"- {GetDisplayName(sentenceWords[modifierIndex])}");
            }
        }

        List<string> boundaryNames = GetBoundaryNames(sentenceWords, preview);
        bool stoppedAtSentenceEdge = preview.StopsAtLeftSentenceEdge || preview.StopsAtRightSentenceEdge;
        if (boundaryNames.Count > 0 || stoppedAtSentenceEdge)
        {
            builder.AppendLine();
            builder.AppendLine("Reading stops at:");
            foreach (string boundaryName in boundaryNames)
            {
                builder.AppendLine($"- {boundaryName}");
            }

            if (stoppedAtSentenceEdge)
            {
                builder.AppendLine("- Sentence edge");
            }
        }

        List<string> unusedModifierNames = GetUnusedModifierNames(objectWord, sentenceWords, preview);
        if (unusedModifierNames.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Unused:");
            foreach (string unusedModifierName in unusedModifierNames)
            {
                builder.AppendLine($"- {unusedModifierName}");
            }
        }

        summaryText.text = builder.ToString().TrimEnd();
    }

    private List<string> GetBoundaryNames(List<DraggableWord> sentenceWords, RunePatternPreview preview)
    {
        List<string> boundaryNames = new List<string>();

        foreach (int stopObjectIndex in preview.StopObjectIndices)
        {
            if (stopObjectIndex < 0 || stopObjectIndex >= sentenceWords.Count) continue;
            boundaryNames.Add(GetDisplayName(sentenceWords[stopObjectIndex]));
        }

        return boundaryNames;
    }

    private List<string> GetUnusedModifierNames(DraggableWord objectWord, List<DraggableWord> sentenceWords, RunePatternPreview preview)
    {
        List<string> unusedModifierNames = new List<string>();

        foreach (DraggableWord word in sentenceWords)
        {
            if (word == objectWord || word.myWordData.wordData.wordType != WordType.Modifier) continue;

            int wordIndex = sentenceWords.IndexOf(word);
            if (!preview.AffectedModifierIndices.Contains(wordIndex))
            {
                unusedModifierNames.Add(GetDisplayName(word));
            }
        }

        return unusedModifierNames;
    }

    private string GetDisplayName(DraggableWord word)
    {
        WordData wordData = word.myWordData.wordData;
        if (!string.IsNullOrWhiteSpace(wordData.translatedText)) return wordData.translatedText;
        return wordData.runeText;
    }

}
