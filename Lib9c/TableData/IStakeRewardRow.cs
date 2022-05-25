using System.Collections.Generic;
using System.Linq;
using Libplanet.Assets;
using Nekoyume.Action;

namespace Nekoyume.TableData
{
    public interface IStakeRewardRow
    {
        long RequiredGold { get; }
        int Level { get; }
    }
}
