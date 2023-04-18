using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    public interface IStats
    {
        int HP { get; }
        int ATK { get; }
        int DEF { get; }
        int CRI { get; }
        int HIT { get; }
        int SPD { get; }
        int DRV { get; }
        int DRR { get; }
        int CDMG { get; }
        int ArmorPenetration { get; }
        int DamageReflection { get; }

        IEnumerable<(StatType statType, int value)> GetStats(bool ignoreZero = false);
    }
}
