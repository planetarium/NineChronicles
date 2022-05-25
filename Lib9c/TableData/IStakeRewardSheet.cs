using System.Collections.Generic;

namespace Nekoyume.TableData
{
    public interface IStakeRewardSheet : ISheet
    {
        IReadOnlyList<IStakeRewardRow> OrderedRows { get; }
    }
}
