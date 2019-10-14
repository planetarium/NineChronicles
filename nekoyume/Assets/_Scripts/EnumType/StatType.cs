using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;

namespace Nekoyume.EnumType
{
    public enum StatType
    {
        /// <summary>
        /// Health Point
        /// </summary>
        HP,

        /// <summary>
        /// Attack Power
        /// </summary>
        ATK,

        /// <summary>
        /// Defence
        /// </summary>
        DEF,

        /// <summary>
        /// Critical Chance
        /// </summary>
        CRI,
        
        /// <summary>
        /// Dodge
        /// </summary>
        DOG,
        
        /// <summary>
        /// Speed
        /// </summary>
        SPD
    }

    [Serializable]
    public class StatTypeComparer : IEqualityComparer<StatType>
    {
        public static readonly StatTypeComparer Instance = new StatTypeComparer();

        public bool Equals(StatType x, StatType y)
        {
            return x == y;
        }

        public int GetHashCode(StatType obj)
        {
            return (int) obj;
        }
    }

    public static class StatTypeExtension
    {
        public static string GetLocalizedString(this StatType value)
        {
            return LocalizationManager.Localize($"STAT_TYPE_{value}");
        }
    }
}
