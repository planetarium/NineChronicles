using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class Stats : IStats, ICloneable
    {
        protected readonly StatMap _statMap;

        public int HP => _statMap[StatType.HP].BaseValueAsInt;
        public int ATK => _statMap[StatType.ATK].BaseValueAsInt;
        public int DEF => _statMap[StatType.DEF].BaseValueAsInt;
        public int CRI => _statMap[StatType.CRI].BaseValueAsInt;
        public int HIT => _statMap[StatType.HIT].BaseValueAsInt;
        public int SPD => _statMap[StatType.SPD].BaseValueAsInt;
        public int DRV => _statMap[StatType.DRV].BaseValueAsInt;
        public int DRR => _statMap[StatType.DRR].BaseValueAsInt;
        public int CDMG => _statMap[StatType.CDMG].BaseValueAsInt;
        public int ArmorPenetration => _statMap[StatType.ArmorPenetration].BaseValueAsInt;
        public int Thorn => _statMap[StatType.Thorn].BaseValueAsInt;

        protected readonly HashSet<StatType> LegacyDecimalStatTypes =
            new HashSet<StatType>{ StatType.CRI, StatType.HIT, StatType.SPD };
        
        public Stats()
        {
            _statMap = new StatMap();
        }

        public Stats(Stats value)
        {
            _statMap = new StatMap(value._statMap);
        }

        public void Reset()
        {
            _statMap.Reset();
        }

        public void Set(StatMap statMap, params Stats[] statsArray)
        {
            foreach (var stat in statMap.GetDecimalStats(false))
            {
                if (!LegacyDecimalStatTypes.Contains(stat.StatType))
                {
                    var sum = statsArray.Sum(s => s.GetStatAsInt(stat.StatType));
                    stat.SetBaseValue(sum);
                }
                else
                {
                    var sum = statsArray.Sum(s => s.GetStat(stat.StatType));
                    stat.SetBaseValue(sum);
                }
            }
        }

        public void Set(StatsMap value)
        {
            foreach (var stat in value.GetDecimalStats(true))
            {
                var statType = stat.StatType;
                var sum = value.GetStat(statType);
                _statMap[statType].SetBaseValue(sum);
            }
        }

        public void Modify(IEnumerable<StatModifier> statModifiers)
        {
            foreach (var statModifier in statModifiers)
            {
                var statType = statModifier.StatType;
                if (!LegacyDecimalStatTypes.Contains(statType))
                {
                    var originalStatValue = GetStatAsInt(statType);
                    var result = statModifier.GetModifiedValue(originalStatValue);
                    _statMap[statModifier.StatType].AddBaseValue(result);
                }
                else
                {
                    var originalStatValue = GetStat(statType);
                    var result = statModifier.GetModifiedValue(originalStatValue);
                    _statMap[statModifier.StatType].AddBaseValue(result);
                }
            }
        }

        public void Set(IEnumerable<StatModifier> statModifiers, params Stats[] baseStats)
        {
            Reset();

            foreach (var statModifier in statModifiers)
            {
                var statType = statModifier.StatType;
                if (!LegacyDecimalStatTypes.Contains(statType))
                {
                    var originalStatValue =
                        baseStats.Sum(stats => stats.GetStatAsInt(statType));
                    var result = statModifier.GetModifiedValue(originalStatValue);
                    _statMap[statModifier.StatType].AddBaseValue(result);
                }
                else
                {
                    var originalStatValue =
                        baseStats.Sum(stats => stats.GetStat(statType));
                    var result = statModifier.GetModifiedValue(originalStatValue);
                    _statMap[statModifier.StatType].AddBaseValue(result);
                }
            }
        }

        public int GetStatAsInt(StatType statType)
        {
            return _statMap.GetStatAsInt(statType);
        }

        public decimal GetStat(StatType statType)
        {
            return _statMap.GetStat(statType);
        }

        /// <summary>
        /// Use this only for testing.
        /// </summary>
        /// <param name="statType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetStatForTest(StatType statType, decimal value)
        {
            _statMap[statType].SetBaseValue(value);
        }

        public IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = false)
        {
            return _statMap.GetStats(ignoreZero);
        }

        public virtual object Clone()
        {
            return new Stats(this);
        }
    }
}
