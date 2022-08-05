using Libplanet.Assets;

namespace Nekoyume
{
    public static class FungibleAssetValueExtensions
    {
        public static string ToCurrencyNotation(this FungibleAssetValue value)
        {
            return value.MajorUnit.ToCurrencyNotation();
        }
    }
}
