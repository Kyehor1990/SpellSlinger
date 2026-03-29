using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SentenceSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private PlayerManaCapacity manaCapacity;
    private SentenceManager sentenceManager;

    private Image slotImage;
    private Color originalColor;
    public Color hoverColor = new Color(1f, 0.9f, 0.5f, 0.8f);
    private void Start()
    {
        manaCapacity = FindFirstObjectByType<PlayerManaCapacity>();
        sentenceManager = FindFirstObjectByType<SentenceManager>();

        slotImage = GetComponent<Image>();
        if(slotImage != null) originalColor = slotImage.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null && transform.childCount == 0)
        {
            if(slotImage != null) slotImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(slotImage != null) slotImage.color = originalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if(slotImage != null) slotImage.color = originalColor;

        if (eventData.pointerDrag == null) return;
        DraggableWord draggedWord = eventData.pointerDrag.GetComponent<DraggableWord>();

        if (draggedWord != null)
        {
            if (transform.childCount > 0) return;

            int cost = draggedWord.myWordData.wordData.manaCost;

            if (draggedWord.parentAfterDrag.GetComponent<SentenceSlot>() == null)
            {
                if (manaCapacity.CanEquipWord(cost))
                {
                    manaCapacity.EquipWord(cost);
                    AcceptWord(draggedWord);
                }
            }
            else 
            {
                AcceptWord(draggedWord);
            }
        }
    }

    private void AcceptWord(DraggableWord word)
    {
        word.parentAfterDrag = transform; 
        
        word.transform.SetParent(transform); 
        
        sentenceManager.RebuildSentenceFromUI(); 
    }
}