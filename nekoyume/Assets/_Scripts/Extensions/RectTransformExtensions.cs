using System;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.Util;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume
{
    public static class RectTransformExtensions
    {
        public static void SetAnchor(this RectTransform rectTransform, AnchorPresetType anchorPresetType,
            int offsetX = 0, int offsetY = 0)
        {
            rectTransform.anchoredPosition = new float2(offsetX, offsetY);

            switch (anchorPresetType)
            {
                case AnchorPresetType.TopLeft:
                    rectTransform.anchorMin = Float2.ZeroOne;
                    rectTransform.anchorMax = Float2.ZeroOne;
                    break;
                case AnchorPresetType.TopCenter:
                    rectTransform.anchorMin = Float2.HalfOne;
                    rectTransform.anchorMax = Float2.HalfOne;
                    break;
                case AnchorPresetType.TopRight:
                    rectTransform.anchorMin = Float2.OneOne;
                    rectTransform.anchorMax = Float2.OneOne;
                    break;
                case AnchorPresetType.MiddleLeft:
                    rectTransform.anchorMin = Float2.ZeroHalf;
                    rectTransform.anchorMax = Float2.ZeroHalf;
                    break;
                case AnchorPresetType.MiddleCenter:
                    rectTransform.anchorMin = Float2.HalfHalf;
                    rectTransform.anchorMax = Float2.HalfHalf;
                    break;
                case AnchorPresetType.MiddleRight:
                    rectTransform.anchorMin = Float2.OneHalf;
                    rectTransform.anchorMax = Float2.OneHalf;
                    break;
                case AnchorPresetType.BottomLeft:
                    rectTransform.anchorMin = Float2.ZeroZero;
                    rectTransform.anchorMax = Float2.ZeroZero;
                    break;
                case AnchorPresetType.BottomCenter:
                    rectTransform.anchorMin = Float2.HalfZero;
                    rectTransform.anchorMax = Float2.HalfZero;
                    break;
                case AnchorPresetType.BottomRight:
                    rectTransform.anchorMin = Float2.OneZero;
                    rectTransform.anchorMax = Float2.OneZero;
                    break;
                case AnchorPresetType.HorStretchTop:
                    rectTransform.anchorMin = Float2.ZeroOne;
                    rectTransform.anchorMax = Float2.OneOne;
                    break;
                case AnchorPresetType.HorStretchMiddle:
                    rectTransform.anchorMin = Float2.ZeroHalf;
                    rectTransform.anchorMax = Float2.OneHalf;
                    break;
                case AnchorPresetType.HorStretchBottom:
                    rectTransform.anchorMin = Float2.ZeroZero;
                    rectTransform.anchorMax = Float2.OneZero;
                    break;
                case AnchorPresetType.VertStretchLeft:
                    rectTransform.anchorMin = Float2.ZeroZero;
                    rectTransform.anchorMax = Float2.ZeroOne;
                    break;
                case AnchorPresetType.VertStretchCenter:
                    rectTransform.anchorMin = Float2.HalfZero;
                    rectTransform.anchorMax = Float2.HalfOne;
                    break;
                case AnchorPresetType.VertStretchRight:
                    rectTransform.anchorMin = Float2.OneZero;
                    rectTransform.anchorMax = Float2.OneOne;
                    break;
                case AnchorPresetType.StretchAll:
                    rectTransform.anchorMin = Float2.ZeroZero;
                    rectTransform.anchorMax = Float2.OneOne;
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
                    rectTransform.pivot = Float2.ZeroOne;
                    break;
                case PivotPresetType.TopCenter:
                    rectTransform.pivot = Float2.HalfOne;
                    break;
                case PivotPresetType.TopRight:
                    rectTransform.pivot = Float2.OneOne;
                    break;
                case PivotPresetType.MiddleLeft:
                    rectTransform.pivot = Float2.ZeroHalf;
                    break;
                case PivotPresetType.MiddleCenter:
                    rectTransform.pivot = Float2.HalfHalf;
                    break;
                case PivotPresetType.MiddleRight:
                    rectTransform.pivot = Float2.OneHalf;
                    break;
                case PivotPresetType.BottomLeft:
                    rectTransform.pivot = Float2.ZeroZero;
                    break;
                case PivotPresetType.BottomCenter:
                    rectTransform.pivot = Float2.HalfZero;
                    break;
                case PivotPresetType.BottomRight:
                    rectTransform.pivot = Float2.OneZero;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pivotPresetType), pivotPresetType, null);
            }
        }

        public static bool TryGetPivotPresetType(this RectTransform rectTransform, out PivotPresetType result)
        {
            int pivotPresetTypeIndex = 0;
            switch(rectTransform.pivot.y)
            {
                case 0:
                    pivotPresetTypeIndex += 6;
                    break;
                case 0.5f:
                    pivotPresetTypeIndex += 3;
                    break;
                case 1:
                    break;
                default:
                    result = default;
                    return false;
            }

            switch(rectTransform.pivot.x)
            {
                case 0:
                    break;
                case 0.5f:
                    pivotPresetTypeIndex += 1;
                    break;
                case 1:
                    pivotPresetTypeIndex += 2;
                    break;
                default:
                    result = default;
                    return false;
            }

            result = (PivotPresetType)pivotPresetTypeIndex;
            return true;
        }

        public static void SetAnchorAndPivot(this RectTransform rectTransform, AnchorPresetType anchorPresetType, PivotPresetType pivotPresetType)
        {
            rectTransform.SetAnchor(anchorPresetType);
            rectTransform.SetPivot(pivotPresetType);
        }

        public static float2 GetPivotPositionFromAnchor(this RectTransform rectTransform, PivotPresetType pivotPresetType)
        {
            var anchoredPosition = new float2();
            var pivot = rectTransform.pivot;
            var size = rectTransform.rect.size * rectTransform.transform.localScale;

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

        public static void GetOffsetsFromPivot(this RectTransform rectTransform, float2 pivot, out float2 bottomLeftOffset, out float2 topRightOffset)
        {
            var size = rectTransform.rect.size;

            bottomLeftOffset = new float2(-size.x * pivot.x, -size.y * pivot.y);
            topRightOffset = new float2(size.x * (1 - pivot.x), size.y * (1 - pivot.y));
        }

        public static void MoveToRelatedPosition(this RectTransform rectTransform, RectTransform target, PivotPresetType pivotPresetType,
             float2 offset)
        {
            if (target is null)
            {
                return;
            }

            rectTransform.position = target.position;
            float2 anchoredPosition = rectTransform.anchoredPosition;

            anchoredPosition += target.GetPivotPositionFromAnchor(pivotPresetType) + offset;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        public static void MoveInsideOfParent(this RectTransform rectTransform)
        {
            MoveInsideOfParent(rectTransform, Float2.ZeroZero);
        }

        public static void MoveInsideOfParent(this RectTransform rectTransform, float2 margin)
        {
            if (!(rectTransform.parent is RectTransform parent))
            {
                return;
            }

            parent.GetOffsetsFromPivot(rectTransform.pivot, out var bottomLeftOffset, out var topRightOffset);

            var anchoredPosition = rectTransform.anchoredPosition;
            var anchoredPositionBottomLeft = rectTransform.GetAnchoredPositionOfPivot(PivotPresetType.BottomLeft);
            var anchoredPositionTopRight = rectTransform.GetAnchoredPositionOfPivot(PivotPresetType.TopRight);

            // Bottom.
            var value = bottomLeftOffset.y + margin.y - anchoredPositionBottomLeft.y;
            if (value > 0f)
            {
                anchoredPosition.y += value;
            }

            // Top.
            value = topRightOffset.y - margin.y - anchoredPositionTopRight.y;
            if (value < 0f)
            {
                anchoredPosition.y += value;
            }

            // Right.
            value = topRightOffset.x - margin.x - anchoredPositionTopRight.x;
            if (value < 0f)
            {
                anchoredPosition.x += value;
            }

            // Left.
            value = bottomLeftOffset.x + margin.x - anchoredPositionBottomLeft.x;
            if (value > 0f)
            {
                anchoredPosition.x += value;
            }

            rectTransform.anchoredPosition = anchoredPosition;
        }

        public static Tweener DoAnchoredMove(this RectTransform rectTransform, Vector2 to,
            float duration, bool relative = false)
        {
            Vector2 endValue;
            if (relative)
            {
                var anchoredPosition = rectTransform.anchoredPosition;
                endValue = to + anchoredPosition;
            }
            else
                endValue = to;

            return DOTween.To(() => rectTransform.anchoredPosition,
                value => rectTransform.anchoredPosition = value, to, duration);
        }

        public static Tweener DoAnchoredMoveX(
            this RectTransform rectTransform, float to, float duration, bool relative = false)
        {
            Vector2 endValue;
            if(relative)
            {
                var anchoredPosition = rectTransform.anchoredPosition;
                endValue = new Vector2(to, 0) + anchoredPosition;
            }
            else
                endValue = new Vector2(rectTransform.anchoredPosition.x, to);

            return DOTween.To(() => rectTransform.anchoredPosition,
                value => rectTransform.anchoredPosition = value, endValue, duration);
        }

        public static Tweener DoAnchoredMoveY(
            this RectTransform rectTransform, float to, float duration, bool relative = false)
        {
            Vector2 endValue;
            if(relative)
            {
                var anchoredPosition = rectTransform.anchoredPosition;
                endValue = new Vector2(0, to) + anchoredPosition;
            }
            else
                endValue = new Vector2(rectTransform.anchoredPosition.x, to);

            return DOTween.To(() => rectTransform.anchoredPosition,
                value => rectTransform.anchoredPosition = value, endValue, duration);
        }

        public static float3 GetWorldPositionOfCenter(this RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var beginPos = (corners[0] + corners[2]) / 2;
            return beginPos;
        }

        public static float3 GetWorldPositionOfPivot(this RectTransform rectTransform, PivotPresetType pivotPresetType)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            switch (pivotPresetType)
            {
                case PivotPresetType.TopLeft:
                    return corners[1];
                case PivotPresetType.TopCenter:
                    return (corners[1] + corners[2]) / 2;
                case PivotPresetType.TopRight:
                    return corners[2];
                case PivotPresetType.MiddleLeft:
                    return (corners[0] + corners[1]) / 2;
                case PivotPresetType.MiddleCenter:
                    return (corners[0] + corners[2]) / 2;
                case PivotPresetType.MiddleRight:
                    return (corners[2] + corners[3]) / 2;
                case PivotPresetType.BottomLeft:
                    return corners[0];
                case PivotPresetType.BottomCenter:
                    return (corners[0] + corners[3]) / 2;
                case PivotPresetType.BottomRight:
                    return corners[3];
                default:
                    throw new ArgumentOutOfRangeException(nameof(pivotPresetType), pivotPresetType, null);
            }
        }

        public static void FlipX(this Transform vfx, bool isFlip)
        {
            var isFlipped = vfx.localScale.x < 0;
            var flipFlag = isFlipped ^ isFlip;

            var localScale = vfx.transform.localScale;
            localScale               = new Vector3(localScale.x * (flipFlag ? -1 : 1), localScale.y, localScale.z);
            vfx.transform.localScale = localScale;
        }
    }
}
