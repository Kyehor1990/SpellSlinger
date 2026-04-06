using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Flow Layout Group")]
public class FlowLayoutGroup : LayoutGroup
{
    public Vector2 spacing;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        float width = rectTransform.rect.width;
        float height = CalculateHeight(width);
        SetLayoutInputForAxis(width, height, -1, 0);
    }

    public override void CalculateLayoutInputVertical() { }

    public override void SetLayoutHorizontal() { SetChildren(0); }

    public override void SetLayoutVertical() { SetChildren(1); }

    private float CalculateHeight(float width)
    {
        float currentX = padding.left;
        float currentY = padding.top;
        float rowHeight = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            float childWidth = rectChildren[i].rect.width;
            float childHeight = rectChildren[i].rect.height;

            if (currentX + childWidth > width - padding.right)
            {
                currentX = padding.left;
                currentY += rowHeight + spacing.y;
                rowHeight = 0;
            }

            currentX += childWidth + spacing.x;
            rowHeight = Mathf.Max(rowHeight, childHeight);
        }

        return currentY + rowHeight + padding.bottom;
    }

    private void SetChildren(int axis)
    {
        float width = rectTransform.rect.width;
        float currentX = padding.left;
        float currentY = padding.top;
        float rowHeight = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            float childWidth = rectChildren[i].rect.width;
            float childHeight = rectChildren[i].rect.height;

            if (currentX + childWidth > width - padding.right)
            {
                currentX = padding.left;
                currentY += rowHeight + spacing.y;
                rowHeight = 0;
            }

            if (axis == 0) SetChildAlongAxis(rectChildren[i], 0, currentX, childWidth);
            else SetChildAlongAxis(rectChildren[i], 1, currentY, childHeight);

            currentX += childWidth + spacing.x;
            rowHeight = Mathf.Max(rowHeight, childHeight);
        }
    }
}