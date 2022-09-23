using System.Collections.Generic;

namespace Nekoyume.TableData
{
    public interface IWorldBossRewardSheet : ISheet
    {
        IReadOnlyList<IWorldBossRewardRow> OrderedRows { get; }
    }
}
