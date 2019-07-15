using UnityEngine;

namespace Nekoyume.Helper
{
    public static class ColorHelper
    {
        public static Color RGBToColor(float r, float g, float b)
        {
            return new Color(r / 255f, g / 255f, b / 255f);
        }

        public static string ColorToHexRGB(Color32 color)
        {
            return $"{color.r:X2}{color.g:X2}{color.b:X2}";
        }

        public static string ColorToHexRGBA(Color32 color)
        {
            return $"{color.r:X2}{color.g:X2}{color.b:X2}{color.a:X2}";
        }

        public static Color32 HexToColorRGB(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length < 6)
                return Color.white;

            var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color32(r, g, b, 255);
        }

        public static Color32 HexToColorRGBA(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length < 8)
                return Color.white;

            var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            var a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color32(r, g, b, a);
        }
    }
}
