using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    public interface IBaseAndAdditionalStats
    {
        decimal BaseHP { get; }
        decimal BaseATK { get; }
        decimal BaseDEF { get; }
        decimal BaseCRI { get; }
        decimal BaseHIT { get; }
        decimal BaseSPD { get; }
        decimal BaseDRV { get; }
        decimal BaseDRR { get; }
        decimal BaseCDMG { get; }
        decimal BaseArmorPenetration { get; }
        decimal BaseDamageReflection { get; }

        decimal AdditionalHP { get; }
        decimal AdditionalATK { get; }
        decimal AdditionalDEF { get; }
        decimal AdditionalCRI { get; }
        decimal AdditionalHIT { get; }
        decimal AdditionalSPD { get; }
        decimal AdditionalDRV { get; }
        decimal AdditionalDRR { get; }
        decimal AdditionalCDMG { get; }
        decimal AdditionalArmorPenetration { get; }
        decimal AdditionalDamageReflection { get; }

        IEnumerable<(StatType statType, decimal baseValue)> GetBaseStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, decimal additionalValue)> GetAdditionalStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, decimal baseValue, decimal additionalValue)> GetBaseAndAdditionalStats(bool ignoreZero = false);
    }
}
