using System.Linq;
using Lib9c;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    /// <summary>
    /// Client side mirror of the market and synthesis restrictions the chain enforces.
    /// </summary>
    /// <remarks>
    /// Every rule here has two sources, read exactly the way <see cref="RegisterProduct"/> and
    /// <see cref="Nekoyume.Action.Synthesize"/> read them on chain: the hardcoded lists that
    /// predate the sheet, plus <see cref="RestrictionSheet"/>, which can only add to those lists.
    /// A sheet that is absent - never patched onto the chain, or not loaded yet - therefore leaves
    /// the hardcoded behaviour exactly as it was.
    /// <para>
    /// Keep this in step with the actions. The client only decides what to offer; the chain still
    /// decides what is allowed, and a client that offers more than the chain accepts turns into a
    /// silent action failure.
    /// </para>
    /// </remarks>
    public static class RestrictionHelper
    {
        /// <summary>
        /// The loaded sheet, or <c>null</c> before the table sheets exist.
        /// </summary>
        private static RestrictionSheet Sheet
        {
            get
            {
                var game = Game.Game.instance;
                return game == null ? null : game.TableSheets?.RestrictionSheet;
            }
        }

        /// <summary>
        /// Whether an item may be selected as a synthesis material.
        /// </summary>
        /// <param name="itemId">The item sheet id.</param>
        public static bool CanUseAsSynthesizeMaterial(int itemId)
        {
            if (Action.Synthesize.InvalidMaterialItemId.Contains(itemId))
            {
                return false;
            }

            return Sheet?.IsItemSynthesizeMaterial(itemId) ?? true;
        }

        /// <summary>
        /// Whether an item may be registered as a market product.
        /// </summary>
        /// <param name="itemBase">The item to register.</param>
        public static bool CanRegisterItem(ItemBase itemBase) =>
            itemBase is ITradableItem && IsItemRegistrableBySheet(itemBase.Id);

        /// <summary>
        /// Whether the sheet allows this item id on the market, leaving the item's own
        /// tradability to the caller.
        /// </summary>
        /// <param name="itemId">The item sheet id.</param>
        /// <remarks>
        /// For the few places that call an item tradable on grounds other than
        /// <see cref="ITradableItem"/>; everywhere else wants
        /// <see cref="CanRegisterItem(ItemBase)"/>.
        /// </remarks>
        public static bool IsItemRegistrableBySheet(int itemId) =>
            Sheet?.IsItemMarketRegistrable(itemId) ?? true;

        /// <summary>
        /// Whether a fungible asset may be registered as a market product.
        /// </summary>
        /// <param name="currency">The currency to register.</param>
        public static bool CanRegisterCurrency(Currency currency) =>
            CanRegisterTicker(currency.Ticker);

        /// <summary>
        /// Whether a fungible asset may be registered as a market product.
        /// </summary>
        /// <param name="ticker">The currency ticker, wrapped or not.</param>
        public static bool CanRegisterTicker(string ticker)
        {
            // Wrapped tickers are the same asset as the ticker they wrap, and an item currency is
            // never a market product, both as RegisterProduct.Register decides it.
            var unwrapped = Currencies.UnwrapTicker(ticker);
            if (Currencies.IsItemCurrencyTicker(unwrapped))
            {
                return false;
            }

            if (RegisterProduct.NonTradableTickerCurrencies.Any(
                currency => currency.Ticker == unwrapped))
            {
                return false;
            }

            return Sheet?.IsCurrencyMarketRegistrable(unwrapped) ?? true;
        }
    }
}
