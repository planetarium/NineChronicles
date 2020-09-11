using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.L10n
{
    [CreateAssetMenu(fileName = "L10nSettings", menuName = "ScriptableObjects/SpawnL10nSettings", order = 1)]
    public class L10nSettings : ScriptableObject
    {
        [SerializeField]
        private List<LanguageTypeSettings> fontAssets;

        public IReadOnlyList<LanguageTypeSettings> FontAssets => fontAssets;

        private void Reset()
        {
            fontAssets = new List<LanguageTypeSettings>();
            var languageType = typeof(LanguageType);
            var languageTypeNames = Enum.GetNames(languageType);
            for (var i = 0; i < languageTypeNames.Length; i++)
            {
                var languageTypeName = languageTypeNames[i];
                var type = (LanguageType) Enum.Parse(languageType, languageTypeName);
                fontAssets.Add(new LanguageTypeSettings
                {
                    languageType = type,
                });
            }
        }
    }
}
