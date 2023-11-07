using System;
using System.Collections.Generic;
using Nekoyume.AssetBundleHelper;
using Nekoyume.L10n;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Stat;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume
{
    static class ElementalTypeUIExtensions
    {
        private const string IconResourceBundle = "UI/Icons/ElementalType";
        private const string FireIconResourcePath = "icon_elemental_fire";
        private const string WaterIconResourcePath = "icon_elemental_water";
        private const string LandIconResourcePath = "icon_elemental_land";
        private const string WindIconResourcePath = "icon_elemental_wind";
        private const string NormalIconResourcePath = "icon_element_normal";

        private static readonly Dictionary<ElementalType, Dictionary<StatType, List<string>>> GetOptionsCache =
            new Dictionary<ElementalType, Dictionary<StatType, List<string>>>(ElementalTypeComparer.Instance);


        public static IEnumerable<string> GetOptions(this ElementalType from, StatType statType)
        {
            if (statType != StatType.ATK &&
                statType != StatType.DEF)
                return new List<string>();

            if (GetOptionsCache.ContainsKey(from) &&
                GetOptionsCache[from].ContainsKey(statType))
            {
                return GetOptionsCache[from][statType];
            }

            if (!GetOptionsCache.ContainsKey(from))
            {
                GetOptionsCache[from] = new Dictionary<StatType, List<string>>(StatTypeComparer.Instance);
            }

            var dict = GetOptionsCache[from];

            if (!dict.ContainsKey(statType))
            {
                dict[statType] = new List<string>();
            }

            var list = dict[statType];

            if (from == ElementalType.Normal)
                return list;

            if (statType == StatType.ATK)
            {
                if (from.TryGetWinCase(out var lose))
                {
                    var format = L10nManager.Localize("ELEMENTAL_TYPE_OPTION_ATK_WIN_FORMAT");
                    list.Add(string.Format(format, lose.GetLocalizedString(), ElementalTypeExtension.WinMultiplier - 1));
                }
            }
            else if (statType == StatType.DEF)
            {
                if (from.TryGetLoseCase(out var win))
                {
                    var format = L10nManager.Localize("ELEMENTAL_TYPE_OPTION_DEF_LOSE_FORMAT");
                    list.Add(string.Format(format, win.GetLocalizedString(), ElementalTypeExtension.WinMultiplier - 1));
                }
            }

            return list;
        }

        public static Sprite GetSprite(this ElementalType type)
        {
            switch (type)
            {
                case ElementalType.Normal:
                    return AssetBundleLoader.LoadAssetBundle<Sprite>(IconResourceBundle, NormalIconResourcePath);
                case ElementalType.Fire:
                    return AssetBundleLoader.LoadAssetBundle<Sprite>(IconResourceBundle, FireIconResourcePath);
                case ElementalType.Water:
                    return AssetBundleLoader.LoadAssetBundle<Sprite>(IconResourceBundle, WaterIconResourcePath);
                case ElementalType.Land:
                    return AssetBundleLoader.LoadAssetBundle<Sprite>(IconResourceBundle, LandIconResourcePath);
                case ElementalType.Wind:
                    return AssetBundleLoader.LoadAssetBundle<Sprite>(IconResourceBundle, WindIconResourcePath);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
