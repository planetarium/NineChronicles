using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using UnityEngine;

namespace Nekoyume.EnumType
{
    public enum ElementalType
    {
        Normal,
        Fire,
        Water,
        Land,
        Wind,
    }

    public class ElementalTypeComparer : IEqualityComparer<ElementalType>
    {
        public static readonly ElementalTypeComparer Instance = new ElementalTypeComparer();

        public bool Equals(ElementalType x, ElementalType y)
        {
            return x == y;
        }

        public int GetHashCode(ElementalType obj)
        {
            return (int) obj;
        }
    }

    public static class ElementalTypeExtension
    {
        private const string FireIconResourcePath = "UI/Textures/icon_elemental_fire";
        private const string WaterIconResourcePath = "UI/Textures/icon_elemental_water";
        private const string LandIconResourcePath = "UI/Textures/icon_elemental_land";
        private const string WindIconResourcePath = "UI/Textures/icon_elemental_wind";

        private const float Multiplier = .5f;

        private static readonly Dictionary<ElementalType, Dictionary<StatType, List<string>>> GetOptionsCache =
            new Dictionary<ElementalType, Dictionary<StatType, List<string>>>(ElementalTypeComparer.Instance);

        public static string GetLocalizedString(this ElementalType value)
        {
            return LocalizationManager.Localize($"ELEMENTAL_TYPE_{value.ToString().ToUpper()}");
        }

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
                    var format = LocalizationManager.Localize("ELEMENTAL_TYPE_OPTION_ATK_WIN_FORMAT");
                    list.Add(string.Format(format, lose.GetLocalizedString(), Multiplier));
                }

                if (from.TryGetLoseCase(out var win))
                {
                    var format = LocalizationManager.Localize("ELEMENTAL_TYPE_OPTION_ATK_LOSE_FORMAT");
                    list.Add(string.Format(format, win.GetLocalizedString(), Multiplier));
                }
            }
            else
            {
                if (from.TryGetWinCase(out var lose))
                {
                    var format = LocalizationManager.Localize("ELEMENTAL_TYPE_OPTION_DEF_WIN_FORMAT");
                    list.Add(string.Format(format, lose.GetLocalizedString(), Multiplier));
                }

                if (from.TryGetLoseCase(out var win))
                {
                    var format = LocalizationManager.Localize("ELEMENTAL_TYPE_OPTION_DEF_LOSE_FORMAT");
                    list.Add(string.Format(format, win.GetLocalizedString(), Multiplier));
                }
            }

            return list;
        }

        public static Sprite GetSprite(this ElementalType type)
        {
            switch (type)
            {
                case ElementalType.Normal:
                    return null;
                case ElementalType.Fire:
                    return Resources.Load<Sprite>(FireIconResourcePath);
                case ElementalType.Water:
                    return Resources.Load<Sprite>(WaterIconResourcePath);
                case ElementalType.Land:
                    return Resources.Load<Sprite>(LandIconResourcePath);
                case ElementalType.Wind:
                    return Resources.Load<Sprite>(WindIconResourcePath);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static bool TryGetWinCase(this ElementalType win, out ElementalType lose)
        {
            switch (win)
            {
                case ElementalType.Normal:
                    lose = ElementalType.Normal;
                    return false;
                case ElementalType.Fire:
                    lose = ElementalType.Wind;
                    return true;
                case ElementalType.Water:
                    lose = ElementalType.Fire;
                    return true;
                case ElementalType.Land:
                    lose = ElementalType.Water;
                    return true;
                case ElementalType.Wind:
                    lose = ElementalType.Land;
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(win), win, null);
            }
        }

        public static bool TryGetLoseCase(this ElementalType lose, out ElementalType win)
        {
            switch (lose)
            {
                case ElementalType.Normal:
                    win = ElementalType.Normal;
                    return false;
                case ElementalType.Fire:
                    win = ElementalType.Water;
                    return true;
                case ElementalType.Water:
                    win = ElementalType.Land;
                    return true;
                case ElementalType.Land:
                    win = ElementalType.Wind;
                    return true;
                case ElementalType.Wind:
                    win = ElementalType.Fire;
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lose), lose, null);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>
        /// 1: Win
        /// 0: Draw
        /// -1: Lose
        /// </returns>
        public static int GetBattleResult(this ElementalType from, ElementalType to)
        {
            if (from == ElementalType.Normal)
                return 0;

            if (from == to)
                return 0;

            if (from.TryGetWinCase(out var lose) &&
                lose == to)
                return 1;

            if (from.TryGetLoseCase(out var win) &&
                win == to)
                return -1;

            return 0;
        }

        public static int GetDamage(this ElementalType from, ElementalType to, int damage)
        {
            var battleResult = from.GetBattleResult(to);
            return Convert.ToInt32(damage * (1 + battleResult * Multiplier));
        }
    }
}
