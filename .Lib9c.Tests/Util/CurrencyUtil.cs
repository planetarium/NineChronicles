namespace Lib9c.Tests.Util
{
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;

    public static class CurrencyUtil
    {
        public static IAccountStateDelta AddCurrency(
            IActionContext context,
            IAccountStateDelta state,
            Address agentAddress,
            Currency currency,
            FungibleAssetValue amount
        )
        {
            return state.MintAsset(context, agentAddress, amount);
        }
    }
}
