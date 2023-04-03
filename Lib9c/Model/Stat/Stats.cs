using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class Stats : IStats, ICloneable
    {
        protected readonly Dictionary<StatType, DecimalStat> _statMap
            = new Dictionary<StatType, DecimalStat>()
            {
                { StatType.HP, new DecimalStat(StatType.HP) },
                { StatType.ATK, new DecimalStat(StatType.ATK) },
                { StatType.DEF, new DecimalStat(StatType.DEF) },
                { StatType.CRI, new DecimalStat(StatType.CRI) },
                { StatType.HIT, new DecimalStat(StatType.HIT) },
                { StatType.SPD, new DecimalStat(StatType.SPD) },
                { StatType.DRV, new DecimalStat(StatType.DRV) },
                { StatType.DRR, new DecimalStat(StatType.DRR) },
                { StatType.CDMG, new DecimalStat(StatType.CDMG) },
            };

        public decimal HP => _statMap[StatType.HP].Value;
        public decimal ATK => _statMap[StatType.ATK].Value;
        public decimal DEF => _statMap[StatType.DEF].Value;
        public decimal CRI => _statMap[StatType.CRI].Value;
        public decimal HIT => _statMap[StatType.HP].Value;
        public decimal SPD => _statMap[StatType.SPD].Value;
        public decimal DRV => _statMap[StatType.DRV].Value;
        public decimal DRR => _statMap[StatType.DRR].Value;
        public decimal CDMG => _statMap[StatType.CDMG].Value;

        public Stats()
        {
        }

        public Stats(Stats value)
        {
            _statMap = value._statMap;
        }

        public void Reset()
        {
            foreach (var property in _statMap.Values)
            {
                property.Reset();
            }
        }

        public void Set(params Stats[] statsArray)
        {
            foreach (var (statType, stat) in _statMap)
            {
                var sum = statsArray.Sum(s => s.GetStat(statType));
                stat.SetValue(sum);
            }
        }

        public void Set(StatsMap value)
        {
            foreach (var (statType, stat) in _statMap)
            {
                var sum = value.GetStat(statType);
                stat.SetValue(sum);
            }
        }

        public void Set(IEnumerable<StatModifier> statModifiers, params Stats[] baseStats)
        {
            Reset();

            foreach (var statModifier in statModifiers)
            {
                var originalStatValue =
                    baseStats.Sum(stats => stats.GetStat(statModifier.StatType));
                var result = statModifier.GetModifiedValue(originalStatValue);
                _statMap[statModifier.StatType].AddValue(result);
            }
        }

        public decimal GetStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var stat))
            {
                throw new KeyNotFoundException($"[Stats] StatType {statType} is missing in statMap.");
            }

            return stat.Value;
        }

        /// <summary>
        /// Use this only for testing.
        /// </summary>
        /// <param name="statType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetStatForTest(StatType statType, decimal value)
        {
            if (!_statMap.ContainsKey(statType))
            {
                throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }

            _statMap[statType].SetValue(value);
        }

        public IEnumerable<(StatType statType, decimal value)> GetStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap)
            {
                if (ignoreZero)
                {
                    if (stat.HasValue)
                    {
                        yield return (statType, stat.Value);
                    }
                }
                else
                {
                    yield return (statType, stat.Value);
                }
            }
        }

        public virtual object Clone()
        {
            return new Stats(this);
        }
    }
}
