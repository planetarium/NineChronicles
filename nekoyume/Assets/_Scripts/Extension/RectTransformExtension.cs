using System;
using System.Collections;
using Nekoyume.EnumType;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume
{
    public static class RectTransformExtensions
    {
        public static readonly float2 ZeroZeroFloat2 = new float2(0f, 0f);
        public static readonly float2 ZeroHalfFloat2 = new float2(0f, 0.5f);
        public static readonly float2 ZeroOneFloat2 = new float2(0f, 1f);
        public static readonly float2 HalfZeroFloat2 = new float2(0.5f, 0f);
        public static readonly float2 HalfHalfFloat2 = new float2(0.5f, 0.5f);
        public static readonly float2 HalfOneFloat2 = new float2(0.5f, 1f);
        public static readonly float2 OneZeroFloat2 = new float2(1f, 0f);
        public static readonly float2 OneHalfFloat2 = new float2(1f, 0.5f);
        public static readonly float2 OneOneFloat2 = new float2(1f, 1f);
        
        public static void SetAnchor(this RectTransform rectTransform, AnchorPresetType anchorPresetType,
            int offsetX = 0, int offsetY = 0)
        {
            rectTransform.anchoredPosition = new float2(offsetX, offsetY);

            switch (anchorPresetType)
            {
                case AnchorPresetType.TopLeft:
                    rectTransform.anchorMin = ZeroOneFloat2;
                    rectTransform.anchorMax = ZeroOneFloat2;
                    break;
                case AnchorPresetType.TopCenter:
                    rectTransform.anchorMin = HalfOneFloat2;
                    rectTransform.anchorMax = HalfOneFloat2;
                    break;
                case AnchorPresetType.TopRight:
                    rectTransform.anchorMin = OneOneFloat2;
                    rectTransform.anchorMax = OneOneFloat2;
                    break;
                case AnchorPresetType.MiddleLeft:
                    rectTransform.anchorMin = ZeroHalfFloat2;
                    rectTransform.anchorMax = ZeroHalfFloat2;
                    break;
                case AnchorPresetType.MiddleCenter:
                    rectTransform.anchorMin = HalfHalfFloat2;
                    rectTransform.anchorMax = HalfHalfFloat2;
                    break;
                case AnchorPresetType.MiddleRight:
                    rectTransform.anchorMin = OneHalfFloat2;
                    rectTransform.anchorMax = OneHalfFloat2;
                    break;
                case AnchorPresetType.BottomLeft:
                    rectTransform.anchorMin = ZeroZeroFloat2;
                    rectTransform.anchorMax = ZeroZeroFloat2;
                    break;
                case AnchorPresetType.BottomCenter:
                    rectTransform.anchorMin = HalfZeroFloat2;
                    rectTransform.anchorMax = HalfZeroFloat2;
                    break;
                case AnchorPresetType.BottomRight:
                    rectTransform.anchorMin = OneZeroFloat2;
                    rectTransform.anchorMax = OneZeroFloat2;
                    break;
                case AnchorPresetType.HorStretchTop:
                    rectTransform.anchorMin = ZeroOneFloat2;
                    rectTransform.anchorMax = OneOneFloat2;
                    break;
                case AnchorPresetType.HorStretchMiddle:
                    rectTransform.anchorMin = ZeroHalfFloat2;
                    rectTransform.anchorMax = OneHalfFloat2;
                    break;
                case AnchorPresetType.HorStretchBottom:
                    rectTransform.anchorMin = ZeroZeroFloat2;
                    rectTransform.anchorMax = OneZeroFloat2;
                    break;
                case AnchorPresetType.VertStretchLeft:
                    rectTransform.anchorMin = ZeroZeroFloat2;
                    rectTransform.anchorMax = ZeroOneFloat2;
                    break;
                case AnchorPresetType.VertStretchCenter:
                    rectTransform.anchorMin = HalfZeroFloat2;
                    rectTransform.anchorMax = HalfOneFloat2;
                    break;
                case AnchorPresetType.VertStretchRight:
                    rectTransform.anchorMin = OneZeroFloat2;
                    rectTransform.anchorMax = OneOneFloat2;
                    break;
                case AnchorPresetType.StretchAll:
                    rectTransform.anchorMin = ZeroZeroFloat2;
                    rectTransform.anchorMax = OneOneFloat2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(anchorPresetType), anchorPresetType, null);
            }
        }

        public static void SetPivot(this RectTransform rectTransform, PivotPresetType pivotPresetType)
        {
            switch (pivotPresetType)
            {
                case PivotPresetType.TopLeft:
                    rectTransform.pivot = ZeroOneFloat2;
                    break;
                case PivotPresetType.TopCenter:
                    rectTransform.pivot = HalfOneFloat2;
                    break;
                case PivotPresetType.TopRight:
                    rectTransform.pivot = OneOneFloat2;
                    break;
                case PivotPresetType.MiddleLeft:
                    rectTransform.pivot = ZeroHalfFloat2;
                    break;
                case PivotPresetType.MiddleCenter:
                    rectTransform.pivot = HalfHalfFloat2;
                    break;
                case PivotPresetType.MiddleRight:
                    rectTransform.pivot = OneHalfFloat2;
                    break;
                case PivotPresetType.BottomLeft:
                    rectTransform.pivot = ZeroZeroFloat2;
                    break;
                case PivotPresetType.BottomCenter:
                    rectTransform.pivot = HalfZeroFloat2;
                    break;
                case PivotPresetType.BottomRight:
                    rectTransform.pivot = OneZeroFloat2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pivotPresetType), pivotPresetType, null);
            }
        }

        public static float2 GetPivotPositionFromAnchor(this RectTransform rectTransform, PivotPresetType pivotPresetType)
        {
            var anchoredPosition = new float2();
            var pivot = rectTransform.pivot;
            var size = rectTransform.rect.size * rectTransform.transform.localScale.x;
            
            switch (pivotPresetType)
            {
                case PivotPresetType.TopLeft:
                    anchoredPosition.x -= pivot.x * size.x;
                    anchoredPosition.y += (1f - pivot.y) * size.y;
                    break;
                case PivotPresetType.TopCenter:
                    anchoredPosition.x += (0.5f - pivot.x) * size.x;
                    anchoredPosition.y += (1f - pivot.y) * size.y;
                    break;
                case PivotPresetType.TopRight:
                    anchoredPosition.x += (1f - pivot.x) * size.x;
                    anchoredPosition.y += (1f - pivot.y) * size.y;
                    break;
                case PivotPresetType.MiddleLeft:
                    anchoredPosition.x -= pivot.x * size.x;
                    anchoredPosition.y += (0.5f - pivot.y) * size.y;
                    break;
                case PivotPresetType.MiddleCenter:
                    anchoredPosition.x += (0.5f - pivot.x) * size.x;
                    anchoredPosition.y += (0.5f - pivot.y) * size.y;
                    break;
                case PivotPresetType.MiddleRight:
                    anchoredPosition.x += (1f - pivot.x) * size.x;
                    anchoredPosition.y += (0.5f - pivot.y) * size.y;
                    break;
                case PivotPresetType.BottomLeft:
                    anchoredPosition.x -= pivot.x * size.x;
                    anchoredPosition.y -= pivot.y * size.y;
                    break;
                case PivotPresetType.BottomCenter:
                    anchoredPosition.x += (0.5f - pivot.x) * size.x;
                    anchoredPosition.y -= pivot.y * size.y;
                    break;
                case PivotPresetType.BottomRight:
                    anchoredPosition.x += (1f - pivot.x) * size.x;
                    anchoredPosition.y -= pivot.y * size.y;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pivotPresetType), pivotPresetType, null);
            }

            return anchoredPosition;
        }
        
        public static float2 GetAnchoredPositionOfPivot(this RectTransform rectTransform, PivotPresetType pivotPresetType)
        {
            float2 anchoredPosition = rectTransform.anchoredPosition;
            return anchoredPosition + rectTransform.GetPivotPositionFromAnchor(pivotPresetType);
        }

        public static void GetPositions(this RectTransform rectTransform, float2 pivot,
            out float2 bottomLeft, out float2 topRight)
        {
            var size = rectTransform.rect.size;
            bottomLeft = new float2(size.x * pivot.x, -(size.y * pivot.y));
            topRight = new float2(size.x * (1f - pivot.x), size.y * (1f - pivot.y));
        }
        
        public static void MoveToRelatedPosition(this RectTransform rectTransform, RectTransform target,
             float2 offset, Vector2 defaultWidgetSize = default)
        {
            if (target is null)
            {
                return;
            }

            float widgetWidth = rectTransform.rect.width == 0 ? defaultWidgetSize.x : rectTransform.rect.width;

            rectTransform.position = target.position;
            float2 anchoredPosition = rectTransform.anchoredPosition;

            PivotPresetType pivotPresetType;

            if (UI.MainCanvas.instance.GetComponent<RectTransform>().rect.width - anchoredPosition.x - target.rect.width / 2 > widgetWidth)
                pivotPresetType = PivotPresetType.TopRight;
            else
            {
                anchoredPosition.x -= widgetWidth;
                pivotPresetType = PivotPresetType.TopLeft;
                offset = new float2(-offset.x, offset.y);
            }

            anchoredPosition += target.GetPivotPositionFromAnchor(pivotPresetType) + offset;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        public static void MoveInsideOfParent(this RectTransform rectTransform)
        {
            MoveInsideOfParent(rectTransform, ZeroZeroFloat2);
        }

        public static void MoveInsideOfParent(this RectTransform rectTransform, float2 margin)
        {
            if (!(rectTransform.parent is RectTransform parent))
            {
                return;
            }

            parent.GetPositions(rectTransform.pivot, out var bottomLeft, out var topRight);
            
            var anchoredPosition = rectTransform.anchoredPosition;
            var anchoredPositionBottomLeft = rectTransform.GetAnchoredPositionOfPivot(PivotPresetType.BottomLeft);
            var anchoredPositionTopRight = rectTransform.GetAnchoredPositionOfPivot(PivotPresetType.TopRight);

            // Bottom.
            var value = bottomLeft.y + margin.y - anchoredPositionBottomLeft.y;
            if (value > 0f)
            {
                anchoredPosition.y += value;
            }
            
            // Top.
            value = topRight.y - margin.y - anchoredPositionTopRight.y;
            if (value < 0f)
            {
                anchoredPosition.y += value;
            }
            
            // Right.
            value = topRight.x - margin.x - anchoredPositionTopRight.x;
            if (value < 0f)
            {
                anchoredPosition.x += value;
            }
            
            // Left.
            value = bottomLeft.x + margin.x - anchoredPositionBottomLeft.x;
            if (value > 0f)
            {
                anchoredPosition.x += value;
            }

            rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}
