using System;
using System.Collections.Generic;
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
        public decimal ArmorPenetration => GetStat(StatType.ArmorPenetration);
        public decimal DamageReflection => GetStat(StatType.DamageReflection);

        public decimal BaseHP => GetBaseStat(StatType.HP);
        public decimal BaseATK => GetBaseStat(StatType.ATK);
        public decimal BaseDEF => GetBaseStat(StatType.DEF);
        public decimal BaseCRI => GetBaseStat(StatType.CRI);
        public decimal BaseHIT => GetBaseStat(StatType.HIT);
        public decimal BaseSPD => GetBaseStat(StatType.SPD);
        public decimal BaseDRV => GetBaseStat(StatType.DRV);
        public decimal BaseDRR => GetBaseStat(StatType.DRR);
        public decimal BaseCDMG => GetBaseStat(StatType.CDMG);
        public decimal BaseArmorPenetration => GetBaseStat(StatType.ArmorPenetration);
        public decimal BaseDamageReflection => GetBaseStat(StatType.DamageReflection);

        public decimal AdditionalHP => GetAdditionalStat(StatType.HP);
        public decimal AdditionalATK => GetAdditionalStat(StatType.ATK);
        public decimal AdditionalDEF => GetAdditionalStat(StatType.DEF);
        public decimal AdditionalCRI => GetAdditionalStat(StatType.CRI);
        public decimal AdditionalHIT => GetAdditionalStat(StatType.HIT);
        public decimal AdditionalSPD => GetAdditionalStat(StatType.SPD);
        public decimal AdditionalDRV => GetAdditionalStat(StatType.DRV);
        public decimal AdditionalDRR => GetAdditionalStat(StatType.DRR);
        public decimal AdditionalCDMG => GetAdditionalStat(StatType.CDMG);
        public decimal AdditionalArmorPenetration => GetAdditionalStat(StatType.ArmorPenetration);
        public decimal AdditionalDamageReflection => GetAdditionalStat(StatType.DamageReflection);

        private readonly StatMap _statMap = new StatMap();

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

        public int GetStatAsInt(StatType statType)
        {
            return _statMap.GetStatAsInt(statType);
        }

        public decimal GetStat(StatType statType)
        {
            return _statMap.GetStat(statType);
        }

        public decimal GetBaseStat(StatType statType)
        {
            return _statMap.GetBaseStat(statType);
        }

        public decimal GetAdditionalStat(StatType statType)
        {
            return _statMap.GetAdditionalStat(statType);
        }

        public void AddStatValue(StatType key, decimal value)
        {
            _statMap[key].AddBaseValue(value);
        }

        public void AddStatAdditionalValue(StatType key, decimal additionalValue)
        {
            _statMap[key].AddAdditionalValue(additionalValue);
        }

        public void AddStatAdditionalValue(StatModifier statModifier)
        {
            AddStatAdditionalValue(statModifier.StatType, statModifier.Value);
        }

        public void SetStatAdditionalValue(StatType key, decimal additionalValue)
        {
            _statMap[key].SetAdditionalValue(additionalValue);
        }

        public IValue Serialize() => _statMap.Serialize();

        public void Deserialize(Dictionary serialized) => _statMap.Deserialize(serialized);

        public IEnumerable<(StatType statType, decimal value)> GetStats(bool ignoreZero = false)
        {
            return _statMap.GetStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, decimal baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            return _statMap.GetBaseStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, decimal additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            return _statMap.GetAdditionalStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, decimal baseValue, decimal additionalValue)> GetBaseAndAdditionalStats(
            bool ignoreZero = false)
        {
            return _statMap.GetBaseAndAdditionalStats(ignoreZero);
        }

        public IEnumerable<DecimalStat> GetDecimalStats()
        {
            return _statMap.GetStats();
        }
    }
}
