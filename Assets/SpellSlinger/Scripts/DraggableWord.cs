using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class DraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public OwnedWord myWordData; 
    
    [HideInInspector] public Transform parentAfterDrag; 
    private CanvasGroup canvasGroup;

    [Header("UI Referansları")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI manaText;

    private Vector3 originalScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if(canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        originalScale = transform.localScale;
    }

    public void Setup(OwnedWord wordData)
    {
        myWordData = wordData;
        if (nameText != null) nameText.text = wordData.wordData.runeText;
        if (levelText != null) levelText.text = "Lvl " + wordData.level;
        if (manaText != null) manaText.text = "Mana: " + wordData.wordData.manaCost;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        parentAfterDrag = transform.parent; 
        Transform topCanvas = GetComponentInParent<Canvas>().rootCanvas.transform;
        transform.SetParent(topCanvas); 
        transform.SetAsLastSibling();
        
        canvasGroup.blocksRaycasts = false; 

        canvasGroup.alpha = 0.8f;
        transform.localScale = originalScale * 1.15f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position; 
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(parentAfterDrag); 
        canvasGroup.blocksRaycasts = true; 

        canvasGroup.alpha = 1f;
        transform.localScale = originalScale;
    }
}