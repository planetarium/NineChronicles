using Libplanet.Action;

namespace Nekoyume.BlockChain
{
    public class ActionTypeLoaderContext : IActionTypeLoaderContext
    {
        public ActionTypeLoaderContext(long index)
        {
            Index = index;
        }

        public long Index { get; }
    }
}
