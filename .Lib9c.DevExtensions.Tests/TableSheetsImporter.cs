using System.Collections.Generic;
using System.IO;

namespace Lib9c.DevExtensions.Tests
{
    public static class TableSheetsImporter
    {
        public static Dictionary<string, string> ImportSheets() =>
            Lib9c.Tests.TableSheetsImporter.ImportSheets(Path
                .GetFullPath("../../")
                .Replace(
                    Path.Combine(".Lib9c.DevExtensions.Tests", "bin"),
                    Path.Combine("Lib9c", "TableCSV")));
    }
}
