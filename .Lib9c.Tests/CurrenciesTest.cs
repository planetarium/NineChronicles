namespace Lib9c.Tests
{
#nullable enable

    using System;
    using System.Linq;
    using Nekoyume.TableData;
    using Xunit;

    public class CurrenciesTest
    {
        [Fact]
        public void GetRune()
        {
            var currency = Currencies.GetRune("ticker");
            Assert.Equal("ticker", currency.Ticker);
            Assert.Equal(0, currency.DecimalPlaces);
            Assert.Null(currency.Minters);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetRune_Throws_ArgumentNullException(string? ticker)
        {
            Assert.Throws<ArgumentNullException>(() => Currencies.GetRune(ticker));
        }

        [Fact]
        public void GetRunes_With_Ticker()
        {
            var currencies = Currencies.GetRunes("ticker1", "ticker2").ToArray();
            Assert.Equal(2, currencies.Length);
            Assert.Equal("ticker1", currencies[0].Ticker);
            Assert.Equal("ticker2", currencies[1].Ticker);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ticker1", null)]
        [InlineData("ticker1", "")]
        [InlineData(null, "ticker2")]
        [InlineData("", "ticker2")]
        public void GetRunes_With_Ticker_Throws_ArgumentNullException(params string?[] tickers)
        {
            Assert.Throws<ArgumentNullException>(() => Currencies.GetRunes(tickers).ToArray());
        }

        [Fact]
        public void GetRunes_With_Sheet()
        {
            Assert.True(TableSheetsImporter.TryGetCsv("RuneSheet", out var csv));
            var sheet = new RuneSheet();
            sheet.Set(csv);
            Assert.NotNull(sheet.OrderedList);
            var currencies = Currencies.GetRunes(sheet).ToArray();
            Assert.Equal(sheet.Count, currencies.Length);
            foreach (var currency in currencies)
            {
                Assert.NotNull(sheet.OrderedList!.FirstOrDefault(row =>
                    row.Ticker == currency.Ticker));
            }
        }

        [Fact]
        public void GetRunes_With_Sheet_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Currencies.GetRunes((RuneSheet?)null));
        }

        [Fact]
        public void GetSoulStone()
        {
            var currency = Currencies.GetSoulStone("ticker");
            Assert.Equal("ticker", currency.Ticker);
            Assert.Equal(0, currency.DecimalPlaces);
            Assert.Null(currency.Minters);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GetSoulStone_Throws_ArgumentNullException(string? ticker)
        {
            Assert.Throws<ArgumentNullException>(() => Currencies.GetSoulStone(ticker));
        }

        [Fact]
        public void GetSoulStones_With_Ticker()
        {
            var currencies = Currencies.GetSoulStones("ticker1", "ticker2").ToArray();
            Assert.Equal(2, currencies.Length);
            Assert.Equal("ticker1", currencies[0].Ticker);
            Assert.Equal("ticker2", currencies[1].Ticker);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ticker1", null)]
        [InlineData("ticker1", "")]
        [InlineData(null, "ticker2")]
        [InlineData("", "ticker2")]
        public void GetSoulStones_With_Ticker_Throws_ArgumentNullException(params string?[] tickers)
        {
            Assert.Throws<ArgumentNullException>(() => Currencies.GetSoulStones(tickers).ToArray());
        }

        [Fact]
        public void GetSoulStones_With_Sheet()
        {
            Assert.True(TableSheetsImporter.TryGetCsv("PetSheet", out var csv));
            var sheet = new PetSheet();
            sheet.Set(csv);
            Assert.NotNull(sheet.OrderedList);
            var currencies = Currencies.GetSoulStones(sheet).ToArray();
            Assert.Equal(sheet.Count, currencies.Length);
            foreach (var currency in currencies)
            {
                Assert.NotNull(sheet.OrderedList!.FirstOrDefault(row =>
                    row.SoulStoneTicker == currency.Ticker));
            }
        }

        [Fact]
        public void GetSoulStones_With_Sheet_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Currencies.GetSoulStones((PetSheet?)null));
        }
    }
}
