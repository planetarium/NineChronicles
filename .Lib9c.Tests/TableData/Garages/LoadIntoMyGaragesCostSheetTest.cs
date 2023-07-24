namespace Lib9c.Tests.TableData.Garages
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Libplanet.Common;
    using Libplanet.Types.Assets;
    using Nekoyume.TableData.Garages;
    using Xunit;

    public class LoadIntoMyGaragesCostSheetTest
    {
        private readonly LoadIntoMyGaragesCostSheet _sheet;

        public LoadIntoMyGaragesCostSheetTest()
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,currency_ticker,fungible_id,garage_cost_per_unit");
            sb.AppendLine(
                "1,,00dfffe23964af9b284d121dae476571b7836b8d9e2e5f510d92a840fecc64fe,0.16");
            sb.AppendLine(
                "2,,3991e04dd808dc0bc24b21f5adb7bf1997312f8700daf1334bf34936e8a0813a,0.0016");
            sb.AppendLine(
                "3,,1a755098a2bc0659a063107df62e2ff9b3cdaba34d96b79519f504b996f53820,1");
            sb.AppendLine(
                "4,,f8faf92c9c0d0e8e06694361ea87bfc8b29a8ae8de93044b98470a57636ed0e0,10");
            sb.AppendLine("5,RUNE_GOLDENLEAF,,10");
            _sheet = new LoadIntoMyGaragesCostSheet();
            _sheet.Set(sb.ToString());
        }

        [Fact]
        public void Set()
        {
            Assert.NotNull(_sheet.OrderedList);
            Assert.Equal(5, _sheet.Count);
            var row = _sheet.OrderedList[0];
            Assert.Equal(1, row.Id);
            Assert.True(string.IsNullOrEmpty(row.CurrencyTicker));
            Assert.Equal(
                "00dfffe23964af9b284d121dae476571b7836b8d9e2e5f510d92a840fecc64fe",
                row.FungibleId.ToString());
            Assert.Equal(0.16m, row.GarageCostPerUnit);
            row = _sheet.OrderedList[1];
            Assert.Equal(2, row.Id);
            Assert.True(string.IsNullOrEmpty(row.CurrencyTicker));
            Assert.Equal(
                "3991e04dd808dc0bc24b21f5adb7bf1997312f8700daf1334bf34936e8a0813a",
                row.FungibleId.ToString());
            Assert.Equal(0.0016m, row.GarageCostPerUnit);
            row = _sheet.OrderedList[2];
            Assert.Equal(3, row.Id);
            Assert.True(string.IsNullOrEmpty(row.CurrencyTicker));
            Assert.Equal(
                "1a755098a2bc0659a063107df62e2ff9b3cdaba34d96b79519f504b996f53820",
                row.FungibleId.ToString());
            Assert.Equal(1m, row.GarageCostPerUnit);
            row = _sheet.OrderedList[3];
            Assert.Equal(4, row.Id);
            Assert.True(string.IsNullOrEmpty(row.CurrencyTicker));
            Assert.Equal(
                "f8faf92c9c0d0e8e06694361ea87bfc8b29a8ae8de93044b98470a57636ed0e0",
                row.FungibleId.ToString());
            Assert.Equal(10m, row.GarageCostPerUnit);
            row = _sheet.OrderedList[4];
            Assert.Equal(5, row.Id);
            Assert.Equal("RUNE_GOLDENLEAF", row.CurrencyTicker);
            Assert.Null(row.FungibleId);
            Assert.Equal(10m, row.GarageCostPerUnit);
        }

        [Fact]
        public void Set_InvalidFungibleId()
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,currency_ticker,fungible_id,garage_cost_per_unit");
            sb.AppendLine(
                "1,,INVALID,0.16");

            var sheet = new LoadIntoMyGaragesCostSheet();
            Assert.Throws<ArgumentOutOfRangeException>(() => sheet.Set(sb.ToString()));
        }

        [Fact]
        public void Set_InvalidGarageCostPerUnit()
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,currency_ticker,fungible_id,garage_cost_per_unit");
            sb.AppendLine(
                "1,,00dfffe23964af9b284d121dae476571b7836b8d9e2e5f510d92a840fecc64fe,INVALID");

            var sheet = new LoadIntoMyGaragesCostSheet();
            Assert.Throws<ArgumentException>(() => sheet.Set(sb.ToString()));
        }

        [Theory]
        [InlineData("RUNE_GOLDENLEAF", 10)]
        public void GetGarageCostPerUnit_CurrencyTicker(
            string currencyTicker,
            decimal expect)
        {
            var actual = _sheet.GetGarageCostPerUnit(currencyTicker);
            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GetGarageCostPerUnit_CurrencyTicker_Failure()
        {
            Assert.Throws<InvalidOperationException>(() => _sheet.GetGarageCostPerUnit("INVALID"));
        }

        [Theory]
        [InlineData(
            "00dfffe23964af9b284d121dae476571b7836b8d9e2e5f510d92a840fecc64fe",
            0.16)]
        [InlineData(
            "3991e04dd808dc0bc24b21f5adb7bf1997312f8700daf1334bf34936e8a0813a",
            0.0016)]
        [InlineData(
            "1a755098a2bc0659a063107df62e2ff9b3cdaba34d96b79519f504b996f53820",
            1)]
        [InlineData(
            "f8faf92c9c0d0e8e06694361ea87bfc8b29a8ae8de93044b98470a57636ed0e0",
            10)]
        public void GetGarageCostPerUnit_FungibleId(
            string fungibleIdHex,
            decimal expect)
        {
            var fungibleId = HashDigest<SHA256>.FromString(fungibleIdHex);
            var actual = _sheet.GetGarageCostPerUnit(fungibleId);
            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GetGarageCostPerUnit_FungibleId_Failure()
        {
            var fungibleId = HashDigest<SHA256>.FromString(
                "1234567890123456789012345678901234567890123456789012345678901234");
            Assert.Throws<InvalidOperationException>(() => _sheet.GetGarageCostPerUnit(fungibleId));
        }

        [Theory]
        [InlineData("RUNE_GOLDENLEAF", 0, 0, 0)]
        [InlineData("RUNE_GOLDENLEAF", 1, 0, 10)]
        [InlineData("RUNE_GOLDENLEAF", 10, 0, 100)]
        public void GetGarageCost_FungibleAssetValue(
            string currencyTicker,
            long majorUnit,
            long minorUnit,
            decimal expect)
        {
            var currency = Currencies.GetMinterlessCurrency(currencyTicker);
            var fav = new FungibleAssetValue(currency, majorUnit, minorUnit);
            var actual = _sheet.GetGarageCost(fav);
            var expectGarage = FungibleAssetValue.Parse(
                Currencies.Garage,
                expect.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(expectGarage, actual);
        }

        [Theory]
        [InlineData(
            "00dfffe23964af9b284d121dae476571b7836b8d9e2e5f510d92a840fecc64fe",
            0,
            0)]
        [InlineData(
            "3991e04dd808dc0bc24b21f5adb7bf1997312f8700daf1334bf34936e8a0813a",
            1,
            0.0016)]
        [InlineData(
            "1a755098a2bc0659a063107df62e2ff9b3cdaba34d96b79519f504b996f53820",
            10,
            10)]
        [InlineData(
            "f8faf92c9c0d0e8e06694361ea87bfc8b29a8ae8de93044b98470a57636ed0e0",
            100,
            1000)]
        public void GetGarageCost_FungibleId_Count(
            string fungibleIdHex,
            int count,
            decimal expect)
        {
            var fungibleId = HashDigest<SHA256>.FromString(fungibleIdHex);
            var actual = _sheet.GetGarageCost(fungibleId, count);
            var expectGarage = FungibleAssetValue.Parse(
                Currencies.Garage,
                expect.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(expectGarage, actual);
        }

        [Fact]
        public void GetGarageCost_FungibleId_Count_Failure()
        {
            var fungibleId = HashDigest<SHA256>.FromString(
                "00dfffe23964af9b284d121dae476571b7836b8d9e2e5f510d92a840fecc64fe");
            Assert.Throws<ArgumentOutOfRangeException>(() => _sheet.GetGarageCost(fungibleId, -1));
        }

        [Fact]
        public void GetGarageCost()
        {
            var currency = Currencies.GetMinterlessCurrency("RUNE_GOLDENLEAF");
            var favTuples = Enumerable.Range(1, 10)
                .Select(i => (
                    fav: new FungibleAssetValue(currency, i, 0),
                    expect: i * 10m))
                .ToArray();
            var fungibleIdAndCountTuples = Enumerable.Range(1, 10)
                .Select(i => (
                    fungibleId: HashDigest<SHA256>.FromString(
                        "00dfffe23964af9b284d121dae476571b7836b8d9e2e5f510d92a840fecc64fe"),
                    count: i,
                    expect: i * 0.16m))
                .ToArray();
            var expect = favTuples.Select(t => t.expect).Sum() +
                         fungibleIdAndCountTuples.Select(t => t.expect).Sum();
            var favArr = favTuples.Select(t => t.fav).ToArray();
            var fungibleIdAndCountArr = fungibleIdAndCountTuples
                .Select(t => (t.fungibleId, t.count)).ToArray();
            var actual = _sheet.GetGarageCost(favArr, fungibleIdAndCountArr);
            var expectGarage = FungibleAssetValue.Parse(
                Currencies.Garage,
                expect.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(expectGarage, actual);
        }

        [Fact]
        public void HasCost_CurrencyTicker()
        {
            Assert.True(_sheet.HasCost("RUNE_GOLDENLEAF"));
            Assert.False(_sheet.HasCost("INVALID"));
        }

        [Fact]
        public void HasCost_FungibleId()
        {
            var fungibleId = HashDigest<SHA256>.FromString(
                "00dfffe23964af9b284d121dae476571b7836b8d9e2e5f510d92a840fecc64fe");
            Assert.True(_sheet.HasCost(fungibleId));
            fungibleId = HashDigest<SHA256>.FromString(
                "1234567890123456789012345678901234567890123456789012345678901234");
            Assert.False(_sheet.HasCost(fungibleId));
        }
    }
}
