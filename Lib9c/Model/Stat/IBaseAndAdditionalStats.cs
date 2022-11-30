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

        bool HasBaseHP { get; }
        bool HasBaseATK { get; }
        bool HasBaseDEF { get; }
        bool HasBaseCRI { get; }
        bool HasBaseHIT { get; }
        bool HasBaseSPD { get; }
        bool HasBaseDRV { get; }
        bool HasBaseDRR { get; }

        int AdditionalHP { get; }
        int AdditionalATK { get; }
        int AdditionalDEF { get; }
        int AdditionalCRI { get; }
        int AdditionalHIT { get; }
        int AdditionalSPD { get; }
        int AdditionalDRV { get; }
        int AdditionalDRR { get; }

        bool HasAdditionalHP { get; }
        bool HasAdditionalATK { get; }
        bool HasAdditionalDEF { get; }
        bool HasAdditionalCRI { get; }
        bool HasAdditionalHIT { get; }
        bool HasAdditionalSPD { get; }
        bool HasAdditionalDRV { get; }
        bool HasAdditionalDRR { get; }
        bool HasAdditionalStats { get; }

        IEnumerable<(StatType statType, int baseValue)> GetBaseStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, int additionalValue)> GetAdditionalStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(bool ignoreZero = false);
    }
}
