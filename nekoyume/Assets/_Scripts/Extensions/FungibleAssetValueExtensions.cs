using Libplanet.Types.Assets;
using Nekoyume.Helper;
using UnityEngine;

namespace Nekoyume
{
    public static class FungibleAssetValueExtensions
    {
        public static string ToCurrencyNotation(this FungibleAssetValue value) =>
            value.MajorUnit.ToCurrencyNotation();

        public static Sprite GetIconSprite(this FungibleAssetValue value) =>
            SpriteHelper.GetFavIcon(value.Currency.Ticker);
    }
}
