namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using System.IO;

    public static class TableSheetsImporter
    {
        public static Dictionary<string, string> ImportSheets(string path = null)
        {
            path ??= GetDefaultPath();
            var files = Directory.GetFiles(path, "*.csv", SearchOption.AllDirectories);
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

        public static bool TryGetCsv(string sheetName, out string csv)
        {
            var path = GetDefaultPath();
            var filePaths = Directory.GetFiles(path, "*.csv", SearchOption.AllDirectories);
            foreach (var filePath in filePaths)
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith(".csv"))
                {
                    fileName = fileName.Split(".csv")[0];
                }

                if (fileName.Equals(sheetName))
                {
                    csv = File.ReadAllText(filePath);
                    return true;
                }
            }

            csv = string.Empty;
            return false;
        }

        private static string GetDefaultPath() => Path
            .GetFullPath("../../")
            .Replace(
                Path.Combine(".Lib9c.Tests", "bin"),
                Path.Combine("Lib9c", "TableCSV"));
    }
}
