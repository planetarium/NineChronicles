using Nekoyume.Helper;
using UnityEngine;

namespace Nekoyume.UI
{
    public static class ColorConfig
    {
        public const string ColorHexForGrade1 = "fff9dd";
        public const string ColorHexForGrade2 = "12ff00";
        public const string ColorHexForGrade3 = "0f91ff";
        public const string ColorHexForGrade4 = "ffae00";
        public const string ColorHexForGrade5 = "ba00ff";

        public static readonly Color ColorForGrade2 = ColorHelper.HexToColorRGB(ColorHexForGrade2);
        public static readonly Color ColorForGrade3 = ColorHelper.HexToColorRGB(ColorHexForGrade3);
        public static readonly Color ColorForGrade4 = ColorHelper.HexToColorRGB(ColorHexForGrade4);
        public static readonly Color ColorForGrade5 = ColorHelper.HexToColorRGB(ColorHexForGrade5);
    }
}
