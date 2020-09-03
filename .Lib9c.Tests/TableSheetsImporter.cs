namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using Nekoyume.Model.State;

    public static class TableSheetsImporter
    {
        public static Dictionary<string, string> ImportSheets()
        {
            var sheets = new Dictionary<string, string>();
            var dir = Path.Combine("Data", "TableCSV");
            var files = Directory.GetFiles(dir, "*.csv", SearchOption.AllDirectories);
            var state = new Action.State();
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
