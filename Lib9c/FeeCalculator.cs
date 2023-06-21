using Lib9c;
using Libplanet.Action;
using Libplanet.Assets;

namespace Nekoyume
{
    public class FeeCalculator : IFeeCalculator
    {
        public FungibleAssetValue CalculateFee(IAction action)
        {
            return 1 * Currencies.Mead;
        }
    }
}
