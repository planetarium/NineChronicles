using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

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

                if (!Directory.Exists(L10nManager.CsvFilesRootDirectoryPath))
                {
                    Directory.CreateDirectory(L10nManager.CsvFilesRootDirectoryPath);
                }

                var path = Path.Combine(L10nManager.CsvFilesRootDirectoryPath, $"{oldCsvAsset.name}.csv");
                File.WriteAllLines(path, lines);
            }
        }

        // NOTE: 진행 중입니다.
        public static void ReplaceEndOfLineCharacters()
        {
            var directoryInfo = new DirectoryInfo(L10nManager.CsvFilesRootDirectoryPath);
            if (!directoryInfo.Exists)
            {
                return;
            }

            foreach (var fileInfo in directoryInfo.EnumerateFiles("*.csv"))
            {
                //
            }
        }

        [MenuItem("Tools/L10n/Download Simplified Chinese 8105 Unicode Range")]
        public static void DownloadSimplifiedChinese8105UnicodeRange()
        {
            var uri = new Uri("http://hanzidb.org/TGSCC-Unicode.txt");
            Debug.Log($"Start to downloading simplified chinese unicode range file from \"{uri}\".");

            var request = UnityWebRequest.Get(uri);
            var requestOperation = request.SendWebRequest();
            requestOperation.completed += asyncOperation =>
            {
                var text = request.downloadHandler.text;
                var lines = text
                    .Split(new[] {"\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                    .Skip(2)
                    .Select(line =>
                    {
                        var begin = line.IndexOf("U+", StringComparison.Ordinal) + 2;
                        return line.Substring(begin, 4);
                    })
                    .ToList();

                var counts = new[] {3500, 3000, 1605};
                for (var i = 0; i < counts.Length; i++)
                {
                    var targetLines = lines
                        .Skip(i == 0 ? 0 : counts[i - 1])
                        .Take(counts[i]);
                    var joined = string.Join(",", targetLines).Trim(',');

                    var directoryPath = Path.Combine(Application.dataPath, "Resources/Font/CharacterFiles");
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    var filePath = Path.Combine(
                        directoryPath,
                        $"simplified-chinese-8105-unicode-range-{i + 1:00}-{counts[i]:0000}.txt");
                    File.WriteAllText(filePath, joined);

                    Debug.Log($"Complete to downloading simplified chinese unicode range file to \"{filePath}\".");
                }

                request.Dispose();
            };
        }
    }
}
