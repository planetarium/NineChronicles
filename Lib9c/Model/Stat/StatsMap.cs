using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class StatsMap : IStats, IBaseAndAdditionalStats, IState
    {
        public decimal HP => GetStat(StatType.HP);
        public decimal ATK => GetStat(StatType.ATK);
        public decimal DEF => GetStat(StatType.DEF);
        public decimal CRI => GetStat(StatType.CRI);
        public decimal HIT => GetStat(StatType.HIT);
        public decimal SPD => GetStat(StatType.SPD);
        public decimal DRV => GetStat(StatType.DRV);
        public decimal DRR => GetStat(StatType.DRR);
        public decimal CDMG => GetStat(StatType.CDMG);

        public decimal BaseHP => GetBaseStat(StatType.HP);
        public decimal BaseATK => GetBaseStat(StatType.ATK);
        public decimal BaseDEF => GetBaseStat(StatType.DEF);
        public decimal BaseCRI => GetBaseStat(StatType.CRI);
        public decimal BaseHIT => GetBaseStat(StatType.HIT);
        public decimal BaseSPD => GetBaseStat(StatType.SPD);
        public decimal BaseDRV => GetBaseStat(StatType.DRV);
        public decimal BaseDRR => GetBaseStat(StatType.DRR);
        public decimal BaseCDMG => GetBaseStat(StatType.CDMG);

        public decimal AdditionalHP => GetAdditionalStat(StatType.HP);
        public decimal AdditionalATK => GetAdditionalStat(StatType.ATK);
        public decimal AdditionalDEF => GetAdditionalStat(StatType.DEF);
        public decimal AdditionalCRI => GetAdditionalStat(StatType.CRI);
        public decimal AdditionalHIT => GetAdditionalStat(StatType.HIT);
        public decimal AdditionalSPD => GetAdditionalStat(StatType.SPD);
        public decimal AdditionalDRV => GetAdditionalStat(StatType.DRV);
        public decimal AdditionalDRR => GetAdditionalStat(StatType.DRR);
        public decimal AdditionalCDMG => GetAdditionalStat(StatType.CDMG);

        private readonly Dictionary<StatType, DecimalStat> _statMap =
            new Dictionary<StatType, DecimalStat>(StatTypeComparer.Instance)
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

        protected bool Equals(StatsMap other)
        {
            return Equals(_statMap, other._statMap);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StatsMap)obj);
        }

        public override int GetHashCode()
        {
            return _statMap != null ? _statMap.GetHashCode() : 0;
        }

        public decimal GetStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"[StatsMap] StatType {statType} is missing in statMap.");
            }

            return decimalStat.TotalValue;
        }

        public decimal GetBaseStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"[StatsMap] StatType {statType} is missing in statMap.");
            }

            return decimalStat.Value;
        }

        public decimal GetAdditionalStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"[StatsMap] StatType {statType} is missing in statMap.");
            }

            return decimalStat.AdditionalValue;
        }

        public void AddStatValue(StatType key, decimal value)
        {
            if (!_statMap.ContainsKey(key))
            {
                throw new KeyNotFoundException($"[StatsMap] StatType {key} is missing in statMap.");
            }

            _statMap[key].AddValue(value);
            PostStatValueChanged(key);
        }

        public void AddStatAdditionalValue(StatType key, decimal additionalValue)
        {
            if (!_statMap.ContainsKey(key))
            {
                throw new KeyNotFoundException($"[StatsMap] StatType {key} is missing in statMap.");
            }

            _statMap[key].AddAdditionalValue(additionalValue);
            PostStatValueChanged(key);
        }

        public void AddStatAdditionalValue(StatModifier statModifier)
        {
            AddStatAdditionalValue(statModifier.StatType, statModifier.Value);
        }

        public void SetStatAdditionalValue(StatType key, decimal additionalValue)
        {
            if (!_statMap.ContainsKey(key))
            {
                throw new KeyNotFoundException($"[StatsMap] StatType {key} is missing in statMap.");
            }

            _statMap[key].SetAdditionalValue(additionalValue);
            PostStatValueChanged(key);
        }

        private void PostStatValueChanged(StatType key)
        {
            if (!_statMap.ContainsKey(key))
            {
                throw new KeyNotFoundException($"[StatsMap] StatType {key} is missing in statMap.");
            }

            var statMap = _statMap[key];
            if (statMap.HasValue)
                return;

            _statMap.Remove(key);
        }

        public IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(
                _statMap.Select(kv =>
                    new KeyValuePair<IKey, IValue>(
                        kv.Key.Serialize(),
                        kv.Value.Serialize()
                    )
                )
            );
#pragma warning restore LAA1002

        public void Deserialize(Dictionary serialized)
        {
#pragma warning disable LAA1002
            foreach (KeyValuePair<IKey, IValue> kv in serialized)
#pragma warning restore LAA1002
            {
                _statMap[StatTypeExtension.Deserialize((Binary)kv.Key)] =
                    new DecimalStat((Dictionary)kv.Value);
            }
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

        public IEnumerable<(StatType statType, decimal baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap)
            {
                if (ignoreZero)
                {
                    if (stat.HasBaseValue)
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

        public IEnumerable<(StatType statType, decimal additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap)
            {
                if (ignoreZero)
                {
                    if (stat.HasAdditionalValue)
                    {
                        yield return (statType, stat.AdditionalValue);
                    }
                }
                else
                {
                    yield return (statType, stat.AdditionalValue);
                }
            }
        }

        public IEnumerable<(StatType statType, decimal baseValue, decimal additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap)
            {
                if (ignoreZero)
                {
                    if (stat.HasBaseValue || stat.HasAdditionalValue)
                    {
                        yield return (statType, stat.Value, stat.AdditionalValue);
                    }
                }
                else
                {
                    yield return (statType, stat.Value, stat.AdditionalValue);
                }
            }
        }

        public IEnumerable<DecimalStat> GetStats()
        {
            foreach (var stat in _statMap)
            {
                yield return stat.Value;
            }
        }
    }
}
