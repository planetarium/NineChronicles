// #define TEST_LOG

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.L10n
{
    public static class L10nManager
    {
        public enum State
        {
            None,
            InInitializing,
            Initialized,
            InLanguageChanging,
        }

        public const string SettingsAssetPathInResources = "L10nSettings/L10nSettings";

        public static readonly string CsvFilesRootDirectoryPath =
            Path.Combine(Application.streamingAssetsPath, "Localization");

        private static IReadOnlyDictionary<string, string> _dictionary =
            new Dictionary<string, string>();

        public static State CurrentState { get; private set; } = State.None;

        private static Dictionary<string, Dictionary<LanguageType, string>> _additionalDic = new Dictionary<string, Dictionary<LanguageType, string>>();
        private static Dictionary<string, bool> _initializedURLs = new Dictionary<string, bool>();

        #region Language

        public static LanguageType CurrentLanguage { get; private set; } = SystemLanguage;

        private static LanguageType SystemLanguage
        {
            get
            {
                switch (Application.systemLanguage)
                {
                    case UnityEngine.SystemLanguage.Chinese:
                    case UnityEngine.SystemLanguage.ChineseSimplified:
                    case UnityEngine.SystemLanguage.ChineseTraditional:
                        return LanguageType.ChineseSimplified;
                    case UnityEngine.SystemLanguage.Portuguese:
                        return LanguageType.Portuguese;
                }

                var systemLang = Application.systemLanguage.ToString();
                return !Enum.TryParse<LanguageType>(systemLang, out var languageType)
                    ? default
                    : languageType;
            }
        }

        #endregion

        #region Settings

        private static L10nSettings _settings;

        private static LanguageTypeSettings? _currentLanguageTypeSettingsCache;

        public static LanguageTypeSettings CurrentLanguageTypeSettings
        {
            get
            {
                if (!_currentLanguageTypeSettingsCache.HasValue ||
                    _currentLanguageTypeSettingsCache.Value.languageType != CurrentLanguage)
                {
                    if (_settings.FontAssets.Count(asset => asset.languageType.Equals(CurrentLanguage)) > 0)
                    {
                        _currentLanguageTypeSettingsCache = _settings.FontAssets.First(asset =>
                            asset.languageType.Equals(CurrentLanguage));
                    }
                    else
                    {
                        _currentLanguageTypeSettingsCache = _settings.FontAssets.First();
                    }
                }

                return _currentLanguageTypeSettingsCache.Value;
            }
        }

        #endregion

        #region Event

        private static readonly ISubject<LanguageType> OnInitializeSubject =
            new Subject<LanguageType>();

        private static readonly ISubject<LanguageType> OnLanguageChangeSubject =
            new Subject<LanguageType>();

        public static IObservable<LanguageType> OnInitialize => OnInitializeSubject;

        public static IObservable<LanguageType> OnLanguageChange => OnLanguageChangeSubject;

        public static IObservable<LanguageTypeSettings> OnLanguageTypeSettingsChange =>
            OnLanguageChange.Select(_ => CurrentLanguageTypeSettings);

        #endregion

        #region Control

        public static IObservable<LanguageType> Initialize()
        {
            return Initialize(CurrentLanguage);
        }

        public static IObservable<LanguageType> Initialize(LanguageType languageType)
        {
#if TEST_LOG
            Debug.Log($"{nameof(L10nManager)}.{nameof(Initialize)}() called.");
#endif
            switch (CurrentState)
            {
                case State.InInitializing:
                    Debug.LogWarning($"[{nameof(L10nManager)}] Already in initializing now.");
                    return OnInitialize;
                case State.InLanguageChanging:
                    Debug.LogWarning(
                        $"[{nameof(L10nManager)}] Already initialized and in changing language now.");
                    return OnLanguageChange;
                case State.Initialized:
                    Debug.LogWarning(
                        $"[{nameof(L10nManager)}] Already initialized as {CurrentLanguage}.");
                    return Observable.Empty(CurrentLanguage);
            }

            CurrentState = State.InInitializing;
            InitializeInternal(languageType);

            return CurrentState == State.Initialized
                ? Observable.Empty(CurrentLanguage)
                : OnInitialize;
        }

        private static void InitializeInternal(LanguageType languageType)
        {
            if (languageType == LanguageType.Russian)
            {
                languageType = LanguageType.English;
            }

            _dictionary = GetDictionary(languageType);
            CurrentLanguage = languageType;
            _settings = Resources.Load<L10nSettings>(SettingsAssetPathInResources);
            CurrentState = State.Initialized;
            OnInitializeSubject.OnNext(CurrentLanguage);
        }

        public static IObservable<LanguageType> SetLanguage(LanguageType languageType)
        {
#if TEST_LOG
            Debug.Log($"{nameof(L10nManager)}.{nameof(SetLanguage)}({languageType}) called.");
#endif
            if (languageType == CurrentLanguage)
            {
                return Observable.Empty(CurrentLanguage);
            }

            switch (CurrentState)
            {
                case State.None:
                case State.InInitializing:
                    var subject = new Subject<LanguageType>();
                    subject.OnError(new L10nNotInitializedException());
                    return subject;
                case State.InLanguageChanging:
                    Debug.LogWarning($"[{nameof(L10nManager)}] Already in changing language now.");
                    return OnLanguageChange;
            }

            CurrentState = State.InLanguageChanging;
            SetLanguageInternal(languageType);

            return CurrentState == State.Initialized
                ? Observable.Empty(CurrentLanguage)
                : OnLanguageChange;
        }

        private static void SetLanguageInternal(LanguageType languageType)
        {
            if (languageType == LanguageType.Russian)
            {
                languageType = LanguageType.English;
            }

            _dictionary = GetDictionary(languageType);
            CurrentLanguage = languageType;
            CurrentState = State.Initialized;
            OnLanguageChangeSubject.OnNext(CurrentLanguage);
        }

        #endregion

        /// <summary>
        /// Get records manually using getField() with names without using Convert CSV rows into class objects that not support in IL2CPP.
        /// </summary>
        /// <param name="csvReader"></param>
        /// <returns></returns>
        public static List<L10nCsvModel> GetL10nCsvModelRecords(CsvReader csvReader)
        {
            var records = new List<L10nCsvModel>();
            csvReader.Read();
            csvReader.ReadHeader();
            while (csvReader.Read())
            {
                var record = new L10nCsvModel
                {
                    Key = csvReader.GetField<string>("Key"),
                    English = csvReader.GetField<string>("English"),
                    Korean = csvReader.GetField<string>("Korean"),
                    Portuguese = csvReader.GetField<string>("Portuguese"),
                    Japanese = csvReader.GetField<string>("Japanese"),
                    Spanish = csvReader.GetField<string>("Spanish"),
                    Thai = csvReader.GetField<string>("Thai"),
                    Indonesian = csvReader.GetField<string>("Indonesian"),
                    Russian = csvReader.GetField<string>("Russian"),
                    ChineseSimplified = csvReader.GetField<string>("ChineseSimplified"),
                    ChineseTraditional = csvReader.GetField<string>("ChineseTraditional"),
                    Tagalog = csvReader.GetField<string>("Tagalog"),
                };
                records.Add(record);
            }

            return records;
        }

        public static IReadOnlyDictionary<string, string> GetDictionary(LanguageType languageType)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            {
                WWW directory = new WWW(CsvFilesRootDirectoryPath + "/DirectoryForAndroid.txt");
                while (!directory.isDone)
                {
                    // wait for load
                }

                String[] fileNames = directory.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                foreach (String fileName in fileNames)
                {
                    String fullName = CsvFilesRootDirectoryPath + "/" + fileName;
                    WWW csvFile = new WWW(fullName);
                    while (!csvFile.isDone)
                    {
                        // wait for load
                    }

                    using var streamReader = new StreamReader(
                        new MemoryStream(csvFile.bytes),
                        System.Text.Encoding.Default);
                    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        PrepareHeaderForMatch = args => args.Header.ToLower(),
                    };
                    using var csvReader = new CsvReader(streamReader, csvConfig);
#if ENABLE_IL2CPP
                    var records = GetL10nCsvModelRecords(csvReader);
#else
                    var records = csvReader.GetRecords<L10nCsvModel>();
#endif
                    var recordsIndex = 0;
                    try
                    {
                        foreach (var record in records)
                        {
#if TEST_LOG
                        Debug.Log($"{fileName}: {recordsIndex}");
#endif
                            var key = record.Key;
                            if (string.IsNullOrEmpty(key))
                            {
                                recordsIndex++;
                                continue;
                            }

                            var value = (string)typeof(L10nCsvModel)
                                .GetProperty(languageType.ToString())?
                                .GetValue(record);

                            if (string.IsNullOrEmpty(value))
                            {
                                value = record.English;
                            }

                            if (dictionary.ContainsKey(key))
                            {
                                throw new L10nAlreadyContainsKeyException(
                                    $"key: {key}, recordsIndex: {recordsIndex}, csvFileInfo: {fullName}");
                            }

                            dictionary.Add(key, value);
                            recordsIndex++;
                        }
                    }
                    catch (CsvHelper.MissingFieldException e)
                    {
                        Debug.LogWarning($"`{fileName}` file has empty field.\n{e}");
                    }
                }

                return dictionary;
            }
#else
            {
                if (!Directory.Exists(CsvFilesRootDirectoryPath))
                {
                    throw new DirectoryNotFoundException(CsvFilesRootDirectoryPath);
                }

                var dictionary = new Dictionary<string, string>();
                var csvFileInfos = new DirectoryInfo(CsvFilesRootDirectoryPath).GetFiles("*.csv");
                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLower(),
                };
                foreach (var csvFileInfo in csvFileInfos)
                {
                    using (var streamReader = new StreamReader(csvFileInfo.FullName))
                    using (var csvReader = new CsvReader(streamReader, csvConfig))
                    {
                        var records = csvReader.GetRecords<L10nCsvModel>();
                        var recordsIndex = 0;
                        try
                        {
                            foreach (var record in records)
                            {
#if TEST_LOG
                        Debug.Log($"{csvFileInfo.Name}: {recordsIndex}");
#endif
                                var key = record.Key;
                                if (string.IsNullOrEmpty(key))
                                {
                                    recordsIndex++;
                                    continue;
                                }

                                var value = (string)typeof(L10nCsvModel)
                                    .GetProperty(languageType.ToString())?
                                    .GetValue(record);

                                if (string.IsNullOrEmpty(value))
                                {
                                    value = record.English;
                                }

                                if (dictionary.ContainsKey(key))
                                {
                                    Debug.LogError($"L10n duplication Key  key: {key}, recordsIndex: {recordsIndex}, csvFileInfo: {csvFileInfo.FullName}");
                                    dictionary[key] = value;
                                }
                                else
                                {
                                    dictionary.Add(key, value);
                                }

                                recordsIndex++;
                            }
                        }
                        catch (CsvHelper.MissingFieldException e)
                        {
                            Debug.LogWarning($"`{csvFileInfo.Name}` file has empty field.\n{e}");
                        }
                    }
                }

                return dictionary;
            }
