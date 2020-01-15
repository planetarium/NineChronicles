using Nekoyume.EnumType;
using System.Collections.Generic;

namespace Nekoyume.Game
{
    public interface IBaseAndAdditionalStats
    {
        int BaseHP { get; }
        int BaseATK { get; }
        int BaseDEF { get; }
        int BaseCRI { get; }
        int BaseDOG { get; }
        int BaseSPD { get; }
        
        bool HasBaseHP { get; }
        bool HasBaseATK { get; }
        bool HasBaseDEF { get; }
        bool HasBaseCRI { get; }
        bool HasBaseDOG { get; }
        bool HasBaseSPD { get; }
        
        int AdditionalHP { get; }
        int AdditionalATK { get; }
        int AdditionalDEF { get; }
        int AdditionalCRI { get; }
        int AdditionalDOG { get; }
        int AdditionalSPD { get; }
        
        bool HasAdditionalHP { get; }
        bool HasAdditionalATK { get; }
        bool HasAdditionalDEF { get; }
        bool HasAdditionalCRI { get; }
        bool HasAdditionalDOG { get; }
        bool HasAdditionalSPD { get; }
        bool HasAdditionalStats { get; }

        IEnumerable<(StatType statType, int baseValue)> GetBaseStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, int additionalValue)> GetAdditionalStats(bool ignoreZero = false);
        IEnumerable<(StatType statType, int baseValue, int additionalValue)> GetBaseAndAdditionalStats(bool ignoreZero = false);
    }
}
