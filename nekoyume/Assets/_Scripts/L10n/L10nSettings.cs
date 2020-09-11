using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.L10n
{
    [CreateAssetMenu(
        fileName = "L10nSettings",
        menuName = "ScriptableObjects/L10n/Spawn L10nSettings",
        order = 1)]
    public class L10nSettings : ScriptableObject
    {
        [SerializeField]
        private List<LanguageTypeSettings> fontAssets = null;

        public IEnumerable<LanguageTypeSettings> FontAssets => fontAssets;

        private L10nSettings()
        {
            fontAssets = new List<LanguageTypeSettings>();
            var languageType = typeof(LanguageType);
            var languageTypes = Enum
                .GetNames(languageType)
                .Select(languageTypeName =>
                    (LanguageType) Enum.Parse(languageType, languageTypeName))
                .ToList();
            for (var i = 0; i < languageTypes.Count; i++)
            {
                fontAssets.Add(new LanguageTypeSettings
                {
                    languageType = languageTypes[i]
                });
            }
        }
    }
}
