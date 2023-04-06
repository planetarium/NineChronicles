#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class Currencies
    {
        public static readonly Currency NCG = Currency.Legacy(
            "NCG",
            2,
            new Address("0x47D082a115c63E7b58B1532d20E631538eaFADde"));

        public static readonly Currency Crystal = Currency.Legacy(
            "CRYSTAL",
            18,
            minters: null);

        public static readonly Currency StakeRune = Currency.Legacy(
            "RUNE_GOLDENLEAF",
            0,
            minters: null);

        public static readonly Currency DailyRewardRune = Currency.Legacy(
            "RUNE_ADVENTURER",
            0,
            minters: null);

        public static Currency GetRune(string? ticker) =>
            string.IsNullOrEmpty(ticker)
                ? throw new ArgumentNullException(nameof(ticker))
                : Currency.Legacy(
                    ticker,
                    0,
                    minters: null);

        public static IOrderedEnumerable<Currency> GetRunes(params string?[] tickers) =>
            tickers.Select(GetRune).OrderBy(rune => rune.Hash.GetHashCode());

        public static IOrderedEnumerable<Currency> GetRunes(RuneSheet? sheet) =>
            sheet?.OrderedList is null
                ? throw new ArgumentNullException(
                    nameof(sheet),
                    "sheet or sheet.OrderedList is null.")
                : sheet.OrderedList
                    .Select(row => row.Ticker)
                    .Select(GetRune)
                    .OrderBy(rune => rune.Hash.GetHashCode());

        public static Currency GetSoulStone(string? ticker) =>
            string.IsNullOrEmpty(ticker)
                ? throw new ArgumentNullException(nameof(ticker))
                : Currency.Legacy(
                    ticker,
                    0,
                    minters: null);

        public static IOrderedEnumerable<Currency> GetSoulStones(params string?[] tickers) =>
            tickers.Select(GetSoulStone).OrderBy(soulStone => soulStone.Hash.GetHashCode());

        public static IOrderedEnumerable<Currency> GetSoulStones(PetSheet? sheet) =>
            sheet?.OrderedList is null
                ? throw new ArgumentNullException(
                    nameof(sheet),
                    "sheet or sheet.OrderedList is null.")
                : sheet.OrderedList
                    .Select(row => row.SoulStoneTicker)
                    .Select(GetSoulStone)
                    .OrderBy(soulStone => soulStone.Hash.GetHashCode());
    }
}
