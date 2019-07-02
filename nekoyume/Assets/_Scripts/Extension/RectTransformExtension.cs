using Nekoyume.EnumType;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume
{
    public static class RectTransformExtensions
    {
        public static void SetAnchor(this RectTransform source, AnchorPresetType align, int offsetX = 0, int offsetY = 0)
        {
            source.anchoredPosition = new Vector3(offsetX, offsetY, 0);

            switch (align)
            {
                case AnchorPresetType.TopLeft:
                {
                    source.anchorMin = new float2(0, 1);
                    source.anchorMax = new Vector2(0, 1);
                    break;
                }

                case AnchorPresetType.TopCenter:
                {
                    source.anchorMin = new Vector2(0.5f, 1);
                    source.anchorMax = new Vector2(0.5f, 1);
                    break;
                }

                case AnchorPresetType.TopRight:
                {
                    source.anchorMin = new Vector2(1, 1);
                    source.anchorMax = new Vector2(1, 1);
                    break;
                }

                case AnchorPresetType.MiddleLeft:
                {
                    source.anchorMin = new Vector2(0, 0.5f);
                    source.anchorMax = new Vector2(0, 0.5f);
                    break;
                }

                case AnchorPresetType.MiddleCenter:
                {
                    source.anchorMin = new Vector2(0.5f, 0.5f);
                    source.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                }

                case AnchorPresetType.MiddleRight:
                {
                    source.anchorMin = new Vector2(1, 0.5f);
                    source.anchorMax = new Vector2(1, 0.5f);
                    break;
                }

                case AnchorPresetType.BottomLeft:
                {
                    source.anchorMin = new Vector2(0, 0);
                    source.anchorMax = new Vector2(0, 0);
                    break;
                }

                case AnchorPresetType.BottomCenter:
                {
                    source.anchorMin = new Vector2(0.5f, 0);
                    source.anchorMax = new Vector2(0.5f, 0);
                    break;
                }

                case AnchorPresetType.BottomRight:
                {
                    source.anchorMin = new Vector2(1, 0);
                    source.anchorMax = new Vector2(1, 0);
                    break;
                }

                case AnchorPresetType.HorStretchTop:
                {
                    source.anchorMin = new Vector2(0, 1);
                    source.anchorMax = new Vector2(1, 1);
                    break;
                }

                case AnchorPresetType.HorStretchMiddle:
                {
                    source.anchorMin = new Vector2(0, 0.5f);
                    source.anchorMax = new Vector2(1, 0.5f);
                    break;
                }

                case AnchorPresetType.HorStretchBottom:
                {
                    source.anchorMin = new Vector2(0, 0);
                    source.anchorMax = new Vector2(1, 0);
                    break;
                }

                case AnchorPresetType.VertStretchLeft:
                {
                    source.anchorMin = new Vector2(0, 0);
                    source.anchorMax = new Vector2(0, 1);
                    break;
                }

                case AnchorPresetType.VertStretchCenter:
                {
                    source.anchorMin = new Vector2(0.5f, 0);
                    source.anchorMax = new Vector2(0.5f, 1);
                    break;
                }

                case AnchorPresetType.VertStretchRight:
                {
                    source.anchorMin = new Vector2(1, 0);
                    source.anchorMax = new Vector2(1, 1);
                    break;
                }

                case AnchorPresetType.StretchAll:
                {
                    source.anchorMin = new Vector2(0, 0);
                    source.anchorMax = new Vector2(1, 1);
                    break;
                }
            }
        }

        public static void SetPivot(this RectTransform source, PivotPresetType presetType)
        {
            switch (presetType)
            {
                case PivotPresetType.TopLeft:
                {
                    source.pivot = new Vector2(0, 1);
                    break;
                }

                case PivotPresetType.TopCenter:
                {
                    source.pivot = new Vector2(0.5f, 1);
                    break;
                }

                case PivotPresetType.TopRight:
                {
                    source.pivot = new Vector2(1, 1);
                    break;
                }

                case PivotPresetType.MiddleLeft:
                {
                    source.pivot = new Vector2(0, 0.5f);
                    break;
                }

                case PivotPresetType.MiddleCenter:
                {
                    source.pivot = new Vector2(0.5f, 0.5f);
                    break;
                }

                case PivotPresetType.MiddleRight:
                {
                    source.pivot = new Vector2(1, 0.5f);
                    break;
                }

                case PivotPresetType.BottomLeft:
                {
                    source.pivot = new Vector2(0, 0);
                    break;
                }

                case PivotPresetType.BottomCenter:
                {
                    source.pivot = new Vector2(0.5f, 0);
                    break;
                }

                case PivotPresetType.BottomRight:
                {
                    source.pivot = new Vector2(1, 0);
                    break;
                }
            }
        }
    }
}
