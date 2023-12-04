using UnityEngine;
using UnityEngine.UI;

public class CenteredGridLayout : GridLayoutGroup
{
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        AlignItemsInCenter();
    }

    public override void CalculateLayoutInputVertical()
    {
        base.CalculateLayoutInputVertical();
    }

    public override void SetLayoutHorizontal()
    {
    }

    public override void SetLayoutVertical()
    {
    }

    private void AlignItemsInCenter()
    {
        int totalRows = Mathf.CeilToInt(rectChildren.Count / (float)constraintCount);
        float rowWidth = constraintCount * (cellSize.x + spacing.x) - spacing.x;
        float containerWidth = rectTransform.rect.width;
        float offset = (containerWidth - rowWidth) / 2;

        int rowCount = rectChildren.Count / constraintCount;
        int lastRowItemCount = rectChildren.Count % constraintCount;

        float containerHeight = rectTransform.rect.height;
        float totalHeightOfAllItems = totalRows * cellSize.y + (totalRows - 1) * spacing.y;
        float bottomOffset = containerHeight - totalHeightOfAllItems;

        float lastRowOffset = lastRowItemCount > 0 ? (containerWidth - (lastRowItemCount * (cellSize.x + spacing.x) - spacing.x)) / 2 : offset;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            int row = i / constraintCount;
            int column = i % constraintCount;

            float finalOffset = row < rowCount ? offset : lastRowOffset;

            var item = rectChildren[i];
            var xPos = finalOffset + (cellSize.x + spacing.x) * column;
            var yPos = bottomOffset + row * (cellSize.y + spacing.y);

            SetChildAlongAxis(item, 0, xPos, cellSize.x);
            SetChildAlongAxis(item, 1, yPos, cellSize.y);
        }
    }
}
