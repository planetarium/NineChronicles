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

        bool HasBaseHP { get; }
        bool HasBaseATK { get; }
        bool HasBaseDEF { get; }
        bool HasBaseCRI { get; }
        bool HasBaseHIT { get; }
        bool HasBaseSPD { get; }

        int AdditionalHP { get; }
        int AdditionalATK { get; }
        int AdditionalDEF { get; }
        int AdditionalCRI { get; }
        int AdditionalHIT { get; }
        int AdditionalSPD { get; }

        bool HasAdditionalHP { get; }
        bool HasAdditionalATK { get; }
        bool HasAdditionalDEF { get; }
        bool HasAdditionalCRI { get; }
        bool HasAdditionalHIT { get; }
        bool HasAdditionalSPD { get; }
        bool HasAdditionalStats { get; }

        IEnumerable<(StatType statType, int baseValue)> GetBaseStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, int additionalValue)> GetAdditionalStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(bool ignoreZero = false);
    }
}