#endif
        }

#region Localize

        public static string Localize(string key)
        {
            TryLocalize(key, out var text);
            return text;
        }

        public static string Localize(string key, params object[] args)
        {
            return TryLocalize(key, out var text)
                ? string.Format(text, args)
                : text;
        }

        public static bool ContainsKey(string key) => _dictionary.ContainsKey(key);

        // ReSharper disable Unity.PerformanceAnalysis
        private static bool TryLocalize(string key, out string text)
        {
            try
            {
                ValidateStateAndKey(key);
                text = _dictionary[key];
                return true;
            }
            catch (Exception e)
            {
                if (GetAdditionalLocalizedString(key, out var localized))
                {
                    text = localized;
                    return true;
                }
                Debug.LogError($"{e.GetType().FullName}: {e.Message} key: {key}");
                text = $"!{key}!";
                return false;
            }
        }

        public static string LocalizeCharacterName(int characterId)
        {
            var key = $"CHARACTER_NAME_{characterId}";
            return Localize(key);
        }

        public static string LocalizeItemName(int itemId)
        {
            var key = $"ITEM_NAME_{itemId}";
            return Localize(key);
        }

        public static string LocalizeRuneName(int id)
        {
            var key = $"RUNE_NAME_{id}";
            return Localize(key);
        }

        public static string LocalizePetName(int id)
        {
            var key = $"PET_NAME_{id}";
            return Localize(key);
        }

        public static int LocalizedCount(string key)
        {
            ValidateStateAndKey(key);

            var count = 0;
            while (true)
            {
                if (!_dictionary.ContainsKey($"{key}{count}"))
                {
                    return count;
                }

                count++;
            }
        }

        public static Dictionary<string, string> LocalizePattern(string pattern)
        {
            ValidateStateAndKey(pattern);

            return _dictionary.Where(pair => Regex
                    .IsMatch(pair.Key, pattern))
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value);
        }

        public static string LocalizeWorldName(int worldId)
        {
            return worldId switch
            {
                1 => Localize("WORLD_NAME_YGGDRASIL"),
                2 => Localize("WORLD_NAME_ALFHEIM"),
                3 => Localize("WORLD_NAME_SVARTALFHEIM"),
                4 => Localize("WORLD_NAME_ASGARD"),
                5 => Localize("WORLD_NAME_MUSPELHEIM"),
                6 => Localize("WORLD_NAME_JOTUNHEIM"),
                7 => Localize("WORLD_NAME_NIFLHEIM"),
                10001 => Localize("WORLD_NAME_MIMISBRUNNR"),
                _ => throw new ArgumentOutOfRangeException(nameof(worldId), worldId, "invalid world ID")
            };
        }

        public static string LocalizeCurrencyName(string ticker)
        {
            return TryLocalize($"UI_{ticker}", out var text) ? text : ticker;
        }

        #endregion

        private static void ValidateStateAndKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (CurrentState != State.Initialized)
            {
                throw new L10nNotInitializedException();
            }
        }

        public static bool TryGetFontMaterial(FontMaterialType fontMaterialType, out Material material)
        {
            if (fontMaterialType == FontMaterialType.Default)
            {
                material = CurrentLanguageTypeSettings.fontAssetData.FontAsset.material;
                return true;
            }

            foreach (var data in CurrentLanguageTypeSettings.fontAssetData.FontMaterialDataList)
            {
                if (fontMaterialType != data.type)
                {
                    continue;
                }

                material = data.material;
                return true;
            }

            material = default;
            return false;
        }

        public static async UniTask AdditionalL10nTableDownload(string url)
        {
            var client = new HttpClient();
            if(_initializedURLs.TryGetValue(url, out bool initialized))
            {
                return;
            }

            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var data = await resp.Content.ReadAsByteArrayAsync();
            using var streamReader = new StreamReader(new MemoryStream(data), System.Text.Encoding.Default);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };
            using var csvReader = new CsvReader(streamReader, csvConfig);
            var records = csvReader.GetRecords<L10nCsvModel>();
            foreach (var item in records)
            {
                Dictionary<LanguageType, string> l10nKeyValue = new Dictionary<LanguageType, string>();
                foreach (var lang in (LanguageType[])Enum.GetValues(typeof(LanguageType)))
                {
                    var value = (string)typeof(L10nCsvModel)
                                .GetProperty(lang.ToString())?
                                .GetValue(item);

                    if (string.IsNullOrEmpty(value))
                    {
                        value = item.English;
                    }
                    l10nKeyValue.Add(lang, value);
                }
                _additionalDic.TryAdd(item.Key, l10nKeyValue);
            }

            _initializedURLs.Add(url, true);
        }

        private static bool GetAdditionalLocalizedString(string key, out string text)
        {
            if(_additionalDic.TryGetValue(key, out var L10n))
            {
                if(L10n.TryGetValue(CurrentLanguage, out var localized))
                {
                    text = localized;
                    return true;
                }
                else
                {
                    text = string.Empty;
                    Debug.LogError($"_additionalDic can't find value: {key} {CurrentLanguage}");
                    return false;
                }
            }
            text = string.Empty;
            Debug.LogError($"_additionalDic can't find key: {key}");
            return false;
        }
    }
}
