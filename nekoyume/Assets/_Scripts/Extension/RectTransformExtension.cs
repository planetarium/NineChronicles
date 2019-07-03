using System;
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
        
        public static void SetAnchor(this RectTransform rectTransform, AnchorPresetType align, int offsetX = 0, int offsetY = 0)
        {
            rectTransform.anchoredPosition = new float2(offsetX, offsetY);

            switch (align)
            {
                case AnchorPresetType.TopLeft:
                {
                    rectTransform.anchorMin = ZeroOneFloat2;
                    rectTransform.anchorMax = ZeroOneFloat2;
                    break;
                }

                case AnchorPresetType.TopCenter:
                {
                    rectTransform.anchorMin = HalfOneFloat2;
                    rectTransform.anchorMax = HalfOneFloat2;
                    break;
                }

                case AnchorPresetType.TopRight:
                {
                    rectTransform.anchorMin = OneOneFloat2;
                    rectTransform.anchorMax = OneOneFloat2;
                    break;
                }

                case AnchorPresetType.MiddleLeft:
                {
                    rectTransform.anchorMin = ZeroHalfFloat2;
                    rectTransform.anchorMax = ZeroHalfFloat2;
                    break;
                }

                case AnchorPresetType.MiddleCenter:
                {
                    rectTransform.anchorMin = HalfHalfFloat2;
                    rectTransform.anchorMax = HalfHalfFloat2;
                    break;
                }

                case AnchorPresetType.MiddleRight:
                {
                    rectTransform.anchorMin = OneHalfFloat2;
                    rectTransform.anchorMax = OneHalfFloat2;
                    break;
                }

                case AnchorPresetType.BottomLeft:
                {
                    rectTransform.anchorMin = ZeroZeroFloat2;
                    rectTransform.anchorMax = ZeroZeroFloat2;
                    break;
                }

                case AnchorPresetType.BottomCenter:
                {
                    rectTransform.anchorMin = HalfZeroFloat2;
                    rectTransform.anchorMax = HalfZeroFloat2;
                    break;
                }

                case AnchorPresetType.BottomRight:
                {
                    rectTransform.anchorMin = OneZeroFloat2;
                    rectTransform.anchorMax = OneZeroFloat2;
                    break;
                }

                case AnchorPresetType.HorStretchTop:
                {
                    rectTransform.anchorMin = ZeroOneFloat2;
                    rectTransform.anchorMax = OneOneFloat2;
                    break;
                }

                case AnchorPresetType.HorStretchMiddle:
                {
                    rectTransform.anchorMin = ZeroHalfFloat2;
                    rectTransform.anchorMax = OneHalfFloat2;
                    break;
                }

                case AnchorPresetType.HorStretchBottom:
                {
                    rectTransform.anchorMin = ZeroZeroFloat2;
                    rectTransform.anchorMax = OneZeroFloat2;
                    break;
                }

                case AnchorPresetType.VertStretchLeft:
                {
                    rectTransform.anchorMin = ZeroZeroFloat2;
                    rectTransform.anchorMax = ZeroOneFloat2;
                    break;
                }

                case AnchorPresetType.VertStretchCenter:
                {
                    rectTransform.anchorMin = HalfZeroFloat2;
                    rectTransform.anchorMax = HalfOneFloat2;
                    break;
                }

                case AnchorPresetType.VertStretchRight:
                {
                    rectTransform.anchorMin = OneZeroFloat2;
                    rectTransform.anchorMax = OneOneFloat2;
                    break;
                }

                case AnchorPresetType.StretchAll:
                {
                    rectTransform.anchorMin = ZeroZeroFloat2;
                    rectTransform.anchorMax = OneOneFloat2;
                    break;
                }
            }
        }

        public static void SetPivot(this RectTransform rectTransform, PivotPresetType presetType)
        {
            switch (presetType)
            {
                case PivotPresetType.TopLeft:
                {
                    rectTransform.pivot = ZeroOneFloat2;
                    break;
                }

                case PivotPresetType.TopCenter:
                {
                    rectTransform.pivot = HalfOneFloat2;
                    break;
                }

                case PivotPresetType.TopRight:
                {
                    rectTransform.pivot = OneOneFloat2;
                    break;
                }

                case PivotPresetType.MiddleLeft:
                {
                    rectTransform.pivot = ZeroHalfFloat2;
                    break;
                }

                case PivotPresetType.MiddleCenter:
                {
                    rectTransform.pivot = HalfHalfFloat2;
                    break;
                }

                case PivotPresetType.MiddleRight:
                {
                    rectTransform.pivot = OneHalfFloat2;
                    break;
                }

                case PivotPresetType.BottomLeft:
                {
                    rectTransform.pivot = ZeroZeroFloat2;
                    break;
                }

                case PivotPresetType.BottomCenter:
                {
                    rectTransform.pivot = HalfZeroFloat2;
                    break;
                }

                case PivotPresetType.BottomRight:
                {
                    rectTransform.pivot = OneZeroFloat2;
                    break;
                }
            }
        }

        public static void MoveInsideOfScreen(this RectTransform rectTransform, Camera camera, PivotPresetType pivotPresetType)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                new float2(Screen.width, Screen.height), camera, out var localPoint))
            {
                return;
            }
            
            var x = rectTransform.anchorMax.x;
            var y = rectTransform.anchorMin.y;
            
            switch (pivotPresetType)
            {
                case PivotPresetType.TopLeft:
                    if (x > 1f)
                    {
                        x = (x - 1f) * Screen.width;
                    }

                    if (y < 0f)
                    {
                        
                    }
                    break;
                case PivotPresetType.TopCenter:
                    break;
                case PivotPresetType.TopRight:
                    break;
                case PivotPresetType.MiddleLeft:
                    break;
                case PivotPresetType.MiddleCenter:
                    break;
                case PivotPresetType.MiddleRight:
                    break;
                case PivotPresetType.BottomLeft:
                    break;
                case PivotPresetType.BottomCenter:
                    break;
                case PivotPresetType.BottomRight:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pivotPresetType), pivotPresetType, null);
            }
        }
    }
}
