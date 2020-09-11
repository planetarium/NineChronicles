#define TEST_LOG

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using UniRx;
using UnityEngine;

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

        public static readonly string SettingsAssetPathInResources = "L10nSettings";

        public static readonly string CsvFilesRootDirectoryPath =
            Path.Combine(Application.streamingAssetsPath, "Localization");

        private static IReadOnlyDictionary<string, string> _dictionary =
            new Dictionary<string, string>();

        private static L10nSettings _settings;

        public static State CurrentState { get; private set; } = State.None;

        #region Language

        public static LanguageType CurrentLanguage { get; private set; } = SystemLanguage;

        private static LanguageType SystemLanguage
        {
            get
            {
                var systemLang = Application.systemLanguage.ToString();
                return !Enum.TryParse<LanguageType>(systemLang, out var languageType)
                    ? default
                    : languageType;
            }
        }

        #endregion

        #region LanguageType Settings

        public static LanguageTypeSettings CurrentLanguageTypeSettings => _settings.FontAssets
            .First(asset => asset.languageType.Equals(CurrentLanguage));

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
            _dictionary = GetDictionary(languageType);
            CurrentLanguage = languageType;
            CurrentState = State.Initialized;
            OnLanguageChangeSubject.OnNext(CurrentLanguage);
        }

        #endregion

        private static IReadOnlyDictionary<string, string> GetDictionary(LanguageType languageType)
        {
            if (!Directory.Exists(CsvFilesRootDirectoryPath))
            {
                throw new DirectoryNotFoundException(CsvFilesRootDirectoryPath);
            }

            var dictionary = new Dictionary<string, string>();
            var csvFileInfos = new DirectoryInfo(CsvFilesRootDirectoryPath).GetFiles("*.csv");
            foreach (var csvFileInfo in csvFileInfos)
            {
                using (var streamReader = new StreamReader(csvFileInfo.FullName))
                using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
                {
                    csvReader.Configuration.PrepareHeaderForMatch =
                        (header, index) => header.ToLower();
                    var records = csvReader.GetRecords<L10nCsvModel>();
                    foreach (var record in records)
                    {
                        var key = record.Key;
                        string value;
                        switch (languageType)
                        {
                            default:
                            case LanguageType.English:
                                value = record.English;
                                break;
                            case LanguageType.Korean:
                                value = record.Korean;
                                break;
                            case LanguageType.Portuguese:
                                value = record.Portuguese;
                                break;
                        }

                        if (string.IsNullOrEmpty(value))
                        {
                            value = record.English;
                        }

                        if (dictionary.ContainsKey(key))
                        {
                            throw new L10nAlreadyContainsKeyException(key);
                        }

                        dictionary.Add(key, value);
                    }
                }
            }

            return dictionary;
        }

        #region Localize

        public static string Localize(string key)
        {
            ValidateStateAndKey(key);

            if (_dictionary.ContainsKey(key))
            {
                return _dictionary[key];
            }

            Debug.LogError($"Key not found: {key}");
            return $"!{key}!";
        }

        public static string Localize(string key, params object[] args)
        {
            return TryLocalize(key, out var text)
                ? string.Format(text, args)
                : text;
        }

        public static bool TryLocalize(string key, out string text)
        {
            ValidateStateAndKey(key);

            if (_dictionary.ContainsKey(key))
            {
                text = _dictionary[key];
                return true;
            }

            text = string.Empty;
            return false;
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
    }
}
