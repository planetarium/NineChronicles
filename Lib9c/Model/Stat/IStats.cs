using System.Collections.Generic;

namespace Nekoyume.Model.Stat
{
    public interface IStats
    {
        decimal HP { get; }
        decimal ATK { get; }
        decimal DEF { get; }
        decimal CRI { get; }
        decimal HIT { get; }
        decimal SPD { get; }
        decimal DRV { get; }
        decimal DRR { get; }
        decimal CDMG { get; }
        decimal ArmorPenetration { get; }
        decimal DamageReflection { get; }

        IEnumerable<(StatType statType, decimal value)> GetStats(bool ignoreZero = false);
    }
}
