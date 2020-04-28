using System.Collections.Generic;
using System.Globalization;

namespace Nekoyume.TableData
{
    public class CostumeSheet
    {
        public static List<string> GetEyeResources(int colorIndex)
        {
            var result = new List<string>();
            const string format = "eye_{0}_{1}";
            for (var i = 0; i < 2; i++)
            {
                var item1 = GetEyeColor(colorIndex);
                var item2 = i == 0
                    ? "half"
                    : "open";
                var resource = string.Format(
                    CultureInfo.InvariantCulture,
                    format,
                    item1,
                    item2
                );
                result.Add(resource);
            }

            return result;
        }

        public static List<string> GetHairResources(int typeIndex, int colorIndex)
        {
            var typeString = typeIndex == 0
                ? "0001"
                : "0007";
            var resourceCount = typeIndex == 0
                ? 6
                : 8;
            var result = new List<string>();
            for (var i = 0; i < resourceCount; i++)
            {
                result.Add($"hair_{typeString}_{GetHairColor(colorIndex)}_{i + 1:d2}");
            }

            return result;
        }

        private static string GetEyeColor(int index)
        {
            switch (index)
            {
                case 0:
                    return "red";
                case 1:
                    return "blue";
                case 2:
                    return "green";
                case 3:
                    return "violet";
                case 4:
                    return "white";
                case 5:
                    return "yellow";
                default:
                    return "red";
            }
        }

        private static string GetHairColor(int index)
        {
            switch (index)
            {
                case 0:
                    return "brown";
                case 1:
                    return "blue";
                case 2:
                    return "green";
                case 3:
                    return "red";
                case 4:
                    return "white";
                case 5:
                    return "yellow";
                default:
                    return "brown";
            }
        }
    }
}
