using Bencodex.Types;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class StatMap : IStats, IBaseAndAdditionalStats, IState
    {
        public DecimalStat this[StatType type]
        {
            get => _statMap[type];
        }

        public int HP => _statMap[StatType.HP].TotalValueAsInt;
        public int ATK => _statMap[StatType.ATK].TotalValueAsInt;
        public int DEF => _statMap[StatType.DEF].TotalValueAsInt;
        public int CRI => _statMap[StatType.CRI].TotalValueAsInt;
        public int HIT => _statMap[StatType.HIT].TotalValueAsInt;
        public int SPD => _statMap[StatType.SPD].TotalValueAsInt;
        public int DRV => _statMap[StatType.DRV].TotalValueAsInt;
        public int DRR => _statMap[StatType.DRR].TotalValueAsInt;
        public int CDMG => _statMap[StatType.CDMG].TotalValueAsInt;
        public int ArmorPenetration => _statMap[StatType.ArmorPenetration].TotalValueAsInt;
        public int Thorn => _statMap[StatType.Thorn].TotalValueAsInt;

        public int BaseHP => _statMap[StatType.HP].BaseValue;
        public int BaseATK => _statMap[StatType.ATK].BaseValue;
        public int BaseDEF => _statMap[StatType.DEF].BaseValue;
        public int BaseCRI => _statMap[StatType.CRI].BaseValue;
        public int BaseHIT => _statMap[StatType.HIT].BaseValue;
        public int BaseSPD => _statMap[StatType.SPD].BaseValue;
        public int BaseDRV => _statMap[StatType.DRV].BaseValue;
        public int BaseDRR => _statMap[StatType.DRR].BaseValue;
        public int BaseCDMG => _statMap[StatType.CDMG].BaseValue;
        public int BaseArmorPenetration => _statMap[StatType.ArmorPenetration].BaseValue;
        public int BaseThorn => _statMap[StatType.Thorn].BaseValue;

        public int AdditionalHP => _statMap[StatType.HP].AdditionalValue;
        public int AdditionalATK => _statMap[StatType.ATK].AdditionalValue;
        public int AdditionalDEF => _statMap[StatType.DEF].AdditionalValue;
        public int AdditionalCRI => _statMap[StatType.CRI].AdditionalValue;
        public int AdditionalHIT => _statMap[StatType.HIT].AdditionalValue;
        public int AdditionalSPD => _statMap[StatType.SPD].AdditionalValue;
        public int AdditionalDRV => _statMap[StatType.DRV].AdditionalValue;
        public int AdditionalDRR => _statMap[StatType.DRR].AdditionalValue;
        public int AdditionalCDMG => _statMap[StatType.CDMG].AdditionalValue;
        public int AdditionalArmorPenetration => _statMap[StatType.ArmorPenetration].AdditionalValue;
        public int AdditionalThorn => _statMap[StatType.Thorn].AdditionalValue;

        private readonly Dictionary<StatType, DecimalStat> _statMap;

        public StatMap()
        {
            _statMap = new Dictionary<StatType, DecimalStat>(StatTypeComparer.Instance)
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
                { StatType.ArmorPenetration, new DecimalStat(StatType.ArmorPenetration) },
                { StatType.Thorn, new DecimalStat(StatType.Thorn) },
            };
        }

        public StatMap(StatMap statMap)
        {
            _statMap = new Dictionary<StatType, DecimalStat>();
            foreach (var stat in statMap.GetStats())
            {
                _statMap.Add(stat.StatType, (DecimalStat)stat.Clone());
            }
        }

        public void Reset()
        {
            foreach (var property in _statMap.Values)
            {
                property.Reset();
            }
        }

        public int GetStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.TotalValueAsInt;
        }

        public int GetBaseStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.BaseValue;
        }

        public int GetAdditionalStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.AdditionalValue;
        }

        public IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasTotalValue)
                    {
                        yield return (statType, stat.TotalValueAsInt);
                    }
                }
                else
                {
                    yield return (statType, stat.TotalValueAsInt);
                }
            }
        }

        public IEnumerable<(StatType statType, int baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
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

        public IEnumerable<(StatType statType, int additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
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

        public IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
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
            foreach (var stat in _statMap.OrderBy(x => x.Key))
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
