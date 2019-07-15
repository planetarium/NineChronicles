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
        public enum LanguageType
        {
            English,
            Korean
        }

        /// <summary>
        /// Fired when localization changed.
        /// </summary>
        public static event Action LocalizationChanged = delegate { };

        public const LanguageType DefaultLanguage = LanguageType.English; 
        private static readonly Dictionary<LanguageType, Dictionary<string, string>> Dictionary =
            new Dictionary<LanguageType, Dictionary<string, string>>();

        private static LanguageType _language = DefaultLanguage;

        /// <summary>
        /// Get or set language.
        /// </summary>
        public static LanguageType Language
        {
            get => _language;
            set
            {
                _language = value;
                LocalizationChanged();
            }
        }

        public static LanguageType SystemLanguage
        {
            get
            {
                var systemLang = Application.systemLanguage.ToString();
                return !Enum.TryParse<LanguageType>(systemLang, out var languageType)
                    ? DefaultLanguage
                    : languageType;
            }
        }

        /// <summary>
        /// Read localization spreadsheets.
        /// </summary>
        public static void Read(string path = "Localization")
        {
            if (Dictionary.Count > 0) return;

            ReadInternal();
            var languageType = SystemLanguage;
            Language = Dictionary.ContainsKey(languageType)
                ? languageType
                : DefaultLanguage;
        }
        
        public static void Read(LanguageType languageType, string path = "Localization")
        {
            if (Dictionary.Count > 0) return;

            ReadInternal();
            Language = Dictionary.ContainsKey(languageType)
                ? languageType
                : DefaultLanguage;
        }
        
        private static void ReadInternal(string path = "Localization")
        {
            if (Dictionary.Count > 0) return;

            var textAssets = Resources.LoadAll<TextAsset>(path);

            foreach (var textAsset in textAssets)
            {
                var text = ReplaceMarkers(textAsset.text);
                var matches = Regex.Matches(text, "\"[\\s\\S]+?\"");

                foreach (Match match in matches)
                {
                    text = text.Replace(match.Value,
                        match.Value.Replace("\"", null).Replace(",", "[comma]").Replace("\n", "[newline]"));
                }

                // csv파일 저장형식이 라인피드로만 처리되고 있어서 윈도우에서 줄바꿈이 제대로 안되는 문제가 있음
                var lines = text.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries);
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
        }

        /// <summary>
        /// Get localized value by localization key.
        /// </summary>
        public static string Localize(string localizationKey)
        {
            if (Dictionary.Count == 0)
            {
                Read();
            }

            if (!Dictionary.ContainsKey(Language)) throw new KeyNotFoundException("Language not found: " + Language);
            if (!Dictionary[Language].ContainsKey(localizationKey))
                throw new KeyNotFoundException("Translation not found: " + localizationKey);

            return Dictionary[Language][localizationKey];
        }

        /// <summary>
        /// Get localized value by localization key.
        /// </summary>
        public static string Localize(string localizationKey, params object[] args)
        {
            var pattern = Localize(localizationKey);

            return string.Format(pattern, args);
        }

        private static string ReplaceMarkers(string text)
        {
            return text.Replace("[Newline]", "\n");
        }

        /// <summary>
        /// Get localized string count by localization key with numbering.
        /// </summary>
        public static int LocalizedCount(string localizationKey)
        {
            if (Dictionary.Count == 0)
            {
                Read();
            }

            if (!Dictionary.ContainsKey(Language)) throw new KeyNotFoundException("Language not found: " + Language);

            var count = 0;
            while (true)
            {
                if (!Dictionary[Language].ContainsKey($"{localizationKey}{count}"))
                {
                    return count;
                }

                count++;
            }
        }
    }
}
