using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Lib9c;
using Libplanet.Common;
using Libplanet.Types.Assets;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Garages
{
    [Serializable]
    public class LoadIntoMyGaragesCostSheet : Sheet<int, LoadIntoMyGaragesCostSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }

            public string CurrencyTicker { get; private set; }

            public HashDigest<SHA256>? FungibleId { get; private set; }

            public decimal GarageCostPerUnit { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                CurrencyTicker = fields[1];
                FungibleId = string.IsNullOrEmpty(fields[2])
                    ? (HashDigest<SHA256>?)null
                    : HashDigest<SHA256>.FromString(fields[2]);
                GarageCostPerUnit = ParseDecimal(fields[3]);
            }
        }

        public LoadIntoMyGaragesCostSheet() : base(nameof(LoadIntoMyGaragesCostSheet))
        {
        }

        public decimal GetGarageCostPerUnit(string currencyTicker)
        {
            var row = OrderedList!.First(r => r.CurrencyTicker == currencyTicker);
            return row.GarageCostPerUnit;
        }

        public decimal GetGarageCostPerUnit(HashDigest<SHA256> fungibleId)
        {
            var row = OrderedList!.First(r => r.FungibleId?.Equals(fungibleId) ?? false);
            return row.GarageCostPerUnit;
        }

        public FungibleAssetValue GetGarageCost(FungibleAssetValue fav)
        {
            var unitCost = GetGarageCostPerUnit(fav.Currency.Ticker);
            var quantity = decimal.Parse(
                fav.GetQuantityString(),
                CultureInfo.InvariantCulture);
            return FungibleAssetValue.Parse(
                Currencies.Garage,
                (unitCost * quantity).ToString(CultureInfo.InvariantCulture));
        }

        public FungibleAssetValue GetGarageCost(HashDigest<SHA256> fungibleId, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    "count must be positive.");
            }

            if (count == 0)
            {
                return new FungibleAssetValue(Currencies.Garage);
            }

            var unitCost = GetGarageCostPerUnit(fungibleId);
            return FungibleAssetValue.Parse(
                Currencies.Garage,
                (unitCost * count).ToString(CultureInfo.InvariantCulture));
        }

        public FungibleAssetValue GetGarageCost(
            IEnumerable<FungibleAssetValue> fungibleAssetValues,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)> fungibleIdAndCounts)
        {
            var cost = new FungibleAssetValue(Currencies.Garage);
            if (fungibleAssetValues is { })
            {
                foreach (var fav in fungibleAssetValues)
                {
                    cost += GetGarageCost(fav);
                }
            }

            if (fungibleIdAndCounts is { })
            {
                foreach (var (fungibleId, count) in fungibleIdAndCounts)
                {
                    cost += GetGarageCost(fungibleId, count);
                }
            }

            return cost;
        }

        public bool HasCost(string currencyTicker)
        {
            return OrderedList!.Any(r => r.CurrencyTicker == currencyTicker);
        }

        public bool HasCost(HashDigest<SHA256> fungibleId)
        {
            return OrderedList!.Any(r => r.FungibleId?.Equals(fungibleId) ?? false);
        }
    }
}
