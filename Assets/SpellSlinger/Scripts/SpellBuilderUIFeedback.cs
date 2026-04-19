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
        foreach (Transform child in sentencePanel)
        {
            DraggableWord w = child.GetComponent<DraggableWord>();
            if (w != null && w.gameObject.activeSelf && w.myWordData != null && w.myWordData.wordData != null) 
                sentenceWords.Add(w);
        }

        int startIndex = sentenceWords.IndexOf(hoveredWord);
        if (startIndex == -1) return;

        WordData objWordData = hoveredWord.myWordData.wordData;

        switch (objWordData.readingPattern)
        {
            case ReadingPattern.Rightward:
                for (int i = startIndex + 1; i < sentenceWords.Count; i++)
                {
                    if (IsObject(sentenceWords[i])) { sentenceWords[i - 1].ShowBoundary(true, false); break; }
                    sentenceWords[i].SetVisualState(true, false);
                    if (i == sentenceWords.Count - 1) sentenceWords[i].ShowBoundary(true, false);
                }
                break;

            case ReadingPattern.Leftward:
                for (int i = startIndex - 1; i >= 0; i--)
                {
                    if (IsObject(sentenceWords[i])) { sentenceWords[i + 1].ShowBoundary(false, true); break; }
                    sentenceWords[i].SetVisualState(true, false);
                    if (i == 0) sentenceWords[i].ShowBoundary(false, true);
                }
                break;

            case ReadingPattern.Unlimited:
                for (int i = startIndex - 1; i >= 0; i--)
                {
                    if (IsObject(sentenceWords[i])) { sentenceWords[i + 1].ShowBoundary(false, true); break; }
                    sentenceWords[i].SetVisualState(true, false);
                    if (i == 0) sentenceWords[i].ShowBoundary(false, true);
                }
                for (int i = startIndex + 1; i < sentenceWords.Count; i++)
                {
                    if (IsObject(sentenceWords[i])) { sentenceWords[i - 1].ShowBoundary(true, false); break; }
                    sentenceWords[i].SetVisualState(true, false);
                    if (i == sentenceWords.Count - 1) sentenceWords[i].ShowBoundary(true, false);
                }
                break;

            case ReadingPattern.SpreadRadius3:
                int leftLimit = Mathf.Max(0, startIndex - 3);
                for (int i = startIndex - 1; i >= leftLimit; i--)
                {
                    if (IsObject(sentenceWords[i])) { sentenceWords[i + 1].ShowBoundary(false, true); break; }
                    sentenceWords[i].SetVisualState(true, false);
                    if (i == leftLimit) sentenceWords[i].ShowBoundary(false, true);
                }
                int rightLimit = Mathf.Min(sentenceWords.Count - 1, startIndex + 3);
                for (int i = startIndex + 1; i <= rightLimit; i++)
                {
                    if (IsObject(sentenceWords[i])) { sentenceWords[i - 1].ShowBoundary(true, false); break; }
                    sentenceWords[i].SetVisualState(true, false);
                    if (i == rightLimit) sentenceWords[i].ShowBoundary(true, false);
                }
                break;

            case ReadingPattern.ForwardOddSteps:
                // İlk adım bile liste dışındaysa direkt kendi üzerinde sağ sınır göster
                if (startIndex + 1 >= sentenceWords.Count) 
                {
                    hoveredWord.ShowBoundary(true, false);
                }
                else
                {
                    for (int i = startIndex + 1; i < sentenceWords.Count; i += 2)
                    {
                        if (IsObject(sentenceWords[i])) 
                        { 
                            if (i == startIndex + 1) hoveredWord.ShowBoundary(true, false);
                            else sentenceWords[i - 2].ShowBoundary(true, false); 
                            break; 
                        }
                        
                        sentenceWords[i].SetVisualState(true, false);
                        
                        // İki adım sonrası listenin dışına çıkıyorsa sınırı bu ründe göster
                        if (i + 2 >= sentenceWords.Count) sentenceWords[i].ShowBoundary(true, false);
                    }
                }
                break;

            case ReadingPattern.BackwardOddSteps:
                // İlk adım bile liste dışındaysa (0'dan küçükse) direkt kendi üzerinde sol sınır göster
                if (startIndex - 1 < 0) 
                {
                    hoveredWord.ShowBoundary(false, true);
                }
                else
                {
                    for (int i = startIndex - 1; i >= 0; i -= 2)
                    {
                        if (IsObject(sentenceWords[i])) 
                        { 
                            if (i == startIndex - 1) hoveredWord.ShowBoundary(false, true);
                            else sentenceWords[i + 2].ShowBoundary(false, true); 
                            break; 
                        }
                        
                        sentenceWords[i].SetVisualState(true, false);
                        
                        // İki adım öncesi listenin dışına (0'ın altına) iniyorsa sınırı bu ründe göster
                        if (i - 2 < 0) sentenceWords[i].ShowBoundary(false, true);
                    }
                }
                break;
        }

        foreach (var word in sentenceWords)
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

    private bool IsObject(DraggableWord wordUI)
    {
        return wordUI.myWordData.wordData.wordType == WordType.Object;
    }
}