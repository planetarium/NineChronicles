using System;

namespace Nekoyume.EnumType
{
    public enum PivotPresetType
    {
        TopLeft,
        TopCenter,
        TopRight,
 
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
 
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public static class PivotPresetTypeExtension
    {
        public static PivotPresetType Reverse(this PivotPresetType pivotPresetType)
        {
            switch (pivotPresetType)
            {
                case PivotPresetType.TopLeft:
                    return PivotPresetType.BottomRight;
                case PivotPresetType.TopCenter:
                    return PivotPresetType.BottomCenter;
                case PivotPresetType.TopRight:
                    return PivotPresetType.BottomLeft;
                case PivotPresetType.MiddleLeft:
                    return PivotPresetType.MiddleRight;
                case PivotPresetType.MiddleCenter:
                    return PivotPresetType.MiddleCenter;
                case PivotPresetType.MiddleRight:
                    return PivotPresetType.MiddleLeft;
                case PivotPresetType.BottomLeft:
                    return PivotPresetType.TopRight;
                case PivotPresetType.BottomCenter:
                    return PivotPresetType.TopCenter;
                case PivotPresetType.BottomRight:
                    return PivotPresetType.TopLeft;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pivotPresetType), pivotPresetType, null);
            }
        }

        public static PivotPresetType ReverseX(this PivotPresetType pivotPresetType)
        {
            switch(pivotPresetType)
            {
                case PivotPresetType.TopLeft:
                    return PivotPresetType.TopRight;
                case PivotPresetType.TopCenter:
                    return PivotPresetType.TopCenter;
                case PivotPresetType.TopRight:
                    return PivotPresetType.TopLeft;
                case PivotPresetType.MiddleLeft:
                    return PivotPresetType.MiddleRight;
                case PivotPresetType.MiddleCenter:
                    return PivotPresetType.MiddleCenter;
                case PivotPresetType.MiddleRight:
                    return PivotPresetType.MiddleLeft;
                case PivotPresetType.BottomLeft:
                    return PivotPresetType.BottomRight;
                case PivotPresetType.BottomCenter:
                    return PivotPresetType.BottomCenter;
                case PivotPresetType.BottomRight:
                    return PivotPresetType.BottomLeft;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pivotPresetType), pivotPresetType, null);
            }
        }

        public static PivotPresetType ReverseY(this PivotPresetType pivotPresetType)
        {
            switch (pivotPresetType)
            {
                case PivotPresetType.TopLeft:
                    return PivotPresetType.BottomLeft;
                case PivotPresetType.TopCenter:
                    return PivotPresetType.BottomCenter;
                case PivotPresetType.TopRight:
                    return PivotPresetType.BottomRight;
                case PivotPresetType.MiddleLeft:
                    return PivotPresetType.MiddleLeft;
                case PivotPresetType.MiddleCenter:
                    return PivotPresetType.MiddleCenter;
                case PivotPresetType.MiddleRight:
                    return PivotPresetType.MiddleRight;
                case PivotPresetType.BottomLeft:
                    return PivotPresetType.TopLeft;
                case PivotPresetType.BottomCenter:
                    return PivotPresetType.TopCenter;
                case PivotPresetType.BottomRight:
                    return PivotPresetType.TopRight;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pivotPresetType), pivotPresetType, null);
            }
        }
    }
}
