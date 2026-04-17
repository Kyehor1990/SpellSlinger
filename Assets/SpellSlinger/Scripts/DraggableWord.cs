using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class DraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public bool isFromInventory;
    [HideInInspector] public int originalIndex;
    [HideInInspector] public GameObject placeholder;
    public OwnedWord myWordData; 
    
    [HideInInspector] public Transform parentAfterDrag; 
    private CanvasGroup canvasGroup;

    [Header("UI Referansları")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI manaText;

    [Header("Görsel Geri Bildirim (YENİ)")]
    public Image backgroundImage;
    public GameObject rightBoundaryVisual;
    public GameObject leftBoundaryVisual;
    
    public Color normalColor = Color.white;
    public Color activeModifierColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color outOfRangeColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Juice/GameFeel Efektleri")]
    public float scaleFactor = 1.15f;
    public float alphaValue = 0.8f;
    public float snapSpeed = 10f;
    private Transform topCanvas;         
    private Vector3 originalScale;

    private Transform highlightedRune = null;
    private Vector3 highlightedRuneOriginalScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if(canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        originalScale = transform.localScale;
    }

    private void Start() 
    {
        Setup(myWordData);
        ResetVisuals();
    }

    public void Setup(OwnedWord wordData)
    {
        myWordData = wordData;
        if (nameText != null) nameText.text = wordData.wordData.runeText;
        if (levelText != null) levelText.text = "Lvl " + wordData.level;
        if (manaText != null) manaText.text = "Mana: " + wordData.wordData.manaCost;

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void SetVisualState(bool isActiveModifier, bool isOutOfRange)
    {
        if (backgroundImage == null) return;

        if (isActiveModifier) backgroundImage.color = activeModifierColor;
        else if (isOutOfRange) backgroundImage.color = outOfRangeColor;
        else backgroundImage.color = normalColor;
    }

    public void ShowBoundary(bool showRight, bool showLeft)
    {
        if (rightBoundaryVisual != null) rightBoundaryVisual.SetActive(showRight);
        if (leftBoundaryVisual != null) leftBoundaryVisual.SetActive(showLeft);
    }

    public void ResetVisuals()
    {
        if (backgroundImage != null) backgroundImage.color = normalColor;
        if (rightBoundaryVisual != null) rightBoundaryVisual.SetActive(false);
        if (leftBoundaryVisual != null) leftBoundaryVisual.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (transform.parent != null && transform.parent.GetComponent<SentenceDropZone>() != null)
        {
            SpellBuilderUIFeedback.Instance?.PreviewPattern(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SpellBuilderUIFeedback.Instance?.ClearPreview();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        parentAfterDrag = transform.parent;
        isFromInventory = parentAfterDrag.GetComponent<InventoryDropZone>() != null; 
        topCanvas = GetComponentInParent<Canvas>().rootCanvas.transform;

        placeholder = new GameObject("Placeholder");
        RectTransform placeholderRt = placeholder.AddComponent<RectTransform>();
        placeholderRt.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        
        LayoutElement le = placeholder.AddComponent<LayoutElement>();
        le.preferredWidth = placeholderRt.sizeDelta.x;
        le.preferredHeight = placeholderRt.sizeDelta.y;

        placeholder.transform.SetParent(parentAfterDrag);
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        transform.SetParent(topCanvas);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;
        transform.localScale = originalScale * 1.15f; 
        
        SpellBuilderUIFeedback.Instance?.ClearPreview();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;

        SentenceDropZone sentenceZone = FindFirstObjectByType<SentenceDropZone>();
        if (sentenceZone == null) return;

        RectTransform zoneRect = sentenceZone.GetComponent<RectTransform>();
        if (RectTransformUtility.RectangleContainsScreenPoint(zoneRect, eventData.position, eventData.pressEventCamera))
        {
            Transform hoverTarget = GetHoveredRune(sentenceZone.transform, eventData);

            if (isFromInventory)
            {
                if (placeholder.transform.parent != sentenceZone.transform)
                    placeholder.transform.SetParent(sentenceZone.transform);

                if (hoverTarget != null)
                {
                    int targetIndex = hoverTarget.GetSiblingIndex();
                    if (eventData.position.x > hoverTarget.position.x) targetIndex++;
                    placeholder.transform.SetSiblingIndex(targetIndex);
                }
            }
            else
            {
                if (hoverTarget != null && hoverTarget != placeholder.transform)
                {
                    if (highlightedRune != hoverTarget)
                    {
                        ClearHighlight(); 
                        highlightedRune = hoverTarget;
                        highlightedRuneOriginalScale = highlightedRune.localScale;
                        highlightedRune.localScale = highlightedRuneOriginalScale * 1.15f; 
                        highlightedRune.GetComponent<Image>().color = Color.yellow; 
                    }
                }
                else
                {
                    ClearHighlight();
                }
            }
        }
        else
        {
            ClearHighlight();
            if (isFromInventory && placeholder.transform.parent != parentAfterDrag)
            {
                placeholder.transform.SetParent(parentAfterDrag);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        transform.localScale = originalScale;
        ClearHighlight();

        if (transform.parent == topCanvas)
        {
            transform.SetParent(parentAfterDrag);
            if (placeholder != null) transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
        }

        if (placeholder != null) Destroy(placeholder);
    }

    private Transform GetHoveredRune(Transform zone, PointerEventData eventData)
    {
        for (int i = 0; i < zone.childCount; i++)
        {
            Transform child = zone.GetChild(i);
            if (child == placeholder.transform) continue;

            RectTransform rect = child.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera))
            {
                return child;
            }
        }
        return null;
    }

    private void ClearHighlight()
    {
        if (highlightedRune != null)
        {
            highlightedRune.localScale = highlightedRuneOriginalScale;
            highlightedRune.GetComponent<Image>().color = normalColor; // Beyaza/Normal renge döndür
            highlightedRune = null;
        }
    }
}