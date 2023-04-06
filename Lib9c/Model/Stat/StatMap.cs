using Bencodex.Types;
using Nekoyume.Model.State;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nekoyume.Model.Stat
{
    public class StatMap : IState
    {
        public DecimalStat this[StatType type]
        {
            get => _statMap[type];
        }

        public decimal HP => _statMap[StatType.HP].TotalValue;
        public decimal ATK => _statMap[StatType.ATK].TotalValue;
        public decimal DEF => _statMap[StatType.DEF].TotalValue;
        public decimal CRI => _statMap[StatType.CRI].TotalValue;
        public decimal HIT => _statMap[StatType.HIT].TotalValue;
        public decimal SPD => _statMap[StatType.SPD].TotalValue;
        public decimal DRV => _statMap[StatType.DRV].TotalValue;
        public decimal DRR => _statMap[StatType.DRR].TotalValue;
        public decimal CDMG => _statMap[StatType.CDMG].TotalValue;

        public decimal BaseHP => _statMap[StatType.HP].BaseValue;
        public decimal BaseATK => _statMap[StatType.ATK].BaseValue;
        public decimal BaseDEF => _statMap[StatType.DEF].BaseValue;
        public decimal BaseCRI => _statMap[StatType.CRI].BaseValue;
        public decimal BaseHIT => _statMap[StatType.HIT].BaseValue;
        public decimal BaseSPD => _statMap[StatType.SPD].BaseValue;
        public decimal BaseDRV => _statMap[StatType.DRV].BaseValue;
        public decimal BaseDRR => _statMap[StatType.DRR].BaseValue;
        public decimal BaseCDMG => _statMap[StatType.CDMG].BaseValue;

        public decimal AdditionalHP => _statMap[StatType.HP].AdditionalValue;
        public decimal AdditionalATK => _statMap[StatType.ATK].AdditionalValue;
        public decimal AdditionalDEF => _statMap[StatType.DEF].AdditionalValue;
        public decimal AdditionalCRI => _statMap[StatType.CRI].AdditionalValue;
        public decimal AdditionalHIT => _statMap[StatType.HIT].AdditionalValue;
        public decimal AdditionalSPD => _statMap[StatType.SPD].AdditionalValue;
        public decimal AdditionalDRV => _statMap[StatType.DRV].AdditionalValue;
        public decimal AdditionalDRR => _statMap[StatType.DRR].AdditionalValue;
        public decimal AdditionalCDMG => _statMap[StatType.CDMG].AdditionalValue;

        private readonly ImmutableDictionary<StatType, DecimalStat> _statMap;

        public StatMap()
        {
            var dict = new Dictionary<StatType, DecimalStat>(StatTypeComparer.Instance)
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

            _statMap = dict.ToImmutableDictionary();
        }

        public void Reset()
        {
            foreach (var property in _statMap.Values)
            {
                property.Reset();
            }
        }

        public decimal GetStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.TotalValue;
        }

        public decimal GetBaseStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.BaseValue;
        }

        public decimal GetAdditionalStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.AdditionalValue;
        }


        public IEnumerable<(StatType statType, decimal value)> GetStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap)
            {
                if (ignoreZero)
                {
                    if (stat.HasTotalValue)
                    {
                        yield return (statType, stat.TotalValue);
                    }
                }
                else
                {
                    yield return (statType, stat.TotalValue);
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
                        yield return (statType, stat.BaseValue);
                    }
                }
                else
                {
                    yield return (statType, stat.BaseValue);
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
                        yield return (statType, stat.BaseValue, stat.AdditionalValue);
                    }
                }
                else
                {
                    yield return (statType, stat.BaseValue, stat.AdditionalValue);
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
            {
                _statMap[StatTypeExtension.Deserialize((Binary)kv.Key)]
                    .Deserialize((Dictionary)kv.Value);
            }
#pragma warning restore LAA1002
        }

    }
}
