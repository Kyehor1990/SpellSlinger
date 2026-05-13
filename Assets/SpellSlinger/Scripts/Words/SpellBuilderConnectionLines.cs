using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SpellBuilderConnectionLines : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform lineLayer;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Material sharedLineMaterial;
    [SerializeField] private Material flowMarkerMaterial;

    [Header("Ribbon Style")]
    [SerializeField] private Color lineColor = new Color(0.13f, 0.78f, 1f, 0.3f);
    [SerializeField] private Color sharedLineColor = new Color(0.54f, 0.95f, 1f, 0.46f);
    [SerializeField] private Color lineGlowColor = new Color(0.05f, 0.58f, 1f, 0.1f);
    [SerializeField] private Color sharedLineGlowColor = new Color(0.72f, 0.98f, 1f, 0.16f);
    [SerializeField] private Color sharedAccentColor = new Color(1f, 0.9f, 0.45f, 0.42f);
    [SerializeField] private float lineThickness = 2.6f;
    [SerializeField] private float sharedLineThickness = 2.6f;
    [SerializeField] private float lineGlowThicknessMultiplier = 2.4f;
    [SerializeField] private int curveSegments = 7;
    [SerializeField] private float curveHeight = 18f;
    [SerializeField] private float pixelSnapSize = 2f;

    [Header("Flow Animation")]
    [SerializeField] private Color flowMarkerColor = new Color(0.72f, 0.98f, 1f, 0.48f);
    [SerializeField] private Color sharedFlowMarkerColor = new Color(1f, 0.96f, 0.7f, 0.62f);
    [SerializeField] private float flowMarkerLength = 10f;
    [SerializeField] private float flowMarkerThickness = 3f;
    [SerializeField] private int flowMarkersPerLine = 2;
    [SerializeField] private float flowDuration = 1.05f;

    [Header("Anchors")]
    [Range(0.12f, 0.45f)]
    [SerializeField] private float sideAnchorHeight = 0.28f;
    [SerializeField] private float edgeAnchorOffset = 3f;
    [SerializeField] private float nodeLineGap = 7f;
    [SerializeField] private float sourceNodeSize = 15f;
    [SerializeField] private float targetNodeSize = 13f;
    [SerializeField] private Color sourceNodeColor = new Color(0.78f, 0.96f, 1f, 0.94f);
    [SerializeField] private Color targetNodeColor = new Color(0.48f, 0.9f, 1f, 0.86f);
    [SerializeField] private Color sharedNodeColor = new Color(1f, 0.92f, 0.48f, 0.96f);
    [SerializeField] private Color nodeGlowColor = new Color(0.12f, 0.76f, 1f, 0.24f);
    [SerializeField] private Color sharedNodeGlowColor = new Color(1f, 0.92f, 0.5f, 0.32f);

    [Header("Boundary Seal")]
    [SerializeField] private Color stopFlowColor = new Color(1f, 0.08f, 0.35f, 0.24f);
    [SerializeField] private Color stopFlowGlowColor = new Color(1f, 0.05f, 0.45f, 0.12f);
    [SerializeField] private Color stopMarkerColor = new Color(1f, 0.08f, 0.32f, 0.94f);
    [SerializeField] private Color stopMarkerGlowColor = new Color(1f, 0.05f, 0.42f, 0.24f);
    [SerializeField] private float stopMarkerLength = 18f;
    [SerializeField] private float stopMarkerThickness = 3f;
    [SerializeField] private float sealNodeSize = 18f;
    [Range(0.55f, 0.95f)]
    [SerializeField] private float interruptedFlowProgress = 0.78f;

    private enum CircleSpriteStyle
    {
        Solid,
        Soft,
        Ring
    }

    private static Sprite solidCircleSprite;
    private static Sprite softCircleSprite;
    private static Sprite ringCircleSprite;

    private readonly List<GameObject> spawnedVisuals = new List<GameObject>();
    private readonly List<Vector2> activeSourceNodePositions = new List<Vector2>();

    private RectTransform sentencePanel;
    private RectTransform glowLayer;
    private RectTransform ribbonLayer;
    private RectTransform markerLayer;
    private RectTransform nodeLayer;
    private RectTransform sealLayer;

    public void SetSentencePanel(RectTransform panel)
    {
        sentencePanel = panel;
    }

    public void ShowConnections(
        DraggableWord objectWord,
        IReadOnlyList<DraggableWord> sentenceWords,
        RunePatternPreview preview,
        IReadOnlyList<int> sharedReadCounts)
    {
        ClearConnections();

        if (!isActiveAndEnabled ||
            objectWord == null ||
            sentenceWords == null ||
            preview == null ||
            preview.AffectedModifierIndices == null)
        {
            return;
        }

        EnsureLineLayer(objectWord);
        if (lineLayer == null) return;

        RectTransform objectRect = objectWord.GetComponent<RectTransform>();
        if (objectRect == null) return;

        foreach (int modifierIndex in preview.AffectedModifierIndices)
        {
            if (modifierIndex < 0 || modifierIndex >= sentenceWords.Count) continue;

            DraggableWord modifierWord = sentenceWords[modifierIndex];
            if (modifierWord == null ||
                modifierWord.myWordData == null ||
                modifierWord.myWordData.wordData == null ||
                modifierWord.myWordData.wordData.wordType != WordType.Modifier)
            {
                continue;
            }

            RectTransform modifierRect = modifierWord.GetComponent<RectTransform>();
            if (modifierRect == null) continue;

            bool isShared = false;
            CreateFlowLine(objectRect, modifierRect, isShared);
        }

        if (preview.StopObjectIndices == null) return;
        foreach (int stopObjectIndex in preview.StopObjectIndices)
        {
            if (stopObjectIndex < 0 || stopObjectIndex >= sentenceWords.Count) continue;

            DraggableWord stopWord = sentenceWords[stopObjectIndex];
            RectTransform stopRect = stopWord != null ? stopWord.GetComponent<RectTransform>() : null;
            if (stopRect == null) continue;

            CreateStopMarker(objectRect, stopRect);
        }
    }

    public void ClearConnections()
    {
        DOTween.Kill(this);

        for (int i = spawnedVisuals.Count - 1; i >= 0; i--)
        {
            if (spawnedVisuals[i] != null)
            {
                Destroy(spawnedVisuals[i]);
            }
        }

        spawnedVisuals.Clear();
        activeSourceNodePositions.Clear();
    }

    private void OnDisable()
    {
        ClearConnections();
    }

    private void EnsureLineLayer(DraggableWord objectWord)
    {
        RectTransform targetPanel = sentencePanel;
        if (targetPanel == null && objectWord != null)
        {
            targetPanel = objectWord.transform.parent as RectTransform;
        }

        if (targetPanel == null) return;

        if (lineLayer == null)
        {
            GameObject layerObject = new GameObject("RuneConnectionEnergyLayer", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
            layerObject.layer = targetPanel.gameObject.layer;
            layerObject.transform.SetParent(targetPanel, false);

            lineLayer = layerObject.GetComponent<RectTransform>();
            lineLayer.anchorMin = Vector2.zero;
            lineLayer.anchorMax = Vector2.one;
            lineLayer.offsetMin = Vector2.zero;
            lineLayer.offsetMax = Vector2.zero;
            lineLayer.pivot = new Vector2(0.5f, 0.5f);

            CanvasGroup canvasGroup = layerObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            LayoutElement layoutElement = layerObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
        }

        lineLayer.gameObject.SetActive(true);
        lineLayer.transform.SetAsFirstSibling();
        ConfigureNonInteractiveGraphic(lineLayer.gameObject);
        EnsureVisualLayers();
    }

    private void EnsureVisualLayers()
    {
        glowLayer = EnsureChildLayer(glowLayer, "01_Glow");
        ribbonLayer = EnsureChildLayer(ribbonLayer, "02_Ribbon");
        markerLayer = EnsureChildLayer(markerLayer, "03_FlowMarkers");
        nodeLayer = EnsureChildLayer(nodeLayer, "04_AnchorNodes");
        sealLayer = EnsureChildLayer(sealLayer, "05_BoundarySeals");

        glowLayer.SetAsFirstSibling();
        ribbonLayer.SetSiblingIndex(1);
        markerLayer.SetSiblingIndex(2);
        nodeLayer.SetSiblingIndex(3);
        sealLayer.SetAsLastSibling();
    }

    private RectTransform EnsureChildLayer(RectTransform cachedLayer, string layerName)
    {
        if (cachedLayer != null) return cachedLayer;

        Transform existing = lineLayer.Find(layerName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            ConfigureNonInteractiveGraphic(existing.gameObject);
            return existingRect;
        }

        GameObject layerObject = new GameObject(layerName, typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
        layerObject.layer = lineLayer.gameObject.layer;
        layerObject.transform.SetParent(lineLayer, false);
        ConfigureNonInteractiveGraphic(layerObject);

        RectTransform rect = layerObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        CanvasGroup canvasGroup = layerObject.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        LayoutElement layoutElement = layerObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        return rect;
    }

    private void CreateFlowLine(RectTransform fromRect, RectTransform toRect, bool isShared)
    {
        if (!TryGetConnectionGeometry(fromRect, toRect, out Vector2 start, out Vector2 end, out Vector2 sourceNode, out Vector2 targetNode)) return;

        float thickness = isShared ? sharedLineThickness : lineThickness;
        Color baseColor = isShared ? sharedLineColor : lineColor;
        Color glowColor = isShared ? sharedLineGlowColor : lineGlowColor;
        Material baseMaterial = isShared && sharedLineMaterial != null ? sharedLineMaterial : lineMaterial;
        List<Vector2> points = BuildCurvePoints(start, end);

        CreateConnectionNode(sourceNode, true, isShared);
        CreateConnectionNode(targetNode, false, isShared);
        CreateRibbon(points, thickness * lineGlowThicknessMultiplier, glowColor, baseMaterial, "ReadConnectionRibbonGlow", 1.22f, 0.74f, glowLayer);
        CreateRibbon(points, thickness, baseColor, baseMaterial, "ReadConnectionRibbonCore", 1.14f, 0.56f, ribbonLayer);

        if (isShared)
        {
            CreateRibbon(points, Mathf.Max(2f, thickness * 0.42f), sharedAccentColor, sharedLineMaterial != null ? sharedLineMaterial : lineMaterial, "ReadConnectionSharedAccent", 1.18f, 0.64f, markerLayer);
        }

        int markerCount = Mathf.Max(1, flowMarkersPerLine);
        for (int i = 0; i < markerCount; i++)
        {
            float offset = markerCount == 1 ? 0f : i / (float)markerCount;
            CreateFlowMarker(points, isShared, offset);
        }
    }

    private void CreateRibbon(List<Vector2> points, float thickness, Color color, Material material, string segmentName, float pulseScale, float pulseDuration, RectTransform parentLayer)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            RectTransform segment = CreateSegment(segmentName, points[i], points[i + 1], thickness, color, material, -1f, parentLayer);
            if (segment == null) continue;

            segment.DOScaleY(pulseScale, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this)
                .SetUpdate(true);
        }
    }

    private void CreateFlowMarker(List<Vector2> points, bool isShared, float normalizedDelay)
    {
        if (points == null || points.Count < 2) return;

        Color markerColor = isShared ? sharedFlowMarkerColor : flowMarkerColor;
        RectTransform marker = CreateSegment("ReadConnectionFlow", points[0], points[1], flowMarkerThickness, markerColor, flowMarkerMaterial, flowMarkerLength, markerLayer);
        if (marker == null) return;

        Image markerImage = marker.GetComponent<Image>();
        marker.localScale = Vector3.one;
        markerImage.CrossFadeAlpha(0f, 0f, true);

        float progress = 0f;
        Sequence sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
        sequence.AppendInterval(flowDuration * normalizedDelay);
        sequence.AppendCallback(() =>
        {
            if (marker == null || markerImage == null) return;
            progress = 0f;
            SetMarkerOnCurve(marker, points, progress);
            markerImage.CrossFadeAlpha(markerColor.a, 0.08f, true);
        });
        sequence.Append(DOTween.To(() => progress, value =>
        {
            progress = value;
            if (marker != null) SetMarkerOnCurve(marker, points, progress);
        }, 1f, flowDuration).SetEase(Ease.InOutSine));
        sequence.Join(marker.DOScaleX(1.22f, flowDuration * 0.5f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
        sequence.AppendCallback(() =>
        {
            if (marker == null || markerImage == null) return;
            markerImage.CrossFadeAlpha(0f, 0.08f, true);
            SetMarkerOnCurve(marker, points, 0f);
        });
        sequence.SetLoops(-1, LoopType.Restart);
    }

    private void CreateStopMarker(RectTransform fromRect, RectTransform stopRect)
    {
        if (!TryGetConnectionGeometry(fromRect, stopRect, out Vector2 start, out Vector2 end, out Vector2 sourceNode, out Vector2 sealNode)) return;

        List<Vector2> points = BuildCurvePoints(start, end);
        List<Vector2> interruptedPoints = GetPartialCurvePoints(points, interruptedFlowProgress);
        CreateConnectionNode(sourceNode, true, false);

        CreateRibbon(interruptedPoints, lineThickness * lineGlowThicknessMultiplier, stopFlowGlowColor, lineMaterial, "BoundaryInterruptedFlowGlow", 1.16f, 0.4f, glowLayer);
        CreateRibbon(interruptedPoints, Mathf.Max(2.5f, lineThickness * 0.78f), stopFlowColor, lineMaterial, "BoundaryInterruptedFlow", 1.08f, 0.32f, ribbonLayer);
        CreateSealNode(sealNode);
    }

    private void CreateConnectionNode(Vector2 position, bool isSource, bool isShared)
    {
        if (isSource && IsDuplicateSourceNode(position)) return;

        float coreSize = isSource ? sourceNodeSize : targetNodeSize;
        if (isShared) coreSize *= 1.18f;

        Color coreColor = isSource ? sourceNodeColor : targetNodeColor;
        if (isShared && !isSource) coreColor = sharedNodeColor;

        Color glowColor = isShared ? sharedNodeGlowColor : nodeGlowColor;
        float glowSize = coreSize * (isSource ? 2.45f : 2.25f);
        float ringSize = coreSize * 1.45f;

        RectTransform glow = CreateImageElement("RuneConnectionNodeGlow", position, new Vector2(glowSize, glowSize), glowColor, GetCircleSprite(CircleSpriteStyle.Soft), nodeLayer);
        RectTransform ring = CreateImageElement("RuneConnectionNodeRing", position, new Vector2(ringSize, ringSize), coreColor, GetCircleSprite(CircleSpriteStyle.Ring), nodeLayer);
        RectTransform core = CreateImageElement("RuneConnectionNodeCore", position, new Vector2(coreSize, coreSize), coreColor, GetCircleSprite(CircleSpriteStyle.Solid), nodeLayer);

        PulseNode(glow, 1.12f, isShared ? 0.5f : 0.62f);
        PulseNode(ring, 1.08f, isShared ? 0.42f : 0.56f);
        PulseNode(core, 1.05f, isShared ? 0.36f : 0.5f);
    }

    private void CreateSealNode(Vector2 position)
    {
        float glowSize = sealNodeSize * 1.45f;
        RectTransform glow = CreateImageElement("BoundarySealGlow", position, new Vector2(glowSize, glowSize), stopMarkerGlowColor, GetCircleSprite(CircleSpriteStyle.Soft), sealLayer);
        RectTransform ring = CreateImageElement("BoundarySealRing", position, new Vector2(sealNodeSize, sealNodeSize), stopMarkerColor, GetCircleSprite(CircleSpriteStyle.Ring), sealLayer);

        PulseNode(glow, 1.14f, 0.38f);
        if (ring != null)
        {
            ring.DOScale(1.08f, 0.34f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this)
                .SetUpdate(true);
        }

        CreateSealSlash(position, -42f);
    }

    private void CreateSealSlash(Vector2 center, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        CreateStopSegment(center, direction, stopMarkerLength * 1.16f, stopMarkerThickness * 2.15f, stopMarkerGlowColor);
        CreateStopSegment(center, direction, stopMarkerLength, stopMarkerThickness, stopMarkerColor);
    }

    private void CreateStopSegment(Vector2 center, Vector2 direction, float length, float thickness, Color color)
    {
        Vector2 markerStart = center - direction * (length * 0.5f);
        Vector2 markerEnd = center + direction * (length * 0.5f);

        RectTransform marker = CreateSegment("BoundarySealSlash", markerStart, markerEnd, thickness, color, null, -1f, sealLayer);
        if (marker == null) return;

        marker.DOScaleY(1.16f, 0.24f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetTarget(this)
            .SetUpdate(true);
    }

    private bool TryGetConnectionGeometry(
        RectTransform fromRect,
        RectTransform toRect,
        out Vector2 start,
        out Vector2 end,
        out Vector2 sourceNode,
        out Vector2 targetNode)
    {
        Vector2 fromCenter = GetLocalRectPoint(fromRect, fromRect.rect.center);
        Vector2 toCenter = GetLocalRectPoint(toRect, toRect.rect.center);
        float horizontalSign = Mathf.Approximately(toCenter.x, fromCenter.x) ? 1f : Mathf.Sign(toCenter.x - fromCenter.x);

        sourceNode = GetLocalSideAnchor(fromRect, horizontalSign);
        targetNode = GetLocalSideAnchor(toRect, -horizontalSign);

        Vector2 direction = targetNode - sourceNode;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
        {
            start = sourceNode;
            end = targetNode;
            return false;
        }

        Vector2 normalized = direction / distance;
        float gap = Mathf.Min(nodeLineGap, distance * 0.25f);
        start = sourceNode + normalized * gap;
        end = targetNode - normalized * gap;

        return Vector2.Distance(start, end) > 1f;
    }

    private List<Vector2> BuildCurvePoints(Vector2 start, Vector2 end)
    {
        int segmentCount = Mathf.Max(4, curveSegments);
        List<Vector2> points = new List<Vector2>(segmentCount + 1);
        Vector2 delta = end - start;
        Vector2 direction = delta.normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        if (perpendicular.y > 0f) perpendicular = -perpendicular;

        float arcHeight = Mathf.Min(curveHeight, Mathf.Max(14f, delta.magnitude * 0.16f));
        Vector2 control = (start + end) * 0.5f + perpendicular * arcHeight;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            points.Add(SnapToPixel(SampleQuadratic(start, control, end, t)));
        }

        return points;
    }

    private List<Vector2> GetPartialCurvePoints(List<Vector2> points, float progress)
    {
        List<Vector2> partialPoints = new List<Vector2>();
        if (points == null || points.Count < 2) return partialPoints;

        float clampedProgress = Mathf.Clamp01(progress);
        float scaledProgress = clampedProgress * (points.Count - 1);
        int lastWholeIndex = Mathf.Clamp(Mathf.FloorToInt(scaledProgress), 0, points.Count - 2);
        float segmentProgress = scaledProgress - lastWholeIndex;

        for (int i = 0; i <= lastWholeIndex; i++)
        {
            partialPoints.Add(points[i]);
        }

        partialPoints.Add(Vector2.Lerp(points[lastWholeIndex], points[lastWholeIndex + 1], segmentProgress));
        return partialPoints;
    }

    private Vector2 SampleQuadratic(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * start + 2f * oneMinusT * t * control + t * t * end;
    }

    private void SetMarkerOnCurve(RectTransform marker, List<Vector2> points, float progress)
    {
        if (points == null || points.Count < 2) return;

        float scaledProgress = Mathf.Clamp01(progress) * (points.Count - 1);
        int index = Mathf.Min(Mathf.FloorToInt(scaledProgress), points.Count - 2);
        float segmentProgress = scaledProgress - index;
        Vector2 start = points[index];
        Vector2 end = points[index + 1];
        Vector2 position = Vector2.Lerp(start, end, segmentProgress);
        Vector2 direction = end - start;

        marker.anchoredPosition = position;
        marker.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private Vector2 GetLocalSideAnchor(RectTransform rect, float horizontalSign)
    {
        Rect rectValue = rect.rect;
        float x = horizontalSign >= 0f ? rectValue.xMax + edgeAnchorOffset : rectValue.xMin - edgeAnchorOffset;
        float y = Mathf.Lerp(rectValue.yMin, rectValue.yMax, sideAnchorHeight);
        return GetLocalRectPoint(rect, new Vector2(x, y));
    }

    private Vector2 GetLocalRectPoint(RectTransform rect, Vector2 rectPoint)
    {
        Vector3 worldPoint = rect.TransformPoint(rectPoint);
        Vector3 lineLocalPoint = lineLayer.InverseTransformPoint(worldPoint);
        return new Vector2(lineLocalPoint.x, lineLocalPoint.y);
    }

    private RectTransform CreateSegment(string name, Vector2 start, Vector2 end, float thickness, Color color, Material material, float fixedLength = -1f, RectTransform parent = null)
    {
        parent ??= lineLayer;
        Vector2 delta = end - start;
        float length = fixedLength > 0f ? fixedLength : delta.magnitude;
        if (length <= 0.01f) return null;

        GameObject segmentObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        segmentObject.layer = parent.gameObject.layer;
        segmentObject.transform.SetParent(parent, false);
        spawnedVisuals.Add(segmentObject);

        RectTransform segment = segmentObject.GetComponent<RectTransform>();
        segment.anchorMin = new Vector2(0.5f, 0.5f);
        segment.anchorMax = new Vector2(0.5f, 0.5f);
        segment.pivot = new Vector2(0.5f, 0.5f);
        segment.sizeDelta = new Vector2(length, thickness);
        segment.anchoredPosition = SnapToPixel(start + delta * 0.5f);
        segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Image image = segmentObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (material != null) image.material = material;

        return segment;
    }

    private Vector2 SnapToPixel(Vector2 value)
    {
        if (pixelSnapSize <= 0f) return value;

        return new Vector2(
            Mathf.Round(value.x / pixelSnapSize) * pixelSnapSize,
            Mathf.Round(value.y / pixelSnapSize) * pixelSnapSize);
    }

    private RectTransform CreateImageElement(string name, Vector2 position, Vector2 size, Color color, Sprite sprite, RectTransform parent)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        imageObject.transform.SetParent(parent, false);
        spawnedVisuals.Add(imageObject);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = SnapToPixel(position);
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }

    private void PulseNode(RectTransform node, float scale, float duration)
    {
        if (node == null) return;

        node.DOScale(scale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetTarget(this)
            .SetUpdate(true);
    }

    private bool IsDuplicateSourceNode(Vector2 position)
    {
        const float duplicateDistanceSqr = 9f;
        foreach (Vector2 activePosition in activeSourceNodePositions)
        {
            if ((activePosition - position).sqrMagnitude <= duplicateDistanceSqr)
            {
                return true;
            }
        }

        activeSourceNodePositions.Add(position);
        return false;
    }

    private static Sprite GetCircleSprite(CircleSpriteStyle style)
    {
        switch (style)
        {
            case CircleSpriteStyle.Soft:
                return softCircleSprite != null ? softCircleSprite : softCircleSprite = CreateCircleSprite(style);
            case CircleSpriteStyle.Ring:
                return ringCircleSprite != null ? ringCircleSprite : ringCircleSprite = CreateCircleSprite(style);
            default:
                return solidCircleSprite != null ? solidCircleSprite : solidCircleSprite = CreateCircleSprite(style);
        }
    }

    private static Sprite CreateCircleSprite(CircleSpriteStyle style)
    {
        int size = style == CircleSpriteStyle.Soft ? 64 : 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "RuntimeRuneConnectionCircle_" + style,
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float radius = (size - 1) * 0.5f;
        Vector2 center = new Vector2(radius, radius);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha;

                if (style == CircleSpriteStyle.Soft)
                {
                    alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0f, 1f, normalizedDistance));
                }
                else if (style == CircleSpriteStyle.Ring)
                {
                    float outer = 1f - Mathf.SmoothStep(0.86f, 1.02f, normalizedDistance);
                    float inner = Mathf.SmoothStep(0.48f, 0.68f, normalizedDistance);
                    alpha = Mathf.Clamp01(outer * inner);
                }
                else
                {
                    alpha = 1f - Mathf.SmoothStep(0.88f, 1.02f, normalizedDistance);
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static void ConfigureNonInteractiveGraphic(GameObject target)
    {
        if (target == null) return;

        foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
    }
}
