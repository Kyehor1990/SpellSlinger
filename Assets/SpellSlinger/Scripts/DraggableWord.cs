using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class DraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public bool isFromInventory;
    [HideInInspector] public int originalIndex;
    [HideInInspector] public GameObject placeholder;
    [HideInInspector] public bool isHighlighted = false;
    public OwnedWord myWordData; 
    
    [HideInInspector] public Transform parentAfterDrag; 
    private CanvasGroup canvasGroup;

    [Header("UI Referansları")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI manaText;

    [Header("Görsel Geri Bildirim")]
    public Image backgroundImage;
    public GameObject rightBoundaryVisual;
    public GameObject leftBoundaryVisual;
    
    public Color normalColor = Color.white;
    public Color activeModifierColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color outOfRangeColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Juice: Parlama & Gölge")]
    public Image glowImage; // BackgroundImage'ın İÇİNDE (child) ve Stretch-Stretch ayarlı olmalı
    public Color glowNormalColor = new Color(0f, 0f, 0f, 0.3f); 
    public Color glowActiveColor = new Color(0.2f, 1f, 0.2f, 0.6f); 

    [Header("Juice: Hassasiyet Ayarları")]
    public float scaleFactor = 1.15f;
    public float alphaValue = 0.8f;
    [Range(0.1f, 3.0f)] public float tiltSensitivity = 1.2f; // Sürükleme hızı çarpanı (Önerilen: 1.2f)
    public float maxTiltAngle = 20f; // Kart en fazla kaç derece yatabilir
    
    private Transform topCanvas;         
    private Vector3 originalScale;
    
    private Color originalNameColor;
    private Color originalManaColor;

    private Transform highlightedRune = null;
    private Vector3 highlightedRuneOriginalScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if(canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        originalScale = transform.localScale;

        if (nameText != null) originalNameColor = nameText.color;
        if (manaText != null) originalManaColor = manaText.color;
        
        if (glowImage != null) glowImage.color = Color.clear; // Başlangıçta gölge gizli
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

        // ContentSizeFitter'ın anında güncellenmesi için:
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void SetVisualState(bool isActiveModifier, bool isOutOfRange)
    {
        if (backgroundImage == null) return;
        
        isHighlighted = isActiveModifier; 
        backgroundImage.DOKill(); 
        transform.DOKill(false); 
        
        if (glowImage != null) { glowImage.DOKill(); glowImage.transform.DOKill(); }
        if (manaText != null) manaText.transform.DOKill(true);

        if (isActiveModifier) 
        {
            backgroundImage.DOColor(activeModifierColor, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true);
            transform.DOScale(originalScale * scaleFactor, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);

            if (manaText != null) {
                manaText.color = Color.black; 
                manaText.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.35f, 5, 1f).SetUpdate(true);
            }

            if (glowImage != null) {
                glowImage.DOColor(glowActiveColor, 0.25f).SetUpdate(true);
                glowImage.transform.DOScale(1.05f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
            }
        }
        else if (isOutOfRange) 
        {
            backgroundImage.DOColor(outOfRangeColor, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true);
            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
            ResetTextAndGlow();
        }
        else 
        {
            backgroundImage.DOColor(normalColor, 0.25f).SetEase(Ease.OutQuad).SetUpdate(true);
            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
            ResetTextAndGlow();
        }
    }

    private void ResetTextAndGlow()
    {
        if (manaText != null) manaText.color = originalManaColor;
        
        if (glowImage != null && !isHighlighted) {
            glowImage.transform.DOKill();
            glowImage.transform.localScale = Vector3.one;
            glowImage.DOColor(Color.clear, 0.25f).SetUpdate(true);
        }
    }

    public void ShowBoundary(bool showRight, bool showLeft)
    {
        if (showRight && rightBoundaryVisual != null)
        {
            rightBoundaryVisual.SetActive(true);
            rightBoundaryVisual.transform.localScale = Vector3.one;
            rightBoundaryVisual.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.3f, 5, 1f).SetUpdate(true);
        }
        
        if (showLeft && leftBoundaryVisual != null)
        {
            leftBoundaryVisual.SetActive(true);
            leftBoundaryVisual.transform.localScale = Vector3.one;
            leftBoundaryVisual.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.3f, 5, 1f).SetUpdate(true);
        }
    }

    public void ResetVisuals()
    {
        isHighlighted = false; 
        if (backgroundImage != null) 
        {
            backgroundImage.DOKill();
            transform.DOKill(false);
            
            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
            backgroundImage.DOColor(normalColor, 0.2f).SetUpdate(true);
            
            transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
        
        ResetTextAndGlow();

        if (rightBoundaryVisual != null) rightBoundaryVisual.SetActive(false);
        if (leftBoundaryVisual != null) leftBoundaryVisual.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOKill(false);
        transform.DOScale(originalScale * 1.05f, 0.15f).SetEase(Ease.OutBack).SetUpdate(true);

        if (nameText != null) {
            nameText.transform.DOKill(true);
            nameText.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.2f, 5, 1f).SetUpdate(true);
        }

        if (glowImage != null && !isHighlighted) {
            glowImage.DOKill();
            glowImage.DOColor(glowNormalColor, 0.2f).SetUpdate(true);
        }

        if (transform.parent != null && transform.parent.GetComponent<SentenceDropZone>() != null)
        {
            SpellBuilderUIFeedback.Instance?.PreviewPattern(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill(false);
        transform.DOScale(originalScale, 0.15f).SetEase(Ease.OutQuad).SetUpdate(true);
        
        if (glowImage != null && !isHighlighted) {
            glowImage.DOKill();
            glowImage.DOColor(Color.clear, 0.2f).SetUpdate(true);
        }

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
        canvasGroup.alpha = alphaValue;

        transform.DOKill(false);
        transform.DOScale(originalScale * scaleFactor, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
        
        if (glowImage != null) {
            glowImage.DOKill();
            glowImage.DOColor(glowNormalColor, 0.2f).SetUpdate(true);
        }

        SpellBuilderUIFeedback.Instance?.ClearPreview();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;

        // Kartın sürükleme hızına (delta.x) ve hassasiyetine göre eğilmesi
        float tiltAmount = Mathf.Clamp(eventData.delta.x * -tiltSensitivity, -maxTiltAngle, maxTiltAngle);
        transform.DORotate(new Vector3(0, 0, tiltAmount), 0.15f).SetUpdate(true);

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

                        highlightedRune.DOKill(false);
                        highlightedRune.DOScale(highlightedRuneOriginalScale * scaleFactor, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
                        
                        Image highlightImage = highlightedRune.GetComponent<Image>();
                        if(highlightImage != null)
                        {
                            highlightImage.DOKill();
                            highlightImage.DOColor(Color.yellow, 0.2f).SetUpdate(true); 
                        }
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
        ClearHighlight();

        if (transform.parent == topCanvas)
        {
            transform.SetParent(parentAfterDrag);
            if (placeholder != null) transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
        }

        if (placeholder != null) Destroy(placeholder);

        transform.DOKill(false);
        transform.localScale = originalScale;
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.35f, 10, 1f).SetUpdate(true);
        
        // Eğilmeyi tatlı bir yaylanma efektiyle sıfırlar
        transform.DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutElastic).SetUpdate(true);

        if (glowImage != null && !isHighlighted) {
            glowImage.DOKill();
            glowImage.DOColor(Color.clear, 0.3f).SetUpdate(true);
        }
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
            highlightedRune.DOKill(false);
            highlightedRune.DOScale(highlightedRuneOriginalScale, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
            
            Image highlightImage = highlightedRune.GetComponent<Image>();
            if(highlightImage != null)
            {
                highlightImage.DOKill();
                highlightImage.DOColor(normalColor, 0.2f).SetUpdate(true); 
            }
            
            highlightedRune = null;
        }
    }
}