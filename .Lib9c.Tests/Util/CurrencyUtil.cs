namespace Lib9c.Tests.Util
{
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;

    public static class CurrencyUtil
    {
        public static IAccountStateDelta AddCurrency(
            IAccountStateDelta state,
            Address agentAddress,
            Currency currency,
            FungibleAssetValue amount
        )
        {
            return state.MintAsset(agentAddress, amount);
        }
    }
}
