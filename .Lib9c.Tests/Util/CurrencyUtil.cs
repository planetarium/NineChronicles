namespace Lib9c.Tests.Util
{
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;

    public static class CurrencyUtil
    {
        public static IAccount AddCurrency(
            IActionContext context,
            IAccount state,
            Address agentAddress,
            Currency currency,
            FungibleAssetValue amount
        )
        {
            return state.MintAsset(context, agentAddress, amount);
        }
    }
}
