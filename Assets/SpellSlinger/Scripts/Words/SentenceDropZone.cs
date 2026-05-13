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
                Transform targetRune = GetRuneUnderPointer(eventData, draggedWord);

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
                    int endIndex = GetRightEdgeInsertionIndex(eventData, draggedWord);
                    draggedWord.parentAfterDrag = transform;
                    draggedWord.transform.SetParent(transform);
                    draggedWord.transform.SetSiblingIndex(endIndex >= 0 ? endIndex : draggedWord.placeholder.transform.GetSiblingIndex());
                    draggedWord.placeholder.transform.SetParent(null);
                }
            }

            if (draggedWord.placeholder != null) Destroy(draggedWord.placeholder);
            sentenceManager.RebuildSentenceFromUI(); 
        }
    }

    private Transform GetRuneUnderPointer(PointerEventData eventData, DraggableWord draggedWord)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (draggedWord != null && draggedWord.placeholder != null && child == draggedWord.placeholder.transform) continue;
            if (draggedWord != null && child == draggedWord.transform) continue;
            if (!child.TryGetComponent(out DraggableWord word) || word == null) continue;

            RectTransform rect = child as RectTransform;
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera))
            {
                return child;
            }
        }

        return null;
    }

    private int GetRightEdgeInsertionIndex(PointerEventData eventData, DraggableWord draggedWord)
    {
        Transform rightmostRune = null;
        float rightmostX = float.NegativeInfinity;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (draggedWord != null && draggedWord.placeholder != null && child == draggedWord.placeholder.transform) continue;
            if (draggedWord != null && child == draggedWord.transform) continue;
            if (!child.TryGetComponent(out DraggableWord word) || word == null) continue;

            if (child.position.x > rightmostX)
            {
                rightmostX = child.position.x;
                rightmostRune = child;
            }
        }

        if (rightmostRune == null || eventData.position.x <= rightmostRune.position.x) return -1;
        return rightmostRune.GetSiblingIndex() + 1;
    }
}
