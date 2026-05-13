using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public enum RuneFeedbackState
{
    Normal,
    SelectedObject,
    ReadModifier,
    UnusedModifier,
    SharedModifier,
    BlockingObject,
    BoundaryStop
}

public class DraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public bool isFromInventory;
    [HideInInspector] public int originalIndex;
    [HideInInspector] public GameObject placeholder;
    [HideInInspector] public bool isHighlighted = false;
    public OwnedWord myWordData; 
    public RuneFeedbackState CurrentFeedbackState { get; private set; } = RuneFeedbackState.Normal;
    
    [HideInInspector] public Transform parentAfterDrag; 
    private CanvasGroup canvasGroup;

    [Header("UI Referansları")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI manaText;
    [SerializeField] private TMP_Text sharedCountText;
    [SerializeField] private Image sharedCountBadgeImage;
    [SerializeField] private Canvas sharedCountCanvas;
    [SerializeField] private int sharedCountSortingOrder = 60;
    [SerializeField] private Color sharedCountBadgeColor = new Color(0.98f, 0.76f, 0.24f, 0.98f);
    [SerializeField] private Color sharedCountBadgeGlowColor = new Color(0.16f, 0.09f, 0.03f, 0.72f);
    [SerializeField] private Color sharedCountTextColor = new Color(0.08f, 0.06f, 0.02f, 1f);

    [Header("Görsel Geri Bildirim")]
    public Image backgroundImage;
    [SerializeField] private Material hoveredObjectGlowMaterial;
    [SerializeField] private Material readModifierAuraMaterial;
    [SerializeField] private Material sharedModifierOverchargeMaterial;
    public GameObject rightBoundaryVisual;
    public GameObject leftBoundaryVisual;
    
    public Color normalColor = Color.white;
    public Color activeModifierColor = new Color(0.9f, 0.98f, 1f, 1f);
    public Color activeObjectColor = new Color(0.98f, 0.985f, 1f, 1f);
    public Color boundaryObjectColor = new Color(1f, 0.92f, 0.97f, 1f);
    public Color boundaryObjectFlashColor = new Color(1f, 0.18f, 0.62f, 1f);
    public Color outOfRangeColor = new Color(0.82f, 0.88f, 0.94f, 0.55f);

    [Header("Juice: Parlama & Gölge")]
    public Image glowImage; // BackgroundImage'ın İÇİNDE (child) ve Stretch-Stretch ayarlı olmalı
    public Color glowNormalColor = new Color(0.28f, 0.34f, 1f, 0.14f); 
    public Color glowActiveColor = new Color(0.24f, 0.96f, 1f, 0.2f); 

    [Header("Arcane Frame Polish")]
    [SerializeField] private bool createRuntimeStateFrame = true;
    [SerializeField] private Color selectedObjectFrameColor = new Color(0.48f, 0.66f, 1f, 0.96f);
    [SerializeField] private Color selectedObjectCornerColor = new Color(0.78f, 0.52f, 1f, 0.98f);
    [SerializeField] private Color selectedObjectSourceNodeColor = new Color(0.72f, 0.94f, 1f, 0.98f);
    [SerializeField] private Color selectedObjectSourceGlowColor = new Color(0.36f, 0.34f, 1f, 0.28f);
    [SerializeField] private Color readModifierFrameColor = new Color(0.26f, 0.9f, 1f, 0.58f);
    [SerializeField] private Color boundaryFrameColor = new Color(1f, 0.08f, 0.42f, 0.76f);
    [SerializeField] private Color unusedFrameColor = new Color(0.46f, 0.58f, 0.68f, 0.18f);
    [SerializeField] private float framePulseScale = 1.08f;

    [Header("Juice: Hassasiyet Ayarları")]
    public float scaleFactor = 1.15f;
    public float alphaValue = 0.8f;
    [Range(0.1f, 3.0f)] public float tiltSensitivity = 1.2f; // Sürükleme hızı çarpanı (Önerilen: 1.2f)
    public float maxTiltAngle = 20f; // Kart en fazla kaç derece yatabilir
    
    private Transform topCanvas;         
    private Vector3 originalScale;
    
    private Color originalNameColor;
    private Color originalManaColor;
    private Material originalBackgroundMaterial;
    private bool originalBackgroundMaterialCaptured;
    private bool feedbackMaterialApplied;
    private int currentSharedCount;
    private Vector3 originalSharedCountScale = Vector3.one;
    private Vector3 originalSharedBadgeScale = Vector3.one;
    private bool originalSharedCountScaleCaptured;
    private bool originalSharedBadgeScaleCaptured;
    private Canvas sharedCountBadgeCanvas;
    private static Sprite sharedCountBadgeSprite;
    private static Sprite boundaryEdgeSealSprite;
    private static Sprite softRectSprite;
    private static Sprite pixelNodeSprite;
    private static Sprite pixelNodeGlowSprite;

    private RectTransform stateFrameLayer;
    private Image stateUnderGlowImage;
    private Image[] stateEdgeImages;
    private Image[] stateCornerImages;
    private Image stateSourceNodeGlowImage;
    private Image stateSourceNodeCoreImage;
    private Image stateSourceNodeMarkImage;

    private Transform highlightedRune = null;
    private Vector3 highlightedRuneOriginalScale;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if(canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        originalScale = transform.localScale;

        if (nameText != null) originalNameColor = nameText.color;
        if (manaText != null) originalManaColor = manaText.color;
        CaptureOriginalSharedCountScale();
        CaptureOriginalBackgroundMaterial();
        ConfigureSharedCountLayer();
        ConfigureBoundaryVisual(rightBoundaryVisual);
        ConfigureBoundaryVisual(leftBoundaryVisual);
        EnsureStateFrameVisuals();
        PrewarmFeedbackVisuals();
        
        if (glowImage != null)
        {
            glowImage.raycastTarget = false;
            glowImage.color = Color.clear; // Başlangıçta gölge gizli
        }

        ApplyCardVisualsImmediate(normalColor, Color.clear, Color.clear);
    }

    private void Start() 
    {
        if (myWordData != null && myWordData.wordData != null)
        {
            Setup(myWordData);
        }

        ResetVisualsImmediate();
    }

    public void Setup(OwnedWord wordData)
    {
        myWordData = wordData;
        if (wordData == null || wordData.wordData == null) return;

        if (nameText != null) nameText.text = wordData.wordData.runeText;
        if (levelText != null) levelText.text = "Lvl " + wordData.level;
        if (manaText != null) manaText.text = "Mana: " + wordData.wordData.manaCost;
        SetSharedCount(0);

        // ContentSizeFitter'ın anında güncellenmesi için:
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void SetSharedCount(int sharedCount)
    {
        int previousSharedCount = currentSharedCount;
        currentSharedCount = sharedCount;

        if (sharedCountText == null) return;
        ConfigureSharedCountLayer();
        CaptureOriginalSharedCountScale();

        bool shouldShow = myWordData != null &&
                          myWordData.wordData != null &&
                          myWordData.wordData.wordType == WordType.Modifier &&
                          sharedCount >= 2;

        bool wasShowing = sharedCountText.gameObject.activeSelf;
        sharedCountText.gameObject.SetActive(shouldShow);
        SetSharedBadgeVisible(shouldShow);
        if (shouldShow)
        {
            sharedCountText.text = "x" + sharedCount;
            sharedCountText.color = sharedCountTextColor;
            if (!wasShowing || previousSharedCount != sharedCount)
            {
                sharedCountText.transform.DOKill(true);
                sharedCountText.transform.localScale = originalSharedCountScale;
                sharedCountText.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.18f, 4, 0.7f).SetUpdate(true);
                if (sharedCountBadgeImage != null)
                {
                    sharedCountBadgeImage.transform.DOKill(true);
                    sharedCountBadgeImage.transform.localScale = originalSharedBadgeScale;
                    sharedCountBadgeImage.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.18f, 4, 0.7f).SetUpdate(true);
                }
            }
        }
        else
        {
            sharedCountText.transform.DOKill(true);
            sharedCountText.transform.localScale = originalSharedCountScale;
            if (sharedCountBadgeImage != null)
            {
                sharedCountBadgeImage.transform.DOKill(true);
                sharedCountBadgeImage.transform.localScale = originalSharedBadgeScale;
            }
        }
    }

    private void ConfigureSharedCountLayer()
    {
        if (sharedCountText == null) return;

        Transform sharedCountTransform = sharedCountText.transform;
        EnsureSharedCountBadge();
        if (sharedCountTransform != null)
        {
            sharedCountTransform.SetAsLastSibling();
        }

        sharedCountText.raycastTarget = false;
        sharedCountText.alignment = TextAlignmentOptions.Center;
        sharedCountText.fontSize = 13f;
        sharedCountText.fontStyle = FontStyles.Bold;
        sharedCountText.textWrappingMode = TextWrappingModes.NoWrap;
        sharedCountText.overflowMode = TextOverflowModes.Overflow;

        Shadow textShadow = sharedCountText.GetComponent<Shadow>();
        if (textShadow == null)
        {
            textShadow = sharedCountText.gameObject.AddComponent<Shadow>();
        }

        textShadow.effectColor = new Color(1f, 0.96f, 0.62f, 0.5f);
        textShadow.effectDistance = new Vector2(1f, -1f);
        textShadow.useGraphicAlpha = true;

        RectTransform badgeRect = sharedCountText.rectTransform;
        if (badgeRect != null)
        {
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-3f, -22f);
            badgeRect.sizeDelta = new Vector2(28f, 18f);
            badgeRect.localRotation = Quaternion.identity;
        }

        if (sharedCountBadgeImage != null)
        {
            sharedCountBadgeImage.color = sharedCountBadgeColor;
            sharedCountBadgeImage.raycastTarget = false;
            sharedCountBadgeImage.sprite = GetSharedCountBadgeSprite();
            sharedCountBadgeImage.type = Image.Type.Simple;

            Shadow badgeGlow = sharedCountBadgeImage.GetComponent<Shadow>();
            if (badgeGlow == null)
            {
                badgeGlow = sharedCountBadgeImage.gameObject.AddComponent<Shadow>();
            }

            badgeGlow.effectColor = sharedCountBadgeGlowColor;
            badgeGlow.effectDistance = new Vector2(1f, -1f);
            badgeGlow.useGraphicAlpha = true;

            Outline badgeOutline = sharedCountBadgeImage.GetComponent<Outline>();
            if (badgeOutline == null)
            {
                badgeOutline = sharedCountBadgeImage.gameObject.AddComponent<Outline>();
            }

            badgeOutline.effectColor = new Color(1f, 0.98f, 0.66f, 0.66f);
            badgeOutline.effectDistance = new Vector2(1f, 0f);
            badgeOutline.useGraphicAlpha = true;

            RectTransform sharedBadgeRect = sharedCountBadgeImage.rectTransform;
            if (sharedBadgeRect != null)
            {
                sharedBadgeRect.anchorMin = badgeRect.anchorMin;
                sharedBadgeRect.anchorMax = badgeRect.anchorMax;
                sharedBadgeRect.pivot = badgeRect.pivot;
                sharedBadgeRect.anchoredPosition = badgeRect.anchoredPosition;
                sharedBadgeRect.sizeDelta = badgeRect.sizeDelta;
                sharedBadgeRect.localRotation = Quaternion.identity;
            }
        }

        if (sharedCountCanvas == null)
        {
            sharedCountCanvas = sharedCountText.GetComponent<Canvas>();
        }

        if (sharedCountCanvas == null)
        {
            sharedCountCanvas = sharedCountText.gameObject.AddComponent<Canvas>();
        }

        if (sharedCountCanvas != null)
        {
            sharedCountCanvas.overrideSorting = true;
            sharedCountCanvas.sortingOrder = sharedCountSortingOrder;
        }

        ConfigureSharedBadgeCanvas();
        CaptureOriginalSharedBadgeScale();
    }

    public void SetFeedbackState(RuneFeedbackState feedbackState)
    {
        SetFeedbackState(feedbackState, false);
    }

    public void SetFeedbackState(RuneFeedbackState feedbackState, bool isSelected)
    {
        CurrentFeedbackState = feedbackState;

        switch (feedbackState)
        {
            case RuneFeedbackState.Normal:
                ResetVisuals();
                break;

            case RuneFeedbackState.SelectedObject:
                ApplyObjectVisualState(isSelected);
                break;

            case RuneFeedbackState.ReadModifier:
                ApplyReadModifierVisualState();
                break;

            case RuneFeedbackState.UnusedModifier:
                ApplyUnusedModifierVisualState();
                break;

            case RuneFeedbackState.SharedModifier:
                ApplySharedModifierVisualState();
                break;

            case RuneFeedbackState.BlockingObject:
                ApplyBoundaryObjectVisualState();
                break;

            case RuneFeedbackState.BoundaryStop:
                ApplyBoundaryObjectVisualState();
                break;
        }
    }

    public void SetVisualState(bool isActiveModifier, bool isOutOfRange)
    {
        if (backgroundImage == null) return;

        RestoreOriginalBackgroundMaterial();
        
        isHighlighted = isActiveModifier; 
        backgroundImage.DOKill(); 
        transform.DOKill(false); 
        
        if (glowImage != null) { glowImage.DOKill(); glowImage.transform.DOKill(); }
        if (manaText != null) manaText.transform.DOKill(true);

        if (isActiveModifier) 
        {
            ApplyCardVisuals(activeModifierColor, readModifierFrameColor, WithAlpha(readModifierFrameColor, 0.12f), 0.25f);
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
            ApplyCardVisuals(outOfRangeColor, unusedFrameColor, Color.clear, 0.25f);
            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
            ResetTextAndGlow();
        }
        else 
        {
            ApplyCardVisuals(normalColor, Color.clear, Color.clear, 0.25f);
            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
            ResetTextAndGlow();
        }
    }

    private void ApplyReadModifierVisualState()
    {
        if (backgroundImage == null) return;

        RestoreOriginalBackgroundMaterial();
        isHighlighted = true;
        KillCardVisualTweens(true);
        if (manaText != null) manaText.transform.DOKill(true);

        ApplyReadModifierAuraMaterial();
        Color frameColor = readModifierFrameColor;
        ApplyCardVisuals(activeModifierColor, frameColor, WithAlpha(frameColor, 0.1f), 0.18f);
        transform.DOScale(originalScale * 1.06f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);

        if (manaText != null)
        {
            manaText.color = Color.black;
        }

        if (glowImage != null)
        {
            glowImage.transform.localScale = Vector3.one;
            glowImage.DOColor(glowActiveColor, 0.18f).SetUpdate(true);
        }
    }

    private void ApplyUnusedModifierVisualState()
    {
        if (backgroundImage == null) return;

        RestoreOriginalBackgroundMaterial();
        isHighlighted = false;
        KillCardVisualTweens(true);

        ApplyCardVisuals(outOfRangeColor, unusedFrameColor, Color.clear, 0.18f);
        transform.DOScale(originalScale * 0.96f, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true);
        ResetTextAndGlow();
    }

    private void ApplySharedModifierVisualState()
    {
        // Shared usage is intentionally badge-only; no card fill, glow, or frame state.
    }

    public void SetObjectVisualState(bool isSelected)
    {
        SetFeedbackState(RuneFeedbackState.SelectedObject, isSelected);
    }

    private void ApplyObjectVisualState(bool isSelected)
    {
        if (backgroundImage == null) return;

        isHighlighted = true;
        KillCardVisualTweens(true);

        float targetScale = 1.07f;
        ApplyHoveredObjectGlowMaterial();
        ApplyCardVisuals(activeObjectColor, selectedObjectFrameColor, WithAlpha(selectedObjectFrameColor, 0.11f), 0.18f, true, selectedObjectCornerColor);
        TweenSourceNode(true, 0.18f, true);
        transform.DOScale(originalScale * targetScale, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);

        if (glowImage != null)
        {
            glowImage.transform.localScale = Vector3.one;
            glowImage.DOColor(WithAlpha(selectedObjectSourceGlowColor, 0.18f), 0.18f).SetUpdate(true);
        }
    }

    public void SetBoundaryObjectVisualState()
    {
        SetFeedbackState(RuneFeedbackState.BlockingObject);
    }

    private void ApplyBoundaryObjectVisualState()
    {
        RestoreOriginalBackgroundMaterial();
        isHighlighted = true;

        KillCardVisualTweens(true);
        ApplyCardVisuals(boundaryObjectColor, boundaryFrameColor, WithAlpha(boundaryFrameColor, 0.16f), 0.16f, true);

        transform.localScale = originalScale;
        Sequence pulseSequence = DOTween.Sequence().SetTarget(transform).SetUpdate(true);
        pulseSequence.Append(transform.DOScale(originalScale * 1.08f, 0.1f).SetEase(Ease.OutQuad));
        pulseSequence.Append(transform.DOScale(originalScale * 0.99f, 0.08f).SetEase(Ease.InOutQuad));
        pulseSequence.Append(transform.DOScale(originalScale * 1.03f, 0.14f).SetEase(Ease.OutBack));

        if (glowImage != null)
        {
            glowImage.DOKill();
            glowImage.transform.localScale = Vector3.one;
            glowImage.DOColor(WithAlpha(boundaryFrameColor, 0.16f), 0.16f).SetUpdate(true);
        }
    }

    private void ResetTextAndGlow()
    {
        if (nameText != null) nameText.color = originalNameColor;
        if (manaText != null) manaText.color = originalManaColor;
        
        if (glowImage != null && !isHighlighted) {
            glowImage.transform.DOKill();
            glowImage.transform.localScale = Vector3.one;
            glowImage.DOColor(Color.clear, 0.25f).SetUpdate(true);
        }
    }

    private void ResetTextAndGlowImmediate()
    {
        if (nameText != null) nameText.color = originalNameColor;
        if (manaText != null) manaText.color = originalManaColor;

        if (glowImage != null)
        {
            glowImage.DOKill();
            glowImage.transform.DOKill();
            glowImage.transform.localScale = Vector3.one;
            glowImage.color = Color.clear;
        }
    }

    public void ShowBoundary(bool showRight, bool showLeft)
    {
        if (showRight && rightBoundaryVisual != null)
        {
            ConfigureBoundaryVisual(rightBoundaryVisual);
            rightBoundaryVisual.transform.DOKill();
            rightBoundaryVisual.SetActive(true);
            rightBoundaryVisual.transform.SetAsLastSibling();
            rightBoundaryVisual.transform.localScale = Vector3.one;
            rightBoundaryVisual.transform.DOPunchScale(new Vector3(0.16f, 0.16f, 0f), 0.24f, 4, 0.8f).SetUpdate(true);
        }
        
        if (showLeft && leftBoundaryVisual != null)
        {
            ConfigureBoundaryVisual(leftBoundaryVisual);
            leftBoundaryVisual.transform.DOKill();
            leftBoundaryVisual.SetActive(true);
            leftBoundaryVisual.transform.SetAsLastSibling();
            leftBoundaryVisual.transform.localScale = Vector3.one;
            leftBoundaryVisual.transform.DOPunchScale(new Vector3(0.16f, 0.16f, 0f), 0.24f, 4, 0.8f).SetUpdate(true);
        }
    }

    public void ResetVisuals()
    {
        ResetVisuals(false);
    }

    public void ResetVisualsImmediate()
    {
        ResetVisuals(true);
    }

    private void ResetVisuals(bool immediate)
    {
        CurrentFeedbackState = RuneFeedbackState.Normal;
        isHighlighted = false; 
        RestoreOriginalBackgroundMaterial();
        KillCardVisualTweens(true);

        if (immediate)
        {
            transform.localScale = originalScale;
            transform.localRotation = Quaternion.identity;
            ApplyCardVisualsImmediate(normalColor, Color.clear, Color.clear);
            ResetTextAndGlowImmediate();
        }
        else
        {
            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
            ApplyCardVisuals(normalColor, Color.clear, Color.clear, 0.2f);
            transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
            ResetTextAndGlow();
        }

        if (rightBoundaryVisual != null)
        {
            rightBoundaryVisual.transform.DOKill();
            rightBoundaryVisual.SetActive(false);
        }
        if (leftBoundaryVisual != null)
        {
            leftBoundaryVisual.transform.DOKill();
            leftBoundaryVisual.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        bool isSentenceRune = transform.parent != null && transform.parent.GetComponent<SentenceDropZone>() != null;

        if (isSentenceRune)
        {
            if (nameText != null) {
                nameText.transform.DOKill(true);
                nameText.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0), 0.18f, 4, 0.8f).SetUpdate(true);
            }

            SpellBuilderUIFeedback.Instance?.PreviewPattern(this);
            return;
        }

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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill(false);
        transform.DOScale(originalScale, 0.15f).SetEase(Ease.OutQuad).SetUpdate(true);
        
        if (glowImage != null && !isHighlighted) {
            glowImage.DOKill();
            glowImage.DOColor(Color.clear, 0.2f).SetUpdate(true);
        }

        SpellBuilderUIFeedback.Instance?.ClearHoverPreview();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        SpellBuilderUIFeedback.Instance?.ClearPreview();
        RestoreOriginalBackgroundMaterial();

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
                else
                {
                    int endIndex = GetRightEdgeInsertionIndex(sentenceZone.transform, eventData);
                    if (endIndex >= 0)
                    {
                        placeholder.transform.SetSiblingIndex(endIndex);
                    }
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
        RestoreOriginalBackgroundMaterial();
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

        SpellBuilderUIFeedback.Instance?.ClearPreview();
    }

    private void CaptureOriginalBackgroundMaterial()
    {
        if (backgroundImage == null || originalBackgroundMaterialCaptured) return;

        Material currentMaterial = backgroundImage.material;
        originalBackgroundMaterial = currentMaterial != null && currentMaterial != backgroundImage.defaultMaterial
            ? currentMaterial
            : null;
        originalBackgroundMaterialCaptured = true;
    }

    private void CaptureOriginalSharedCountScale()
    {
        if (sharedCountText == null || originalSharedCountScaleCaptured) return;

        originalSharedCountScale = sharedCountText.transform.localScale;
        originalSharedCountScaleCaptured = true;
    }

    private void CaptureOriginalSharedBadgeScale()
    {
        if (sharedCountBadgeImage == null || originalSharedBadgeScaleCaptured) return;

        originalSharedBadgeScale = sharedCountBadgeImage.transform.localScale;
        originalSharedBadgeScaleCaptured = true;
    }

    private void EnsureSharedCountBadge()
    {
        if (sharedCountText == null || sharedCountBadgeImage != null) return;

        Transform parent = sharedCountText.transform.parent;
        if (parent == null) return;

        Transform existingBadge = parent.Find("SharedCountBadge");
        if (existingBadge != null)
        {
            sharedCountBadgeImage = existingBadge.GetComponent<Image>();
        }

        if (sharedCountBadgeImage == null)
        {
            GameObject badgeObject = new GameObject("SharedCountBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            badgeObject.layer = gameObject.layer;
            badgeObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = badgeObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            layoutElement.layoutPriority = 99;

            sharedCountBadgeImage = badgeObject.GetComponent<Image>();
        }

        if (sharedCountBadgeImage != null)
        {
            sharedCountBadgeImage.transform.SetAsLastSibling();
            sharedCountText.transform.SetAsLastSibling();
            sharedCountBadgeImage.raycastTarget = false;
            sharedCountBadgeImage.gameObject.SetActive(sharedCountText.gameObject.activeSelf);
        }
    }

    private void ConfigureSharedBadgeCanvas()
    {
        if (sharedCountBadgeImage == null) return;
        if (sharedCountBadgeImage.gameObject == sharedCountText.gameObject) return;

        if (sharedCountBadgeCanvas == null)
        {
            sharedCountBadgeCanvas = sharedCountBadgeImage.GetComponent<Canvas>();
        }

        if (sharedCountBadgeCanvas == null)
        {
            sharedCountBadgeCanvas = sharedCountBadgeImage.gameObject.AddComponent<Canvas>();
        }

        sharedCountBadgeCanvas.overrideSorting = true;
        sharedCountBadgeCanvas.sortingOrder = sharedCountSortingOrder - 1;
    }

    private void SetSharedBadgeVisible(bool isVisible)
    {
        if (sharedCountBadgeImage == null || sharedCountBadgeImage.gameObject == sharedCountText.gameObject) return;

        sharedCountBadgeImage.gameObject.SetActive(isVisible);
    }

    private void ConfigureBoundaryVisual(GameObject boundaryVisual)
    {
        if (boundaryVisual == null) return;

        Image boundaryImage = boundaryVisual.GetComponent<Image>();
        if (boundaryImage != null)
        {
            boundaryImage.color = boundaryFrameColor;
            boundaryImage.sprite = GetBoundaryEdgeSealSprite();
            boundaryImage.type = Image.Type.Simple;
            boundaryImage.raycastTarget = false;
        }

        RectTransform boundaryRect = boundaryVisual.GetComponent<RectTransform>();
        if (boundaryRect != null)
        {
            boundaryRect.sizeDelta = new Vector2(8f, 42f);
        }
    }

    private void EnsureStateFrameVisuals()
    {
        if (!createRuntimeStateFrame || backgroundImage == null) return;
        if (stateFrameLayer != null) return;

        Transform existingLayer = transform.Find("RuneStateFrame");
        if (existingLayer != null)
        {
            stateFrameLayer = existingLayer as RectTransform;
        }

        if (stateFrameLayer == null)
        {
            GameObject layerObject = new GameObject("RuneStateFrame", typeof(RectTransform), typeof(LayoutElement));
            layerObject.layer = gameObject.layer;
            layerObject.transform.SetParent(transform, false);

            stateFrameLayer = layerObject.GetComponent<RectTransform>();
            stateFrameLayer.anchorMin = Vector2.zero;
            stateFrameLayer.anchorMax = Vector2.one;
            stateFrameLayer.offsetMin = Vector2.zero;
            stateFrameLayer.offsetMax = Vector2.zero;

            LayoutElement layoutElement = layerObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
        }

        int targetSiblingIndex = glowImage != null ? glowImage.transform.GetSiblingIndex() + 1 : 0;
        stateFrameLayer.SetSiblingIndex(Mathf.Clamp(targetSiblingIndex, 0, transform.childCount - 1));

        stateUnderGlowImage = EnsureFrameImage("Underglow", stateFrameLayer);
        ConfigureStretchImage(stateUnderGlowImage.rectTransform, new Vector2(-12f, -10f), new Vector2(12f, 10f));
        stateUnderGlowImage.sprite = GetSoftRectSprite();
        stateUnderGlowImage.color = Color.clear;

        stateEdgeImages ??= new Image[4];
        stateEdgeImages[0] = EnsureFrameImage("EdgeTop", stateFrameLayer);
        ConfigureAnchoredImage(stateEdgeImages[0].rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1f), new Vector2(0f, 3f));
        stateEdgeImages[1] = EnsureFrameImage("EdgeBottom", stateFrameLayer);
        ConfigureAnchoredImage(stateEdgeImages[1].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 1f), new Vector2(0f, 3f));
        stateEdgeImages[2] = EnsureFrameImage("EdgeLeft", stateFrameLayer);
        ConfigureAnchoredImage(stateEdgeImages[2].rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(3f, 0f));
        stateEdgeImages[3] = EnsureFrameImage("EdgeRight", stateFrameLayer);
        ConfigureAnchoredImage(stateEdgeImages[3].rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-1f, 0f), new Vector2(3f, 0f));

        stateCornerImages ??= new Image[8];
        ConfigureCornerImage(0, "CornerTopLeftH", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(3f, -3f), new Vector2(18f, 3f));
        ConfigureCornerImage(1, "CornerTopLeftV", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(3f, -3f), new Vector2(3f, 18f));
        ConfigureCornerImage(2, "CornerTopRightH", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-3f, -3f), new Vector2(18f, 3f));
        ConfigureCornerImage(3, "CornerTopRightV", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-3f, -3f), new Vector2(3f, 18f));
        ConfigureCornerImage(4, "CornerBottomLeftH", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(3f, 3f), new Vector2(18f, 3f));
        ConfigureCornerImage(5, "CornerBottomLeftV", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(3f, 3f), new Vector2(3f, 18f));
        ConfigureCornerImage(6, "CornerBottomRightH", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-3f, 3f), new Vector2(18f, 3f));
        ConfigureCornerImage(7, "CornerBottomRightV", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-3f, 3f), new Vector2(3f, 18f));

        stateSourceNodeGlowImage = EnsureFrameImage("SourceNodeGlow", stateFrameLayer);
        stateSourceNodeGlowImage.sprite = GetPixelNodeGlowSprite();
        ConfigureAnchoredImage(stateSourceNodeGlowImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(24f, 16f));

        stateSourceNodeCoreImage = EnsureFrameImage("SourceNodeCore", stateFrameLayer);
        stateSourceNodeCoreImage.sprite = GetPixelNodeSprite();
        ConfigureAnchoredImage(stateSourceNodeCoreImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(14f, 10f));

        stateSourceNodeMarkImage = EnsureFrameImage("SourceNodeMark", stateFrameLayer);
        stateSourceNodeMarkImage.sprite = GetPixelNodeSprite();
        ConfigureAnchoredImage(stateSourceNodeMarkImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(6f, 4f));

        ApplyFrameColorImmediate(Color.clear, Color.clear);
        ApplySourceNodeImmediate(Color.clear, Color.clear);
    }

    private Image EnsureFrameImage(string imageName, Transform parent)
    {
        Transform existing = parent.Find(imageName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (image == null)
        {
            GameObject imageObject = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            imageObject.layer = gameObject.layer;
            imageObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = imageObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            image = imageObject.GetComponent<Image>();
        }

        image.raycastTarget = false;
        return image;
    }

    private void ConfigureStretchImage(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void ConfigureAnchoredImage(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private void ConfigureCornerImage(int index, string imageName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        stateCornerImages[index] = EnsureFrameImage(imageName, stateFrameLayer);
        ConfigureAnchoredImage(stateCornerImages[index].rectTransform, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
    }

    private void PrewarmFeedbackVisuals()
    {
        GetSharedCountBadgeSprite();
        GetBoundaryEdgeSealSprite();
        GetSoftRectSprite();
        GetPixelNodeSprite();
        GetPixelNodeGlowSprite();
        PrewarmMaterial(hoveredObjectGlowMaterial);
        PrewarmMaterial(readModifierAuraMaterial);
        PrewarmMaterial(sharedModifierOverchargeMaterial);
    }

    private void PrewarmMaterial(Material material)
    {
        if (material == null) return;
        if (material.HasProperty("_Color"))
        {
            material.GetColor("_Color");
        }
    }

    private void ApplyCardVisuals(Color bodyColor, Color frameColor, Color underGlowColor, float duration, bool pulseFrame = false, Color? cornerOverride = null)
    {
        EnsureStateFrameVisuals();
        KillCardVisualTweens(false);

        if (backgroundImage != null)
        {
            backgroundImage.DOColor(bodyColor, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        TweenFrameColor(frameColor, underGlowColor, duration, cornerOverride);
        TweenSourceNode(false, duration, false);
        if (pulseFrame)
        {
            PulseFrameVisuals();
        }
    }

    private void ApplyCardVisualsImmediate(Color bodyColor, Color frameColor, Color underGlowColor)
    {
        EnsureStateFrameVisuals();
        KillCardVisualTweens(false);

        if (backgroundImage != null)
        {
            backgroundImage.color = bodyColor;
        }

        ApplyFrameColorImmediate(frameColor, underGlowColor);
        ApplySourceNodeImmediate(Color.clear, Color.clear);
    }

    private void TweenFrameColor(Color frameColor, Color underGlowColor, float duration, Color? cornerOverride = null)
    {
        TweenImageColor(stateUnderGlowImage, underGlowColor, duration);

        if (stateEdgeImages != null)
        {
            foreach (Image edgeImage in stateEdgeImages)
            {
                TweenImageColor(edgeImage, frameColor, duration);
            }
        }

        Color cornerColor = cornerOverride ?? frameColor;
        if (!cornerOverride.HasValue)
        {
            cornerColor.a = Mathf.Clamp01(cornerColor.a * 1.18f);
        }
        if (stateCornerImages != null)
        {
            foreach (Image cornerImage in stateCornerImages)
            {
                TweenImageColor(cornerImage, cornerColor, duration);
            }
        }
    }

    private void ApplyFrameColorImmediate(Color frameColor, Color underGlowColor)
    {
        SetImageColor(stateUnderGlowImage, underGlowColor);

        if (stateEdgeImages != null)
        {
            foreach (Image edgeImage in stateEdgeImages)
            {
                SetImageColor(edgeImage, frameColor);
            }
        }

        Color cornerColor = frameColor;
        cornerColor.a = Mathf.Clamp01(cornerColor.a * 1.18f);
        if (stateCornerImages != null)
        {
            foreach (Image cornerImage in stateCornerImages)
            {
                SetImageColor(cornerImage, cornerColor);
            }
        }
    }

    private void TweenSourceNode(bool isVisible, float duration, bool pulse)
    {
        Color coreColor = isVisible ? selectedObjectSourceNodeColor : Color.clear;
        Color glowColor = isVisible ? selectedObjectSourceGlowColor : Color.clear;

        TweenImageColor(stateSourceNodeGlowImage, glowColor, duration);
        TweenImageColor(stateSourceNodeCoreImage, coreColor, duration);

        Color markColor = isVisible ? new Color(1f, 1f, 1f, 0.92f) : Color.clear;
        TweenImageColor(stateSourceNodeMarkImage, markColor, duration);

        if (!isVisible)
        {
            ResetSourceNodeScales();
            return;
        }

        if (!pulse) return;

        PulseSourceNode(stateSourceNodeGlowImage, 1.16f, 0.48f);
        PulseSourceNode(stateSourceNodeCoreImage, 1.08f, 0.38f);
        PulseSourceNode(stateSourceNodeMarkImage, 1.05f, 0.38f);
    }

    private void ApplySourceNodeImmediate(Color coreColor, Color glowColor)
    {
        SetImageColor(stateSourceNodeGlowImage, glowColor);
        SetImageColor(stateSourceNodeCoreImage, coreColor);
        SetImageColor(stateSourceNodeMarkImage, coreColor.a > 0f ? new Color(1f, 1f, 1f, 0.92f) : Color.clear);
        ResetSourceNodeScales();
    }

    private void PulseSourceNode(Image image, float scale, float duration)
    {
        if (image == null) return;

        image.transform.DOKill();
        image.transform.localScale = Vector3.one;
        image.transform.DOScale(scale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void ResetSourceNodeScales()
    {
        ResetSourceNodeScale(stateSourceNodeGlowImage);
        ResetSourceNodeScale(stateSourceNodeCoreImage);
        ResetSourceNodeScale(stateSourceNodeMarkImage);
    }

    private void ResetSourceNodeScale(Image image)
    {
        if (image == null) return;

        image.transform.DOKill();
        image.transform.localScale = Vector3.one;
    }

    private void TweenImageColor(Image image, Color color, float duration)
    {
        if (image == null) return;

        image.DOKill();
        image.DOColor(color, duration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void SetImageColor(Image image, Color color)
    {
        if (image == null) return;

        image.DOKill();
        image.color = color;
    }

    private void PulseFrameVisuals()
    {
        if (stateFrameLayer == null) return;

        stateFrameLayer.DOKill();
        stateFrameLayer.localScale = Vector3.one;
        stateFrameLayer.DOScale(framePulseScale, 0.18f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void KillCardVisualTweens(bool includeTransform)
    {
        if (backgroundImage != null) backgroundImage.DOKill();
        if (glowImage != null) { glowImage.DOKill(); glowImage.transform.DOKill(); }
        if (stateFrameLayer != null) stateFrameLayer.DOKill();
        if (stateUnderGlowImage != null) stateUnderGlowImage.DOKill();
        KillSourceNodeTween(stateSourceNodeGlowImage);
        KillSourceNodeTween(stateSourceNodeCoreImage);
        KillSourceNodeTween(stateSourceNodeMarkImage);

        if (stateEdgeImages != null)
        {
            foreach (Image edgeImage in stateEdgeImages)
            {
                if (edgeImage != null) edgeImage.DOKill();
            }
        }

        if (stateCornerImages != null)
        {
            foreach (Image cornerImage in stateCornerImages)
            {
                if (cornerImage != null) cornerImage.DOKill();
            }
        }

        if (includeTransform)
        {
            transform.DOKill(false);
        }
    }

    private void KillSourceNodeTween(Image image)
    {
        if (image == null) return;

        image.DOKill();
        image.transform.DOKill();
    }

    private Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void ApplyHoveredObjectGlowMaterial()
    {
        if (backgroundImage == null || hoveredObjectGlowMaterial == null || !IsObjectRune()) return;

        CaptureOriginalBackgroundMaterial();
        backgroundImage.material = hoveredObjectGlowMaterial;
        feedbackMaterialApplied = true;
    }

    private void ApplyReadModifierAuraMaterial()
    {
        if (backgroundImage == null || readModifierAuraMaterial == null || !IsModifierRune()) return;

        CaptureOriginalBackgroundMaterial();
        backgroundImage.material = readModifierAuraMaterial;
        feedbackMaterialApplied = true;
    }

    private void ApplySharedModifierOverchargeMaterial()
    {
        if (backgroundImage == null || sharedModifierOverchargeMaterial == null || !IsSharedModifier()) return;

        CaptureOriginalBackgroundMaterial();
        backgroundImage.material = sharedModifierOverchargeMaterial;
        feedbackMaterialApplied = true;
    }

    private void RestoreOriginalBackgroundMaterial()
    {
        if (backgroundImage == null || !feedbackMaterialApplied) return;

        backgroundImage.material = originalBackgroundMaterial;
        feedbackMaterialApplied = false;
    }

    private bool IsObjectRune()
    {
        return myWordData != null &&
               myWordData.wordData != null &&
               myWordData.wordData.wordType == WordType.Object;
    }

    private bool IsModifierRune()
    {
        return myWordData != null &&
               myWordData.wordData != null &&
               myWordData.wordData.wordType == WordType.Modifier;
    }

    private bool IsSharedModifier()
    {
        return IsModifierRune() && currentSharedCount >= 2;
    }

    private static Sprite GetPixelNodeSprite()
    {
        if (pixelNodeSprite != null) return pixelNodeSprite;

        const int width = 16;
        const int height = 12;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "RuntimeRuneSourceNode",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedDiamondDistance =
                    Mathf.Abs(x - center.x) / (width * 0.5f) +
                    Mathf.Abs(y - center.y) / (height * 0.5f);
                bool inside = normalizedDiamondDistance <= 1.05f;
                bool notch = y == Mathf.RoundToInt(center.y) && x >= 4 && x <= width - 5;
                float alpha = inside || notch ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        pixelNodeSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
        pixelNodeSprite.hideFlags = HideFlags.HideAndDontSave;
        return pixelNodeSprite;
    }

    private static Sprite GetBoundaryEdgeSealSprite()
    {
        if (boundaryEdgeSealSprite != null) return boundaryEdgeSealSprite;

        const int width = 8;
        const int height = 42;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "RuntimeRuneBoundaryEdgeSeal",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool spine = x >= 2 && x <= 5 && y >= 4 && y <= height - 5;
                bool topCut = y >= height - 11 && y <= height - 7 && x >= 1 && x <= 6;
                bool bottomCut = y >= 7 && y <= 11 && x >= 1 && x <= 6;
                bool notch = (y == height - 13 || y == 13) && x >= 0 && x <= 7;
                bool inside = spine || topCut || bottomCut || notch;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
            }
        }

        texture.Apply();
        boundaryEdgeSealSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
        boundaryEdgeSealSprite.hideFlags = HideFlags.HideAndDontSave;
        return boundaryEdgeSealSprite;
    }

    private static Sprite GetPixelNodeGlowSprite()
    {
        if (pixelNodeGlowSprite != null) return pixelNodeGlowSprite;

        const int width = 24;
        const int height = 16;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "RuntimeRuneSourceNodeGlow",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedDiamondDistance =
                    Mathf.Abs(x - center.x) / (width * 0.5f) +
                    Mathf.Abs(y - center.y) / (height * 0.5f);
                float alpha = 0f;
                if (normalizedDiamondDistance <= 0.7f)
                {
                    alpha = 0.72f;
                }
                else if (normalizedDiamondDistance <= 1.02f)
                {
                    alpha = 0.38f;
                }
                else if (normalizedDiamondDistance <= 1.24f)
                {
                    alpha = 0.16f;
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        pixelNodeGlowSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
        pixelNodeGlowSprite.hideFlags = HideFlags.HideAndDontSave;
        return pixelNodeGlowSprite;
    }

    private static Sprite GetSharedCountBadgeSprite()
    {
        if (sharedCountBadgeSprite != null) return sharedCountBadgeSprite;

        const int width = 34;
        const int height = 22;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "RuntimeSharedRuneBadge",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool clippedLowerLeft = x + y < 7;
                bool clippedLowerRight = x > width - 3 && y < 3;
                bool clippedTopLeft = x < 2 && y > height - 3;
                bool isInside = !clippedLowerLeft && !clippedLowerRight && !clippedTopLeft;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, isInside ? 1f : 0f));
            }
        }

        texture.Apply();
        sharedCountBadgeSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
        sharedCountBadgeSprite.hideFlags = HideFlags.HideAndDontSave;
        return sharedCountBadgeSprite;
    }

    private static Sprite GetSoftRectSprite()
    {
        if (softRectSprite != null) return softRectSprite;

        const int width = 64;
        const int height = 48;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "RuntimeRuneSoftRect",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 uv = new Vector2(Mathf.Abs(x - center.x) / center.x, Mathf.Abs(y - center.y) / center.y);
                float edgeDistance = Mathf.Max(uv.x, uv.y);
                float alpha = 1f - Mathf.SmoothStep(0.28f, 1f, edgeDistance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
        }

        texture.Apply();
        softRectSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
        softRectSprite.hideFlags = HideFlags.HideAndDontSave;
        return softRectSprite;
    }

    private Transform GetHoveredRune(Transform zone, PointerEventData eventData)
    {
        for (int i = 0; i < zone.childCount; i++)
        {
            Transform child = zone.GetChild(i);
            if (child == placeholder.transform) continue;
            if (child.GetComponent<DraggableWord>() == null) continue;

            RectTransform rect = child.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera))
            {
                return child;
            }
        }
        return null;
    }

    private int GetRightEdgeInsertionIndex(Transform zone, PointerEventData eventData)
    {
        Transform rightmostRune = null;
        float rightmostX = float.NegativeInfinity;

        for (int i = 0; i < zone.childCount; i++)
        {
            Transform child = zone.GetChild(i);
            if (placeholder != null && child == placeholder.transform) continue;
            if (child.GetComponent<DraggableWord>() == null) continue;

            RectTransform rect = child as RectTransform;
            if (rect == null) continue;

            if (rect.position.x > rightmostX)
            {
                rightmostX = rect.position.x;
                rightmostRune = child;
            }
        }

        if (rightmostRune == null || eventData.position.x <= rightmostRune.position.x) return -1;
        return rightmostRune.GetSiblingIndex() + 1;
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
