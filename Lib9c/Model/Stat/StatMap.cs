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

        public int BaseHP => _statMap[StatType.HP].BaseValueAsInt;
        public int BaseATK => _statMap[StatType.ATK].BaseValueAsInt;
        public int BaseDEF => _statMap[StatType.DEF].BaseValueAsInt;
        public int BaseCRI => _statMap[StatType.CRI].BaseValueAsInt;
        public int BaseHIT => _statMap[StatType.HIT].BaseValueAsInt;
        public int BaseSPD => _statMap[StatType.SPD].BaseValueAsInt;
        public int BaseDRV => _statMap[StatType.DRV].BaseValueAsInt;
        public int BaseDRR => _statMap[StatType.DRR].BaseValueAsInt;
        public int BaseCDMG => _statMap[StatType.CDMG].BaseValueAsInt;
        public int BaseArmorPenetration => _statMap[StatType.ArmorPenetration].BaseValueAsInt;
        public int BaseThorn => _statMap[StatType.Thorn].BaseValueAsInt;

        public int AdditionalHP => _statMap[StatType.HP].AdditionalValueAsInt;
        public int AdditionalATK => _statMap[StatType.ATK].AdditionalValueAsInt;
        public int AdditionalDEF => _statMap[StatType.DEF].AdditionalValueAsInt;
        public int AdditionalCRI => _statMap[StatType.CRI].AdditionalValueAsInt;
        public int AdditionalHIT => _statMap[StatType.HIT].AdditionalValueAsInt;
        public int AdditionalSPD => _statMap[StatType.SPD].AdditionalValueAsInt;
        public int AdditionalDRV => _statMap[StatType.DRV].AdditionalValueAsInt;
        public int AdditionalDRR => _statMap[StatType.DRR].AdditionalValueAsInt;
        public int AdditionalCDMG => _statMap[StatType.CDMG].AdditionalValueAsInt;
        public int AdditionalArmorPenetration => _statMap[StatType.ArmorPenetration].AdditionalValueAsInt;
        public int AdditionalThorn => _statMap[StatType.Thorn].AdditionalValueAsInt;

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
                { StatType.ArmorPenetration, new DecimalStat(StatType.ArmorPenetration) },
                { StatType.Thorn, new DecimalStat(StatType.Thorn) },
            };

        public StatMap()
        {
        }

        public StatMap(StatMap statMap)
        {
            foreach (var stat in statMap.GetDecimalStats(false))
            {
                _statMap[stat.StatType] = (DecimalStat)stat.Clone();
            }
        }

        public void Reset()
        {
            foreach (var property in _statMap.Values)
            {
                property.Reset();
            }
        }

        public int GetStatAsInt(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.TotalValueAsInt;
        }

        public decimal GetStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.TotalValue;
        }

        public int GetBaseStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.BaseValueAsInt;
        }

        public int GetAdditionalStat(StatType statType)
        {
            if (!_statMap.TryGetValue(statType, out var decimalStat))
            {
                throw new KeyNotFoundException($"StatType {statType} is missing in statMap.");
            }

            return decimalStat.AdditionalValueAsInt;
        }

        public IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasTotalValueAsInt)
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
                    if (stat.HasBaseValueAsInt)
                    {
                        yield return (statType, stat.BaseValueAsInt);
                    }
                }
                else
                {
                    yield return (statType, stat.BaseValueAsInt);
                }
            }
        }

        public IEnumerable<(StatType statType, int additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            foreach (var (statType, stat) in _statMap.OrderBy(x => x.Key))
            {
                if (ignoreZero)
                {
                    if (stat.HasAdditionalValueAsInt)
                    {
                        yield return (statType, stat.AdditionalValueAsInt);
                    }
                }
                else
                {
                    yield return (statType, stat.AdditionalValueAsInt);
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
                    if (stat.HasBaseValueAsInt || stat.HasAdditionalValueAsInt)
                    {
                        yield return (statType, stat.BaseValueAsInt, stat.AdditionalValueAsInt);
                    }
                }
                else
                {
                    yield return (statType, stat.BaseValueAsInt, stat.AdditionalValueAsInt);
                }
            }
        }

        public IEnumerable<DecimalStat> GetDecimalStats(bool ignoreZero)
        {
            var values = _statMap.OrderBy(x => x.Key).Select(x => x.Value);
            return ignoreZero ?
                values.Where(x => x.HasBaseValueAsInt || x.HasAdditionalValueAsInt) :
                values;
        }

        public IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(
                _statMap
                    .Where(x => x.Value.HasBaseValue || x.Value.HasAdditionalValue)
                    .Select(kv =>
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
