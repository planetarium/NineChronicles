using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nekoyume.L10n.Editor
{
    public static class L10nManagerEditor
    {
        private const string OldCsvFilesRootPath = "Localization";

        [MenuItem("Tools/L10n/Migrate Old Csv Files")]
        public static void MigrateOldCsvFiles()
        {
            var oldCsvAssets = Resources.LoadAll<TextAsset>(OldCsvFilesRootPath);
            foreach (var oldCsvAsset in oldCsvAssets)
            {
                var oldLines = oldCsvAsset.text
                    .Split(new[] {"\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);

                var lines = oldLines.Select((oldLine, lineIndex) => lineIndex == 0
                    ? oldLine
                    : oldLine
                        .Split(',')
                        .Select((oldColumn, columnIndex) => columnIndex == 0
                            ? oldColumn
                            : $"\"{oldColumn}\""
                                .Replace("[Newline]", "\r\n")
                                .Replace("[newline]", "\r\n")
                                .Replace("[Comma]", ",")
                                .Replace("[comma]", ","))
                        .Aggregate((column1, column2) => $"{column1},{column2}")
                );

                if (!Directory.Exists(L10nManager.CsvFilesRootPath))
                {
                    Directory.CreateDirectory(L10nManager.CsvFilesRootPath);
                }

                var path = Path.Combine(L10nManager.CsvFilesRootPath, $"{oldCsvAsset.name}.csv");
                File.WriteAllLines(path, lines);
            }
        }
    }
}
