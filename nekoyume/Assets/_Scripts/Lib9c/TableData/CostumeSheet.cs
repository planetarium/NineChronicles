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
    }
}
