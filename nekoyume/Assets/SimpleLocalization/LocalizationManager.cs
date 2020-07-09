using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets.SimpleLocalization
{
    /// <summary>
    /// Localization manager.
    /// </summary>
    public static class LocalizationManager
    {
        /// <summary>
        /// UnityEngine.SystemLanguage 안에서 선택적으로 포함합니다.
        /// </summary>
        public enum LanguageType
        {
            English,
            Korean
        }

        private static bool _initialized = false;

        private static readonly Dictionary<LanguageType, Dictionary<string, string>> Dictionary =
            new Dictionary<LanguageType, Dictionary<string, string>>();

        private static LanguageType _currentLanguage = SystemLanguage;

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

        public static LanguageType CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                OnChangeLanguage?.Invoke();
            }
        }

        public static event Action OnChangeLanguage;

        public static void Initialize(string path = "Localization")
        {
            if (_initialized)
            {
                return;
            }

            var textAssets = Resources.LoadAll<TextAsset>(path);
            foreach (var textAsset in textAssets)
            {
                var text = ReplaceMarkers(textAsset.text);
                var matches = Regex.Matches(text, "\"[\\s\\S]+?\"");
                text = matches
                    .Cast<Match>()
                    .Aggregate(text, (current, match) => current
                        .Replace(match.Value, match.Value
                            .Replace("\"", null)
                            .Replace(",", "[comma]")
                            .Replace("\n", "[newline]")
                            .Replace("\r\n", "[newline]")));

                // csv파일 저장형식이 라인피드로만 처리되고 있어서 윈도우에서 줄바꿈이 제대로 안되는 문제가 있음
                var lines = text.Split(new[] {"\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                var languages = lines[0]
                    .Split(',')
                    .Skip(1)
                    .Select(value =>
                    {
                        value = value.Trim();
                        if (!Enum.TryParse<LanguageType>(value, out var languageType))
                        {
                            throw new InvalidCastException(value);
                        }

                        return languageType;
                    })
                    .ToList();

                foreach (var languageType in languages)
                {
                    if (!Dictionary.ContainsKey(languageType))
                    {
                        Dictionary.Add(languageType, new Dictionary<string, string>());
                    }
                }

                for (var i = 1; i < lines.Length; i++)
                {
                    var columns = lines[i]
                        .Split(',')
                        .Select(j => j
                            .Trim()
                            .Replace("[comma]", ",")
                            .Replace("[newline]", "\n"))
                        .ToList();
                    var key = columns[0];
                    for (var j = 0; j < languages.Count; j++)
                    {
                        Dictionary[languages[j]].Add(key, columns[j + 1]);
                    }
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// Get localized value by localization key.
        /// </summary>
        public static string Localize(string localizationKey)
        {
            if (string.IsNullOrEmpty(localizationKey))
            {
                throw new ArgumentNullException(nameof(localizationKey));
            }

            if (Dictionary.Count == 0)
            {
                Initialize();
            }

            if (!Dictionary.ContainsKey(CurrentLanguage))
            {
                throw new KeyNotFoundException("Language not found: " + CurrentLanguage);
            }

            try
            {
                return Dictionary[CurrentLanguage][localizationKey];
            }
            catch (KeyNotFoundException)
            {
                Debug.LogError($"Key not found: {localizationKey}");
                return $"!{localizationKey}!";
            }
        }

        public static string LocalizeCharacterName(int characterId)
        {
            var localizationKey = $"CHARACTER_NAME_{characterId}";
            return Localize(localizationKey);
        }

        public static string LocalizeItemName(int itemId)
        {
            var localizationKey = $"ITEM_NAME_{itemId}";
            return Localize(localizationKey);
        }

        /// <summary>
        /// Get localized value by localization key.
        /// </summary>
        public static string Localize(string localizationKey, params object[] args)
        {
            var pattern = Localize(localizationKey);

            return string.Format(pattern, args);
        }

        /// <summary>
        /// Get localized string count by localization key with numbering.
        /// </summary>
        public static int LocalizedCount(string localizationKey)
        {
            if (Dictionary.Count == 0)
            {
                Initialize();
            }

            if (!Dictionary.ContainsKey(CurrentLanguage))
            {
                throw new KeyNotFoundException("Language not found: " + CurrentLanguage);
            }

            // FixMe. 무한루프 가능성이 열려 있음.
            var count = 0;
            while (true)
            {
                if (!Dictionary[CurrentLanguage].ContainsKey($"{localizationKey}{count}"))
                {
                    return count;
                }

                count++;
            }
        }

        public static Dictionary<string, string> LocalizePattern(string pattern)
        {
            var result = new Dictionary<string, string>();

            if (Dictionary.Count == 0)
            {
                Initialize();
            }

            var dict = Dictionary[CurrentLanguage];
            foreach (var pair in dict)
            {
                if (Regex.IsMatch(pair.Key, pattern))
                {
                    result.Add(pair.Key, pair.Value);
                }
            }

            return result;
        }

        private static string ReplaceMarkers(string text)
        {
            return text
                .Replace("[Newline]", "[newline]")
                .Replace("[Comma]", "[comma]");
        }
    }
}
