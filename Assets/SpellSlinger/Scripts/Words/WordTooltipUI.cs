using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordTooltipUI : MonoBehaviour
{
    private const string RuntimeTooltipName = "Runtime Word Tooltip";

    public static WordTooltipUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Behavior")]
    [SerializeField, Min(0f)] private float hoverDelay = 0.25f;
    [SerializeField] private Vector2 offsetFromMouse = new Vector2(18f, -18f);
    [SerializeField] private bool followMouse = true;
    [SerializeField] private bool clampToCanvas = true;
    [SerializeField] private int tooltipSortingOrder = 30000;
    [SerializeField] private Vector2 tooltipSize = new Vector2(340f, 190f);

    private Canvas rootCanvas;
    private Canvas tooltipCanvas;
    private CanvasGroup canvasGroup;
    private Coroutine showRoutine;
    private RectTransform pendingSource;
    private RectTransform activeSource;
    private Vector2 lastPointerScreenPosition;
    private bool hasPointerScreenPosition;
    private bool usingDefaultPanel;

    public static void ShowOwnedWordDelayed(OwnedWord ownedWord, RectTransform sourceRect = null, Vector2? pointerScreenPosition = null)
    {
        WordTooltipUI tooltip = GetOrCreate(sourceRect);
        if (tooltip != null)
        {
            tooltip.ShowDelayed(ownedWord, sourceRect, pointerScreenPosition);
        }
    }

    public static void ShowWordDelayed(WordData wordData, int level = 1, RectTransform sourceRect = null, Vector2? pointerScreenPosition = null)
    {
        WordTooltipUI tooltip = GetOrCreate(sourceRect);
        if (tooltip != null)
        {
            tooltip.ShowDelayed(wordData, level, sourceRect, pointerScreenPosition);
        }
    }

    public static void HideForSource(RectTransform sourceRect)
    {
        WordTooltipUI tooltip = FindExisting();
        if (tooltip != null)
        {
            tooltip.Hide(sourceRect);
        }
    }

    public static void HideAll()
    {
        WordTooltipUI tooltip = FindExisting();
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }

    private void Awake()
    {
        Instance = this;
        EnsureReferences();
        ConfigureRaycastBlocking();
        Hide();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (followMouse && tooltipPanel != null && canvasGroup != null && canvasGroup.alpha > 0f)
        {
            PositionTooltip();
        }
    }

    public void ShowDelayed(OwnedWord ownedWord, RectTransform sourceRect = null, Vector2? pointerScreenPosition = null)
    {
        if (ownedWord == null)
        {
            Debug.LogWarning("WordTooltipUI could not show a tooltip because the hovered card has no OwnedWord data.", sourceRect);
            Hide(sourceRect);
            return;
        }

        ShowDelayed(ownedWord.wordData, ownedWord.level, sourceRect, pointerScreenPosition);
    }

    public void ShowDelayed(WordData wordData, int level = 1, RectTransform sourceRect = null, Vector2? pointerScreenPosition = null)
    {
        if (wordData == null || !EnsureReferences())
        {
            Debug.LogWarning("WordTooltipUI could not show a tooltip because WordData or tooltip UI references are missing.", sourceRect);
            Hide(sourceRect);
            return;
        }

        CancelPendingShow();
        HidePanel();

        pendingSource = sourceRect;
        activeSource = null;
        SetPointerScreenPosition(pointerScreenPosition);

        int safeLevel = Mathf.Max(1, level);
        if (hoverDelay <= 0f)
        {
            ShowNow(wordData, safeLevel, sourceRect);
            return;
        }

        showRoutine = StartCoroutine(ShowAfterDelay(wordData, safeLevel, sourceRect));
    }

    public void Hide(RectTransform sourceRect)
    {
        if (sourceRect == null || sourceRect == pendingSource || sourceRect == activeSource)
        {
            Hide();
        }
    }

    public void Hide()
    {
        CancelPendingShow();
        pendingSource = null;
        activeSource = null;
        hasPointerScreenPosition = false;
        HidePanel();
    }

    private IEnumerator ShowAfterDelay(WordData wordData, int level, RectTransform sourceRect)
    {
        yield return new WaitForSecondsRealtime(hoverDelay);
        showRoutine = null;

        if (wordData == null)
        {
            yield break;
        }

        if (sourceRect != null && !sourceRect.gameObject.activeInHierarchy)
        {
            yield break;
        }

        ShowNow(wordData, level, sourceRect);
    }

    private void ShowNow(WordData wordData, int level, RectTransform sourceRect)
    {
        if (wordData == null || !EnsureReferences())
        {
            return;
        }

        pendingSource = null;
        activeSource = sourceRect;

        if (titleText != null)
        {
            titleText.text = GetDisplayName(wordData);
        }

        if (bodyText != null)
        {
            bodyText.text = BuildBodyText(wordData, level);
        }

        tooltipPanel.gameObject.SetActive(true);
        ApplyVisibilityDefaults();
        ConfigureRaycastBlocking();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
        PositionTooltip();
    }

    private string BuildBodyText(WordData wordData, int level)
    {
        StringBuilder builder = new StringBuilder();
        int safeLevel = Mathf.Max(1, level);
        WordLevelStats stats = wordData.GetStatsForLevel(safeLevel);

        builder.AppendLine("Level " + safeLevel);
        builder.AppendLine("Mana: " + Mathf.Max(0, wordData.manaCost));
        builder.AppendLine();

        string description = GetDescription(wordData);
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine(description);
        }

        int valuesStartLength = builder.Length;
        AppendGameplayValues(builder, wordData, stats);

        if (builder.Length > valuesStartLength && valuesStartLength > 0 && builder[valuesStartLength - 1] != '\n')
        {
            builder.Insert(valuesStartLength, "\n");
        }

        return builder.ToString().TrimEnd();
    }

    private void AppendGameplayValues(StringBuilder builder, WordData wordData, WordLevelStats stats)
    {
        if (wordData.wordType == WordType.Object)
        {
            AppendLine(builder, "Damage: " + FormatNumber(stats != null ? stats.damage : wordData.baseDamage));
            AppendLine(builder, "Cooldown: " + FormatSeconds(wordData.baseCooldown));

            int projectileCount = stats != null ? Mathf.Max(1, stats.projectileCount) : 1;
            if (projectileCount > 1)
            {
                AppendLine(builder, "Projectiles: " + projectileCount);
            }
        }
        else
        {
            float cooldownReduction = stats != null ? stats.cooldownReduction : wordData.cooldownReduction;
            if (cooldownReduction > 0f)
            {
                AppendLine(builder, "Cooldown Reduction: " + FormatSeconds(cooldownReduction));
            }
        }

        AppendMechanicValues(builder, wordData.mechanicToAdd, stats);
    }

    private void AppendMechanicValues(StringBuilder builder, SpecialMechanic mechanic, WordLevelStats stats)
    {
        if (mechanic == SpecialMechanic.None || stats == null)
        {
            return;
        }

        switch (mechanic)
        {
            case SpecialMechanic.FireBurn:
                AppendLine(builder, "Burn Damage: " + FormatNumber(stats.burnDamage));
                AppendLine(builder, "Burn Duration: " + FormatSeconds(stats.burnDuration));
                break;

            case SpecialMechanic.WaterSlow:
                AppendLine(builder, "Slow: " + FormatPercent(stats.waterSlowPercent));
                AppendLine(builder, "Slow Duration: " + FormatSeconds(stats.waterSlowDuration));
                break;

            case SpecialMechanic.AirSlash:
                AppendLine(builder, "Follow-up Damage: " + FormatPercent(stats.airSlashDamageMultiplier));
                break;

            case SpecialMechanic.RockStun:
                AppendLine(builder, "Adds rock stacks.");
                break;

            case SpecialMechanic.LightningChain:
                AppendLine(builder, "Chain Radius: " + FormatNumber(stats.lightningChainRadius));
                AppendLine(builder, "Chain Targets: " + stats.lightningChainTargets);
                AppendLine(builder, "Chain Damage: " + FormatPercent(stats.lightningChainDamageMultiplier));
                break;

            case SpecialMechanic.IceArrow:
                AppendLine(builder, "Ice Slow: " + FormatPercent(stats.iceSlowPercent));
                AppendLine(builder, "Slow Duration: " + FormatSeconds(stats.iceSlowDuration));
                AppendLine(builder, "Explosion Radius: " + FormatNumber(stats.iceExplosionRadius));
                break;

            case SpecialMechanic.Explosive:
                AppendLine(builder, "Adds explosive impact.");
                break;

            case SpecialMechanic.Bounce:
                AppendLine(builder, "Bounces: " + stats.bounceCount);
                break;

            case SpecialMechanic.Split:
                AppendLine(builder, "Split Projectiles: " + stats.splitProjectileCount);
                break;

            case SpecialMechanic.Execution:
                AppendLine(builder, "Execution Multiplier: " + FormatNumber(stats.executionDamageMultiplier) + "x");
                break;

            case SpecialMechanic.Pierce:
                AppendLine(builder, "Pierces: " + stats.pierceCount);
                AppendLine(builder, "Pierce Damage: " + FormatPercent(stats.pierceDamageMultiplier));
                break;

            case SpecialMechanic.Acceleration:
                AppendLine(builder, "Acceleration Duration: " + FormatSeconds(stats.accelerationDuration));
                break;

            case SpecialMechanic.DamageBoost:
                AppendLine(builder, "Damage Bonus: " + FormatSignedPercent(stats.damageBonusMultiplier - 1f));
                break;
        }
    }

    private string GetDisplayName(WordData wordData)
    {
        if (!string.IsNullOrWhiteSpace(wordData.runeText))
        {
            return wordData.runeText;
        }

        if (!string.IsNullOrWhiteSpace(wordData.translatedText))
        {
            return wordData.translatedText;
        }

        return "Unknown Word";
    }

    private string GetDescription(WordData wordData)
    {
        if (!string.IsNullOrWhiteSpace(wordData.secretHint))
        {
            return wordData.secretHint.Trim();
        }

        if (!string.IsNullOrWhiteSpace(wordData.translatedText) && wordData.translatedText != wordData.runeText)
        {
            return wordData.translatedText.Trim();
        }

        if (wordData.wordType == WordType.Object)
        {
            return "Casts this spell.";
        }

        if (wordData.mechanicToAdd != SpecialMechanic.None)
        {
            return "Modifies linked spells with " + GetMechanicName(wordData.mechanicToAdd) + ".";
        }

        return "Modifies linked spells.";
    }

    private string GetMechanicName(SpecialMechanic mechanic)
    {
        switch (mechanic)
        {
            case SpecialMechanic.FireBurn: return "burn";
            case SpecialMechanic.WaterSlow: return "slow";
            case SpecialMechanic.AirSlash: return "air slash";
            case SpecialMechanic.RockStun: return "rock stun";
            case SpecialMechanic.LightningChain: return "lightning chain";
            case SpecialMechanic.IceArrow: return "ice";
            case SpecialMechanic.Explosive: return "explosion";
            case SpecialMechanic.Bounce: return "bounce";
            case SpecialMechanic.Split: return "split";
            case SpecialMechanic.Execution: return "execution";
            case SpecialMechanic.Pierce: return "pierce";
            case SpecialMechanic.Acceleration: return "acceleration";
            case SpecialMechanic.DamageBoost: return "damage bonus";
            default: return "an effect";
        }
    }

    private void AppendLine(StringBuilder builder, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (builder.Length > 0 && builder[builder.Length - 1] != '\n')
        {
            builder.AppendLine();
        }

        builder.AppendLine(line);
    }

    private string FormatSeconds(float value)
    {
        return FormatNumber(value) + "s";
    }

    private string FormatPercent(float value)
    {
        return Mathf.RoundToInt(value * 100f) + "%";
    }

    private string FormatSignedPercent(float value)
    {
        int percent = Mathf.RoundToInt(value * 100f);
        return percent >= 0 ? "+" + percent + "%" : percent + "%";
    }

    private string FormatNumber(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
        {
            return Mathf.RoundToInt(value).ToString();
        }

        return value.ToString("0.##");
    }

    private void PositionTooltip()
    {
        if (tooltipPanel == null)
        {
            return;
        }

        RectTransform parentRect = tooltipPanel.parent as RectTransform;
        if (parentRect == null)
        {
            Debug.LogWarning("WordTooltipUI cannot position the tooltip because its panel has no RectTransform parent.", this);
            return;
        }

        RectTransform tooltipRootRect = GetTooltipRootRect();
        if (tooltipRootRect == null)
        {
            return;
        }

        NormalizeRuntimeRootRect(tooltipRootRect);

        Canvas canvas = tooltipCanvas != null ? tooltipCanvas : GetRootCanvas();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (TryGetPointerScreenPosition(out Vector2 pointerScreenPosition))
        {
            lastPointerScreenPosition = pointerScreenPosition;
            hasPointerScreenPosition = true;
        }
        else if (hasPointerScreenPosition)
        {
            pointerScreenPosition = lastPointerScreenPosition;
        }
        else if (!TryGetSourceScreenPosition(eventCamera, out pointerScreenPosition))
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipRootRect, pointerScreenPosition, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        localPoint += offsetFromMouse;
        tooltipPanel.anchoredPosition = clampToCanvas ? ClampToParent(tooltipRootRect, localPoint) : localPoint;
    }

    private RectTransform GetTooltipRootRect()
    {
        if (tooltipCanvas != null)
        {
            return tooltipCanvas.transform as RectTransform;
        }

        Canvas canvas = GetRootCanvas();
        if (canvas != null)
        {
            return canvas.transform as RectTransform;
        }

        return tooltipPanel != null ? tooltipPanel.parent as RectTransform : null;
    }

    private void NormalizeRuntimeRootRect(RectTransform rootRect)
    {
        if (rootRect == null || rootRect == tooltipPanel)
        {
            return;
        }

        rootRect.localScale = Vector3.one;
        rootRect.localRotation = Quaternion.identity;

        if (rootRect.parent != null)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }
    }

    private void SetPointerScreenPosition(Vector2? pointerScreenPosition)
    {
        if (!pointerScreenPosition.HasValue)
        {
            return;
        }

        lastPointerScreenPosition = pointerScreenPosition.Value;
        hasPointerScreenPosition = true;
    }

    private bool TryGetSourceScreenPosition(Camera eventCamera, out Vector2 screenPosition)
    {
        if (activeSource != null)
        {
            screenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, activeSource.position);
            return true;
        }

        if (pendingSource != null)
        {
            screenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, pendingSource.position);
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
    }

    private bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        screenPosition = UnityEngine.Input.mousePosition;
        return true;
