using UnityEngine;
using System.Collections.Generic;

public class SpellBuilderUIFeedback : MonoBehaviour
{
    public static SpellBuilderUIFeedback Instance { get; private set; }

    [Header("Referanslar")]
    public Transform sentencePanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ClearPreview()
    {
        if (sentencePanel == null) return;
        foreach (Transform child in sentencePanel)
        {
            DraggableWord wordUI = child.GetComponent<DraggableWord>();
            if (wordUI != null) wordUI.ResetVisuals();
        }
    }

    public void PreviewPattern(DraggableWord hoveredWord)
    {
        ClearPreview();

        if (hoveredWord == null || hoveredWord.myWordData.wordData.wordType != WordType.Object) return;

        List<DraggableWord> sentenceWords = new List<DraggableWord>();
        List<WordData> sentenceData = new List<WordData>();
        foreach (Transform child in sentencePanel)
        {
            DraggableWord wordUI = child.GetComponent<DraggableWord>();
            if (wordUI != null && wordUI.gameObject.activeSelf && wordUI.myWordData != null && wordUI.myWordData.wordData != null)
            {
                sentenceWords.Add(wordUI);
                sentenceData.Add(wordUI.myWordData.wordData);
            }
        }

        int startIndex = sentenceWords.IndexOf(hoveredWord);
        if (startIndex == -1) return;

        RunePatternPreview preview = RunePatternResolver.GetPreview(sentenceData, startIndex);
        foreach (int modifierIndex in preview.AffectedModifierIndices)
        {
            sentenceWords[modifierIndex].SetVisualState(true, false);
        }

        if (preview.LeftBoundaryIndex != -1)
        {
            sentenceWords[preview.LeftBoundaryIndex].ShowBoundary(false, true);
        }

        if (preview.RightBoundaryIndex != -1)
        {
            sentenceWords[preview.RightBoundaryIndex].ShowBoundary(true, false);
        }

        foreach (DraggableWord word in sentenceWords)
        {
            if (word != hoveredWord && word.myWordData.wordData.wordType == WordType.Modifier)
            {
                if (!word.isHighlighted)
                {
                    word.SetVisualState(false, true);
                }
            }
        }
    }

}
