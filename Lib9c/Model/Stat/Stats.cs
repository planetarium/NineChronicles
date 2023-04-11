using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class Stats : IStats, ICloneable
    {
        protected readonly StatMap _statMap = new StatMap();

        public decimal HP => _statMap[StatType.HP].BaseValue;
        public decimal ATK => _statMap[StatType.ATK].BaseValue;
        public decimal DEF => _statMap[StatType.DEF].BaseValue;
        public decimal CRI => _statMap[StatType.CRI].BaseValue;
        public decimal HIT => _statMap[StatType.HIT].BaseValue;
        public decimal SPD => _statMap[StatType.SPD].BaseValue;
        public decimal DRV => _statMap[StatType.DRV].BaseValue;
        public decimal DRR => _statMap[StatType.DRR].BaseValue;
        public decimal CDMG => _statMap[StatType.CDMG].BaseValue;
        public decimal ArmorPenetration => _statMap[StatType.ArmorPenetration].BaseValue;
        public decimal DamageReflection => _statMap[StatType.DamageReflection].BaseValue;
        
        public Stats()
        {
        }

        public Stats(Stats value)
        {
            _statMap = new StatMap(value._statMap);
        }

        public void Reset()
        {
            _statMap.Reset();
        }

        public void Set(params Stats[] statsArray)
        {
            foreach (var stat in _statMap.GetStats())
            {
                var sum = statsArray.Sum(s => s.GetStat(stat.StatType));
                stat.SetBaseValue(sum);
            }
        }

        public void Set(StatsMap value)
        {
            foreach (var stat in _statMap.GetStats())
            {
                var sum = value.GetStat(stat.StatType);
                stat.SetBaseValue(sum);
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
                _statMap[statModifier.StatType].AddBaseValue(result);
            }
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

        public IEnumerable<(StatType statType, decimal value)> GetStats(bool ignoreZero = false)
        {
            return _statMap.GetStats(ignoreZero);
        }

        public virtual object Clone()
        {
            return new Stats(this);
        }
    }
}
