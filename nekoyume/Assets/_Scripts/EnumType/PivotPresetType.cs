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
    }
}
