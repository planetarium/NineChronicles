using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    public interface IBaseAndAdditionalStats
    {
        int BaseHP { get; }
        int BaseATK { get; }
        int BaseDEF { get; }
        int BaseCRI { get; }
        int BaseHIT { get; }
        int BaseSPD { get; }
        int BaseDRV { get; }
        int BaseDRR { get; }
        int BaseCDMG { get; }
        int BaseArmorPenetration { get; }
        int BaseThorn { get; }

        int AdditionalHP { get; }
        int AdditionalATK { get; }
        int AdditionalDEF { get; }
        int AdditionalCRI { get; }
        int AdditionalHIT { get; }
        int AdditionalSPD { get; }
        int AdditionalDRV { get; }
        int AdditionalDRR { get; }
        int AdditionalCDMG { get; }
        int AdditionalArmorPenetration { get; }
        int AdditionalThorn { get; }

        IEnumerable<(StatType statType, int baseValue)> GetBaseStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, int additionalValue)> GetAdditionalStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(bool ignoreZero = false);
    }
}
