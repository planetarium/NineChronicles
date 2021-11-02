namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using System.IO;

    public static class TableSheetsImporter
    {
        public static Dictionary<string, string> ImportSheets(
            string dir = null)
        {
            var sheets = new Dictionary<string, string>();
            dir ??= Path.GetFullPath("..\\")
                .Replace(".Lib9c.Tests\\bin\\Debug\\", "Lib9c\\TableCSV\\");
            var files = Directory.GetFiles(dir, "*.csv", SearchOption.AllDirectories);
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
