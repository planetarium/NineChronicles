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
            long amount
        )
        {
            return state.MintAsset(agentAddress, currency * amount);
        }
    }
}
