namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using Nekoyume.Model.State;

    public static class TableSheetsImporter
    {
        public static TableSheetsState ImportTableSheets()
        {
            var sheets = new Dictionary<string, string>();
            var dir = Path.Combine("Data", "TableCSV");
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

            return new TableSheetsState(sheets);
        }
    }
}
