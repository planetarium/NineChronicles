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
        var totalRows = Mathf.CeilToInt(rectChildren.Count / (float)constraintCount);
        var rowWidth = constraintCount * (cellSize.x + spacing.x) - spacing.x;
        var containerWidth = rectTransform.rect.width;
        var offset = (containerWidth - rowWidth) / 2;

        var rowCount = rectChildren.Count / constraintCount;
        var lastRowItemCount = rectChildren.Count % constraintCount;

        var containerHeight = rectTransform.rect.height;
        var totalHeightOfAllItems = totalRows * cellSize.y + (totalRows - 1) * spacing.y;
        var bottomOffset = containerHeight - totalHeightOfAllItems;

        var lastRowOffset = lastRowItemCount > 0 ? (containerWidth - (lastRowItemCount * (cellSize.x + spacing.x) - spacing.x)) / 2 : offset;

        for (var i = 0; i < rectChildren.Count; i++)
        {
            var row = i / constraintCount;
            var column = i % constraintCount;

            var finalOffset = row < rowCount ? offset : lastRowOffset;

            var item = rectChildren[i];
            var xPos = finalOffset + (cellSize.x + spacing.x) * column;
            var yPos = bottomOffset + row * (cellSize.y + spacing.y);

            SetChildAlongAxis(item, 0, xPos, cellSize.x);
            SetChildAlongAxis(item, 1, yPos, cellSize.y);
        }
    }
}
