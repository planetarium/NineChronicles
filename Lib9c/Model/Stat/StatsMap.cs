using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Stat
{
    [Serializable]
    public class StatsMap : IStats, IBaseAndAdditionalStats, IState
    {
        public int HP => GetStatAsInt(StatType.HP);
        public int ATK => GetStatAsInt(StatType.ATK);
        public int DEF => GetStatAsInt(StatType.DEF);
        public int CRI => GetStatAsInt(StatType.CRI);
        public int HIT => GetStatAsInt(StatType.HIT);
        public int SPD => GetStatAsInt(StatType.SPD);
        public int DRV => GetStatAsInt(StatType.DRV);
        public int DRR => GetStatAsInt(StatType.DRR);
        public int CDMG => GetStatAsInt(StatType.CDMG);
        public int ArmorPenetration => GetStatAsInt(StatType.ArmorPenetration);
        public int Thorn => GetStatAsInt(StatType.Thorn);

        public int BaseHP => GetBaseStat(StatType.HP);
        public int BaseATK => GetBaseStat(StatType.ATK);
        public int BaseDEF => GetBaseStat(StatType.DEF);
        public int BaseCRI => GetBaseStat(StatType.CRI);
        public int BaseHIT => GetBaseStat(StatType.HIT);
        public int BaseSPD => GetBaseStat(StatType.SPD);
        public int BaseDRV => GetBaseStat(StatType.DRV);
        public int BaseDRR => GetBaseStat(StatType.DRR);
        public int BaseCDMG => GetBaseStat(StatType.CDMG);
        public int BaseArmorPenetration => GetBaseStat(StatType.ArmorPenetration);
        public int BaseThorn => GetBaseStat(StatType.Thorn);

        public int AdditionalHP => GetAdditionalStat(StatType.HP);
        public int AdditionalATK => GetAdditionalStat(StatType.ATK);
        public int AdditionalDEF => GetAdditionalStat(StatType.DEF);
        public int AdditionalCRI => GetAdditionalStat(StatType.CRI);
        public int AdditionalHIT => GetAdditionalStat(StatType.HIT);
        public int AdditionalSPD => GetAdditionalStat(StatType.SPD);
        public int AdditionalDRV => GetAdditionalStat(StatType.DRV);
        public int AdditionalDRR => GetAdditionalStat(StatType.DRR);
        public int AdditionalCDMG => GetAdditionalStat(StatType.CDMG);
        public int AdditionalArmorPenetration => GetAdditionalStat(StatType.ArmorPenetration);
        public int AdditionalThorn => GetAdditionalStat(StatType.Thorn);

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

        public int GetBaseStat(StatType statType)
        {
            return _statMap.GetBaseStat(statType);
        }

        public int GetAdditionalStat(StatType statType)
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

        public IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = false)
        {
            return _statMap.GetStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, int baseValue)> GetBaseStats(bool ignoreZero = false)
        {
            return _statMap.GetBaseStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, int additionalValue)> GetAdditionalStats(bool ignoreZero = false)
        {
            return _statMap.GetAdditionalStats(ignoreZero);
        }

        public IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(
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
