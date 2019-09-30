using Assets.SimpleLocalization;

namespace Nekoyume.EnumType
{
    public enum TradeType
    {
        Buy,
        Sell
    }

    public static class TradeTypeExtension
    {
        public static string GetLocalizedString(this TradeType value)
        {
            return LocalizationManager.Localize($"TRADE_TYPE_{value}");
        }
    }
}
