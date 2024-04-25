using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Action;
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

        public static bool IsTradable(this FungibleAssetValue value) =>
            !RegisterProduct.NonTradableTickerCurrencies.Contains(value.Currency);
    }
}
