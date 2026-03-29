using UnityEngine;
using UnityEngine.EventSystems;

public class SentenceDropZone : MonoBehaviour, IDropHandler
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
            int cost = draggedWord.myWordData.wordData.manaCost;

            if (draggedWord.isFromInventory)
            {
                if (manaCapacity.CanEquipWord(cost))
                {
                    manaCapacity.EquipWord(cost);
                    draggedWord.parentAfterDrag = transform;
                    draggedWord.transform.SetParent(transform);
                    
                    draggedWord.transform.SetSiblingIndex(draggedWord.placeholder.transform.GetSiblingIndex());

                    draggedWord.placeholder.transform.SetParent(null); 
                }
            }
            else 
            {
                Transform targetRune = null;
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child == draggedWord.placeholder.transform) continue;

                    RectTransform rect = child.GetComponent<RectTransform>();
                    if (RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera))
                    {
                        targetRune = child;
                        break;
                    }
                }

                if (targetRune != null)
                {
                    int pIndex = draggedWord.placeholder.transform.GetSiblingIndex();
                    int tIndex = targetRune.GetSiblingIndex();

                    draggedWord.parentAfterDrag = transform;
                    draggedWord.transform.SetParent(transform);

                    draggedWord.transform.SetSiblingIndex(tIndex);
                    targetRune.SetSiblingIndex(pIndex);

                    draggedWord.placeholder.transform.SetParent(null);
                }
                else
                {
                    draggedWord.parentAfterDrag = transform;
                    draggedWord.transform.SetParent(transform);
                    draggedWord.transform.SetSiblingIndex(draggedWord.placeholder.transform.GetSiblingIndex());
                    draggedWord.placeholder.transform.SetParent(null);
                }
            }

            if (draggedWord.placeholder != null) Destroy(draggedWord.placeholder);
            sentenceManager.RebuildSentenceFromUI(); 
        }
    }
}