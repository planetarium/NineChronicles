using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro.EditorUtilities;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.L10n.Editor
{
    public static class L10nManagerEditor
    {
        private const string OldCsvFilesRootPath = "Localization";
        private static readonly string CharacterFilesPath =
            Path.Combine(Application.dataPath, "Font/CharacterFiles");
        private const string GenerationSettingsPath = "L10nSettings/FontAssetGenerationSettings";
        private static readonly object EditorCoroutineObject = new object();

        // Depending on the computer environment, the corresponding time may change
        private const float WaitTimeForGenerateAtlas = 5f;
        private const float WaitTimeForGenerateSaveSDF = 1f;

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

        [MenuItem("Tools/L10n/Download Simplified Chinese 8105 Unicode Range")]
        public static void DownloadSimplifiedChinese8105UnicodeRange()
        {
            PrepareCharacterFilesDirectory();

            var uri = new Uri("http://hanzidb.org/TGSCC-Unicode.txt");
            NcDebug.Log($"Start to downloading simplified chinese unicode range file from \"{uri}\".");

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
                    var filePath = Path.Combine(
                        CharacterFilesPath,
                        $"simplified-chinese-8105-unicode-range-{i + 1:00}-{counts[i]:0000}.txt");
                    File.WriteAllText(filePath, joined);

                    NcDebug.Log($"Complete to downloading simplified chinese unicode range file to \"{filePath}\".");
                }

                request.Dispose();
            };
        }

        [MenuItem("Tools/L10n/Generate Unicode Hex Range Files")]
        public static async Task GenerateUnicodeHexRangeFiles()
        {
            PrepareCharacterFilesDirectory();

            var defaultCharacters = Enumerable.Range(32, 127 - 32)
                .Union(new List<int>
                {
                    // NOTE: 이빠진 곳에 넣을 네모입니다.
                    '\x25A1'
                })
                .Select(Convert.ToChar)
                .ToList();

            await L10nManager.AdditionalL10nTableDownload("https://assets.nine-chronicles.com/live-assets/Csv/RemoteCsv.csv", true);

            foreach (var languageType in Enum.GetValues(typeof(LanguageType)).OfType<LanguageType>())
            {
                var dict = L10nManager.GetDictionary(languageType);
                var charArray = dict.Values.SelectMany(value => value.ToCharArray()).ToList();

                var additionalDict = L10nManager.GetAdditionalDictionary(languageType);
                if (additionalDict != null)
                {
                    charArray.AddRange(additionalDict.Values.SelectMany(value => value.ToCharArray()));
                }
                
                var unicodeHexes = defaultCharacters
                    .Union(charArray.Select(char.ToLower))
                    .Union(charArray.Select(char.ToUpper))
                    .Distinct()
                    .Select(character =>
                        Convert.ToInt32(
                            Encoding.Unicode.GetString(BitConverter.GetBytes(character))[0]))
                    .OrderBy(characterNumber => characterNumber)
                    .Select(characterNumber => characterNumber.ToString("X4"))
                    .ToList();
                var unicodeHexesCount = unicodeHexes.Count;
                NcDebug.LogWarning($"{languageType} unicodeHexes count: {unicodeHexesCount}");
                var maxCharacterCountForEachFile = GetMaxCharacterCountForEachSDF(languageType);
                var fileIndex = 0;
                while (true)
                {
                    var characterCountForEachFile =
                        unicodeHexesCount - maxCharacterCountForEachFile * fileIndex;
                    if (characterCountForEachFile <= 0)
                    {
                        break;
                    }

                    characterCountForEachFile = Math.Min(
                        characterCountForEachFile,
                        maxCharacterCountForEachFile);
                    var filePath = Path.Combine(
                        CharacterFilesPath,
                        $"{languageType.ToString()}-unicode-hex-range-{fileIndex + 1:00}.txt");
                    var joined = string.Join(
                        ",",
                        unicodeHexes.GetRange(fileIndex, characterCountForEachFile));
                    File.WriteAllText(filePath, joined);

                    fileIndex++;
                }
            }
        }

        [MenuItem("Tools/L10n/Generate Font Asset Files at Once")]
        public static async void GenerateFontAssetFiles()
        {
            await GenerateUnicodeHexRangeFiles();
            EditorCoroutineUtility.StartCoroutine(CoGenerateFontAssetFile(), EditorCoroutineObject);
        }

        private static IEnumerator CoGenerateFontAssetFile()
        {
            var charactersPath = Path.Combine(Application.dataPath, "Font/CharacterFiles");
            var window = EditorWindow.GetWindow<TMPro_FontAssetCreatorWindow>();

            var languageTypes = Enum.GetValues(typeof(LanguageType)).OfType<LanguageType>();
            var settingsList = Resources.Load<FontAssetGenerationSettings>(GenerationSettingsPath).settings;
            foreach (var languageType in languageTypes)
            {
                NcDebug.Log($"-------------Generate Start : {languageType}-------------");
                var settings = settingsList[(int)languageType];

                var characterPath = Path.Combine(charactersPath,
                    $"{languageType.ToString()}-unicode-hex-range-{1:00}.txt");
                var unicodeHexes = File.ReadAllLines(characterPath);
                settings.characterSequence = unicodeHexes[0];

                var fontAssetFullPath = Path.GetFullPath(AssetDatabase.GetAssetPath(settings.referencedFontAsset))
                    .Replace("\\", "/");

                var generator = new FontAssetGenerator(window);
                generator.GenerateAtlas(settings);
                yield return new EditorWaitForSeconds(WaitTimeForGenerateAtlas);
                generator.SaveFontAssetToSDF(fontAssetFullPath);
                yield return new EditorWaitForSeconds(WaitTimeForGenerateSaveSDF);
                NcDebug.Log($"-------------Generate End : {languageType}-------------");
            }
        }

        private static void PrepareCharacterFilesDirectory()
        {
            if (!Directory.Exists(CharacterFilesPath))
            {
                Directory.CreateDirectory(CharacterFilesPath);
            }
        }

        /// <summary>
        /// 언어에 따라서 ttf나 otf 파일이 다를 수 있는데, 폰트 파일 차이에서 오는 문자 크기 등의 차이가 SDF 파일을
        /// 만드는데 영향을 줄 수 있습니다. 따라서 한 SDF 파일에 들어가는 문자 수를 언어 별로 설정해서 그 차이에서
        /// 발생하는 문제를 최소화 합니다.
        /// </summary>
        /// <param name="languageType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static int GetMaxCharacterCountForEachSDF(LanguageType languageType)
        {
            switch (languageType)
            {
                case LanguageType.English:
                case LanguageType.Korean:
                case LanguageType.Portuguese:
                case LanguageType.Japanese:
                case LanguageType.ChineseSimplified:
                case LanguageType.Thai:
                case LanguageType.Spanish:
                case LanguageType.Indonesian:
                case LanguageType.Russian:
                case LanguageType.ChineseTraditional:
                case LanguageType.Tagalog:
                case LanguageType.Vietnam:
                    return 2500;
                default:
                    throw new ArgumentOutOfRangeException(nameof(languageType), languageType, null);
            }
        }
    }
}
