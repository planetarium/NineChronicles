using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class Stats : IStats, ICloneable
    {
        protected readonly StatMap _statMap = new StatMap();

        public int HP => _statMap[StatType.HP].BaseValue;
        public int ATK => _statMap[StatType.ATK].BaseValue;
        public int DEF => _statMap[StatType.DEF].BaseValue;
        public int CRI => _statMap[StatType.CRI].BaseValue;
        public int HIT => _statMap[StatType.HIT].BaseValue;
        public int SPD => _statMap[StatType.SPD].BaseValue;
        public int DRV => _statMap[StatType.DRV].BaseValue;
        public int DRR => _statMap[StatType.DRR].BaseValue;
        public int CDMG => _statMap[StatType.CDMG].BaseValue;
        public int ArmorPenetration => _statMap[StatType.ArmorPenetration].BaseValue;
        public int Thorn => _statMap[StatType.Thorn].BaseValue;
        
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

        public int GetStat(StatType statType)
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