#else
        screenPosition = Vector2.zero;
        return false;
#endif
    }

    private Vector2 ClampToParent(RectTransform parentRect, Vector2 localPoint)
    {
        Rect parentBounds = parentRect.rect;
        Rect tooltipBounds = tooltipPanel.rect;
        Vector2 pivot = tooltipPanel.pivot;

        float minX = parentBounds.xMin + tooltipBounds.width * pivot.x;
        float maxX = parentBounds.xMax - tooltipBounds.width * (1f - pivot.x);
        float minY = parentBounds.yMin + tooltipBounds.height * pivot.y;
        float maxY = parentBounds.yMax - tooltipBounds.height * (1f - pivot.y);

        if (minX <= maxX)
        {
            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
        }

        if (minY <= maxY)
        {
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);
        }

        return localPoint;
    }

    private bool EnsureReferences()
    {
        if (tooltipPanel == null)
        {
            RectTransform ownRect = GetComponent<RectTransform>();
            if (ownRect != null && GetComponent<Graphic>() != null)
            {
                tooltipPanel = ownRect;
            }
            else
            {
                tooltipPanel = CreateDefaultPanel(GetRootCanvasTransform());
                usingDefaultPanel = true;
            }
        }

        if (tooltipPanel == null)
        {
            return false;
        }

        if (rootCanvas == null)
        {
            rootCanvas = tooltipPanel.GetComponentInParent<Canvas>()?.rootCanvas;
        }

        EnsureTooltipCanvasSorting();
        EnsureDefaultTextReferences();
        ApplyVisibilityDefaults();

        if (canvasGroup == null)
        {
            canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tooltipPanel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        return true;
    }

    private void EnsureTooltipCanvasSorting()
    {
        if (tooltipPanel == null)
        {
            return;
        }

        if (tooltipCanvas == null)
        {
            tooltipCanvas = GetComponent<Canvas>();
        }

        if (tooltipCanvas == null)
        {
            tooltipCanvas = tooltipPanel.GetComponent<Canvas>();
            if (tooltipCanvas == null)
            {
                tooltipCanvas = tooltipPanel.gameObject.AddComponent<Canvas>();
            }
        }

        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = tooltipSortingOrder;
    }

    private void ApplyVisibilityDefaults()
    {
        if (tooltipPanel == null)
        {
            return;
        }

        tooltipPanel.localScale = Vector3.one;
        tooltipPanel.localRotation = Quaternion.identity;

        if (usingDefaultPanel || tooltipPanel.rect.width < 40f || tooltipPanel.rect.height < 40f)
        {
            tooltipPanel.sizeDelta = tooltipSize;
            tooltipPanel.pivot = new Vector2(0f, 1f);
            tooltipPanel.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipPanel.anchorMax = new Vector2(0.5f, 0.5f);
        }

        Image panelImage = tooltipPanel.GetComponent<Image>();
        if (panelImage != null && panelImage.color.a < 0.1f)
        {
            panelImage.color = new Color(0.06f, 0.055f, 0.07f, 0.94f);
        }

        if (titleText != null)
        {
            titleText.color = new Color(1f, 0.94f, 0.72f, 1f);
            titleText.fontSize = Mathf.Max(18f, titleText.fontSize);
            titleText.raycastTarget = false;
        }

        if (bodyText != null)
        {
            bodyText.color = new Color(0.92f, 0.91f, 0.88f, 1f);
            bodyText.fontSize = Mathf.Max(14f, bodyText.fontSize);
            bodyText.raycastTarget = false;
        }
    }

    private static WordTooltipUI GetOrCreate(RectTransform sourceRect)
    {
        WordTooltipUI existing = FindExisting();
        if (existing != null)
        {
            if (!existing.gameObject.activeSelf)
            {
                existing.gameObject.SetActive(true);
            }

            existing.EnsureReferences();
            existing.ConfigureRaycastBlocking();
            return existing;
        }

        Canvas targetCanvas = sourceRect != null ? sourceRect.GetComponentInParent<Canvas>()?.rootCanvas : null;
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject("Runtime Tooltip Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = canvasObject.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject tooltipObject = new GameObject(RuntimeTooltipName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipRect.anchorMin = Vector2.zero;
        tooltipRect.anchorMax = Vector2.one;
        tooltipRect.offsetMin = Vector2.zero;
        tooltipRect.offsetMax = Vector2.zero;
        tooltipRect.pivot = new Vector2(0.5f, 0.5f);
        tooltipRect.localScale = Vector3.one;

        Canvas tooltipCanvas = tooltipObject.GetComponent<Canvas>();
        tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = 30000;

        CanvasScaler tooltipScaler = tooltipObject.GetComponent<CanvasScaler>();
        CanvasScaler targetScaler = targetCanvas.rootCanvas.GetComponent<CanvasScaler>();
        if (targetScaler != null)
        {
            tooltipScaler.uiScaleMode = targetScaler.uiScaleMode;
            tooltipScaler.referenceResolution = targetScaler.referenceResolution;
            tooltipScaler.screenMatchMode = targetScaler.screenMatchMode;
            tooltipScaler.matchWidthOrHeight = targetScaler.matchWidthOrHeight;
        }
        else
        {
            tooltipScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            tooltipScaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GraphicRaycaster tooltipRaycaster = tooltipObject.GetComponent<GraphicRaycaster>();
        tooltipRaycaster.enabled = false;

        WordTooltipUI tooltip = tooltipObject.AddComponent<WordTooltipUI>();
        tooltip.rootCanvas = tooltipCanvas.rootCanvas;
        tooltip.tooltipCanvas = tooltipCanvas;
        tooltip.usingDefaultPanel = true;
        tooltip.EnsureReferences();
        tooltip.ConfigureRaycastBlocking();
        tooltip.Hide();
        return tooltip;
    }

    private static WordTooltipUI FindExisting()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindFirstObjectByType<WordTooltipUI>(FindObjectsInactive.Include);
        return Instance;
    }

    private Transform GetRootCanvasTransform()
    {
        Canvas canvas = GetRootCanvas();
        if (canvas != null)
        {
            return canvas.transform;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            rootCanvas = parentCanvas.rootCanvas;
            return rootCanvas.transform;
        }

        return transform;
    }

    private RectTransform CreateDefaultPanel(Transform parent)
    {
        GameObject panelObject = new GameObject("Word Tooltip Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObject.transform.SetParent(parent != null ? parent : transform, false);
        panelObject.transform.SetAsLastSibling();

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = tooltipSize;
        panelRect.localScale = Vector3.one;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.055f, 0.07f, 0.94f);

        titleText = CreateDefaultText(panelRect, "Title", 20f, FontStyles.Bold);
        bodyText = CreateDefaultText(panelRect, "Body", 16f, FontStyles.Normal);

        return panelRect;
    }

    private TMP_Text CreateDefaultText(RectTransform parent, string objectName, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = objectName == "Title" ? new Color(1f, 0.94f, 0.72f, 1f) : new Color(0.92f, 0.91f, 0.88f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        RectTransform textRect = text.rectTransform;
        textRect.pivot = new Vector2(0f, 1f);
        textRect.localScale = Vector3.one;

        if (objectName == "Title")
        {
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(14f, -42f);
            textRect.offsetMax = new Vector2(-14f, -10f);
        }
        else
        {
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(14f, 12f);
            textRect.offsetMax = new Vector2(-14f, -50f);
        }

        return text;
    }

    private void EnsureDefaultTextReferences()
    {
        if (tooltipPanel == null)
        {
            return;
        }

        TMP_Text[] texts = tooltipPanel.GetComponentsInChildren<TMP_Text>(true);
        if (titleText == null && texts.Length > 0)
        {
            titleText = texts[0];
        }

        if (bodyText == null && texts.Length > 1)
        {
            bodyText = texts[1];
        }

        if (titleText == null)
        {
            titleText = CreateDefaultText(tooltipPanel, "Title", 20f, FontStyles.Bold);
        }

        if (bodyText == null)
        {
            bodyText = CreateDefaultText(tooltipPanel, "Body", 16f, FontStyles.Normal);
        }
    }

    private Canvas GetRootCanvas()
    {
        if (rootCanvas == null && tooltipPanel != null)
        {
            rootCanvas = tooltipPanel.GetComponentInParent<Canvas>()?.rootCanvas;
        }

        return rootCanvas;
    }

    private void ConfigureRaycastBlocking()
    {
        if (!EnsureReferences())
        {
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        Graphic[] graphics = tooltipPanel.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].raycastTarget = false;
            }
        }
    }

    private void HidePanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void CancelPendingShow()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }
    }
}
