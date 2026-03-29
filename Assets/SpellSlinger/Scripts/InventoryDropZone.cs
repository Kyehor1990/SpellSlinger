using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDropZone : MonoBehaviour, IDropHandler
{
    private PlayerManaCapacity manaCapacity;
    private SentenceManager sentenceManager;

    private void Start()
    {
        manaCapacity = FindFirstObjectByType<PlayerManaCapacity>(); 
        sentenceManager = FindFirstObjectByType<SentenceManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        DraggableWord draggedWord = eventData.pointerDrag.GetComponent<DraggableWord>();

        if (draggedWord != null)
        {
            if (draggedWord.placeholder != null)
            {
                draggedWord.placeholder.transform.SetParent(null);
                Destroy(draggedWord.placeholder);
            }

            if (draggedWord.parentAfterDrag.GetComponent<SentenceDropZone>() != null)
            {
                manaCapacity.UnequipWord(draggedWord.myWordData.wordData.manaCost);
                
                draggedWord.parentAfterDrag = transform;
                draggedWord.transform.SetParent(transform); 
                
                sentenceManager.RebuildSentenceFromUI(); 
            }
            else
            {
                draggedWord.parentAfterDrag = transform; 
                draggedWord.transform.SetParent(transform); 
            }

            draggedWord.Setup(draggedWord.myWordData);
        }
    }
}