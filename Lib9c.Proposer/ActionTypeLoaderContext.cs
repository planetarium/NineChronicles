using Libplanet.Action;

namespace Nekoyume.Blockchain
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
