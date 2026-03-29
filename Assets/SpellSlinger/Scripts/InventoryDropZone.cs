using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDropZone : MonoBehaviour, IDropHandler
{
    private PlayerManaCapacity manaCapacity;
    private SentenceManager sentenceManager;

    private void Start()
    {
        manaCapacity = FindObjectOfType<PlayerManaCapacity>(); 
        sentenceManager = FindObjectOfType<SentenceManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        DraggableWord draggedWord = eventData.pointerDrag.GetComponent<DraggableWord>();

        if (draggedWord != null)
        {
            if (draggedWord.parentAfterDrag.GetComponent<SentenceSlot>() != null)
            {
                manaCapacity.UnequipWord(draggedWord.myWordData.wordData.manaCost);
                
                // HATA ÇÖZÜMÜ: Anında ebeveyni envanter yapıyoruz
                draggedWord.parentAfterDrag = transform;
                draggedWord.transform.SetParent(transform); 
                
                sentenceManager.RebuildSentenceFromUI(); 
            }
            else
            {
                draggedWord.parentAfterDrag = transform; 
                draggedWord.transform.SetParent(transform); 
            }
        }
    }
}