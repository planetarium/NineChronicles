namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using System.IO;

    public static class TableSheetsImporter
    {
        public static Dictionary<string, string> ImportSheets(string dir = null)
        {
            dir ??= Path
                .GetFullPath($"..{Path.DirectorySeparatorChar}")
                .Replace(
                    $".Lib9c.Tests{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}",
                    $"Lib9c{Path.DirectorySeparatorChar}TableCSV{Path.DirectorySeparatorChar}");
            var files = Directory.GetFiles(dir, "*.csv", SearchOption.AllDirectories);
            var sheets = new Dictionary<string, string>();
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith(".csv"))
                {
                    fileName = fileName.Split(".csv")[0];
                }

                sheets[fileName] = File.ReadAllText(filePath);
            }

            return sheets;
        }
    }
}
